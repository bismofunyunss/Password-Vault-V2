using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using Windows.Storage.Streams;
using static Password_Vault_V2.Crypto;
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
        private static byte[] Extract(byte[] salt, byte[] inputKeyMaterial)
        {
            using var hmac = new HMACSHA256(salt ?? new byte[32]);
            return hmac.ComputeHash(inputKeyMaterial);
        }

        private static byte[] Expand(byte[] prk, byte[] info, int outputLength)
        {
            var hashLen = 32; // SHA256 output length in bytes
            var iterations = (int)Math.Ceiling((double)outputLength / hashLen);
            if (iterations > 255)
                throw new ArgumentOutOfRangeException(nameof(outputLength), "Cannot expand to more than 255 blocks");

            var okm = new byte[outputLength];
            var previous = Array.Empty<byte>();

            using var hmac = new HMACSHA256(prk);

            var pos = 0;
            for (var i = 1; i <= iterations; i++)
            {
                // T(i) = HMAC-PRK( T(i-1) | info | i )
                hmac.Initialize();

                hmac.TransformBlock(previous, 0, previous.Length, null, 0);
                hmac.TransformBlock(info, 0, info.Length, null, 0);
                hmac.TransformFinalBlock(new[] { (byte)i }, 0, 1);

                previous = hmac.Hash!;
                hmac.Initialize();

                var toCopy = Math.Min(hashLen, outputLength - pos);
                Array.Copy(previous, 0, okm, pos, toCopy);
                pos += toCopy;
            }

            return okm;
        }

        public static byte[] DeriveKey(byte[] inputKeyMaterial, byte[] salt, byte[] info, int outputLength)
        {
            var prk = Extract(salt, inputKeyMaterial);
            return Expand(prk, info, outputLength);
        }
    }

public static class AesKeyWrapRfc5649
{
    private const int BlockSize = 8; // 64-bit
    private static readonly byte[] DefaultIvPrefix = { 0xA6, 0x59, 0x59, 0xA6 };

        public static byte[] Wrap(byte[] kek, byte[] keyToWrap)
        {
            if (kek == null) throw new ArgumentNullException(nameof(kek));
            if (keyToWrap == null || keyToWrap.Length == 0) throw new ArgumentException("Key to wrap cannot be empty.", nameof(keyToWrap));

            const int BlockSize = 8;
            int n = (keyToWrap.Length + 7) / BlockSize; // number of 64-bit blocks
            byte[] P = new byte[n * BlockSize];
            Buffer.BlockCopy(keyToWrap, 0, P, 0, keyToWrap.Length); // zero padding

            // Q = IV prefix || MLI (big-endian 32-bit)
            Span<byte> Q = stackalloc byte[8];
            DefaultIvPrefix.CopyTo(Q); // first 4 bytes
            BinaryPrimitives.WriteUInt32BigEndian(Q.Slice(4, 4), (uint)keyToWrap.Length);
            ulong A = BinaryPrimitives.ReadUInt64BigEndian(Q);

            FileLogger.Log($"Wrap - Initial Q: {BitConverter.ToString(Q.ToArray()).Replace("-", "")}");
            FileLogger.Log($"Wrap - Initial P: {BitConverter.ToString(P).Replace("-", "")}");

            // Special-case (MLI <= 8): encrypt A || P1
            if (keyToWrap.Length <= BlockSize)
            {
                byte[] block = new byte[16];
                // write A (the IV+MLI) as big-endian
                BinaryPrimitives.WriteUInt64BigEndian(block.AsSpan(0, 8), A);
                // write P (padded to 8 bytes) into block[8..15]
                Array.Copy(P, 0, block, 8, BlockSize);

                using var aes = CreateEcbAes(kek);
                var C = aes.CreateEncryptor().TransformFinalBlock(block, 0, 16);

                FileLogger.Log($"Wrap - Output (short): {BitConverter.ToString(C).Replace("-", "")}");
                return C;
            }

            // General case (RFC 3394-style rounds)
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

            FileLogger.Log($"Wrap - Final A: {A:X16}");
            FileLogger.Log($"Wrap - Final R: {BitConverter.ToString(R).Replace("-", "")}");
            FileLogger.Log($"Wrap - Output: {BitConverter.ToString(Ctotal).Replace("-", "")}");

            return Ctotal;
        }

        public static byte[] Unwrap(byte[] kek, byte[] wrapped)
    {
        if (kek == null) throw new ArgumentNullException(nameof(kek));
        if (wrapped == null || wrapped.Length < 16 || wrapped.Length % 8 != 0)
            throw new ArgumentException("Invalid wrapped key length.", nameof(wrapped));

        if (wrapped.Length == 16)
        {
            // Special case: MLI <= 8
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
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;
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
}