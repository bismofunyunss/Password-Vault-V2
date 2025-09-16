using System.Buffers.Binary;
using System.Security.Cryptography;
using static Password_Vault_V2.Crypto;
using Buffer = System.Buffer;

namespace Password_Vault_V2;

internal abstract class FipsCrypto
{
    internal const int KeySize = 32;
    internal const int SaltSize = 32;
    internal static bool FipsEnabled;

    internal static Task<byte[]> Pbkdf2(byte[] password, byte[] salt, int outputLength)
    {
        return Task.FromResult(Rfc2898DeriveBytes.Pbkdf2(password, salt, Settings.Default.Iterations, HashAlgorithmName.SHA256,
            outputLength));
    }

    internal static class FipsHkdf
    {
        /// <summary>
        ///     Derive a key using SP800-108 HMAC Counter KDF (FIPS-approved)
        /// </summary>
        /// <param name="inputKeyMaterial">Input key material (IKM)</param>
        /// <param name="salt">Salt (used as KDF key)</param>
        /// <param name="info">Optional label/context</param>
        /// <param name="outputLength">Length of output key in bytes</param>
        /// <returns>Derived key</returns>
        public static byte[] DeriveKey(byte[] inputKeyMaterial, byte[] salt, byte[] info, int outputLength)
        {
            if (inputKeyMaterial == null || inputKeyMaterial.Length == 0)
                throw new ArgumentNullException(nameof(inputKeyMaterial));
            if (salt == null || salt.Length == 0)
                throw new ArgumentException("Salt must be non-null and non-empty.", nameof(salt));
            if (outputLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputLength));

            // Use salt as key for SP800-108 KDF
            using var kdf = new SP800108HmacCounterKdf(salt, HashAlgorithmName.SHA256);

            // DeriveKey takes label and context
            var label = info ?? Array.Empty<byte>();
            var context = inputKeyMaterial; // using input key material as context

            return kdf.DeriveKey(label, context, outputLength);
        }
    }

    public static class AesKeyWrapRfc3394
    {
        private const int BlockSize = 8; // 64-bit blocks
        private static readonly ulong DefaultIv = 0xA6A6A6A6A6A6A6A6UL;

        /// <summary>
        ///     Wraps a key using AES Key Wrap (RFC 3394).
        /// </summary>
        /// <param name="kek">Key encryption key (AES key, 128/192/256 bits)</param>
        /// <param name="keyToWrap">Key to wrap (must be multiple of 8 bytes)</param>
        /// <returns>Wrapped key</returns>
        public static byte[] Wrap(byte[] kek, byte[] keyToWrap)
        {
            if (kek == null) throw new ArgumentNullException(nameof(kek));
            if (keyToWrap == null || keyToWrap.Length % BlockSize != 0)
                throw new ArgumentException("Key must be multiple of 8 bytes", nameof(keyToWrap));

            var n = keyToWrap.Length / BlockSize;
            var R = new byte[keyToWrap.Length];
            Array.Copy(keyToWrap, R, keyToWrap.Length);
            var A = DefaultIv;

            using var aes = CreateEcbAes(kek);
            using var encryptor = aes.CreateEncryptor();

            for (var j = 0; j <= 5; j++)
            for (var i = 0; i < n; i++)
            {
                var B = new byte[16];
                BinaryPrimitives.WriteUInt64BigEndian(B.AsSpan(0, 8), A);
                Array.Copy(R, i * BlockSize, B, 8, BlockSize);

                var C = encryptor.TransformFinalBlock(B, 0, 16);
                A = BinaryPrimitives.ReadUInt64BigEndian(C.AsSpan(0, 8)) ^ (ulong)(n * j + i + 1);
                Array.Copy(C, 8, R, i * BlockSize, BlockSize);
            }

            var result = new byte[(n + 1) * BlockSize];
            BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(0, 8), A);
            Array.Copy(R, 0, result, 8, R.Length);
            return result;
        }

        /// <summary>
        ///     Unwraps a key using AES Key Wrap (RFC 3394).
        /// </summary>
        /// <param name="kek">Key encryption key (AES key, 128/192/256 bits)</param>
        /// <param name="wrappedKey">Wrapped key</param>
        /// <returns>Unwrapped key</returns>
        public static byte[] Unwrap(byte[] kek, byte[] wrappedKey)
        {
            if (kek == null) throw new ArgumentNullException(nameof(kek));
            if (wrappedKey == null || wrappedKey.Length < 16 || wrappedKey.Length % 8 != 0)
                throw new ArgumentException("Invalid wrapped key length", nameof(wrappedKey));

            var n = wrappedKey.Length / BlockSize - 1;
            var R = new byte[n * BlockSize];
            Array.Copy(wrappedKey, 8, R, 0, R.Length);
            var A = BinaryPrimitives.ReadUInt64BigEndian(wrappedKey);

            using var aes = CreateEcbAes(kek);
            using var decryptor = aes.CreateDecryptor();

            for (var j = 5; j >= 0; j--)
            for (var i = n - 1; i >= 0; i--)
            {
                var t = (ulong)(n * j + i + 1);
                var Atemp = A ^ t;

                var B = new byte[16];
                BinaryPrimitives.WriteUInt64BigEndian(B.AsSpan(0, 8), Atemp);
                Array.Copy(R, i * BlockSize, B, 8, BlockSize);

                var C = decryptor.TransformFinalBlock(B, 0, 16);
                A = BinaryPrimitives.ReadUInt64BigEndian(C.AsSpan(0, 8));
                Array.Copy(C, 8, R, i * BlockSize, BlockSize);
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
            var iv = RandomNumberGenerator.GetBytes(16);

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
            var hmac = encrypted[..32]; // first 32 bytes
            var iv = encrypted[32..48]; // next 16 bytes
            var ciphertext = encrypted[48..]; // remainder

            // Verify HMAC
            using (var h = new HMACSHA256(hmacKey))
            {
                var computed = h.ComputeHash(iv.Concat(ciphertext).ToArray());
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
        private const int GcmTagSize = 16; // 128-bit tag
        private const int GcmNonceSize = 12; // 96-bit nonce
        private const int BaseNonceSize = 8; // first 8 bytes random, last 4 = chunkIndex

        public static async Task EncryptFileAesGcmParallelAsync(
            Stream input, Stream output, byte[] key,
            IProgress<double>? progress = null, int chunkSize = 64 * 1024, int maxParallelism = 4)
        {
            if (input == null || output == null) throw new ArgumentNullException();
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 32 bytes.");

            var totalLength = input.Length;
            var baseNonce = CryptoUtilities.RndByteSized(8);

            // Write baseNonce at start of ciphertext
            await output.WriteAsync(baseNonce, 0, baseNonce.Length).ConfigureAwait(false);

            var chunkIndex = 0;
            long totalRead = 0;

            var semaphore = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>();
            var chunks = new List<(byte[] plaintext, int index)>();

            while (totalRead < totalLength)
            {
                var read = (int)Math.Min(chunkSize, totalLength - totalRead);
                var buffer = new byte[read];
                await input.ReadAsync(buffer, 0, read).ConfigureAwait(false);

                var chunkCopy = buffer; // capture for closure
                var currentIndex = chunkIndex;

                await semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var ciphertext = new byte[chunkCopy.Length];
                        var tag = new byte[GcmTagSize];
                        var nonce = new byte[GcmNonceSize];
                        Buffer.BlockCopy(baseNonce, 0, nonce, 0, 8);
                        BitConverter.GetBytes(currentIndex).CopyTo(nonce, 8);

                        using var aesGcm = new AesGcm(key, GcmTagSize);
                        aesGcm.Encrypt(nonce, chunkCopy, ciphertext, tag);

                        lock (chunks) // preserve write order later
                        {
                            chunks.Add((ciphertext.Concat(tag).ToArray(), currentIndex));
                        }
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

            var totalLength = input.Length;

            // Read baseNonce first (8 bytes)
            var baseNonce = new byte[8];
            var nRead = await input.ReadAsync(baseNonce, 0, 8).ConfigureAwait(false);
            if (nRead != 8) throw new InvalidDataException("Failed to read base nonce.");

            var chunkIndex = 0;
            var semaphore = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>();
            var plaintextChunks = new List<(byte[] data, int index)>();

            while (input.Position < totalLength)
            {
                var read = (int)Math.Min(chunkSize, totalLength - input.Position - GcmTagSize);
                var ciphertext = new byte[read];
                var cRead = await input.ReadAsync(ciphertext, 0, read).ConfigureAwait(false);
                if (cRead != read) throw new InvalidDataException("Failed to read full ciphertext.");

                var tag = new byte[GcmTagSize];
                var tRead = await input.ReadAsync(tag, 0, GcmTagSize).ConfigureAwait(false);
                if (tRead != GcmTagSize) throw new InvalidDataException("Failed to read auth tag.");

                var cipherCopy = ciphertext; // capture
                var tagCopy = tag;
                var currentIndex = chunkIndex;

                await semaphore.WaitAsync();
                var task = Task.Run(() =>
                {
                    try
                    {
                        var plaintext = new byte[cipherCopy.Length];
                        var nonce = new byte[GcmNonceSize];
                        Buffer.BlockCopy(baseNonce, 0, nonce, 0, 8);
                        BitConverter.GetBytes(currentIndex).CopyTo(nonce, 8);

                        using var aesGcm = new AesGcm(key, GcmTagSize);
                        aesGcm.Decrypt(nonce, cipherCopy, tagCopy, plaintext);

                        lock (plaintextChunks)
                        {
                            plaintextChunks.Add((plaintext, currentIndex));
                        }
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

        public void Dispose()
        {
            _rsaKey?.Dispose();
            _rsaKey = null;
        }

        /// <summary>
        ///     Create (if needed) or open a TPM-backed RSA key and return it.
        /// </summary>
        private CngKey GetOrCreateTpmRsaKey(int keySizeBits = 2048)
        {
            if (CngKey.Exists(_keyName, _platform))
                return CngKey.Open(_keyName, _platform);

            var creationParams = new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftPlatformCryptoProvider,
                KeyUsage = CngKeyUsages.Decryption | CngKeyUsages.KeyAgreement,
                ExportPolicy = CngExportPolicies.None
            };

            // Optional: request VSM protection flag (platform dependent)
            // In some environments adding a property named "VsmKey" with value=1 signals VSM.
            try
            {
                creationParams.Parameters.Add(new CngProperty("VsmKey", BitConverter.GetBytes(1),
                    CngPropertyOptions.None));
            }
            catch
            {
                // property may not be supported on older OSes; fallback to default provider behavior
            }

            // Set RSA length, e.g. 2048
            creationParams.Parameters.Add(new CngProperty("Length", BitConverter.GetBytes(2048),
                CngPropertyOptions.None));
            var rsaKey = CngKey.Create(CngAlgorithm.Rsa, _keyName, creationParams);
            return rsaKey;
        }

        /// <summary>
        ///     RSA-encrypt (TPM-backed) the RFC-wrapped AES key. Returns the RSA-encrypted blob which the caller should persist.
        /// </summary>
        /// <param name="rfcWrappedKey">AES key already wrapped with RFC3394/5649 (your "wrappedKey")</param>
        public byte[] SealWrappedKey(byte[] rfcWrappedKey)
        {
            if (rfcWrappedKey == null || rfcWrappedKey.Length == 0)
                throw new ArgumentNullException(nameof(rfcWrappedKey));

            // create or open TPM-backed RSA key
            _rsaKey ??= GetOrCreateTpmRsaKey();

            using var rsa = new RSACng(_rsaKey);
            // Encrypt with RSA-OAEP-SHA256. RSA public operation can be done without pin.
            var rsaEncrypted = rsa.Encrypt(rfcWrappedKey, RSAEncryptionPadding.OaepSHA256);

            // The caller should persist rsaEncrypted (e.g. write to JSON/DB)
            return rsaEncrypted;
        }

        /// <summary>
        ///     RSA-decrypt (TPM-backed) the blob, then RFC-unwrap with KEK to get AES key.
        /// </summary>
        /// <param name="rsaEncryptedBlob">bytes previously returned by SealWrappedKey (RSA-encrypted RFC-wrapped AES key)</param>
        /// <param name="kek">KEK used for RFC unwrap</param>
        public byte[] UnsealKey(byte[] rsaEncryptedBlob, byte[] kek, byte[] hmacKey)
        {
            if (rsaEncryptedBlob == null || rsaEncryptedBlob.Length == 0)
                throw new ArgumentNullException(nameof(rsaEncryptedBlob));
            if (kek == null || kek.Length == 0) throw new ArgumentNullException(nameof(kek));

            if (!CngKey.Exists(_keyName, _platform))
                throw new InvalidOperationException("TPM-backed RSA key not found.");

            using var rsaKey = CngKey.Open(_keyName, _platform);
            using var rsa = new RSACng(rsaKey);

            // Step 1: RSA decrypt via TPM
            var toSeal = rsa.Decrypt(rsaEncryptedBlob, RSAEncryptionPadding.OaepSHA256);

            // Step 2: Verify HMAC before unwrapping
            VerifyWrappedKeyMac(toSeal, hmacKey);

            // Step 3: Extract AES-KW portion
            var wrappedKeyLength = toSeal.Length - 32; // last 32 bytes = HMAC
            var wrappedKey = toSeal[..wrappedKeyLength];

            // Step 4: RFC unwrap to get raw AES master key
            var masterKey = AesKeyWrapRfc3394.Unwrap(kek, wrappedKey);

            // Step 5: Zero sensitive buffers
            CryptographicOperations.ZeroMemory(toSeal);
            CryptographicOperations.ZeroMemory(wrappedKey);

            return masterKey;
        }

        public static void VerifyWrappedKeyMac(byte[] toSeal, byte[] hmacKey)
        {
            if (toSeal.Length < 32) // HMACSHA256 is 32 bytes
                throw new CryptographicException("Data too short to contain HMAC.");

            var wrappedKeyLength = toSeal.Length - 32;
            var wrappedKey = new byte[wrappedKeyLength];
            var storedMac = new byte[32];

            Buffer.BlockCopy(toSeal, 0, wrappedKey, 0, wrappedKeyLength);
            Buffer.BlockCopy(toSeal, wrappedKeyLength, storedMac, 0, 32);

            // Compute HMAC over wrapped key
            byte[] computedMac;
            using (var h = new HMACSHA256(hmacKey))
            {
                computedMac = h.ComputeHash(wrappedKey);
            }

            // Compare in constant time
            if (!CryptographicOperations.FixedTimeEquals(computedMac, storedMac))
                throw new CryptographicException("HMAC verification failed. Wrapped key may be tampered.");
        }
    }
}