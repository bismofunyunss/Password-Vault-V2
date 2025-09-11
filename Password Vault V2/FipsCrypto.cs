using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Windows.Storage.Streams;
using static Password_Vault_V2.Crypto;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using Buffer = System.Buffer;

namespace Password_Vault_V2;

internal abstract class FipsCrypto
{
    internal const int KeySize = 32;
    internal const int SaltSize = 32;
    internal static bool FipsEnabled = false;

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

    internal static class AesKeyWrapRfc5649
    {
        private const int BlockSize = 8; // 64-bit blocks
        private static readonly byte[] DefaultIvPrefix = { 0xA6, 0x59, 0x59, 0xA6 };

        public static byte[] Wrap(byte[] kek, byte[] keyToWrap)
        {
            if (kek == null) throw new ArgumentNullException(nameof(kek));
            if (keyToWrap == null || keyToWrap.Length == 0) throw new ArgumentException("Key to wrap cannot be empty.", nameof(keyToWrap));

            int n = (keyToWrap.Length + 7) / BlockSize;
            byte[] P = new byte[n * BlockSize];
            Array.Copy(keyToWrap, P, keyToWrap.Length); // zero padding

            Span<byte> Q = stackalloc byte[8];
            DefaultIvPrefix.CopyTo(Q);
            BinaryPrimitives.WriteUInt32BigEndian(Q.Slice(4, 4), (uint)keyToWrap.Length);
            ulong A = BinaryPrimitives.ReadUInt64BigEndian(Q);

            if (keyToWrap.Length <= BlockSize)
            {
                byte[] block = new byte[16];
                BinaryPrimitives.WriteUInt64BigEndian(block.AsSpan(0, 8), A);
                Array.Copy(P, 0, block, 8, BlockSize);

                using var aes = CreateEcbAes(kek);
                return aes.CreateEncryptor().TransformFinalBlock(block, 0, block.Length);
            }

            byte[] R = new byte[n * BlockSize];
            Array.Copy(P, R, P.Length);

            using var aesWrap = CreateEcbAes(kek);
            using var enc = aesWrap.CreateEncryptor();

            for (int j = 0; j <= 5; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    byte[] B = new byte[16];
                    BinaryPrimitives.WriteUInt64BigEndian(B.AsSpan(0, 8), A);
                    Array.Copy(R, i * BlockSize, B, 8, BlockSize);

                    var C = enc.TransformFinalBlock(B, 0, 16);
                    ulong msb = BinaryPrimitives.ReadUInt64BigEndian(C.AsSpan(0, 8));
                    ulong t = (ulong)(j * n + (i + 1));
                    A = msb ^ t;

                    Array.Copy(C, 8, R, i * BlockSize, BlockSize);
                }
            }

            byte[] Ctotal = new byte[(n + 1) * BlockSize];
            BinaryPrimitives.WriteUInt64BigEndian(Ctotal.AsSpan(0, 8), A);
            Array.Copy(R, 0, Ctotal, 8, R.Length);

            return Ctotal;
        }

        public static byte[] Unwrap(byte[] kek, byte[] wrapped)
        {
            if (kek == null) throw new ArgumentNullException(nameof(kek));
            if (wrapped == null || wrapped.Length < 16 || wrapped.Length % 8 != 0)
                throw new ArgumentException("Invalid wrapped key length.", nameof(wrapped));

            if (wrapped.Length == 16)
            {
                using var aes = CreateEcbAes(kek);
                var D = aes.CreateDecryptor().TransformFinalBlock(wrapped, 0, 16);

                for (int i = 0; i < 4; i++)
                    if (D[i] != DefaultIvPrefix[i])
                        throw new CryptographicException("RFC5649 IV prefix check failed.");

                uint mli = BinaryPrimitives.ReadUInt32BigEndian(D.AsSpan(4, 4));
                if (mli == 0 || mli > 8) throw new CryptographicException("Invalid MLI for key <= 8");

                for (int i = (int)mli; i < 8; i++)
                    if (D[8 + i] != 0) throw new CryptographicException("Non-zero padding");

                byte[] K = new byte[mli];
                Array.Copy(D, 8, K, 0, (int)mli);
                return K;
            }

            int n = wrapped.Length / 8 - 1;
            ulong A = BinaryPrimitives.ReadUInt64BigEndian(wrapped.AsSpan(0, 8));
            byte[] R = new byte[n * BlockSize];
            Array.Copy(wrapped, 8, R, 0, R.Length);

            using var aesWrap = CreateEcbAes(kek);
            using var dec = aesWrap.CreateDecryptor();

            for (int j = 5; j >= 0; j--)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    ulong t = (ulong)(j * n + (i + 1));
                    ulong Atemp = A ^ t;

                    byte[] B = new byte[16];
                    BinaryPrimitives.WriteUInt64BigEndian(B.AsSpan(0, 8), Atemp);
                    Array.Copy(R, i * BlockSize, B, 8, BlockSize);

                    B = dec.TransformFinalBlock(B, 0, 16);

                    A = BinaryPrimitives.ReadUInt64BigEndian(B.AsSpan(0, 8));
                    Array.Copy(B, 8, R, i * BlockSize, BlockSize);
                }
            }

            Span<byte> Q = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(Q, A);
            for (int i = 0; i < 4; i++)
                if (Q[i] != DefaultIvPrefix[i])
                    throw new CryptographicException("RFC5649 IV prefix check failed.");

            uint mliFinal = BinaryPrimitives.ReadUInt32BigEndian(Q.Slice(4, 4));
            if (mliFinal == 0 || mliFinal > R.Length) throw new CryptographicException("Invalid MLI after unwrap");

            for (int i = (int)mliFinal; i < R.Length; i++)
                if (R[i] != 0) throw new CryptographicException("Non-zero padding detected");

            byte[] Kfinal = new byte[mliFinal];
            Array.Copy(R, 0, Kfinal, 0, (int)mliFinal);
            return Kfinal;
        }

        private static Aes CreateEcbAes(byte[] key)
        {
            // Use FIPS-compliant AES provider
            var aes = new AesCng
            {
                KeySize = key.Length * 8, // bits
                Key = key,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };
            return aes;
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

                        using var aesGcm = new AesGcm(key);
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

                        using var aesGcm = new AesGcm(key);
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
}
