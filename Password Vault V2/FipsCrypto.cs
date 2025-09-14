using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using static Password_Vault_V2.Crypto;
using Buffer = System.Buffer;

namespace Password_Vault_V2;

internal abstract class FipsCrypto
{
    internal const int KeySize = 32;
    internal const int SaltSize = 32;
    internal static bool FipsEnabled;

    public static byte[] Pbkdf2(byte[] password, byte[] salt, int outputLength)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, Settings.Default.Iterations, HashAlgorithmName.SHA256,
            outputLength);
    }

    internal static class Hkdf
    {
        /// <summary>
        /// Extract step of HKDF: PRK = HMAC(salt, inputKeyMaterial)
        /// </summary>
        private static byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            // If salt is null, use an all-zero array the size of the hash (32 bytes for SHA256)
            salt ??= new byte[32];

            using var hmac = new HMACSHA256(salt);
            return hmac.ComputeHash(inputKeyMaterial);
        }

        /// <summary>
        /// Expand step of HKDF: OKM = T(1) | T(2) | ... truncated to outputLength
        /// </summary>
        private static byte[] Expand(byte[] prk, byte[] info, int outputLength)
        {
            if (outputLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputLength), "Output length must be positive.");

            const int hashLen = 32; // SHA256 output length in bytes
            int iterations = (int)Math.Ceiling((double)outputLength / hashLen);
            if (iterations > 255)
                throw new ArgumentOutOfRangeException(nameof(outputLength), "Cannot expand to more than 255 blocks.");

            var okm = new byte[outputLength];
            byte[] previous = Array.Empty<byte>();

            using var hmac = new HMACSHA256(prk);
            int pos = 0;

            for (int i = 1; i <= iterations; i++)
            {
                hmac.Initialize();

                // Compute T(i) = HMAC-PRK( T(i-1) | info | i )
                hmac.TransformBlock(previous, 0, previous.Length, null, 0);
                if (info != null)
                    hmac.TransformBlock(info, 0, info.Length, null, 0);
                hmac.TransformFinalBlock(new[] { (byte)i }, 0, 1);

                previous = hmac.Hash!;
                hmac.Initialize();

                int toCopy = Math.Min(hashLen, outputLength - pos);
                Array.Copy(previous, 0, okm, pos, toCopy);
                pos += toCopy;
            }

            CryptoUtilities.ClearMemoryNative(prk, previous);
            return okm;
        }

        /// <summary>
        /// Derive a key from input key material using HKDF (FIPS mode).
        /// </summary>
        public static byte[] DeriveKey(byte[] inputKeyMaterial, byte[] salt, byte[] info, int outputLength)
        {
            if (inputKeyMaterial == null || inputKeyMaterial.Length == 0)
                throw new ArgumentNullException(nameof(inputKeyMaterial));

            byte[] prk = Extract(salt, inputKeyMaterial);
            return Expand(prk, info, outputLength);
        }
    }

public static class AesKeyWrapRfc3394
{
    private const int BlockSize = 8; // 64-bit blocks
    private static readonly ulong DefaultIv = 0xA6A6A6A6A6A6A6A6UL;

    /// <summary>
    /// Wraps a key using AES Key Wrap (RFC 3394).
    /// </summary>
    /// <param name="kek">Key encryption key (AES key, 128/192/256 bits)</param>
    /// <param name="keyToWrap">Key to wrap (must be multiple of 8 bytes)</param>
    /// <returns>Wrapped key</returns>
    public static byte[] Wrap(byte[] kek, byte[] keyToWrap)
    {
        if (kek == null) throw new ArgumentNullException(nameof(kek));
        if (keyToWrap == null || keyToWrap.Length % BlockSize != 0)
            throw new ArgumentException("Key must be multiple of 8 bytes", nameof(keyToWrap));

        int n = keyToWrap.Length / BlockSize;
        byte[] R = new byte[keyToWrap.Length];
        Array.Copy(keyToWrap, R, keyToWrap.Length);
        ulong A = DefaultIv;

        using var aes = CreateEcbAes(kek);
        using var encryptor = aes.CreateEncryptor();

        for (int j = 0; j <= 5; j++)
        {
            for (int i = 0; i < n; i++)
            {
                byte[] B = new byte[16];
                BinaryPrimitives.WriteUInt64BigEndian(B.AsSpan(0, 8), A);
                Array.Copy(R, i * BlockSize, B, 8, BlockSize);

                var C = encryptor.TransformFinalBlock(B, 0, 16);
                A = BinaryPrimitives.ReadUInt64BigEndian(C.AsSpan(0, 8)) ^ (ulong)((n * j) + (i + 1));
                Array.Copy(C, 8, R, i * BlockSize, BlockSize);
            }
        }

        byte[] result = new byte[(n + 1) * BlockSize];
        BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(0, 8), A);
        Array.Copy(R, 0, result, 8, R.Length);
        return result;
    }

    /// <summary>
    /// Unwraps a key using AES Key Wrap (RFC 3394).
    /// </summary>
    /// <param name="kek">Key encryption key (AES key, 128/192/256 bits)</param>
    /// <param name="wrappedKey">Wrapped key</param>
    /// <returns>Unwrapped key</returns>
    public static byte[] Unwrap(byte[] kek, byte[] wrappedKey)
    {
        if (kek == null) throw new ArgumentNullException(nameof(kek));
        if (wrappedKey == null || wrappedKey.Length < 16 || wrappedKey.Length % 8 != 0)
            throw new ArgumentException("Invalid wrapped key length", nameof(wrappedKey));

        int n = wrappedKey.Length / BlockSize - 1;
        byte[] R = new byte[n * BlockSize];
        Array.Copy(wrappedKey, 8, R, 0, R.Length);
        ulong A = BinaryPrimitives.ReadUInt64BigEndian(wrappedKey);

        using var aes = CreateEcbAes(kek);
        using var decryptor = aes.CreateDecryptor();

        for (int j = 5; j >= 0; j--)
        {
            for (int i = n - 1; i >= 0; i--)
            {
                ulong t = (ulong)(n * j + (i + 1));
                ulong Atemp = A ^ t;

                byte[] B = new byte[16];
                BinaryPrimitives.WriteUInt64BigEndian(B.AsSpan(0, 8), Atemp);
                Array.Copy(R, i * BlockSize, B, 8, BlockSize);

                var C = decryptor.TransformFinalBlock(B, 0, 16);
                A = BinaryPrimitives.ReadUInt64BigEndian(C.AsSpan(0, 8));
                Array.Copy(C, 8, R, i * BlockSize, BlockSize);
            }
        }

        if (A != DefaultIv)
            throw new CryptographicException("Integrity check failed.");

        return R;
    }

    private static Aes CreateEcbAes(byte[] key)
    {
        return new AesCng
        {
            KeySize = key.Length * 8,
            Key = key,
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None
        };
    }
}

public static class SimpleAesHmac
    {
        public static byte[] Encrypt(byte[] key, byte[] hmacKey, byte[] input)
        {
            // Generate random IV
            byte[] iv = RandomNumberGenerator.GetBytes(16);

            byte[] ciphertext;
            using (var aes = new AesCng { Key = key, IV = iv, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            using (var encryptor = aes.CreateEncryptor())
            {
                ciphertext = encryptor.TransformFinalBlock(input, 0, input.Length);
            }

            // Compute HMAC over IV + ciphertext
            byte[] hmac;
            using (var h = new HMACSHA256(hmacKey))
            {
                hmac = h.ComputeHash(iv.Concat(ciphertext).ToArray());
            }

            // Return concatenated HMAC || IV || ciphertext
            return hmac.Concat(iv).Concat(ciphertext).ToArray();
        }

        public static byte[] Decrypt(byte[] key, byte[] hmacKey, byte[] encrypted)
        {
            if (encrypted.Length < 32 + 16)
                throw new ArgumentException("Invalid encrypted data");

            // Extract components
            byte[] hmac = encrypted[..32]; // first 32 bytes
            byte[] iv = encrypted[32..48]; // next 16 bytes
            byte[] ciphertext = encrypted[48..]; // remainder

            // Verify HMAC
            using (var h = new HMACSHA256(hmacKey))
            {
                byte[] computed = h.ComputeHash(iv.Concat(ciphertext).ToArray());
                if (!CryptographicOperations.FixedTimeEquals(hmac, computed))
                    throw new CryptographicException("HMAC validation failed");
            }

            // Decrypt
            using var aes = new AesCng { Key = key, IV = iv, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 };
            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }
    }

    public static class FipsAesGcmParallel
    {
        private const int GcmTagSize = 16;  // 128-bit tag
        private const int GcmNonceSize = 12; // 96-bit nonce
        private const int BaseNonceSize = 8; // first 8 bytes random, last 4 = chunkIndex

        public static async Task EncryptFileAesGcmParallelAsync(
            Stream input, Stream output, byte[] key,
            IProgress<double>? progress = null, int chunkSize = 64 * 1024, int maxParallelism = 4)
        {
            if (input == null || output == null) throw new ArgumentNullException();
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 32 bytes.");

            long totalLength = input.Length;
            byte[] baseNonce = CryptoUtilities.RndByteSized(8);

            // Write baseNonce at start of ciphertext
            await output.WriteAsync(baseNonce, 0, baseNonce.Length).ConfigureAwait(false);

            int chunkIndex = 0;
            long totalRead = 0;

            var semaphore = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>();
            var chunks = new List<(byte[] plaintext, int index)>();

            while (totalRead < totalLength)
            {
                int read = (int)Math.Min(chunkSize, totalLength - totalRead);
                byte[] buffer = new byte[read];
                await input.ReadAsync(buffer, 0, read).ConfigureAwait(false);

                byte[] chunkCopy = buffer; // capture for closure
                int currentIndex = chunkIndex;

                await semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        byte[] ciphertext = new byte[chunkCopy.Length];
                        byte[] tag = new byte[GcmTagSize];
                        byte[] nonce = new byte[GcmNonceSize];
                        Buffer.BlockCopy(baseNonce, 0, nonce, 0, 8);
                        BitConverter.GetBytes(currentIndex).CopyTo(nonce, 8);

                        using var aesGcm = new AesGcm(key, GcmTagSize);
                        aesGcm.Encrypt(nonce, chunkCopy, ciphertext, tag);

                        lock (chunks) // preserve write order later
                            chunks.Add((ciphertext.Concat(tag).ToArray(), currentIndex));

                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
                totalRead += read;
                chunkIndex++;
            }

            await Task.WhenAll(tasks);

            // Write chunks sequentially to maintain order
            foreach (var chunk in chunks.OrderBy(c => c.index))
                await output.WriteAsync(chunk.plaintext, 0, chunk.plaintext.Length);

            progress?.Report(1.0);
            await output.FlushAsync();
        }


        public static async Task DecryptFileAesGcmParallelAsync(
        Stream input, Stream output, byte[] key,
        IProgress<double>? progress = null, int chunkSize = 64 * 1024, int maxParallelism = 4)
        {
            if (input == null || output == null) throw new ArgumentNullException();
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 32 bytes.");

            long totalLength = input.Length;

            // Read baseNonce first (8 bytes)
            byte[] baseNonce = new byte[8];
            int nRead = await input.ReadAsync(baseNonce, 0, 8).ConfigureAwait(false);
            if (nRead != 8) throw new InvalidDataException("Failed to read base nonce.");

            int chunkIndex = 0;
            var semaphore = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>();
            var plaintextChunks = new List<(byte[] data, int index)>();

            while (input.Position < totalLength)
            {
                int read = (int)Math.Min(chunkSize, totalLength - input.Position - GcmTagSize);
                byte[] ciphertext = new byte[read];
                int cRead = await input.ReadAsync(ciphertext, 0, read).ConfigureAwait(false);
                if (cRead != read) throw new InvalidDataException("Failed to read full ciphertext.");

                byte[] tag = new byte[GcmTagSize];
                int tRead = await input.ReadAsync(tag, 0, GcmTagSize).ConfigureAwait(false);
                if (tRead != GcmTagSize) throw new InvalidDataException("Failed to read auth tag.");

                byte[] cipherCopy = ciphertext; // capture
                byte[] tagCopy = tag;
                int currentIndex = chunkIndex;

                await semaphore.WaitAsync();
                var task = Task.Run(() =>
                {
                    try
                    {
                        byte[] plaintext = new byte[cipherCopy.Length];
                        byte[] nonce = new byte[GcmNonceSize];
                        Buffer.BlockCopy(baseNonce, 0, nonce, 0, 8);
                        BitConverter.GetBytes(currentIndex).CopyTo(nonce, 8);

                        using var aesGcm = new AesGcm(key, GcmTagSize);
                        aesGcm.Decrypt(nonce, cipherCopy, tagCopy, plaintext);

                        lock (plaintextChunks)
                            plaintextChunks.Add((plaintext, currentIndex));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
                chunkIndex++;
            }

            await Task.WhenAll(tasks);

            // Write plaintext sequentially
            foreach (var chunk in plaintextChunks.OrderBy(c => c.index))
                await output.WriteAsync(chunk.data, 0, chunk.data.Length);

            progress?.Report(1.0);
            await output.FlushAsync();
        }
    }

public sealed class WrappedAesKeyStore : IDisposable
{
    private readonly string _keyName;
    private readonly CngProvider _platform = CngProvider.MicrosoftPlatformCryptoProvider;
    private CngKey? _rsaKey;

    public WrappedAesKeyStore(string keyName)
    {
        _keyName = keyName ?? throw new ArgumentNullException(nameof(keyName));
    }

    /// <summary>
    /// Create (if needed) or open a TPM-backed RSA key and return it.
    /// </summary>
    private CngKey GetOrCreateTpmRsaKey(int keySizeBits = 2048)
    {
        if (CngKey.Exists(_keyName, _platform))
            return CngKey.Open(_keyName, _platform);

        var creationParams = new CngKeyCreationParameters
        {
            Provider = _platform,
            KeyUsage = CngKeyUsages.Decryption,      // allow decrypt (private) operations
            ExportPolicy = CngExportPolicies.None    // do not allow exporting private key
        };

        // Explicitly set length (many TPMs only accept 2048/3072)
        creationParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(keySizeBits), CngPropertyOptions.None));

        return CngKey.Create(CngAlgorithm.Rsa, _keyName, creationParams);
    }

    /// <summary>
    /// RSA-encrypt (TPM-backed) the RFC-wrapped AES key. Returns the RSA-encrypted blob which the caller should persist.
    /// </summary>
    /// <param name="rfcWrappedKey">AES key already wrapped with RFC3394/5649 (your "wrappedKey")</param>
    public byte[] SealWrappedKey(byte[] rfcWrappedKey)
    {
        if (rfcWrappedKey == null || rfcWrappedKey.Length == 0) throw new ArgumentNullException(nameof(rfcWrappedKey));

        // create or open TPM-backed RSA key
        _rsaKey ??= GetOrCreateTpmRsaKey();

        using var rsa = new RSACng(_rsaKey);
        // Encrypt with RSA-OAEP-SHA256. RSA public operation can be done without pin.
        byte[] rsaEncrypted = rsa.Encrypt(rfcWrappedKey, RSAEncryptionPadding.OaepSHA256);

        // The caller should persist rsaEncrypted (e.g. write to JSON/DB)
        return rsaEncrypted;
    }

    /// <summary>
    /// RSA-decrypt (TPM-backed) the blob, then RFC-unwrap with KEK to get AES key.
    /// </summary>
    /// <param name="rsaEncryptedBlob">bytes previously returned by SealWrappedKey (RSA-encrypted RFC-wrapped AES key)</param>
    /// <param name="kek">KEK used for RFC unwrap</param>
    public byte[] UnsealKey(byte[] rsaEncryptedBlob, byte[] kek)
    {
        if (rsaEncryptedBlob == null || rsaEncryptedBlob.Length == 0) throw new ArgumentNullException(nameof(rsaEncryptedBlob));
        if (kek == null || kek.Length == 0) throw new ArgumentNullException(nameof(kek));

        if (!CngKey.Exists(_keyName, _platform))
            throw new InvalidOperationException("TPM-backed RSA key not found.");

        using var rsaKey = CngKey.Open(_keyName, _platform);
        using var rsa = new RSACng(rsaKey);

        // Step 1: RSA decrypt via TPM
        byte[] rfcWrapped = rsa.Decrypt(rsaEncryptedBlob, RSAEncryptionPadding.OaepSHA256);

        // Step 2: RFC unwrap to get the raw AES key
        byte[] aesKey = AesKeyWrapRfc3394.Unwrap(kek, rfcWrapped);

        // zero temporary sensitive buffers
        CryptographicOperations.ZeroMemory(rfcWrapped);

        return aesKey;
    }

    public void Dispose()
    {
        _rsaKey?.Dispose();
        _rsaKey = null;
    }
}

}
  