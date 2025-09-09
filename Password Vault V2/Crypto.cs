using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using Konscious.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Sodium;

namespace Password_Vault_V2;

internal abstract class Crypto
{
    /// <summary>
    ///     Compresses and encrypts the provided input data using the specified key and HKDF-derived salt.
    /// </summary>
    /// <param name="input">The plaintext data to encrypt.</param>
    /// <param name="keyBytes">The encryption key bytes derived from the master key using HKDF.</param>
    /// <param name="hkdfSalt">The salt used during HKDF derivation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the encrypted byte array.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="input" />, <paramref name="keyBytes" />, or <paramref name="hkdfSalt" /> is null or
    ///     empty.
    /// </exception>
    /// <exception cref="Exception">
    ///     Thrown if encryption fails due to an internal error. See inner exception for details.
    /// </exception>
    internal static async Task<byte[]> EncryptFile(byte[] input, byte[] keyBytes, byte[] hkdfSalt)
    {
        if (input == null || input.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(input));
        if (keyBytes == null || keyBytes.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(keyBytes));
        if (hkdfSalt == null || hkdfSalt.Length == 0)
            throw new ArgumentException("Salt was empty.", nameof(hkdfSalt));

        using var bufferSet = BufferInit.InitBuffers(keyBytes, hkdfSalt);

        try
        {
            var compressedFile = await CompressText(input).ConfigureAwait(false);
            var encryptedFile = EncryptionDecryption.EncryptV3(
                compressedFile,
                bufferSet.Key, bufferSet.Key2, bufferSet.Key3, bufferSet.Key4, bufferSet.Key5,
                bufferSet.HMacKey, bufferSet.HMacKey2, bufferSet.HMacKey3
            );

            return encryptedFile;
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            throw;
        }
        finally
        {
            CryptoUtilities.ClearMemoryNative(bufferSet.BuffersToClear.Concat(new[] { input }).ToArray());
        }
    }

    /// <summary>
    ///     Decrypts and decompresses the provided encrypted input data using the specified key and HKDF-derived salt.
    /// </summary>
    /// <param name="input">The encrypted data to decrypt.</param>
    /// <param name="keyBytes">The decryption key bytes derived from the master key using HKDF.</param>
    /// <param name="hkdfSalt">The salt used during HKDF derivation.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the decrypted and decompressed
    ///     byte array.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="input" />, <paramref name="keyBytes" />, or <paramref name="hkdfSalt" /> is null or
    ///     empty.
    /// </exception>
    /// <exception cref="Exception">
    ///     Thrown if decryption or decompression fails due to an internal error. See inner exception for details.
    /// </exception>
    public static async Task<byte[]> DecryptFile(byte[] input, byte[] keyBytes, byte[] hkdfSalt)
    {
        if (input == null || input.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(input));
        if (keyBytes == null || keyBytes.Length == 0)
            throw new ArgumentException("Value was empty.", nameof(keyBytes));
        if (hkdfSalt == null || hkdfSalt.Length == 0)
            throw new ArgumentException("Salt was empty.", nameof(hkdfSalt));

        using var bufferSet = BufferInit.InitBuffers(keyBytes, hkdfSalt);

        try
        {
            var decryptedFile = EncryptionDecryption.DecryptV3(
                input,
                bufferSet.Key, bufferSet.Key2, bufferSet.Key3, bufferSet.Key4, bufferSet.Key5,
                bufferSet.HMacKey, bufferSet.HMacKey2, bufferSet.HMacKey3
            );

            var decompressedFile = await DecompressText(decryptedFile).ConfigureAwait(false);
            return decompressedFile;
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            throw;
        }
        finally
        {
            CryptoUtilities.ClearMemoryNative(bufferSet.BuffersToClear.Concat(new[] { input }).ToArray());
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
    private static async Task<byte[]> CompressText(byte[] inputBytes)
    {
        if (inputBytes == null)
            throw new ArgumentNullException(nameof(inputBytes), "Input byte array cannot be null.");

        using var outputStream = new MemoryStream();
        await using (var zipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, true))
        {
            await zipStream.WriteAsync(inputBytes).ConfigureAwait(false);
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
    private static async Task<byte[]> DecompressText(byte[] inputBytes)
    {
        if (inputBytes == null)
            throw new ArgumentNullException(nameof(inputBytes), "Input byte array cannot be null.");

        using var inputStream = new MemoryStream(inputBytes);
        await using var zipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        await zipStream.CopyToAsync(outputStream).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    private static async Task CompressText(Stream inputStream, Stream outputStream)
    {
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream), "Input stream cannot be null.");
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream), "Output stream cannot be null.");

        await using var gzipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, true);
        await inputStream.CopyToAsync(gzipStream).ConfigureAwait(false);
        await gzipStream.FlushAsync().ConfigureAwait(false);
    }

    private static async Task DecompressText(Stream compressedStream, Stream outputStream)
    {
        if (compressedStream == null)
            throw new ArgumentNullException(nameof(compressedStream), "Compressed stream cannot be null.");
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream), "Output stream cannot be null.");

        await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, true);
        await gzipStream.CopyToAsync(outputStream).ConfigureAwait(false);
        await gzipStream.FlushAsync().ConfigureAwait(false);
    }


    /// <summary>
    ///     Derives a cryptographic key from the specified parameters using HKDF and pins the resulting buffer in memory.
    /// </summary>
    /// <param name="key">The input key material.</param>
    /// <param name="salt">The salt used in HKDF.</param>
    /// <param name="label">A context-specific label (info) for key derivation.</param>
    /// <param name="length">The desired length of the derived key.</param>
    /// <returns>A derived key byte array.</returns>
    public static byte[] DeriveAndPin(byte[] key, byte[] salt, byte[] label, int length)
    {
        return HKDF.HkdfDerivePinned(key, salt, label, length);
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer, totalRead, count - totalRead).ConfigureAwait(false);
            if (read == 0) break; // End of stream
            totalRead += read;
        }

        return totalRead;
    }

    private static async Task<byte[]> ReadExactAsync(Stream stream, int count)
    {
        var buffer = new byte[count];
        var offset = 0;

        while (offset < count)
        {
            var bytesRead = await stream.ReadAsync(buffer, offset, count - offset);
            if (bytesRead == 0) throw new EndOfStreamException($"Expected {count} bytes, got {offset}.");
            offset += bytesRead;
        }

        return buffer;
    }

    public static IProgress<long> CreateSegmentedProgress(
        long layerLength,
        double segmentStartPercent,
        double segmentEndPercent,
        IProgress<double> overallProgress)
    {
        return new Progress<long>(bytesSoFar =>
        {
            var fraction = bytesSoFar / (double)layerLength;
            var percent = segmentStartPercent + (segmentEndPercent - segmentStartPercent) * fraction;
            percent = Math.Clamp(percent, segmentStartPercent, segmentEndPercent);
            overallProgress.Report(percent);
        });
    }

    /// <summary>
    ///     Utility class for cryptographic constants and initialization.
    /// </summary>
    internal static class CryptoConstants
    {
        // Sizes in bytes
        public const int SaltSize = 128; // Salt size for key derivation or encryption
        public const int HmacLength = 64; // Length of HMAC output
        public const int ChaChaNonceSize = 24; // Nonce size for ChaCha algorithms
        public const int KeySize = 32; // Symmetric key size (e.g., 256-bit key)
        public const int Iv = 16; // Initialization Vector size (usually 128 bits)
        public const int ThreeFish = 128; // Block size or key size for Threefish cipher
        public const int ShuffleKey = 128; // Size for shuffle key (custom)
        public const int BlockBitSize = 128; // Block size in bits
        public const int KeyBitSize = 256; // Key size in bits
        public const int UuidSize = 16; // UUID size in bytes
        public const int PasswordHashSize = 64; // Password hash output size

        // Configurable parameters (read from settings)
        public static readonly int Iterations = Settings.Default.Iterations;

        public static readonly double
            MemorySize = Settings.Default.MemorySize * Math.Pow(1024, 2); // Convert MB to bytes

        public static readonly int Parallelism = Settings.Default.Parallelism;

        // Random number generator - thread-safe and reusable
        public static readonly RandomNumberGenerator RndNum = RandomNumberGenerator.Create();

        // File signature as a byte array (ASCII encoded)
        public static readonly byte[] FileSignature = "v1.0"u8.ToArray();
    }

    internal static class MasterKey
    {
        private static GCHandle? _pinnedHandle;
        private static byte[]? _keyBytes;
        private static bool _isDisposed;

        /// <summary>
        ///     Pins and stores a copy of the master key securely in memory.
        /// </summary>
        /// <param name="masterKey">The master key bytes to secure.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">If a key is already set.</exception>
        public static void SecureKey(byte[] masterKey)
        {
            if (masterKey == null || masterKey.Length == 0)
                throw new ArgumentException("Master key was null or empty.", nameof(masterKey));
            if (_keyBytes != null)
                throw new InvalidOperationException("Master key is already set.");
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MasterKey), "Cannot reuse disposed MasterKey.");

            _keyBytes = new byte[masterKey.Length];
            Buffer.BlockCopy(masterKey, 0, _keyBytes, 0, masterKey.Length);

            // Immediately zero original array
            CryptoUtilities.ClearMemoryNative(masterKey);

            _pinnedHandle = GCHandle.Alloc(_keyBytes, GCHandleType.Pinned);

            // Lock in memory to prevent paging
            if (!NativeMethods.VirtualLock(_pinnedHandle.Value.AddrOfPinnedObject(),
                    (UIntPtr)_keyBytes.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to lock memory with VirtualLock");
        }


        /// <summary>
        ///     Gets the pinned master key bytes.
        /// </summary>
        /// <returns>The pinned master key byte array.</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public static byte[] GetKey()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MasterKey), "Master key has been disposed.");

            if (_keyBytes == null)
                throw new InvalidOperationException("Master key has not been set.");

            return _keyBytes;
        }

        /// <summary>
        ///     Clears the master key bytes from memory and unpins the buffer.
        /// </summary>
        public static void Dispose()
        {
            if (!_isDisposed)
            {
                if (_keyBytes != null)
                {
                    CryptoUtilities.ClearMemoryNative(_keyBytes);

                    if (_pinnedHandle is { IsAllocated: true })
                    {
                        _pinnedHandle.Value.Free();
                        NativeMethods.VirtualUnlock(_pinnedHandle.Value.AddrOfPinnedObject(),
                            (UIntPtr)_keyBytes.Length);
                    }

                    _keyBytes = null;
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        ///     Resets the static MasterKey so it can be re-used after logout.
        /// </summary>
        public static void Reset()
        {
            if (_keyBytes != null)
            {
                CryptoUtilities.ClearMemoryNative(_keyBytes);

                if (_pinnedHandle is { IsAllocated: true })
                {
                    _pinnedHandle.Value.Free();
                    NativeMethods.VirtualUnlock(_pinnedHandle.Value.AddrOfPinnedObject(),
                        (UIntPtr)_keyBytes.Length);
                }

                _keyBytes = null;
            }

            _isDisposed = false;
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool VirtualLock(IntPtr lpAddress, UIntPtr dwSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool VirtualUnlock(IntPtr lpAddress, UIntPtr dwSize);
        }
    }

    internal static class HashingMethods
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

            var result = await argon2.GetBytesAsync(outputSize).ConfigureAwait(false);

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

    /// <summary>
    ///     Provides a secure, unmanaged memory buffer for storing sensitive byte and character data,
    ///     such as passwords, to reduce the risk of data leakage from managed memory.
    /// </summary>
    internal sealed class SecurePasswordBuffer : IDisposable
    {
        private IntPtr _buffer;
        private int _capacity;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SecurePasswordBuffer" /> class with a specified initial capacity.
        /// </summary>
        /// <param name="initialCapacity">The initial buffer size in bytes. Defaults to 120 if smaller.</param>
        public SecurePasswordBuffer(int initialCapacity = 120)
        {
            _capacity = Math.Max(initialCapacity, 120);
            _buffer = Marshal.AllocHGlobal(_capacity);
            ZeroMemory(_buffer, _capacity);
        }

        /// <summary>
        ///     Gets the number of bytes currently stored in the buffer.
        /// </summary>
        internal int Length { get; private set; }

        /// <summary>
        ///     Releases unmanaged resources and securely clears the buffer contents.
        /// </summary>
        public void Dispose()
        {
            if (_buffer != IntPtr.Zero)
            {
                ZeroMemory(_buffer, _capacity);
                Marshal.FreeHGlobal(_buffer);
                _buffer = IntPtr.Zero;
            }

            Length = 0;
            _capacity = 0;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Appends a byte array to the buffer.
        /// </summary>
        /// <param name="bytes">The byte array to append. Null or empty arrays are ignored.</param>
        public void Append(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return;

            EnsureCapacity(Length + bytes.Length);
            Marshal.Copy(bytes, 0, _buffer + Length, bytes.Length);
            Length += bytes.Length;
        }

        /// <summary>
        ///     Converts the buffer's contents to a managed <see cref="byte" /> array.
        ///     Remember to securely clear the returned array after use.
        /// </summary>
        /// <returns>A new byte array containing the buffer's contents.</returns>
        internal byte[] ToByteArray()
        {
            var output = new byte[Length];
            Marshal.Copy(_buffer, output, 0, Length);
            return output;
        }

        /// <summary>
        ///     Converts the buffer's contents to a managed <see cref="char" /> array using UTF-8 decoding.
        ///     Remember to securely clear the returned array after use.
        /// </summary>
        /// <returns>A new character array representing the buffer's contents.</returns>
        public char[] ToCharArray()
        {
            var bytes = ToByteArray();
            var chars = Encoding.UTF8.GetChars(bytes);
            ZeroArray(bytes);
            return chars;
        }

        /// <summary>
        ///     Clears the contents of the buffer by zeroing out memory and resetting the length.
        /// </summary>
        public void Clear()
        {
            ZeroMemory(_buffer, _capacity);
            Length = 0;
        }

        /// <summary>
        ///     Finalizer that ensures unmanaged memory is cleared and released if <see cref="Dispose" /> was not called.
        /// </summary>
        ~SecurePasswordBuffer()
        {
            Dispose();
        }

        /// <summary>
        ///     Adds a single byte to the buffer.
        /// </summary>
        /// <param name="b">The byte to add.</param>
        public void Add(byte b)
        {
            EnsureCapacity(Length + 1);
            Marshal.WriteByte(_buffer + Length, b);
            Length++;
        }

        /// <summary>
        ///     Removes the byte at the specified index and shifts subsequent bytes to the left.
        /// </summary>
        /// <param name="index">The zero-based index of the byte to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is out of bounds.</exception>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe
            {
                var ptr = (byte*)_buffer;
                ptr[index] = 0;

                for (var i = index + 1; i < Length; i++)
                    ptr[i - 1] = ptr[i];

                ptr[Length - 1] = 0;
            }

            Length--;
        }

        /// <summary>
        ///     Zeros out the unmanaged memory in the given range.
        /// </summary>
        /// <param name="ptr">The pointer to unmanaged memory.</param>
        /// <param name="length">The number of bytes to zero out.</param>
        private static unsafe void ZeroMemory(IntPtr ptr, int length)
        {
            if (ptr == IntPtr.Zero || length <= 0)
                return;

            var p = (byte*)ptr.ToPointer();
            for (var i = 0; i < length; i++) p[i] = 0;
        }

        /// <summary>
        ///     Zeros out the contents of a managed byte array.
        /// </summary>
        /// <param name="array">The byte array to clear.</param>
        private static void ZeroArray(byte[] array)
        {
            if (array == null)
                return;

            CryptoUtilities.ClearMemoryNative(array);
        }

        /// <summary>
        ///     Zeros out the contents of a managed character array.
        /// </summary>
        /// <param name="arr">The character array to clear.</param>
        public static void ZeroArray(char[] arr)
        {
            if (arr == null)
                return;

            var byteSpan = MemoryMarshal.AsBytes<char>(arr);
            CryptoUtilities.ClearMemoryNative(byteSpan);
        }

        /// <summary>
        ///     Ensures the buffer has enough capacity to hold the specified number of bytes.
        ///     If not, a larger buffer is allocated and existing contents are copied.
        /// </summary>
        /// <param name="required">The required total capacity.</param>
        private void EnsureCapacity(int required)
        {
            unsafe
            {
                if (required <= _capacity) return;

                var newCapacity = Math.Max(_capacity * 2, required);
                var newBuffer = Marshal.AllocHGlobal(newCapacity);
                ZeroMemory(newBuffer, newCapacity);

                if (Length > 0)
                    Buffer.MemoryCopy((void*)_buffer, (void*)newBuffer, newCapacity, Length);

                ZeroMemory(_buffer, _capacity);
                Marshal.FreeHGlobal(_buffer);

                _buffer = newBuffer;
                _capacity = newCapacity;
            }
        }
    }

    internal static class CryptoUtilities
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
        ///     Pins multiple byte arrays in memory to prevent them from being moved by the garbage collector.
        ///     Useful for scenarios that require fixed memory locations, such as unmanaged operations.
        /// </summary>
        /// <param name="arrays">The byte arrays to pin.</param>
        /// <returns>
        ///     An array of <see cref="GCHandle" /> instances representing the pinned handles.
        ///     Each handle must be freed using <see cref="FreeArrays" /> to avoid memory leaks.
        /// </returns>
        public static GCHandle[] PinArrays(params byte[][] arrays)
        {
            var handles = new GCHandle[arrays.Length];

            for (var i = 0; i < arrays.Length; i++)
                handles[i] = GCHandle.Alloc(arrays[i], GCHandleType.Pinned);

            return handles;
        }

        /// <summary>
        ///     Frees an array of pinned <see cref="GCHandle" /> instances, releasing the pinned memory.
        /// </summary>
        /// <param name="handles">The handles to free. Any unallocated handle will be ignored.</param>
        public static void FreeArrays(params GCHandle[] handles)
        {
            foreach (var handle in handles)
                if (handle.IsAllocated)
                    handle.Free();
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

        public static async Task<byte[]> ReadTrailingTagAsync(Stream stream, int tagLength)
        {
            if (!stream.CanSeek)
                throw new InvalidOperationException("Stream must support seeking to read trailing tag.");

            if (tagLength <= 0 || stream.Length < tagLength)
                throw new ArgumentOutOfRangeException(nameof(tagLength), "Invalid tag length or stream too short.");

            var tag = new byte[tagLength];

            stream.Seek(-tagLength, SeekOrigin.End);
            var bytesRead = await stream.ReadAsync(tag, 0, tagLength).ConfigureAwait(false);

            if (bytesRead != tagLength)
                throw new IOException($"Expected to read {tagLength} bytes for tag, got {bytesRead}.");

            stream.Seek(0, SeekOrigin.Begin); // Reset stream position if caller wants to re-read content
            return tag;
        }

        public static async Task<byte[]> ReadExactAsync(Stream stream, int count)
        {
            var buffer = new byte[count];
            var offset = 0;

            while (offset < count)
            {
                var read = await stream.ReadAsync(buffer, offset, count - offset).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException($"Expected {count} bytes but reached end of stream at {offset}.");
                offset += read;
            }

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
        ///     Securely clears multiple byte arrays by overwriting their contents with zeros.
        ///     Each array is pinned in memory before clearing to prevent optimization or movement by the garbage collector.
        /// </summary>
        /// <param name="arrays">An array of byte arrays to securely clear.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="arrays" /> is null or empty.</exception>
        public static void ClearMemoryNative(params byte[][] arrays)
        {
            if (arrays == null || arrays.Length == 0)
                throw new ArgumentNullException(nameof(arrays), "Input array was null or empty.");

            foreach (var byteArray in arrays)
            {
                if (byteArray == null)
                    continue;

                GCHandle handle = default;
                try
                {
                    handle = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
                    var ptr = handle.AddrOfPinnedObject();
                    CryptographicOperations.ZeroMemory(byteArray);
                }
                finally
                {
                    if (handle.IsAllocated)
                        handle.Free();
                }
            }
        }

        public static void ClearCharArray(params char[][] arrays)
        {
            if (arrays == null || arrays.Length == 0)
                throw new ArgumentNullException(nameof(arrays), "Input array was null or empty.");

            foreach (var charArray in arrays)
            {
                if (charArray == null)
                    continue;

                var handle = GCHandle.Alloc(charArray, GCHandleType.Pinned);
                try
                {
                    var byteSpan = MemoryMarshal.AsBytes<char>(charArray);

                    CryptographicOperations.ZeroMemory(byteSpan);
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        /// <summary>
        ///     Securely clears a single byte array by overwriting its contents with zeros.
        /// </summary>
        /// <param name="array">The byte array to securely clear.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="array" /> is null or empty.</exception>
        public static void ClearMemoryNative(byte[] array)
        {
            if (array == null || array.Length == 0)
                throw new ArgumentNullException(nameof(array), "Input array was null or empty.");

            CryptographicOperations.ZeroMemory(array);
        }

        /// <summary>
        ///     Securely clears a span of bytes by overwriting its contents with zeros.
        /// </summary>
        /// <param name="array">The byte span to securely clear.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="array" /> is empty or has length zero.</exception>
        public static void ClearMemoryNative(Span<byte> array)
        {
            if (array.IsEmpty || array.Length == 0)
                throw new ArgumentNullException(nameof(array), "Input array was null or empty.");

            // Securely clear the span
            CryptographicOperations.ZeroMemory(array);
        }
    }

    /// <summary>
    ///     Utility class for buffer initialization that contains the necessary keys for cryptographic functions.
    /// </summary>
    private static class BufferInit
    {
        /// <summary>
        ///     Initializes a set of cryptographic buffers by deriving multiple encryption and HMAC keys using HKDF.
        ///     All buffers are pinned in memory for security.
        /// </summary>
        /// <param name="keyBytes">The base key material to derive from.</param>
        /// <param name="hkdfSalt">The HKDF salt to ensure key uniqueness and security.</param>
        /// <returns>A <see cref="DisposableBufferSet" /> containing all derived and pinned keys.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="keyBytes" /> or <paramref name="hkdfSalt" /> are null or
        ///     empty.
        /// </exception>
        public static DisposableBufferSet InitBuffers(byte[] keyBytes, byte[] hkdfSalt)
        {
            if (keyBytes == null || keyBytes.Length == 0)
                throw new ArgumentException("Key is null or empty.");

            if (hkdfSalt == null || hkdfSalt.Length == 0)
                throw new ArgumentException("HKDF salt is null or empty.");

            var set = new DisposableBufferSet();

            try
            {
                set.Add("Key",
                    DeriveAndPin(keyBytes, hkdfSalt, "encryption key 1"u8.ToArray(), CryptoConstants.KeySize));
                set.Add("Key2",
                    DeriveAndPin(keyBytes, hkdfSalt, "encryption key 2"u8.ToArray(), CryptoConstants.ThreeFish));
                set.Add("Key3",
                    DeriveAndPin(keyBytes, hkdfSalt, "encryption key 3"u8.ToArray(), CryptoConstants.KeySize));
                set.Add("Key4",
                    DeriveAndPin(keyBytes, hkdfSalt, "encryption key 4"u8.ToArray(), CryptoConstants.KeySize));
                set.Add("Key5",
                    DeriveAndPin(keyBytes, hkdfSalt, "encryption key 5"u8.ToArray(), CryptoConstants.ShuffleKey));
                set.Add("HMacKey",
                    DeriveAndPin(keyBytes, hkdfSalt, "hmac key 1"u8.ToArray(), CryptoConstants.HmacLength));
                set.Add("HMacKey2",
                    DeriveAndPin(keyBytes, hkdfSalt, "hmac key 2"u8.ToArray(), CryptoConstants.HmacLength));
                set.Add("HMacKey3",
                    DeriveAndPin(keyBytes, hkdfSalt, "hmac key 3"u8.ToArray(), CryptoConstants.HmacLength));
            }
            catch
            {
                set.Dispose();
                throw;
            }

            return set;
        }
    }


    internal static class HKDF
    {
        /// <summary>
        ///     Derives a key using HKDF with SHA3-512 and returns a securely initialized byte array.
        /// </summary>
        /// <param name="inputKey">The base input key material.</param>
        /// <param name="salt">The salt used for the HKDF derivation.</param>
        /// <param name="info">Contextual information for HKDF ("info" parameter).</param>
        /// <param name="length">The desired length of the output key.</param>
        /// <returns>A derived byte array of the specified length.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if <paramref name="inputKey" /> or <paramref name="salt" /> are null or
        ///     empty.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="info" /> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="length" /> is less than or equal to zero.</exception>
        public static byte[] HkdfDerivePinned(byte[] inputKey, byte[] salt, byte[] info, int length)
        {
            if (inputKey == null || inputKey.Length == 0)
                throw new ArgumentException("Input key was null or empty.", nameof(inputKey));
            if (salt == null || salt.Length == 0)
                throw new ArgumentException("Salt was null or empty.", nameof(salt));
            ArgumentNullException.ThrowIfNull(info);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

            var hkdf = new HkdfBytesGenerator(new Sha3Digest(512));
            hkdf.Init(new HkdfParameters(inputKey, salt, info));

            var pinned = new byte[length];
            hkdf.GenerateBytes(pinned, 0, length);

            return pinned;
        }
    }

    /// <summary>
    ///     Represents a set of cryptographic buffers that are pinned in memory for secure use and disposal.
    /// </summary>
    private sealed class DisposableBufferSet : IDisposable
    {
        private readonly Dictionary<string, (byte[] buffer, GCHandle handle)> _buffers = new();
        private bool _disposed;


        private byte[] this[string name] => _buffers.TryGetValue(name, out var tuple)
            ? tuple.buffer
            : throw new KeyNotFoundException(name);

        /// <summary>Gets the first encryption key (Key1).</summary>
        public byte[] Key => this["Key"];

        /// <summary>Gets the second encryption key (Key2).</summary>
        public byte[] Key2 => this["Key2"];

        /// <summary>Gets the third encryption key (Key3).</summary>
        public byte[] Key3 => this["Key3"];

        /// <summary>Gets the fourth encryption key (Key4).</summary>
        public byte[] Key4 => this["Key4"];

        /// <summary>Gets the fifth encryption key (Key5).</summary>
        public byte[] Key5 => this["Key5"];

        /// <summary>Gets the first HMAC key.</summary>
        public byte[] HMacKey => this["HMacKey"];

        /// <summary>Gets the second HMAC key.</summary>
        public byte[] HMacKey2 => this["HMacKey2"];

        /// <summary>Gets the third HMAC key.</summary>
        public byte[] HMacKey3 => this["HMacKey3"];

        /// <summary>
        ///     Gets all buffers stored in this set as an array of byte arrays.
        /// </summary>
        public byte[][] BuffersToClear => _buffers.Values.Select(v => v.buffer).ToArray();

        /// <summary>
        ///     Disposes the buffer set by securely clearing and freeing all pinned memory.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            foreach (var (_, (buffer, handle)) in _buffers)
            {
                CryptographicOperations.ZeroMemory(buffer);
                if (handle.IsAllocated)
                    handle.Free();
            }

            _buffers.Clear();
            _disposed = true;
        }

        /// <summary>
        ///     Adds a derived buffer to the buffer set and pins it in memory.
        /// </summary>
        /// <param name="name">The key name used to identify the buffer.</param>
        /// <param name="buffer">The derived byte array buffer.</param>
        public void Add(string name, byte[] buffer)
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _buffers[name] = (buffer, handle);
        }
    }

    /// <summary>
    ///     A class that contains different algorithms for encrypting and decrypting.
    /// </summary>
    private class Algorithms
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
        private static int[] GenerateShuffleIndices(int size, byte[] key)
        {
            var indices = new int[size - 1];
            var bytesNeeded = (size - 1) * 4;
            var randomBytes = new byte[bytesNeeded];

            var counter = 0;
            var filled = 0;
            while (filled < bytesNeeded)
            {
                var ctr = BitConverter.GetBytes(counter++);
                var hash = HashingMethods.HmacSha3(ctr, key);
                var copy = Math.Min(hash.Length, bytesNeeded - filled);
                Buffer.BlockCopy(hash, 0, randomBytes, filled, copy);
                filled += copy;
            }

            for (var i = 0; i < indices.Length; i++)
            {
                var value = BitConverter.ToInt32(randomBytes, i * 4);
                indices[i] = Math.Abs(value % (i + 1));
            }

            return indices;
        }

        /// <summary>
        ///     Shuffles the input byte array using a deterministic sequence based on the provided key.
        ///     This method performs a keyed permutation to obfuscate the original data order.
        /// </summary>
        /// <param name="input">The byte array to shuffle.</param>
        /// <param name="key">The key used to generate the shuffle pattern.</param>
        /// <returns>A new byte array containing the shuffled data.</returns>
        /// <remarks>
        ///     The same key and input length must be used for the corresponding <see cref="Unshuffle(byte[], byte[])" /> call
        ///     to reverse the operation.
        /// </remarks>
        public static byte[] Shuffle(byte[] input, byte[] key)
        {
            var result = input.ToArray();
            var indices = GenerateShuffleIndices(input.Length, key);

            for (var i = result.Length - 1; i > 0; i--)
            {
                var j = indices[result.Length - 1 - i];
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }

        /// <summary>
        ///     Reverses the shuffle operation and restores the original order of a byte array shuffled with the same key.
        /// </summary>
        /// <param name="input">The byte array that was previously shuffled.</param>
        /// <param name="key">The same key used during the shuffle operation.</param>
        /// <returns>A new byte array containing the original unshuffled data.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if the key or input lengths are invalid or inconsistent with the original shuffle operation.
        /// </exception>
        public static byte[] Unshuffle(byte[] input, byte[] key)
        {
            var result = input.ToArray();
            var indices = GenerateShuffleIndices(input.Length, key);

            for (var i = 1; i < result.Length; i++)
            {
                var j = indices[result.Length - 1 - i];
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
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
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input data is null or empty.", nameof(input));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Encryption key is null or empty.", nameof(key));
            if (nonce == null || nonce.Length == 0)
                throw new ArgumentException("IV is null or empty.", nameof(nonce));

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
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input data is null or empty.", nameof(input));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Encryption key is null or empty.", nameof(key));
            if (nonce == null || nonce.Length == 0)
                throw new ArgumentException("IV is null or empty.", nameof(nonce));

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
            if (inputText == null || inputText.Length == 0)
                throw new ArgumentException("Input data is null or empty.", nameof(inputText));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Encryption key is null or empty.", nameof(key));
            if (iv == null || iv.Length == 0)
                throw new ArgumentException("IV is null or empty.", nameof(iv));
            if (hMacKey == null || hMacKey.Length == 0)
                throw new ArgumentException("HMAC key is null or empty.", nameof(hMacKey));

            using var aes = Aes.Create();
            aes.KeySize = CryptoConstants.KeyBitSize;
            aes.BlockSize = CryptoConstants.BlockBitSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] cipherText;
            using (var memStream = new MemoryStream())
            using (var encryptor = aes.CreateEncryptor(key, iv))
            using (var cryptoStream = new CryptoStream(memStream, encryptor, CryptoStreamMode.Write))
            {
                cryptoStream.Write(inputText, 0, inputText.Length);
                cryptoStream.FlushFinalBlock();
                cipherText = memStream.ToArray();
            }

            // Combine IV and ciphertext
            var combined = new byte[iv.Length + cipherText.Length];
            Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
            Buffer.BlockCopy(cipherText, 0, combined, iv.Length, cipherText.Length);

            // Compute HMAC
            var tag = HashingMethods.HmacSha3(combined, hMacKey);

            // Combine data + tag
            var result = new byte[combined.Length + tag.Length];
            Buffer.BlockCopy(combined, 0, result, 0, combined.Length);
            Buffer.BlockCopy(tag, 0, result, combined.Length, tag.Length);

            return result;
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
            if (inputText == null || inputText.Length == 0)
                throw new ArgumentException("Input data is null or empty.", nameof(inputText));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Decryption key is null or empty.", nameof(key));
            if (hMacKey == null || hMacKey.Length == 0)
                throw new ArgumentException("HMAC key is null or empty.", nameof(hMacKey));

            var tagLength = CryptoConstants.HmacLength;
            var ivLength = CryptoConstants.Iv;

            if (inputText.Length < ivLength + tagLength)
                throw new CryptographicException("Input data is too short.");

            // Separate tag
            var expectedTag = new byte[tagLength];
            Buffer.BlockCopy(inputText, inputText.Length - tagLength, expectedTag, 0, tagLength);

            // Extract ciphertext with IV
            var dataLength = inputText.Length - tagLength;
            var cipherWithIv = new byte[dataLength];
            Buffer.BlockCopy(inputText, 0, cipherWithIv, 0, dataLength);

            // Verify HMAC
            var actualTag = HashingMethods.HmacSha3(cipherWithIv, hMacKey);
            if (!CryptographicOperations.FixedTimeEquals(expectedTag, actualTag))
                throw new CryptographicException("Authentication tag does not match.");

            // Extract IV and ciphertext
            var iv = new byte[ivLength];
            var cipherText = new byte[dataLength - ivLength];
            Buffer.BlockCopy(cipherWithIv, 0, iv, 0, ivLength);
            Buffer.BlockCopy(cipherWithIv, ivLength, cipherText, 0, cipherText.Length);

            // Decrypt
            using var aes = Aes.Create();
            aes.KeySize = CryptoConstants.KeyBitSize;
            aes.BlockSize = CryptoConstants.BlockBitSize;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var memStream = new MemoryStream();
            using var decryptor = aes.CreateDecryptor(key, iv);
            using var cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Write);
            cryptoStream.Write(cipherText, 0, cipherText.Length);
            cryptoStream.FlushFinalBlock();

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
            if (inputText == null || inputText.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (iv == null || iv.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(iv));
            if (hMacKey == null || hMacKey.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(hMacKey));

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
            if (inputText == null || inputText.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (hMacKey == null || hMacKey.Length == 0)
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
            if (inputText == null || inputText.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (iv == null || iv.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(iv));
            if (hMacKey == null || hMacKey.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(hMacKey));

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
            if (inputText == null || inputText.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(inputText));
            if (key == null || key.Length == 0)
                throw new ArgumentException("Value was empty or null.", nameof(key));
            if (hMacKey == null || hMacKey.Length == 0)
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
        ///     Encrypts a plaintext byte array using a multi-layer encryption scheme consisting of:
        ///     XChaCha20-Poly1305, Threefish, Serpent, and AES, with an initial shuffle transformation.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt.</param>
        /// <param name="key">The encryption key for the XChaCha20-Poly1305 layer.</param>
        /// <param name="key2">The encryption key for the Threefish layer.</param>
        /// <param name="key3">The encryption key for the Serpent layer.</param>
        /// <param name="key4">The encryption key for the AES layer.</param>
        /// <param name="key5">The shuffle key used to permute the plaintext before encryption.</param>
        /// <param name="hMacKey">HMAC key for the Threefish layer.</param>
        /// <param name="hMacKey2">HMAC key for the Serpent layer.</param>
        /// <param name="hMacKey3">HMAC key for the AES layer.</param>
        /// <returns>
        ///     A byte array containing the concatenated nonces and the final encrypted ciphertext.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when any input array is empty.</exception>
        /// <exception cref="CryptoException">Thrown if an error occurs during any encryption stage.</exception>
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

            var shuffleText = Algorithms.Shuffle(plaintext, key5);

            var cipherText = Algorithms.EncryptXChaCha20Poly1305(shuffleText, nonce, key) ??
                             throw new CryptoException("An error occured during encryption.");
            var cipherTextL2 = Algorithms.EncryptThreeFish(cipherText, key2, nonce2, hMacKey) ??
                               throw new CryptoException("An error occured during encryption.");
            var cipherTextL3 = Algorithms.EncryptSerpent(cipherTextL2, key3, nonce3, hMacKey2) ??
                               throw new CryptoException("An error occured during encryption.");
            var cipherTextL4 = Algorithms.EncryptAes(cipherTextL3, key4, nonce4, hMacKey3) ??
                               throw new CryptoException("An error occured during encryption.");

            var result = nonce.Concat(nonce2).Concat(nonce3).Concat(nonce4).Concat(cipherTextL4).ToArray();

            return result;
        }

        /// <summary>
        ///     Decrypts a byte array that was encrypted with <see cref="EncryptV3" /> using the corresponding multi-layer
        ///     decryption process. This includes AES, Serpent, Threefish, and XChaCha20-Poly1305, followed by an unshuffle
        ///     operation.
        /// </summary>
        /// <param name="cipherText">The encrypted data including prepended nonces.</param>
        /// <param name="key">The encryption key for the XChaCha20-Poly1305 layer.</param>
        /// <param name="key2">The encryption key for the Threefish layer.</param>
        /// <param name="key3">The encryption key for the Serpent layer.</param>
        /// <param name="key4">The encryption key for the AES layer.</param>
        /// <param name="key5">The shuffle key originally used to permute the plaintext.</param>
        /// <param name="hMacKey">HMAC key for the Threefish layer.</param>
        /// <param name="hMacKey2">HMAC key for the Serpent layer.</param>
        /// <param name="hMacKey3">HMAC key for the AES layer.</param>
        /// <returns>The original plaintext byte array after full decryption and unshuffling.</returns>
        /// <exception cref="ArgumentException">Thrown when any input array is empty.</exception>
        /// <exception cref="CryptoException">Thrown if an error occurs during any decryption stage.</exception>
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

            var nonce = new byte[CryptoConstants.ChaChaNonceSize];
            Buffer.BlockCopy(cipherText, 0, nonce, 0, nonce.Length);
            var nonce2 = new byte[CryptoConstants.ThreeFish];
            Buffer.BlockCopy(cipherText, nonce.Length, nonce2, 0, nonce2.Length);
            var nonce3 = new byte[CryptoConstants.Iv];
            Buffer.BlockCopy(cipherText, nonce.Length + nonce2.Length, nonce3, 0, nonce3.Length);
            var nonce4 = new byte[CryptoConstants.Iv];
            Buffer.BlockCopy(cipherText, nonce.Length + nonce2.Length + nonce3.Length, nonce4, 0, nonce4.Length);

            var cipherResult =
                new byte[cipherText.Length - nonce4.Length - nonce3.Length - nonce2.Length - nonce.Length];

            Buffer.BlockCopy(cipherText, nonce.Length + nonce2.Length + nonce3.Length + nonce4.Length,
                cipherResult,
                0,
                cipherResult.Length);

            var resultL4 = Algorithms.DecryptAes(cipherResult, key4, hMacKey3) ??
                           throw new CryptoException("An error occured during decryption.");
            var resultL3 = Algorithms.DecryptSerpent(resultL4, key3, hMacKey2) ??
                           throw new CryptoException("An error occured during decryption.");
            var resultL2 = Algorithms.DecryptThreeFish(resultL3, key2, hMacKey) ??
                           throw new CryptoException("An error occured during decryption.");
            var result = Algorithms.DecryptXChaCha20Poly1305(resultL2, nonce, key) ??
                         throw new CryptoException("An error occured during decryption.");

            return Algorithms.Unshuffle(result, key5);
        }
    }

    public static class FileLogger
    {
        private static readonly string LogFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "crypto-log.txt");

        private static readonly object _lock = new();

        public static void Log(string message)
        {
            try
            {
                var logLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
                lock (_lock)
                {
                    File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
                }
            }
            catch
            {
                // Fails silently. You can optionally raise an event or add fallback logging.
            }
        }

        public static void LogBytes(string label, byte[] data, int length = 64)
        {
            if (data == null)
            {
                Log($"{label}: <null>");
                return;
            }

            var displayLength = Math.Min(length, data.Length);
            var hex = BitConverter.ToString(data, 0, displayLength).Replace("-", "");
            if (displayLength < data.Length)
                hex += "...";

            Log($"{label} ({data.Length} bytes): {hex}");
        }
    }


    private sealed class HmacSha3Stream : IDisposable
    {
        private readonly HMac hmac;
        private bool _disposed; // To detect redundant calls

        public HmacSha3Stream(byte[] key, int bits = 512)
        {
            IDigest digest = bits switch
            {
                224 => new Sha3Digest(224),
                256 => new Sha3Digest(256),
                384 => new Sha3Digest(384),
                _ => new Sha3Digest(512)
            };

            hmac = new HMac(digest);
            hmac.Init(new KeyParameter(key));
        }

        // Public dispose method
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Update(byte[] buffer, int offset, int count)
        {
            hmac.BlockUpdate(buffer, offset, count);
        }

        public byte[] Final()
        {
            var result = new byte[hmac.GetMacSize()];
            hmac.DoFinal(result, 0);
            hmac.Reset();
            return result;
        }

        // Protected virtual dispose method
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
            }

            _disposed = true;
        }
    }

    public class LayeredProgressTracker
    {
        private readonly List<long> _expectedBytesPerLayer = new();
        private readonly object _lock = new();

        private readonly IProgress<long>? _reportProgress;
        private int _currentLayer;

        private long _totalBytesExpected;
        private long _totalBytesProcessed;

        public LayeredProgressTracker(IProgress<long>? reportProgress = null)
        {
            _reportProgress = reportProgress;
        }

        public void BeginLayer(string name)
        {
            lock (_lock)
            {
                _currentLayer++;
                // Ensure list is large enough
                while (_expectedBytesPerLayer.Count <= _currentLayer)
                    _expectedBytesPerLayer.Add(0);
            }
        }

        public void AddExpectedBytes(long bytes)
        {
            lock (_lock)
            {
                while (_expectedBytesPerLayer.Count <= _currentLayer)
                    _expectedBytesPerLayer.Add(0);

                _expectedBytesPerLayer[_currentLayer] += bytes;
                _totalBytesExpected += bytes;
                FileLogger.Log($"Total expected bytes: {_totalBytesExpected}");
            }
        }

        public void ReportBytesProcessed(long bytes)
        {
            lock (_lock)
            {
                _totalBytesProcessed += bytes;
                _reportProgress?.Report(_totalBytesProcessed); // Report INCREMENTAL bytes
            }
        }


        public void Complete()
        {
            lock (_lock)
            {
                _totalBytesProcessed = _totalBytesExpected;
                _reportProgress?.Report(_totalBytesProcessed);
            }
        }
    }

    public static class ParallelCtrEncryptor
    {
        internal static async Task EncryptFile(
            Stream input,
            Stream output,
            byte[] keyBytes,
            byte[] hkdfSalt,
            IProgress<double> progress)
        {
            FileLogger.Log("EncryptFile: Starting encryption process.");

            var encryptedTempPath = Path.GetTempFileName();

            try
            {
                using var bufferSet = BufferInit.InitBuffers(keyBytes, hkdfSalt);

                // Step 1: Encrypt to temporary encrypted file
                FileLogger.Log("EncryptFile: Writing encrypted output to temp file...");
                await using (var encryptedTempStream =
                             File.Open(encryptedTempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await EncryptV3(
                            input,
                            encryptedTempStream,
                            bufferSet.Key, bufferSet.Key2, bufferSet.Key3, bufferSet.Key4, bufferSet.Key5,
                            bufferSet.HMacKey, bufferSet.HMacKey2, bufferSet.HMacKey3,
                            progress)
                        .ConfigureAwait(false);
                }

                // Step 2: Copy encrypted temp file to final output stream
                FileLogger.Log("EncryptFile: Copying encrypted data to output stream...");
                await using (var encryptedTempRead = File.OpenRead(encryptedTempPath))
                {
                    await encryptedTempRead.CopyToAsync(output).ConfigureAwait(false);
                    await output.FlushAsync().ConfigureAwait(false);
                }

                CryptoUtilities.ClearMemoryNative(bufferSet.BuffersToClear);
                FileLogger.Log("EncryptFile: Cleared sensitive buffers.");
            }
            finally
            {
                if (File.Exists(encryptedTempPath))
                    File.Delete(encryptedTempPath);
            }
        }


        internal static async Task DecryptFile(
            Stream input,
            Stream output,
            byte[] keyBytes,
            byte[] hkdfSalt,
            IProgress<double> progress)
        {
            FileLogger.Log("DecryptFile: Starting decryption process.");

            var decryptedTempPath = Path.GetTempFileName();

            try
            {
                using var bufferSet = BufferInit.InitBuffers(keyBytes, hkdfSalt);

                // Step 1: Decrypt input stream to a temp file
                FileLogger.Log("DecryptFile: Decrypting stream to temp file...");
                await using (var decryptedTempStream =
                             File.Open(decryptedTempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await DecryptV3(
                            input,
                            decryptedTempStream,
                            bufferSet.Key, bufferSet.Key2, bufferSet.Key3, bufferSet.Key4, bufferSet.Key5,
                            bufferSet.HMacKey, bufferSet.HMacKey2, bufferSet.HMacKey3,
                            progress)
                        .ConfigureAwait(false);
                }

                // Step 2: Copy decrypted temp file to output stream
                FileLogger.Log("DecryptFile: Copying decrypted output to final output stream...");
                await using (var decryptedTempRead = File.OpenRead(decryptedTempPath))
                {
                    await decryptedTempRead.CopyToAsync(output).ConfigureAwait(false);
                    await output.FlushAsync().ConfigureAwait(false);
                }

                CryptoUtilities.ClearMemoryNative(bufferSet.BuffersToClear);
                FileLogger.Log("DecryptFile: Cleared sensitive buffers.");
            }
            finally
            {
                if (File.Exists(decryptedTempPath))
                    File.Delete(decryptedTempPath);
            }
        }


        // EncryptV3 with logging
        private static async Task EncryptV3(
            Stream inputStream,
            Stream outputStream,
            byte[] xchachaKey,
            byte[] threefishKey,
            byte[] serpentKey,
            byte[] aesKey,
            byte[] shuffleKey,
            byte[] threefishHmacKey,
            byte[] serpentHmacKey,
            byte[] aesHmacKey,
            IProgress<double>? progress,
            int chunkSize = 64 * 1024)
        {
            FileLogger.Log("EncryptV3: Starting");

            // Generate IVs/Nonces
            var xchachaNonce = CryptoUtilities.RndByteSized(16);
            var threefishIv = CryptoUtilities.RndByteSized(120);
            var serpentIv = CryptoUtilities.RndByteSized(8);
            var aesIv = CryptoUtilities.RndByteSized(8);

            // Create temp file paths
            var shuffledPath = Path.GetTempFileName();
            var temp1Path = Path.GetTempFileName();
            var temp2Path = Path.GetTempFileName();
            var temp3Path = Path.GetTempFileName();
            var temp4Path = Path.GetTempFileName();

            try
            {
                var shuffledProgress = CreateSegmentedProgress(inputStream.Length, 0, 20, progress);
                await using (var shuffledOut = File.OpenWrite(shuffledPath))
                {
                    await ShuffleStreamAsync(inputStream, shuffledOut, shuffleKey, shuffledProgress);
                }

                var shuffleSize = new FileInfo(shuffledPath).Length;
                var xChaChaProgress = CreateSegmentedProgress(shuffleSize, 20, 40, progress);

                await using (var xIn = File.OpenRead(shuffledPath))
                await using (var xOut = File.Create(temp1Path))
                {
                    await EncryptXChaCha20Poly1305ParallelRawAsync(xIn, xOut, xchachaKey, xchachaNonce,
                        xChaChaProgress);
                }

                var xchachaSize = new FileInfo(temp1Path).Length;
                var threeFishProgress = CreateSegmentedProgress(xchachaSize, 40, 60, progress);

                byte[] threefishTag;
                await using (var tIn = File.OpenRead(temp1Path))
                await using (var tOut = File.Create(temp2Path))
                {
                    threefishTag = await EncryptParallelAsync(
                        tIn, tOut, threefishKey, threefishHmacKey, () => new ThreefishEngine(1024), threefishIv,
                        threeFishProgress, chunkSize);
                }

                var threefishSize = new FileInfo(temp2Path).Length;
                var serpentProgress = CreateSegmentedProgress(threefishSize, 60, 80, progress);

                byte[] serpentTag;
                await using (var sIn = File.OpenRead(temp2Path))
                await using (var sOut = File.Create(temp3Path))
                {
                    serpentTag = await EncryptParallelAsync(
                        sIn, sOut, serpentKey, serpentHmacKey, () => new SerpentEngine(), serpentIv,
                        serpentProgress, chunkSize);
                }

                var serpentSize = new FileInfo(temp3Path).Length;
                var aesProgress = CreateSegmentedProgress(serpentSize, 80, 90, progress);

                byte[] aesTag;
                await using (var aIn = File.OpenRead(temp3Path))
                await using (var aOut = File.Create(temp4Path))
                {
                    aesTag = await EncryptParallelAsync(
                        aIn, aOut, aesKey, aesHmacKey, () => new AesEngine(), aesIv,
                        aesProgress, chunkSize);
                }

                await outputStream.WriteAsync(xchachaNonce);
                await outputStream.WriteAsync(threefishIv);
                await outputStream.WriteAsync(threefishTag);
                await outputStream.WriteAsync(serpentIv);
                await outputStream.WriteAsync(serpentTag);
                await outputStream.WriteAsync(aesIv);
                await outputStream.WriteAsync(aesTag);
                progress.Report(95);

                await using (var finalCipher = File.OpenRead(temp4Path))
                {
                    await outputStream.FlushAsync().ConfigureAwait(false);
                    await finalCipher.CopyToAsync(outputStream);
                    outputStream.Position = 0;
                    inputStream.Seek(0, SeekOrigin.Begin);
                }

                progress.Report(100);
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Encryption error: {ex}");
                throw;
            }
            finally
            {
                FileLogger.Log("EncryptV3: Cleaning up temporary files");
                await IO.SecurelyWipeFileAsync(shuffledPath);
                await IO.SecurelyWipeFileAsync(temp1Path);
                await IO.SecurelyWipeFileAsync(temp2Path);
                await IO.SecurelyWipeFileAsync(temp3Path);
                await IO.SecurelyWipeFileAsync(temp4Path);
            }
        }

        // DecryptV3 with logging
        private static async Task DecryptV3(
            Stream inputStream,
            Stream outputStream,
            byte[] xchachaKey,
            byte[] threefishKey,
            byte[] serpentKey,
            byte[] aesKey,
            byte[] shuffleKey,
            byte[] threefishHmacKey,
            byte[] serpentHmacKey,
            byte[] aesHmacKey,
            IProgress<double>? progress,
            int chunkSize = 64 * 1024)
        {
            FileLogger.Log("DecryptV3: Starting decryption");

            // Sizes for IVs and tags as constants
            const int xchachaNonceSize = 16;
            const int threefishIvSize = 120;
            const int hmacTagSize = 64; // Assuming 64 bytes tag length for all HMACs, adjust if different
            const int serpentIvSize = 8;
            const int aesIvSize = 8;

            // Read IVs and Tags sequentially
            var xchachaNonce = await ReadExactAsync(inputStream, xchachaNonceSize);
            var threefishIv = await ReadExactAsync(inputStream, threefishIvSize);
            var threefishTag = await ReadExactAsync(inputStream, hmacTagSize);
            var serpentIv = await ReadExactAsync(inputStream, serpentIvSize);
            var serpentTag = await ReadExactAsync(inputStream, hmacTagSize);
            var aesIv = await ReadExactAsync(inputStream, aesIvSize);
            var aesTag = await ReadExactAsync(inputStream, hmacTagSize);

            // Create temp files for intermediate layers
            var tempAesOutPath = Path.GetTempFileName();
            var tempSerpentOutPath = Path.GetTempFileName();
            var tempThreefishOutPath = Path.GetTempFileName();
            var tempXChachaOutPath = Path.GetTempFileName();
            var tempUnshufflePath = Path.GetTempFileName();
            try
            {
                var aesOutput = CreateSegmentedProgress(inputStream.Length, 0, 20, progress);
                await using (var aesOut = File.Create(tempAesOutPath))
                {
                    await DecryptParallelAsync(
                        inputStream,
                        aesOut,
                        aesKey,
                        aesHmacKey,
                        () => new AesEngine(),
                        aesIv,
                        aesTag,
                        aesOutput,
                        chunkSize);
                }

                var serpentProgress = CreateSegmentedProgress(new FileInfo(tempAesOutPath).Length, 20, 40, progress);

                // Step 2: Serpent decryption
                FileLogger.Log("Step 2: Decrypting Serpent layer");

                await using (var serpentIn = File.OpenRead(tempAesOutPath))
                await using (var serpentOut = File.Create(tempSerpentOutPath))
                {
                    await DecryptParallelAsync(
                        serpentIn,
                        serpentOut,
                        serpentKey,
                        serpentHmacKey,
                        () => new SerpentEngine(),
                        serpentIv,
                        serpentTag,
                        serpentProgress,
                        chunkSize);
                }

                var threefishProgress =
                    CreateSegmentedProgress(new FileInfo(tempSerpentOutPath).Length, 40, 60, progress);

                // Step 3: Threefish decryption
                FileLogger.Log("Step 3: Decrypting Threefish layer");

                await using (var threefishIn = File.OpenRead(tempSerpentOutPath))
                await using (var threefishOut = File.Create(tempThreefishOutPath))
                {
                    await DecryptParallelAsync(
                        threefishIn,
                        threefishOut,
                        threefishKey,
                        threefishHmacKey,
                        () => new ThreefishEngine(1024),
                        threefishIv,
                        threefishTag,
                        threefishProgress,
                        chunkSize);
                }

                var xChaProgress = CreateSegmentedProgress(new FileInfo(tempThreefishOutPath).Length, 60, 80, progress);

                await using (var xchachaIn = File.OpenRead(tempThreefishOutPath))
                await using (var xchachaOut = File.Create(tempXChachaOutPath))
                {
                    await DecryptXChaCha20Poly1305ParallelRawAsync(
                        xchachaIn,
                        xchachaOut,
                        xchachaKey,
                        xchachaNonce,
                        xChaProgress);
                }

                // Step 5: Unshuffle
                FileLogger.Log("Step 5: Unshuffling final output");

                await using (var shuffledIn = File.OpenRead(tempXChachaOutPath))
                {
                    var unshuffProgress =
                        CreateSegmentedProgress(new FileInfo(tempXChachaOutPath).Length, 80, 95, progress);
                    await UnshuffleStreamAsync(shuffledIn, outputStream, shuffleKey, unshuffProgress);
                    await using (var finalCipher = File.OpenRead(tempUnshufflePath))
                    {
                        await outputStream.FlushAsync().ConfigureAwait(false);
                        await finalCipher.CopyToAsync(outputStream);
                        progress?.Report(100);
                    }

                    if (outputStream.CanSeek)
                        outputStream.Position = 0;
                    else
                        FileLogger.Log("Final output stream is not seekable.");
                }

                FileLogger.Log("DecryptV3: Completed successfully");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"DecryptV3: EXCEPTION: {ex}");
                throw;
            }
            finally
            {
                FileLogger.Log("DecryptV3: Cleaning up temporary files");
                await IO.SecurelyWipeFileAsync(tempAesOutPath);
                await IO.SecurelyWipeFileAsync(tempSerpentOutPath);
                await IO.SecurelyWipeFileAsync(tempThreefishOutPath);
                await IO.SecurelyWipeFileAsync(tempXChachaOutPath);
            }
        }


        // EncryptParallelAsync with logging
        private static async Task<byte[]> EncryptParallelAsync(
            Stream input,
            Stream output,
            byte[] key,
            byte[] hmacKey,
            Func<IBlockCipher> cipherFactory,
            byte[] baseIv,
            IProgress<long> progress = null,
            int chunkSize = 64 * 1024,
            int maxParallelism = 4)
        {
            FileLogger.Log("=== EncryptParallelAsync: Starting encryption ===");
            FileLogger.Log(
                $"EncryptParallelAsync: Base IV ({baseIv.Length} bytes): {DataConversionHelpers.ByteArrayToHexString(baseIv)}");

            using var hmac = new HmacSha3Stream(hmacKey);
            using var sha256 = SHA256.Create();

            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxParallelism,
                EnsureOrdered = true
            };

            var encryptBlock =
                new TransformBlock<(byte[] buffer, int bytesRead, long index), (int index, byte[] ciphertext, int length
                    )>(
                    task =>
                    {
                        var (buffer, bytesRead, index) = task;

                        var engine = cipherFactory();
                        var cipher = new SicBlockCipher(engine);
                        var blockSize = cipher.GetBlockSize();

                        var nonce = DeriveNonce(baseIv, index, blockSize);
                        cipher.Init(true, new ParametersWithIV(new KeyParameter(key), nonce));

                        var encrypted = new byte[bytesRead];
                        var processed = 0;

                        // Process full blocks
                        var fullBlocks = bytesRead / blockSize;
                        for (var i = 0; i < fullBlocks; i++)
                        {
                            cipher.ProcessBlock(buffer, i * blockSize, encrypted, i * blockSize);
                            processed += blockSize;
                        }

                        // Handle partial last block if any
                        var remaining = bytesRead - processed;
                        if (remaining > 0)
                        {
                            // CTR mode: last partial block encryption by XOR with keystream block
                            // Generate one keystream block
                            var keystreamBlock = new byte[blockSize];
                            cipher.ProcessBlock(new byte[blockSize], 0, keystreamBlock, 0);

                            for (var i = 0; i < remaining; i++)
                                encrypted[processed + i] = (byte)(buffer[processed + i] ^ keystreamBlock[i]);
                        }

                        return ((int)index, encrypted, bytesRead);
                    }, options);

            long totalBytesProcessed = 0;

            var writeBlock = new ActionBlock<(int index, byte[] ciphertext, int length)>(
                async result =>
                {
                    var (index, ciphertext, length) = result;
                    await output.WriteAsync(ciphertext, 0, length).ConfigureAwait(false);

                    hmac.Update(ciphertext, 0, length);

                    var newTotal = Interlocked.Add(ref totalBytesProcessed, length);

                    // Report raw bytes processed instead of percentage
                    progress.Report(newTotal);
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 1
                });


            encryptBlock.LinkTo(writeBlock, new DataflowLinkOptions { PropagateCompletion = true });

            long chunkIndex = 0;
            while (true)
            {
                var buffer = new byte[chunkSize];
                var bytesRead = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                if (bytesRead < buffer.Length)
                    Array.Resize(ref buffer, bytesRead);

                await encryptBlock.SendAsync((buffer, bytesRead, chunkIndex)).ConfigureAwait(false);
                chunkIndex++;
            }

            encryptBlock.Complete();
            await writeBlock.Completion.ConfigureAwait(false);

            // Finalize HMAC
            var hmacTag = hmac.Final();

            return hmacTag;
        }

        private static async Task DecryptParallelAsync(Stream input,
            Stream output,
            byte[] key,
            byte[] hmacKey,
            Func<IBlockCipher> cipherFactory,
            byte[] baseIv,
            byte[] expectedTag,
            IProgress<long>? progress = null,
            int chunkSize = 64 * 1024,
            int maxParallelism = 4)
        {
            FileLogger.Log("=== DecryptParallelAsync: Starting decryption ===");
            FileLogger.LogBytes("DecryptParallelAsync: Base IV", baseIv);
            FileLogger.LogBytes("DecryptParallelAsync: Expected HMAC Tag", expectedTag);

            using var hmac = new HmacSha3Stream(hmacKey);
            using var sha256 = SHA256.Create();

            var channel = Channel.CreateBounded<(int index, byte[] ciphertext)>(maxParallelism * 2);
            var writer = channel.Writer;
            var reader = channel.Reader;

            var chunkIndex = 0;

            // Producer reads until end of stream
            var producer = Task.Run(async () =>
            {
                var buffer = new byte[chunkSize];
                int bytesRead;

                while ((bytesRead = await input.ReadAsync(buffer, 0, chunkSize).ConfigureAwait(false)) > 0)
                {
                    var chunk = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);

                    hmac.Update(chunk, 0, chunk.Length);
                    sha256.TransformBlock(chunk, 0, chunk.Length, null, 0);

                    await writer.WriteAsync((chunkIndex++, chunk)).ConfigureAwait(false);
                }

                writer.Complete();
            });

            var processedChunks = new ConcurrentDictionary<int, byte[]>();
            var nextWriteIndex = 0;
            var writeLock = new SemaphoreSlim(1, 1);

            var processors = Enumerable.Range(0, maxParallelism).Select(_ => Task.Run(async () =>
            {
                await foreach (var (index, ciphertext) in reader.ReadAllAsync())
                {
                    var engine = cipherFactory();
                    var cipher = new SicBlockCipher(engine);
                    var blockSize = cipher.GetBlockSize();

                    var nonce = DeriveNonce(baseIv, index, blockSize);
                    FileLogger.LogBytes($"DecryptParallelAsync: Nonce [chunk {index}]", nonce);

                    cipher.Init(false, new ParametersWithIV(new KeyParameter(key), nonce));

                    var decrypted = new byte[ciphertext.Length];
                    var processed = 0;

                    var fullBlocks = ciphertext.Length / blockSize;
                    for (var i = 0; i < fullBlocks; i++)
                    {
                        cipher.ProcessBlock(ciphertext, i * blockSize, decrypted, i * blockSize);
                        processed += blockSize;
                    }

                    var remaining = ciphertext.Length - processed;
                    if (remaining > 0)
                    {
                        var keystreamBlock = new byte[blockSize];
                        cipher.ProcessBlock(new byte[blockSize], 0, keystreamBlock, 0);
                        for (var i = 0; i < remaining; i++)
                            decrypted[processed + i] = (byte)(ciphertext[processed + i] ^ keystreamBlock[i]);
                    }

                    processedChunks[index] = decrypted;

                    await writeLock.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        while (processedChunks.TryRemove(nextWriteIndex, out var readyPlaintext))
                        {
                            await output.WriteAsync(readyPlaintext, 0, readyPlaintext.Length).ConfigureAwait(false);
                            progress?.Report(readyPlaintext.Length);
                            nextWriteIndex++;
                        }
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                }
            })).ToArray();

            await producer.ConfigureAwait(false);
            await Task.WhenAll(processors).ConfigureAwait(false);

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var ciphertextHash = BitConverter.ToString(sha256.Hash!).Replace("-", "");
            FileLogger.Log($"DecryptParallelAsync: Ciphertext SHA256 hash: {ciphertextHash}");

            var actualTag = hmac.Final();

            if (actualTag.Length > expectedTag.Length)
                actualTag = actualTag.AsSpan(0, expectedTag.Length).ToArray();

            FileLogger.LogBytes("DecryptParallelAsync: Computed HMAC Tag", actualTag);

            if (!CryptographicOperations.FixedTimeEquals(expectedTag, actualTag))
            {
                FileLogger.Log("DecryptParallelAsync: HMAC verification failed!");
                throw new CryptographicException("HMAC verification failed.");
            }

            await output.FlushAsync().ConfigureAwait(false);
            FileLogger.Log("=== DecryptParallelAsync: Decryption complete ===");

            writeLock.Dispose();
        }


        private static async Task EncryptXChaCha20Poly1305ParallelRawAsync(
            Stream input,
            Stream output,
            byte[] key,
            byte[] baseNonce,
            IProgress<long>? progress = null,
            int chunkSize = 64 * 1024,
            int maxParallelism = 4)
        {
            using var semaphore = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>();
            var writeLock = new object();
            var chunkIndex = 0;

            FileLogger.Log(
                $"EncryptXChaCha20: Starting encryption. Chunk size: {chunkSize}, Max parallelism: {maxParallelism}");

            while (true)
            {
                var buffer = new byte[chunkSize];
                var bytesRead = await input.ReadAsync(buffer, 0, chunkSize);
                if (bytesRead <= 0)
                {
                    FileLogger.Log("EncryptXChaCha20: Reached end of input stream.");
                    break;
                }

                var chunk = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);

                var currentIndex = chunkIndex++;
                await semaphore.WaitAsync();

                FileLogger.Log($"EncryptXChaCha20: Chunk {currentIndex}, plaintext size = {bytesRead}");

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var nonce = DeriveNonce(baseNonce, currentIndex, 24);

                        var ciphertext = SecretAeadXChaCha20Poly1305.Encrypt(chunk, nonce, key);

                        lock (writeLock)
                        {
                            var lenBytes = BitConverter.GetBytes(ciphertext.Length);
                            output.Write(lenBytes, 0, 4);
                            output.Write(ciphertext, 0, ciphertext.Length);

                            FileLogger.Log(
                                $"EncryptXChaCha20: Chunk {currentIndex} written. Ciphertext + tag length = {ciphertext.Length}");
                        }

                        progress?.Report(bytesRead);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Log($"EncryptXChaCha20: Chunk {currentIndex} FAILED: {ex}");
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            FileLogger.Log("EncryptXChaCha20: All chunks encrypted.");
        }


        private static async Task DecryptXChaCha20Poly1305ParallelRawAsync(
            Stream input,
            Stream output,
            byte[] key,
            byte[] baseNonce,
            IProgress<long>? progress = null,
            int chunkSize = 64 * 1024,
            int maxParallelism = 4)
        {
            FileLogger.Log("DecryptXChaCha20: Started");

            var semaphore = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>();
            var chunkOutputs = new ConcurrentDictionary<long, byte[]>();
            var progressLock = new object();

            long totalBytesProcessed = 0;
            long chunkIndex = 0;

            while (true)
            {
                FileLogger.Log($"DecryptXChaCha20: Reading chunk at input position {input.Position}");

                var lenBytes = new byte[4];
                var lenRead = await input.ReadAsync(lenBytes, 0, 4);
                if (lenRead == 0) break; // End of stream
                if (lenRead < 4)
                    throw new EndOfStreamException("Failed to read full chunk length prefix.");

                var chunkLength = BitConverter.ToInt32(lenBytes, 0);
                if (chunkLength <= 0 || chunkLength > 100 * 1024 * 1024)
                    throw new InvalidDataException($"Invalid chunk length: {chunkLength}");

                if (input.Length - input.Position < chunkLength)
                    throw new InvalidDataException("Input does not contain enough bytes for declared chunk.");

                var buffer = new byte[chunkLength];
                var actualRead = await ReadExactAsync(input, buffer, chunkLength);
                if (actualRead != chunkLength)
                    throw new EndOfStreamException("Did not receive expected chunk bytes.");

                FileLogger.Log($"DecryptXChaCha20: Chunk {chunkIndex}, ciphertext length = {actualRead}");

                var currentIndex = chunkIndex++;
                var ciphertext = buffer;

                await semaphore.WaitAsync();

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var nonce = DeriveNonce(baseNonce, currentIndex, 24);

                        FileLogger.Log(
                            $"DecryptXChaCha20: Decrypting chunk {currentIndex} with nonce: {Convert.ToHexString(nonce)}");

                        var plaintext = SecretAeadXChaCha20Poly1305.Decrypt(ciphertext, nonce, key);

                        chunkOutputs[currentIndex] = plaintext;

                        lock (progressLock)
                        {
                            totalBytesProcessed += plaintext.Length;
                            progress?.Report(totalBytesProcessed);
                        }

                        FileLogger.Log($"DecryptXChaCha20: Chunk {currentIndex} successfully decrypted.");
                    }
                    catch (Exception ex)
                    {
                        FileLogger.Log($"DecryptXChaCha20: Chunk {currentIndex} failed: {ex}");
                        throw;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            FileLogger.Log("DecryptXChaCha20: Writing chunks to output in order.");
            for (var i = 0; i < chunkIndex; i++)
                if (chunkOutputs.TryGetValue(i, out var chunk))
                {
                    await output.WriteAsync(chunk, 0, chunk.Length);
                }
                else
                {
                    FileLogger.Log($"DecryptXChaCha20: Missing chunk {i} in output map!");
                    throw new InvalidOperationException($"Missing chunk {i}");
                }

            FileLogger.Log("DecryptXChaCha20: Completed");
        }

        private static byte[] DeriveNonce(byte[] baseIv, long index, int requiredLength)
        {
            if (baseIv.Length > requiredLength - 8)
                throw new ArgumentException("Base IV too long for derived nonce");

            var nonce = new byte[requiredLength];
            Buffer.BlockCopy(baseIv, 0, nonce, 0, baseIv.Length);
            Buffer.BlockCopy(BitConverter.GetBytes((ulong)index), 0, nonce, requiredLength - 8, 8);
            return nonce;
        }

        private static async Task ShuffleStreamAsync(Stream input, Stream output, byte[] key,
            IProgress<long>? progress = null)
        {
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input stream was null or empty.", nameof(input));
            if (!input.CanSeek)
                throw new NotSupportedException("Input stream must support seeking.");

            const int chunkSize = 64 * 1024; // 64 KB
            const long blockSize = 2L * 1024 * 1024 * 1024; // 2 GB
            var totalBlocks = (input.Length + blockSize - 1) / blockSize;
            var ioLock = new SemaphoreSlim(1, 1);
            long totalBytesWritten = 0;

            for (long blockIndex = 0; blockIndex < totalBlocks; blockIndex++)
            {
                var blockStart = blockIndex * blockSize;
                var remaining = input.Length - blockStart;
                var currentBlockSize = Math.Min(blockSize, remaining);
                var chunkCount = (int)((currentBlockSize + chunkSize - 1) / chunkSize);

                if (currentBlockSize <= 0)
                    continue;

                var indices = GenerateSecureShuffleIndices(chunkCount, key, blockIndex);
                var chunks = new byte[chunkCount][];

                await Parallel.ForEachAsync(
                    Enumerable.Range(0, chunkCount),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    async (i, _) =>
                    {
                        var buffer = new byte[chunkSize];
                        await ioLock.WaitAsync();
                        try
                        {
                            input.Seek(blockStart + (long)i * chunkSize, SeekOrigin.Begin);
                            var read = await input.ReadAsync(buffer, 0, chunkSize).ConfigureAwait(false);
                            chunks[i] = buffer.AsSpan(0, read).ToArray();
                        }
                        finally
                        {
                            ioLock.Release();
                        }

                        CryptographicOperations.ZeroMemory(buffer);
                    });

                var shuffledChunks = new byte[chunkCount][];
                for (var i = 0; i < chunkCount; i++)
                {
                    if (chunks[i] == null)
                        throw new InvalidOperationException($"Chunk {i} was null in block {blockIndex}");

                    shuffledChunks[indices[i]] = chunks[i];
                }

                double lastReportedPercent = 0;

                foreach (var chunk in shuffledChunks)
                {
                    await output.WriteAsync(chunk, 0, chunk.Length).ConfigureAwait(false);
                    var written = Interlocked.Add(ref totalBytesWritten, chunk.Length);

                    if (progress != null)
                    {
                        var percent = 100.0 * written / input.Length;
                        if (Math.Abs(percent - lastReportedPercent) >= 2)
                        {
                            lastReportedPercent = percent;
                            progress.Report(written); // ✅ report actual bytes written (long)
                        }
                    }

                    CryptographicOperations.ZeroMemory(chunk);
                }
            }
        }


        private static async Task UnshuffleStreamAsync(Stream input, Stream output, byte[] key,
            IProgress<long>? progress = null)
        {
            if (input == null || input.Length == 0)
                throw new ArgumentException("Input stream was null or empty.", nameof(input));
            if (!input.CanSeek)
                throw new NotSupportedException("Input stream must support seeking.");

            const int chunkSize = 64 * 1024;
            const long blockSize = 2L * 1024 * 1024 * 1024;
            var totalBlocks = (input.Length + blockSize - 1) / blockSize;
            var ioLock = new SemaphoreSlim(1, 1);
            long totalBytesWritten = 0;

            for (long blockIndex = 0; blockIndex < totalBlocks; blockIndex++)
            {
                var blockStart = blockIndex * blockSize;
                var remaining = input.Length - blockStart;
                var currentBlockSize = Math.Min(blockSize, remaining);
                var chunkCount = (int)((currentBlockSize + chunkSize - 1) / chunkSize);

                var indices = GenerateSecureShuffleIndices(chunkCount, key, blockIndex);
                var shuffledChunks = new byte[chunkCount][];

                await Parallel.ForEachAsync(
                    Enumerable.Range(0, chunkCount),
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    async (i, _) =>
                    {
                        var buffer = new byte[chunkSize];
                        await ioLock.WaitAsync();
                        try
                        {
                            input.Seek(blockStart + (long)i * chunkSize, SeekOrigin.Begin);
                            var read = await input.ReadAsync(buffer, 0, chunkSize).ConfigureAwait(false);
                            shuffledChunks[i] = buffer.AsSpan(0, read).ToArray();
                        }
                        finally
                        {
                            ioLock.Release();
                        }

                        CryptographicOperations.ZeroMemory(buffer);
                    });

                var unshuffledChunks = new byte[chunkCount][];
                for (var i = 0; i < chunkCount; i++)
                {
                    var shuffledIndex = indices[i];
                    unshuffledChunks[i] = shuffledChunks[shuffledIndex];
                }

                foreach (var chunk in unshuffledChunks)
                {
                    await output.WriteAsync(chunk, 0, chunk.Length).ConfigureAwait(false);
                    CryptographicOperations.ZeroMemory(chunk);

                    Interlocked.Add(ref totalBytesWritten, chunk.Length);
                    progress?.Report(totalBytesWritten);
                }

                totalBytesWritten += currentBlockSize;
            }
        }


        public static int[] GenerateSecureShuffleIndices(int count, byte[] key, long blockIndex)
        {
            using var hmac = new HmacSha3Stream(key);

            // Convert blockIndex to bytes
            var blockIndexBytes = BitConverter.GetBytes(blockIndex);

            // Feed bytes into HMAC
            hmac.Update(blockIndexBytes, 0, blockIndexBytes.Length);

            // Compute the final HMAC digest
            var seedBytes = hmac.Final();

            // Extract seed int (non-negative)
            var seed = BitConverter.ToInt32(seedBytes, 0) & 0x7FFFFFFF;

            var rng = new Random(seed);
            var indices = Enumerable.Range(0, count).ToArray();

            for (var i = count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }
    }
}