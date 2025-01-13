using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Sodium;

namespace Password_Vault_V2;

public static partial class Crypto
{
    /// <summary>
    ///     Encrypts the contents of a file using Argon2 key derivation and four layers of encryption.
    /// </summary>
    /// <param name="userName">The username associated with the user's salt for key derivation.</param>
    /// <param name="passWord">The user's password used for key derivation.</param>
    /// <param name="plainText">The plaintext to encrypt.</param>
    /// <returns>
    ///     A Task that completes with the encrypted content of the specified file.
    ///     If any error occurs during the process, returns an empty byte array.
    /// </returns>
    /// <remarks>
    ///     This method performs the following steps:
    ///     1. Validates input parameters to ensure they are not null or empty.
    ///     2. Retrieves the user-specific salt for key derivation.
    ///     3. Derives an encryption key from the user's password and the obtained salt using Argon2id.
    ///     4. Extracts key components for encryption, including two keys and an HMAC key.
    ///     5. Reads and encodes the content of the specified file.
    ///     6. Encrypts the file content using XChaCha20-Poly1305 encryption.
    ///     7. Clears sensitive information, such as the user's password, from memory.
    /// </remarks>
    public static async Task<byte[]> EncryptFile(string userName, byte[] passWord, string plainText)
    {
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("Value was empty.", nameof(userName));
        if (passWord.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(passWord));
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Value was empty.", nameof(plainText));

        var salt = Authentication.GetUserSalt(userName);

        var bytes = await HashingMethods.Argon2Id(passWord, salt, 544);
        if (bytes == Array.Empty<byte>())
            throw new Exception("Value was empty.");

        var fileBytes = DataConversionHelpers.StringToByteArray(await File.ReadAllTextAsync(plainText));

        if (fileBytes == null || fileBytes.Length == 0 || salt == null || salt.Length == 0)
            throw new ArgumentException("Value was empty.");

        var (key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3) = BufferInit.InitBuffers(bytes);

        var compressedText = await CryptoUtilities.CompressText(fileBytes);
        var encryptedFile = EncryptionDecryption.EncryptV3(compressedText, key, key2, key3, key4,
            key5, hMacKey,
            hMacKey2, hMacKey3);

        var arrays = new[]
        {
            key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3, bytes
        };

        CryptoUtilities.ClearMemory(arrays);

        return encryptedFile;
    }

    /// <summary>
    ///     Decrypts the contents of an encrypted file using Argon2 key derivation and four layers of decryption.
    /// </summary>
    /// <param name="userName">The username associated with the user's salt for key derivation.</param>
    /// <param name="passWord">The user's password used for key derivation.</param>
    /// <param name="cipherText">The ciphertext to decrypt.</param>
    /// <returns>
    ///     A Task that completes with the decrypted content of the specified encrypted file.
    ///     If any error occurs during the process, returns an empty byte array.
    /// </returns>
    /// <remarks>
    ///     This method performs the following steps:
    ///     1. Validates input parameters to ensure they are not null or empty.
    ///     2. Retrieves the user-specific salts for key derivation.
    ///     3. Derives an encryption key from the user's password and the obtained salt using Argon2id.
    ///     4. Extracts key components for decryption, including two keys and an HMAC key.
    ///     5. Reads and decodes the content of the encrypted file.
    ///     6. Decrypts the file content using ChaCha20-Poly1305 decryption.
    ///     7. Clears sensitive information, such as the user's password, from memory.
    /// </remarks>
    public static async Task<byte[]> DecryptFile(string userName, byte[] passWord, string cipherText)
    {
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("Value was empty.", nameof(userName));
        if (passWord.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(passWord));
        if (string.IsNullOrEmpty(cipherText))
            throw new ArgumentException("Value was empty.", nameof(cipherText));

        var salt = Authentication.GetUserSalt(userName);

        var bytes = await HashingMethods.Argon2Id(passWord, salt, 544);
        var (key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3) = BufferInit.InitBuffers(bytes);

        var fileStr = await File.ReadAllTextAsync(cipherText);
        var fileBytes = DataConversionHelpers.Base64StringToByteArray(fileStr);

        if (fileBytes == Array.Empty<byte>() || salt == Array.Empty<byte>())
            throw new ArgumentException("Value was empty.");

        var decryptedFile =
            EncryptionDecryption.DecryptV3(fileBytes, key, key2, key3, key4, key5, hMacKey,
                hMacKey2, hMacKey3);
        var decompressedText = await CryptoUtilities.DecompressText(decryptedFile);

        var arrays = new[]
        {
            key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3, bytes
        };
        CryptoUtilities.ClearMemory(arrays);

        return decompressedText;
    }

    public static async Task<byte[]> EncryptFile(string userName, byte[] input, byte[] passWord, byte[] salt)
    {
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("Value was empty.", nameof(userName));
        if (input.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(input));
        if (passWord.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(passWord));
        if (salt.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(salt));

        var bytes = await HashingMethods.Argon2Id(passWord, salt, 544);
        var (key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3) = BufferInit.InitBuffers(bytes);
        byte[] encryptedFile;

        try
        {
            encryptedFile = EncryptionDecryption.EncryptV3(input, key, key2, key3, key4,
                key5, hMacKey, hMacKey2, hMacKey3);
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            throw;
        }
        finally
        {
            var arrays = new[]
            {
                key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3, bytes
            };

            CryptoUtilities.ClearMemory(arrays);
        }

        return encryptedFile;
    }

    public static async Task<byte[]> DecryptFile(string userName, byte[] input, byte[] passWord, byte[] salt)
    {
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentException("Value was empty.", nameof(userName));
        if (input.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(input));
        if (passWord.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(passWord));
        if (salt.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(salt));

        var bytes = await HashingMethods.Argon2Id(passWord, salt, 544);
        var (key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3) = BufferInit.InitBuffers(bytes);
        byte[] decryptedFile;
        try
        {
            decryptedFile = EncryptionDecryption.DecryptV3(input, key, key2, key3, key4,
                key5, hMacKey, hMacKey2, hMacKey3);
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            throw;
        }
        finally
        {
            var arrays = new[]
            {
                key, key2, key3, key4, key5, hMacKey, hMacKey2, hMacKey3, bytes
            };

            CryptoUtilities.ClearMemory(arrays);
        }

        return decryptedFile;
    }

    private static partial class Memset
    {
        [LibraryImport("msvcrt.dll", EntryPoint = "memset", SetLastError = false)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void MemSet(IntPtr dest, int c, int byteCount);
    }

    /// <summary>
    ///     Utility class for cryptographic settings and initialization.
    /// </summary>
    public static class CryptoConstants
    {
        public const int SaltSize = 128;
        public const int HmacLength = 64;
        public const int ChaChaNonceSize = 24;
        public const int KeySize = 32;
        public const int Iv = 16;
        public const int ThreeFish = 128;
        public const int ShuffleKey = 128;
        public const int BlockBitSize = 128;
        public const int KeyBitSize = 256;
        public static int Iterations = Settings.Default.Iterations;
        public static double MemorySize = Settings.Default.MemorySize * Math.Pow(1024, 2);
        public static int Parallelism = Settings.Default.Parallelism;

        public static readonly RandomNumberGenerator RndNum = RandomNumberGenerator.Create();
        public static byte[] SecurePasswordSalt = [];
#pragma warning disable CA2211

        public static byte[] Hash = [];
        public static byte[] SecurePassword = [];
        public static byte[] PasswordBytes = [];

#pragma warning restore CA2211
    }

    public static class ConversionMethods
    {
        public static byte[] ToByteArray(SecureString secureString)
        {
            if (secureString == null)
                throw new ArgumentNullException(nameof(secureString), "Value was null.");

            var unmanagedString = IntPtr.Zero;
            try
            {
                // Marshal the SecureString to an unmanaged string
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);

                // Calculate the number of characters in the SecureString
                var charCount = secureString.Length;

                // Allocate a byte array to hold the UTF-16 bytes (2 bytes per character)
                var utf16Bytes = new byte[charCount * sizeof(char)];

                // Copy the UTF-16 bytes from unmanaged memory to the byte array
                Marshal.Copy(unmanagedString, utf16Bytes, 0, utf16Bytes.Length);

                // Allocate a char array and copy the UTF-16 bytes into it
                var utf16Chars = new char[charCount];
                Buffer.BlockCopy(utf16Bytes, 0, utf16Chars, 0, utf16Bytes.Length);

                // Determine the length of the resulting UTF-8 byte array
                var utf8ByteCount = Encoding.UTF8.GetByteCount(utf16Chars);

                // Allocate a byte array to hold the UTF-8 bytes
                var utf8Bytes = new byte[utf8ByteCount];

                // Convert the UTF-16 byte array to the UTF-8 byte array
                Encoding.UTF8.GetBytes(utf16Chars, 0, utf16Chars.Length, utf8Bytes, 0);

                CryptoUtilities.ClearMemory(utf16Bytes);
                CryptoUtilities.ClearMemory(utf16Chars);
                return utf8Bytes;
            }
            finally
            {
                // Zero out the unmanaged memory
                if (unmanagedString != IntPtr.Zero) Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }

    public static class HashingMethods
    {
        /// <summary>
        ///     Hashes a password inside a char array or derives a key from a password.
        /// </summary>
        /// <param name="passWord">The char array to hash.</param>
        /// <param name="salt">The salt used during the argon2id hashing process.</param>
        /// <param name="outputSize">The output size in bytes.</param>
        /// <returns>Either a derived key or password hash byte array.</returns>
        public static async Task<byte[]> Argon2Id(byte[] passWord, byte[] salt, int outputSize)
        {
            if (passWord == null || passWord.Length == 0)
                throw new ArgumentException("Password cannot be null or empty.", nameof(passWord));
            if (salt == null || salt.Length == 0)
                throw new ArgumentException("Salt cannot be null or empty.", nameof(salt));

            using var argon2 = new Argon2id(passWord);
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = CryptoConstants.Parallelism;
            argon2.Iterations = CryptoConstants.Iterations;
            argon2.MemorySize = (int)CryptoConstants.MemorySize;

            var result = await argon2.GetBytesAsync(outputSize);

            return result;
        }

        /// <summary>
        ///     Computes the Hash-based Message Authentication Code (HMAC) using the SHA3-512 algorithm.
        /// </summary>
        /// <param name="input">The byte array to be authenticated.</param>
        /// <param name="key">The key used for HMAC computation.</param>
        /// <returns>The HMAC-SHA3-512 authentication code as a byte array.</returns>
        public static byte[] HmacSha3(byte[] input, byte[] key)
        {
            var digest = new Sha3Digest(512);

            var hMac = new HMac(digest);
            hMac.Init(new KeyParameter(key));

            hMac.BlockUpdate(input, 0, input.Length);

            var result = new byte[hMac.GetMacSize()];
            hMac.DoFinal(result, 0);

            return result;
        }

        /// <summary>
        ///     Computes the SHA3-512 hash for the given input byte array.
        /// </summary>
        /// <param name="input">The input byte array for which the hash needs to be computed.</param>
        /// <returns>A byte array representing the SHA3-512 hash of the input.</returns>
        public static byte[] Sha3Hash(byte[] input)
        {
            var digest = new Sha3Digest(512);

            digest.BlockUpdate(input, 0, input.Length);
            var result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, 0);

            return result;
        }
    }

    public static class Memory
    {
        private static readonly Dictionary<IntPtr, int> MemorySizes = [];

        public static IntPtr AllocateMemory(int size)
        {
            var ptr = Marshal.AllocHGlobal(size);
            MemorySizes[ptr] = size;
            return ptr;
        }

        public static void FreeMemory(IntPtr ptr)
        {
            if (MemorySizes.ContainsKey(ptr))
            {
                MemorySizes.Remove(ptr);
                Marshal.FreeHGlobal(ptr);
            }
            else
            {
                throw new ArgumentException("Pointer not found in allocated memory.");
            }
        }

        public static void ClearMemory(params IntPtr[] ptrs)
        {
            if (ptrs == null)
                throw new ArgumentNullException(nameof(ptrs), "Input cannot be null.");

            foreach (var ptr in ptrs)
                if (MemorySizes.TryGetValue(ptr, out var size))
                    try
                    {
                        Memset.MemSet(ptr, 0, size);
                    }
                    catch (AccessViolationException ex)
                    {
                        ErrorLogging.ErrorLog(ex);
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                else
                    throw new ArgumentException("Pointer not found in allocated memory.");

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
            GC.WaitForPendingFinalizers();
        }
    }

    public static class CryptoUtilities
    {
        /// <summary>
        ///     Generates a random integer using a cryptographically secure random number generator.
        /// </summary>
        /// <returns>A random integer value.</returns>
        /// <remarks>
        ///     This method generates a random integer by obtaining random bytes from a cryptographically secure
        ///     random number generator and converting them to an integer using the BitConverter class.
        /// </remarks>
        private static int RndInt()
        {
            var buffer = new byte[sizeof(int)];

            CryptoConstants.RndNum.GetBytes(buffer);

            var result = BitConverter.ToInt32(buffer, 0);

            return result;
        }

        /// <summary>
        ///     Generates a random integer within the specified inclusive range.
        /// </summary>
        /// <param name="min">The minimum value of the range (inclusive).</param>
        /// <param name="max">The maximum value of the range (inclusive).</param>
        /// <returns>A random integer within the specified range.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if the specified minimum value is greater than or equal to the maximum
        ///     value.
        /// </exception>
        /// <remarks>
        ///     This method generates a random integer within the specified inclusive range using the RndInt method.
        ///     The generated integer is constrained to the provided range by applying modular arithmetic.
        /// </remarks>
        public static int BoundedInt(int min, int max)
        {
            if (min >= max)
                throw new ArgumentException("Min must be less than max.");

            var value = RndInt();

            var range = max - min;

            var result = min + Math.Abs(value) % range;

            return result;
        }

        /// <summary>
        ///     Generates a random byte array of the specified size using a cryptographically secure random number generator.
        /// </summary>
        /// <param name="size">The size of the byte array to generate.</param>
        /// <returns>A random byte array of the specified size.</returns>
        /// <remarks>
        ///     This method generates a random byte array by obtaining random bytes from a cryptographically secure
        ///     random number generator. The size of the byte array is determined by the input parameter 'size'.
        /// </remarks>
        public static byte[] RndByteSized(int size)
        {
            var buffer = new byte[size];

            CryptoConstants.RndNum.GetBytes(buffer);

            return buffer;
        }

        /// <summary>
        ///     Compares two password hashes in a secure manner using fixed-time comparison.
        /// </summary>
        /// <param name="hash1">The first password hash.</param>
        /// <param name="hash2">The second password hash.</param>
        /// <returns>
        ///     A bool that completes with 'true' if the hashes are equal, and 'false' otherwise.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if either hash is null or empty.
        /// </exception>
        /// <remarks>
        ///     This method uses fixed-time comparison to mitigate certain types of timing attacks.
        /// </remarks>
        public static bool ComparePassword(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length == 0 || hash1 == null)
                throw new ArgumentException("Value was empty or null.", nameof(hash1));
            if (hash2.Length == 0 || hash2 == null)
                throw new ArgumentException("Value was empty or null.", nameof(hash2));

            return CryptographicOperations.FixedTimeEquals(hash1, hash2);
        }

        /// <summary>
        ///     Clears the sensitive information stored in one or more byte arrays using memset.
        /// </summary>
        /// <remarks>
        ///     This method uses a pinned array and the SecureMemoryClear function to overwrite the memory
        ///     containing sensitive information, enhancing security by preventing the information from being
        ///     easily accessible in memory.
        /// </remarks>
        /// <param name="arrays">The byte arrays containing sensitive information to be cleared.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the input arrays is null.</exception>
        public static void ClearMemory(params byte[][] arrays)
        {
            if (arrays == null)
                throw new ArgumentNullException(nameof(arrays), "Input cannot be null.");

            foreach (var byteArray in arrays)
            {
                var handle = GCHandle.Alloc(byteArray, GCHandleType.Pinned);

                try
                {
                    Memset.MemSet(handle.AddrOfPinnedObject(), 0, byteArray.Length);
                }
                catch (AccessViolationException ex)
                {
                    ErrorLogging.ErrorLog(ex);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    handle.Free();

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        public static void ClearMemory(byte[] array)
        {
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);

            try
            {
                Memset.MemSet(handle.AddrOfPinnedObject(), 0, array.Length * sizeof(byte));
            }
            catch (AccessViolationException ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        ///     Clears the sensitive information stored in one or more char arrays securely.
        /// </summary>
        /// <remarks>
        ///     This method uses a pinned array and the SecureMemoryClear function to overwrite the memory
        ///     containing sensitive information, enhancing security by preventing the information from being
        ///     easily accessible in memory.
        /// </remarks>
        /// <param name="arrays">The char arrays containing sensitive information to be cleared.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the input arrays is null.</exception>
        public static void ClearMemory(char[][] arrays)
        {
            if (arrays == null)
                throw new ArgumentNullException(nameof(arrays), "Input strings cannot be null.");

            foreach (var value in arrays)
            {
                var handle = GCHandle.Alloc(value, GCHandleType.Pinned);

                try
                {
                    Memset.MemSet(handle.AddrOfPinnedObject(), 0, value.Length * sizeof(char));
                }
                catch (AccessViolationException ex)
                {
                    ErrorLogging.ErrorLog(ex);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        public static void ClearMemory(char[] array)
        {
            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);

            try
            {
                Memset.MemSet(handle.AddrOfPinnedObject(), 0, array.Length * sizeof(char));
            }
            catch (AccessViolationException ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                handle.Free();
            }
        }

        public static void ClearMemory(char c)
        {
            var handle = GCHandle.Alloc(c, GCHandleType.Pinned);

            try
            {
                Memset.MemSet(handle.AddrOfPinnedObject(), 0, 1);
            }
            catch (AccessViolationException ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        ///     Clears the sensitive information stored in one or more strings securely.
        /// </summary>
        /// <remarks>
        ///     This method uses a pinned string and the SecureMemoryClear function to overwrite the memory
        ///     containing sensitive information, enhancing security by preventing the information from being
        ///     easily accessible in memory.
        /// </remarks>
        /// <param name="str">The strings containing sensitive information to be cleared.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the input strings is null.</exception>
        public static void ClearMemory(params string[][] str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str), "Input strings cannot be null.");

            foreach (var value in str)
            {
                var handle = GCHandle.Alloc(value, GCHandleType.Pinned);

                try
                {
                    Memset.MemSet(handle.AddrOfPinnedObject(), 0, value.Length * 2);
                }
                catch (AccessViolationException ex)
                {
                    ErrorLogging.ErrorLog(ex);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        public static void ClearMemory(string str)
        {
            var handle = GCHandle.Alloc(str, GCHandleType.Pinned);

            try
            {
                Memset.MemSet(handle.AddrOfPinnedObject(), 0, str.Length * 2);
            }
            catch (AccessViolationException ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        ///     Clears the sensitive information stored in one or more pointers securely.
        /// </summary>
        /// <remarks>
        ///     This method uses a pinned string and the SecureMemoryClear function to overwrite the memory
        ///     containing sensitive information, enhancing security by preventing the information from being
        ///     easily accessible in memory.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if any of the input strings is null.</exception>
        public static void ClearMemory(int size, ref IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(ptr), "Invalid ptr.");

            var handle = GCHandle.Alloc(ptr, GCHandleType.Pinned);

            try
            {
                Memset.MemSet(handle.AddrOfPinnedObject(), 0, size);
            }
            catch (AccessViolationException ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        ///     Compresses a byte array using the GZip compression algorithm.
        /// </summary>
        /// <remarks>
        ///     This method takes a byte array as input, compresses it using the GZip compression algorithm,
        ///     and returns the compressed byte array. The compression level used is <see cref="CompressionLevel.SmallestSize" />.
        /// </remarks>
        /// <param name="inputText">The byte array representing the uncompressed text to be compressed.</param>
        /// <returns>A compressed byte array using the GZip compression algorithm.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the input byte array is null.</exception>
        public static async Task<byte[]> CompressText(byte[] inputText)
        {
            if (inputText == null)
                throw new ArgumentNullException(nameof(inputText), "Input byte array cannot be null.");

            using var outputStream = new MemoryStream();
            await using (var zipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, true))
            {
                await zipStream.WriteAsync(inputText).ConfigureAwait(false);
            }

            return outputStream.ToArray();
        }

        /// <summary>
        ///     Decompresses a byte array that was compressed using the GZip compression algorithm.
        /// </summary>
        /// <remarks>
        ///     This method takes a compressed byte array as input, decompresses it using the GZip compression algorithm,
        ///     and returns the decompressed byte array.
        /// </remarks>
        /// <param name="inputText">The byte array representing the compressed text to be decompressed.</param>
        /// <returns>A decompressed byte array using the GZip compression algorithm.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the input byte array is null.</exception>
        public static async Task<byte[]> DecompressText(byte[] inputText)
        {
            if (inputText == null)
                throw new ArgumentNullException(nameof(inputText), "Input byte array cannot be null.");

            using var inputStream = new MemoryStream(inputText);
            await using var zipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();
            await zipStream.CopyToAsync(outputStream);

            return outputStream.ToArray();
        }
    }

    /// <summary>
    ///     Utility class for buffer initialization that contains the necessary keys for cryptographic functions.
    /// </summary>
    private static class BufferInit
    {
        /// <summary>
        ///     Initializes multiple byte arrays from a source byte array for cryptographic operations.
        /// </summary>
        /// <remarks>
        ///     This method takes a source byte array and extracts key components for encryption and HMAC operations.
        ///     It initializes multiple byte arrays for different cryptographic purposes and returns them as a tuple.
        /// </remarks>
        /// <param name="src">The source byte array used for initializing cryptographic buffers.</param>
        /// <returns>
        ///     A tuple containing byte arrays for encryption keys (key, key2, key3, key4, key5)
        ///     and HMAC keys (hMacKey, hMackey2, hMacKey3).
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if the source byte array is null.</exception>
        public static (byte[] key, byte[] key2, byte[] key3, byte[] key4, byte[] key5, byte[] hMacKey, byte[]
            hMackey2, byte[] hMacKey3)
            InitBuffers(byte[] src)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src), "Source byte array cannot be null.");

            var key = new byte[CryptoConstants.KeySize];
            var key2 = new byte[CryptoConstants.ThreeFish];
            var key3 = new byte[CryptoConstants.KeySize];
            var key4 = new byte[CryptoConstants.KeySize];
            var key5 = new byte[CryptoConstants.ShuffleKey];
            var hMacKey = new byte[CryptoConstants.HmacLength];
            var hMackey2 = new byte[CryptoConstants.HmacLength];
            var hMacKey3 = new byte[CryptoConstants.HmacLength];

            CopyBytes(src, key, key2, key3, key4, key5, hMacKey, hMackey2, hMacKey3);

            return (key, key2, key3, key4, key5, hMacKey, hMackey2, hMacKey3);
        }

        /// <summary>
        ///     Copies bytes from a source byte array to multiple destination byte arrays.
        /// </summary>
        /// <remarks>
        ///     This method copies bytes from a source byte array to multiple destination byte arrays.
        ///     It uses Buffer.BlockCopy for efficient copying and advances the offset for each destination array.
        /// </remarks>
        /// <param name="src">The source byte array from which bytes are copied.</param>
        /// <param name="dest">The destination byte arrays where bytes are copied to.</param>
        /// <exception cref="ArgumentNullException">Thrown if the source byte array or any destination byte array is null.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if the total length of destination arrays exceeds the length of the source
        ///     array.
        /// </exception>
        private static void CopyBytes(byte[] src, params byte[][] dest)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src), "Source byte array cannot be null.");

            var currentIndex = 0;

            foreach (var dst in dest)
            {
                if (dst == null)
                    throw new ArgumentNullException(nameof(dest), "Destination byte array cannot be null.");

                if (src.Length < dst.Length)
                    throw new ArgumentException(
                        "Length of the destination array cannot exceed the length of the source array.");

                Buffer.BlockCopy(src, currentIndex, dst, 0, dst.Length);
                currentIndex += dst.Length;
            }
        }
    }

    /// <summary>
    ///     A class that contains different algorithms for encrypting and decrypting.
    /// </summary>
    public class Algorithms
    {
        /// <summary>
        ///     Generates an array of random indices for shuffling based on a given size and key.
        /// </summary>
        /// <param name="size">The size of the array for which shuffle exchanges are generated.</param>
        /// <param name="key">The key used for generating random indices.</param>
        /// <returns>An array of random indices for shuffling.</returns>
        /// <remarks>
        ///     The method uses a random number generator with the specified key to generate
        ///     unique indices for shuffling a byte array of the given size.
        /// </remarks>
        private static int[] GetShuffleExchanges(int size, byte[] key)
        {
            var exchanges = new int[size - 1];
            var rand = new Random(BitConverter.ToInt32(key));

            for (var i = size - 1; i > 0; i--) exchanges[size - 1 - i] = rand.Next(i + 1);

            return exchanges;
        }

        /// <summary>
        ///     Shuffles a byte array based on a given key using a custom exchange algorithm.
        /// </summary>
        /// <param name="input">The byte array to be shuffled.</param>
        /// <param name="key">The key used for shuffling.</param>
        /// <returns>The shuffled byte array.</returns>
        /// <remarks>
        ///     The shuffling is performed using a custom exchange algorithm based on the specified key.
        /// </remarks>
        public static byte[] Shuffle(byte[] input, byte[] key)
        {
            var size = input.Length;
            var exchanges = GetShuffleExchanges(size, key);

            for (var i = size - 1; i > 0; i--)
            {
                var n = exchanges[size - 1 - i];
                (input[i], input[n]) = (input[n], input[i]);
            }

            return input;
        }

        /// <summary>
        ///     Shuffles a char array based on a given key using a custom exchange algorithm.
        /// </summary>
        /// <param name="input">The char array to be shuffled.</param>
        /// <param name="key">The key used for shuffling.</param>
        /// <returns>The shuffled char array.</returns>
        /// <remarks>
        ///     The shuffling is performed using a custom exchange algorithm based on the specified key.
        /// </remarks>
        public static char[] Shuffle(char[] input, byte[] key)
        {
            var size = input.Length;
            var exchanges = GetShuffleExchanges(size, key);

            for (var i = size - 1; i > 0; i--)
            {
                var n = exchanges[size - 1 - i];
                (input[i], input[n]) = (input[n], input[i]);
            }

            return input;
        }

        /// <summary>
        ///     De-shuffles a byte array based on a given key using a custom exchange algorithm.
        /// </summary>
        /// <param name="input">The byte array to be de-shuffled.</param>
        /// <param name="key">The key used for de-shuffling.</param>
        /// <returns>The de-shuffled byte array.</returns>
        /// <remarks>
        ///     The de-shuffling is performed using a custom exchange algorithm based on the specified key.
        /// </remarks>
        public static byte[] DeShuffle(byte[] input, byte[] key)
        {
            var size = input.Length;
            var exchanges = GetShuffleExchanges(size, key);

            for (var i = 1; i < size; i++)
            {
                var n = exchanges[size - i - 1];
                (input[i], input[n]) = (input[n], input[i]);
            }

            return input;
        }

        /// <summary>
        ///     De-shuffles a char array based on a given key using a custom exchange algorithm.
        /// </summary>
        /// <param name="input">The char array to be de-shuffled.</param>
        /// <param name="key">The key used for de-shuffling.</param>
        /// <returns>The de-shuffled char array.</returns>
        /// <remarks>
        ///     The de-shuffling is performed using a custom exchange algorithm based on the specified key.
        /// </remarks>
        public static char[] DeShuffle(char[] input, byte[] key)
        {
            var size = input.Length;
            var exchanges = GetShuffleExchanges(size, key);

            for (var i = 1; i < size; i++)
            {
                var n = exchanges[size - i - 1];
                (input[i], input[n]) = (input[n], input[i]);
            }

            return input;
        }

        /// <summary>
        ///     Encrypts data using the XChaCha20-Poly1305 authenticated encryption algorithm.
        /// </summary>
        /// <param name="input">The data to be encrypted.</param>
        /// <param name="key">The encryption key.</param>
        /// <param name="nonce">The nonce (number used once) for encryption.</param>
        /// <returns>A byte array representing the encrypted data.</returns>
        public static byte[] EncryptXChaCha20Poly1305(byte[] input, byte[] key, byte[] nonce)
        {
            var result = SecretAeadXChaCha20Poly1305.Encrypt(input, key, nonce);

            return result;
        }

        /// <summary>
        ///     Decrypts data encrypted using the XChaCha20-Poly1305 authenticated encryption algorithm.
        /// </summary>
        /// <param name="input">The encrypted data.</param>
        /// <param name="key">The decryption key.</param>
        /// <param name="nonce">The nonce used during encryption.</param>
        /// <returns>A byte array representing the decrypted data.</returns>
        public static byte[] DecryptXChaCha20Poly1305(byte[] input, byte[] nonce, byte[] key)
        {
            var result = SecretAeadXChaCha20Poly1305.Decrypt(input, nonce, key);

            return result;
        }

        /// <summary>
        ///     Decrypts a byte array that has been encrypted using the AES block cipher in Cipher Block Chaining
        ///     (CBC) mode
        ///     with HMAC-SHA3 authentication.
        /// </summary>
        /// <param name="inputText">The byte array to be decrypted.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="iv"></param>
        /// <param name="hMacKey">The key used for HMAC-SHA3 authentication.</param>
        /// <returns>The decrypted byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided inputText, key, or hMacKey is empty or null.</exception>
        /// <exception cref="CryptographicException">Thrown when the authentication tag does not match.</exception>
        public static byte[] EncryptAes(byte[] inputText, byte[] key, byte[] iv, byte[] hMacKey)
        {
            if (inputText == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (iv == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(iv));
            if (hMacKey == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(hMacKey));

            using var aes = Aes.Create();
            aes.BlockSize = CryptoConstants.BlockBitSize;
            aes.KeySize = CryptoConstants.KeyBitSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] cipherText;

            using (var encryptor = aes.CreateEncryptor(key, iv))
            using (var memStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
                using (var cipherStream = new MemoryStream(inputText))
                {
                    cipherStream.CopyTo(cryptoStream, (int)cipherStream.Length);
                    cryptoStream.FlushFinalBlock();
                }

                cipherText = memStream.ToArray();
            }

            var prependItems = new byte[cipherText.Length + iv.Length];
            Buffer.BlockCopy(iv, 0, prependItems, 0, iv.Length);
            Buffer.BlockCopy(cipherText, 0, prependItems, iv.Length, cipherText.Length);

            var tag = HashingMethods.HmacSha3(prependItems, hMacKey);
            var authenticatedBuffer = new byte[prependItems.Length + tag.Length];
            Buffer.BlockCopy(prependItems, 0, authenticatedBuffer, 0, prependItems.Length);
            Buffer.BlockCopy(tag, 0, authenticatedBuffer, prependItems.Length, tag.Length);

            return authenticatedBuffer;
        }

        /// <summary>
        ///     Decrypts a byte array that has been encrypted using the AES block cipher in Cipher Block Chaining
        ///     (CBC) mode
        ///     with HMAC-SHA3 authentication.
        /// </summary>
        /// <param name="inputText">The byte array to be decrypted.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="hMacKey">The key used for HMAC-SHA3 authentication.</param>
        /// <returns>The decrypted byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided inputText, key, or hMacKey is empty or null.</exception>
        /// <exception cref="CryptographicException">Thrown when the authentication tag does not match.</exception>
        public static byte[] DecryptAes(byte[] inputText, byte[] key, byte[] hMacKey)
        {
            if (inputText == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (hMacKey == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(hMacKey));

            using var aes = Aes.Create();
            aes.BlockSize = CryptoConstants.BlockBitSize;
            aes.KeySize = CryptoConstants.KeyBitSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var receivedHash = new byte[CryptoConstants.HmacLength];
            Buffer.BlockCopy(inputText, inputText.Length - CryptoConstants.HmacLength, receivedHash, 0,
                CryptoConstants.HmacLength);

            var cipherWithIv = new byte[inputText.Length - CryptoConstants.HmacLength];
            Buffer.BlockCopy(inputText, 0, cipherWithIv, 0, inputText.Length - CryptoConstants.HmacLength);

            var hashedInput = HashingMethods.HmacSha3(cipherWithIv, hMacKey);

            var isMatch = CryptographicOperations.FixedTimeEquals(receivedHash, hashedInput);
            if (!isMatch)
                throw new CryptographicException("Authentication tag does not match.");

            var iv = new byte[CryptoConstants.Iv];
            var cipherResult = new byte[inputText.Length - CryptoConstants.Iv - CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(inputText, iv.Length, cipherResult, 0, cipherResult.Length);

            using var decryptor = aes.CreateDecryptor(key, iv);
            using var memStream = new MemoryStream();

            using (var decryptStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Write))
            using (var plainStream = new MemoryStream(cipherResult))
            {
                plainStream.CopyTo(decryptStream, (int)plainStream.Length);
                plainStream.Flush();
                decryptStream.FlushFinalBlock();
            }

            return memStream.ToArray();
        }

        /// <summary>
        ///     Encrypts a byte array using the ThreeFish block cipher in Cipher Block Chaining (CBC) mode with HMAC-SHA3
        ///     authentication.
        /// </summary>
        /// <param name="inputText">The byte array to be encrypted.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector used in CBC mode.</param>
        /// <param name="hMacKey">The key used for HMAC-SHA3 authentication.</param>
        /// <returns>The encrypted and authenticated byte array.</returns>
        public static byte[] EncryptThreeFish(byte[] inputText, byte[] key, byte[] iv, byte[] hMacKey)
        {
            // Initialize ThreeFish block cipher with CBC mode and PKCS7 padding
            var threeFish = new ThreefishEngine(1024);
            var cipherMode = new CbcBlockCipher(threeFish);
            var padding = new Pkcs7Padding();

            var cbcCipher = new PaddedBufferedBlockCipher(cipherMode, padding);

            var keyParam = new KeyParameter(key);
            cbcCipher.Init(true, new ParametersWithIV(keyParam, iv));

            var cipherText = new byte[cbcCipher.GetOutputSize(inputText.Length)];
            var processLength = cbcCipher.ProcessBytes(inputText, 0, inputText.Length, cipherText, 0);
            var finalLength = cbcCipher.DoFinal(cipherText, processLength);
            var finalCipherText = new byte[finalLength + processLength];
            Buffer.BlockCopy(cipherText, 0, finalCipherText, 0, finalCipherText.Length);

            var prependItems = new byte[finalCipherText.Length + iv.Length];
            Buffer.BlockCopy(iv, 0, prependItems, 0, iv.Length);
            Buffer.BlockCopy(finalCipherText, 0, prependItems, iv.Length, finalCipherText.Length);

            var tag = HashingMethods.HmacSha3(prependItems, hMacKey);
            var authenticatedBuffer = new byte[prependItems.Length + tag.Length];
            Buffer.BlockCopy(prependItems, 0, authenticatedBuffer, 0, prependItems.Length);
            Buffer.BlockCopy(tag, 0, authenticatedBuffer, prependItems.Length, tag.Length);

            return authenticatedBuffer;
        }

        /// <summary>
        ///     Decrypts a byte array that has been encrypted using the ThreeFish block cipher in Cipher Block Chaining (CBC) mode
        ///     with HMAC-SHA3 authentication.
        /// </summary>
        /// <param name="inputText">The byte array to be decrypted.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="hMacKey">The key used for HMAC-SHA3 authentication.</param>
        /// <returns>The decrypted byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided inputText, key, or hMacKey is empty or null.</exception>
        /// <exception cref="CryptographicException">Thrown when the authentication tag does not match.</exception>
        public static byte[] DecryptThreeFish(byte[] inputText, byte[] key, byte[] hMacKey)
        {
            if (inputText == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (hMacKey == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(hMacKey));

            var receivedHash = new byte[CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, inputText.Length - CryptoConstants.HmacLength, receivedHash, 0,
                CryptoConstants.HmacLength);

            var cipherWithIv = new byte[inputText.Length - CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, 0, cipherWithIv, 0, inputText.Length - CryptoConstants.HmacLength);

            var hashedInput = HashingMethods.HmacSha3(cipherWithIv, hMacKey);

            var isMatch = CryptographicOperations.FixedTimeEquals(receivedHash, hashedInput);

            if (!isMatch)
                throw new CryptographicException("Authentication tag does not match.");

            var iv = new byte[CryptoConstants.ThreeFish];
            var cipherResult = new byte[inputText.Length - CryptoConstants.ThreeFish - CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(inputText, iv.Length, cipherResult, 0, cipherResult.Length);

            var threeFish = new ThreefishEngine(1024);
            var cipherMode = new CbcBlockCipher(threeFish);
            var padding = new Pkcs7Padding();

            var cbcCipher = new PaddedBufferedBlockCipher(cipherMode, padding);

            var keyParam = new KeyParameter(key);
            cbcCipher.Init(false, new ParametersWithIV(keyParam, iv));

            var plainText = new byte[cbcCipher.GetOutputSize(cipherResult.Length)];
            var processLength = cbcCipher.ProcessBytes(cipherResult, 0, cipherResult.Length, plainText, 0);
            var finalLength = cbcCipher.DoFinal(plainText, processLength);
            var finalPlainText = new byte[finalLength + processLength];
            Buffer.BlockCopy(plainText, 0, finalPlainText, 0, finalPlainText.Length);

            return finalPlainText;
        }

        /// <summary>
        ///     Encrypts a byte array using the Serpent block cipher in Cipher Block Chaining (CBC) mode with HMAC-SHA3
        ///     authentication.
        /// </summary>
        /// <param name="inputText">The byte array to be encrypted.</param>
        /// <param name="key">The key used for encryption.</param>
        /// <param name="iv">The initialization vector used in CBC mode.</param>
        /// <param name="hMacKey">The key used for HMAC-SHA3 authentication.</param>
        /// <returns>The encrypted and authenticated byte array.</returns>
        public static byte[] EncryptSerpent(byte[] inputText, byte[] key, byte[] iv, byte[] hMacKey)
        {
            var serpent = new SerpentEngine();
            var cipherMode = new CbcBlockCipher(serpent);
            var padding = new Pkcs7Padding();

            var cbcCipher = new PaddedBufferedBlockCipher(cipherMode, padding);

            var keyParam = new KeyParameter(key);
            cbcCipher.Init(true, new ParametersWithIV(keyParam, iv));

            var cipherText = new byte[cbcCipher.GetOutputSize(inputText.Length)];
            var processLength = cbcCipher.ProcessBytes(inputText, 0, inputText.Length, cipherText, 0);
            var finalLength = cbcCipher.DoFinal(cipherText, processLength);
            var finalCipherText = new byte[finalLength + processLength];
            Buffer.BlockCopy(cipherText, 0, finalCipherText, 0, finalCipherText.Length);

            var prependItems = new byte[finalCipherText.Length + iv.Length];
            Buffer.BlockCopy(iv, 0, prependItems, 0, iv.Length);
            Buffer.BlockCopy(finalCipherText, 0, prependItems, iv.Length, finalCipherText.Length);

            var tag = HashingMethods.HmacSha3(prependItems, hMacKey);
            var authenticatedBuffer = new byte[prependItems.Length + tag.Length];
            Buffer.BlockCopy(prependItems, 0, authenticatedBuffer, 0, prependItems.Length);
            Buffer.BlockCopy(tag, 0, authenticatedBuffer, prependItems.Length, tag.Length);

            return authenticatedBuffer;
        }

        /// <summary>
        ///     Decrypts a byte array that has been encrypted using the Serpent block cipher in Cipher Block Chaining (CBC) mode
        ///     with HMAC-SHA3 authentication.
        /// </summary>
        /// <param name="inputText">The byte array to be decrypted.</param>
        /// <param name="key">The key used for decryption.</param>
        /// <param name="hMacKey">The key used for HMAC-SHA3 authentication.</param>
        /// <returns>The decrypted byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when the provided inputText, key, or hMacKey is empty or null.</exception>
        /// <exception cref="CryptographicException">Thrown when the authentication tag does not match.</exception>
        public static byte[] DecryptSerpent(byte[] inputText, byte[] key, byte[] hMacKey)
        {
            if (inputText == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (hMacKey == Array.Empty<byte>())
                throw new ArgumentException("Value was empty or null.", nameof(hMacKey));

            var receivedHash = new byte[CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, inputText.Length - CryptoConstants.HmacLength, receivedHash, 0,
                CryptoConstants.HmacLength);

            var cipherWithIv = new byte[inputText.Length - CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, 0, cipherWithIv, 0, inputText.Length - CryptoConstants.HmacLength);

            var hashedInput = HashingMethods.HmacSha3(cipherWithIv, hMacKey);

            var isMatch = CryptographicOperations.FixedTimeEquals(receivedHash, hashedInput);

            if (!isMatch)
                throw new CryptographicException("Authentication tag does not match.");

            var iv = new byte[CryptoConstants.Iv];
            var cipherResult = new byte[inputText.Length - CryptoConstants.Iv - CryptoConstants.HmacLength];

            Buffer.BlockCopy(inputText, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(inputText, iv.Length, cipherResult, 0, cipherResult.Length);

            var serpent = new SerpentEngine();
            var cipherMode = new CbcBlockCipher(serpent);
            var padding = new Pkcs7Padding();

            var cbcCipher = new PaddedBufferedBlockCipher(cipherMode, padding);

            var keyParam = new KeyParameter(key);
            cbcCipher.Init(false, new ParametersWithIV(keyParam, iv));

            var plainText = new byte[cbcCipher.GetOutputSize(cipherResult.Length)];
            var processLength = cbcCipher.ProcessBytes(cipherResult, 0, cipherResult.Length, plainText, 0);
            var finalLength = cbcCipher.DoFinal(plainText, processLength);
            var finalPlainText = new byte[finalLength + processLength];
            Buffer.BlockCopy(plainText, 0, finalPlainText, 0, finalPlainText.Length);

            return finalPlainText;
        }
    }

    /// <summary>
    ///     A class that contains encryption and decryption methods.
    /// </summary>
    private static class EncryptionDecryption
    {
        /// <summary>
        ///     Asynchronously encrypts a byte array using a multi-layer encryption approach.
        /// </summary>
        /// <param name="plaintext">The byte array to be encrypted.</param>
        /// <param name="key">The key used for the first layer of encryption (XChaCha20-Poly1305).</param>
        /// <param name="key2">The key used for the second layer of encryption (ThreeFish).</param>
        /// <param name="key3">The key used for the third layer of encryption.</param>
        /// <param name="key4">The key used for the fourth layer of encryption.</param>
        /// <param name="key5">The key used for shuffling the final encrypted result.</param>
        /// <param name="hMacKey">The key used for HMAC in the second layer of encryption.</param>
        /// <param name="hMacKey2">The key used for HMAC in the third layer of encryption.</param>
        /// <param name="hMacKey3">The key used for HMAC in the fourth layer of encryption.</param>
        /// <returns>A shuffled byte array containing nonces and the final encrypted result.</returns>
        /// <exception cref="ArgumentException">Thrown when any of the input parameters is an empty array.</exception>
        /// <exception cref="Exception">Thrown when any intermediate or final encrypted value is empty.</exception>
        public static byte[] EncryptV3(byte[] plaintext,
            byte[] key, byte[] key2, byte[] key3, byte[] key4, byte[] key5, byte[] hMacKey,
            byte[] hMacKey2,
            byte[] hMacKey3)
        {
            if (plaintext == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(plaintext));
            if (key == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key));
            if (key2 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key2));
            if (key3 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key3));
            if (key4 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key4));
            if (key5 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key5));
            if (hMacKey == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(hMacKey));
            if (hMacKey2 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(hMacKey2));
            if (hMacKey3 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(hMacKey3));

            var nonce = CryptoUtilities.RndByteSized(CryptoConstants.ChaChaNonceSize);
            var nonce2 = CryptoUtilities.RndByteSized(CryptoConstants.ThreeFish);
            var nonce3 = CryptoUtilities.RndByteSized(CryptoConstants.Iv);
            var nonce4 = CryptoUtilities.RndByteSized(CryptoConstants.Iv);

            var cipherText = Algorithms.EncryptXChaCha20Poly1305(plaintext, nonce, key) ??
                             throw new Exception("Value was empty.");
            var cipherTextL2 = Algorithms.EncryptThreeFish(cipherText, key2, nonce2, hMacKey) ??
                               throw new Exception("Value was empty.");
            var cipherTextL3 = Algorithms.EncryptSerpent(cipherTextL2, key3, nonce3, hMacKey2) ??
                               throw new Exception("Value was empty.");
            var cipherTextL4 = Algorithms.EncryptAes(cipherTextL3, key4, nonce4, hMacKey3) ??
                               throw new Exception("Value was empty.");

            var result = nonce.Concat(nonce2).Concat(nonce3).Concat(nonce4).Concat(cipherTextL4).ToArray();
            var shuffledResult = Algorithms.Shuffle(result, key5);

            return shuffledResult;
        }

        /// <summary>
        ///     Asynchronously decrypts a byte array that has been encrypted using a multi-layer encryption approach.
        /// </summary>
        /// <param name="cipherText">The byte array to be decrypted.</param>
        /// <param name="key">The key used for the first layer of decryption (XChaCha20-Poly1305).</param>
        /// <param name="key2">The key used for the second layer of decryption (ThreeFish).</param>
        /// <param name="key3">The key used for the third layer of decryption.</param>
        /// <param name="key4">The key used for the fourth layer of encryption.</param>
        /// <param name="key5">The key used for unshuffling the ciphertext.</param>
        /// <param name="hMacKey">The key used for HMAC in the second layer of decryption.</param>
        /// <param name="hMacKey2">The key used for HMAC in the third layer of decryption.</param>
        /// <param name="hMacKey3">The key used for HMAC in the fourth layer of encryption.</param>
        /// <returns>The decrypted byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when any of the input parameters is an empty array.</exception>
        /// <exception cref="Exception">Thrown when any intermediate or final decrypted value is empty.</exception>
        public static byte[] DecryptV3(byte[] cipherText,
            byte[] key, byte[] key2, byte[] key3, byte[] key4, byte[] key5, byte[] hMacKey,
            byte[] hMacKey2,
            byte[] hMacKey3)
        {
            if (cipherText == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(cipherText));
            if (key == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key));
            if (key2 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key2));
            if (key3 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key3));
            if (key4 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key4));
            if (key5 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(key5));
            if (hMacKey == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(hMacKey));
            if (hMacKey2 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(hMacKey2));
            if (hMacKey3 == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(hMacKey3));

            var unshuffledResult = Algorithms.DeShuffle(cipherText, key5);

            var nonce = new byte[CryptoConstants.ChaChaNonceSize];
            Buffer.BlockCopy(unshuffledResult, 0, nonce, 0, nonce.Length);
            var nonce2 = new byte[CryptoConstants.ThreeFish];
            Buffer.BlockCopy(unshuffledResult, nonce.Length, nonce2, 0, nonce2.Length);
            var nonce3 = new byte[CryptoConstants.Iv];
            Buffer.BlockCopy(unshuffledResult, nonce.Length + nonce2.Length, nonce3, 0, nonce3.Length);
            var nonce4 = new byte[CryptoConstants.Iv];
            Buffer.BlockCopy(unshuffledResult, nonce.Length + nonce2.Length + nonce3.Length, nonce4, 0, nonce4.Length);

            var cipherResult =
                new byte[unshuffledResult.Length - nonce4.Length - nonce3.Length - nonce2.Length - nonce.Length];

            Buffer.BlockCopy(unshuffledResult, nonce.Length + nonce2.Length + nonce3.Length + nonce4.Length,
                cipherResult,
                0,
                cipherResult.Length);

            var resultL4 = Algorithms.DecryptAes(cipherResult, key4, hMacKey3) ??
                           throw new Exception("Value was empty.");
            var resultL3 = Algorithms.DecryptSerpent(resultL4, key3, hMacKey2) ??
                           throw new Exception("Value was empty.");
            var resultL2 = Algorithms.DecryptThreeFish(resultL3, key2, hMacKey) ??
                           throw new Exception("Value was empty.");
            var result = Algorithms.DecryptXChaCha20Poly1305(resultL2, nonce, key) ??
                         throw new Exception("Value was empty.");

            return result;
        }
    }
}