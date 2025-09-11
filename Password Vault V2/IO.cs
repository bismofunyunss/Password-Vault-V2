using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Password_Vault_V2;

public static class IO
{
    /// <summary>
    /// Creates a user directory and user file for the specified username under the local application data folder.
    /// </summary>
    /// <param name="userName">The username to create the directory and file for.</param>
    /// <returns>The full file path of the created user file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a user directory already exists for the specified username.</exception>
    public static string CreateUserPath(string userName)
    {
        var userDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Password Vault", "Users", userName);

        if (Directory.Exists(userDirectory))
            throw new InvalidOperationException("A user with this username already exists.");

        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Password Vault",
            "Users", userName, $"{userName}.user");

        Directory.CreateDirectory(userDirectory);

        File.Create(filePath).Dispose();

        return filePath;
    }

    /// <summary>
    /// Builds a user file content by concatenating multiple byte array components with their lengths prefixed.
    /// </summary>
    /// <param name="components">An array of byte arrays to concatenate with length prefixes.</param>
    /// <returns>A single byte array representing all components combined with length prefixes.</returns>
    public static byte[] BuildUserFile(params byte[][] components)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, true);

        if (components != null)
            foreach (var part in components)
            {
                writer.Write(part.Length);
                writer.Write(part);
            }

        return ms.ToArray();
    }

    /// <summary>
    /// Asynchronously reads the entire contents of a file into a byte array.
    /// </summary>
    /// <param name="path">The full path to the file to read.</param>
    /// <returns>A task representing the asynchronous read operation. The task result contains the file contents as a byte array, or an empty array if the file is empty.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    /// <exception cref="OutOfMemoryException">Thrown if the file size exceeds the maximum allowed limit (2,000,000,000 bytes).</exception>
    public static async Task<byte[]?> ReadFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File doesn't exist.", path);

        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);

        if (fs.Length == 0)
            return [];

        var length = (int)fs.Length;
        var buffer = new byte[length];

        var bytesRead = 0;
        while (bytesRead < buffer.Length)
        {
            var read = await fs.ReadAsync(buffer.AsMemory(bytesRead, buffer.Length - bytesRead));
            if (read == 0)
                break;
            bytesRead += read;
        }

        return buffer;
    }

    /// <summary>
    /// Asynchronously writes the specified byte array data to a file, overwriting if it already exists.
    /// </summary>
    /// <param name="path">The full file path where data will be written.</param>
    /// <param name="data">The byte array data to write to the file.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="data"/> parameter is null or empty.</exception>
    public static async Task WriteFile(string path, byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("No data to write.", nameof(data));

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        const int chunkSize = 81920;
        var offset = 0;

        while (offset < data.Length)
        {
            var remaining = data.Length - offset;
            var toWrite = Math.Min(chunkSize, remaining);
            await fs.WriteAsync(data.AsMemory(offset, toWrite));
            offset += toWrite;
        }

        await fs.FlushAsync();
    }

    public static FileStream OpenFileStream(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("File doesn't exist.", path);

        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            bufferSize: 81920,
            options: FileOptions.SequentialScan | FileOptions.Asynchronous);
    }


    public static async Task WriteFileStreamAsync(string path, Stream inputStream)
    {
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream));

        await using var output = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Write,
            bufferSize: 81920,
            useAsync: true);

        if (inputStream.CanSeek)
            inputStream.Position = 0;

        await inputStream.CopyToAsync(output).ConfigureAwait(false);
    }

    public static async Task SecurelyWipeFileAsync(string path, int passes = 3)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        var fileInfo = new FileInfo(path);
        long length = fileInfo.Length;

        try
        {
            for (int pass = 0; pass < passes; pass++)
            {
                using var fs = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);
                fs.Position = 0;

                byte[] buffer = new byte[81920];

                if (pass == passes - 1)
                {
                    Array.Clear(buffer, 0, buffer.Length); // Last pass: zeros
                }
                else
                {
                    RandomNumberGenerator.Fill(buffer);    // Random data
                }

                long remaining = length;
                while (remaining > 0)
                {
                    int toWrite = (int)Math.Min(buffer.Length, remaining);
                    await fs.WriteAsync(buffer.AsMemory(0, toWrite)).ConfigureAwait(false);
                    remaining -= toWrite;
                }

                await fs.FlushAsync().ConfigureAwait(false);
                // Stream disposed here (end of using)
            }

            // Flush OS buffers to device
            FlushFileSystem(path);

            // Delete the file after wiping
            File.Delete(path);
        }
        catch (Exception ex)
        {
           ErrorLogging.ErrorLog(ex);
        }
    }

public static class SecureFileHandler
{
    /// <summary>
    /// Securely processes a large file. On HDD, uses multi-pass overwrite. On SSD, uses AES-GCM encryption.
    /// </summary>
    public static async Task SecurelyProcessLargeFileAsync(string path, bool isSSD, int passes = 3, int bufferSize = 64 * 1024)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        if (isSSD)
            await WipeWithAesGcmAsync(path, bufferSize);
        else
            await WipeWithRandomAsync(path, passes, bufferSize);
    }

    /// <summary>
    /// Multi-pass overwrite for HDDs.
    /// </summary>
    private static async Task WipeWithRandomAsync(string path, int passes, int bufferSize)
    {
        var fileInfo = new FileInfo(path);
        long length = fileInfo.Length;
        byte[] buffer = new byte[bufferSize];

        for (int pass = 0; pass < passes; pass++)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            fs.Position = 0;

            RandomNumberGenerator rng = RandomNumberGenerator.Create();

            long remaining = length;
            while (remaining > 0)
            {
                int toWrite = (int)Math.Min(buffer.Length, remaining);
                if (pass == passes - 1)
                    Array.Clear(buffer, 0, buffer.Length); // last pass: zeros
                else
                    rng.GetBytes(buffer, 0, toWrite);

                await fs.WriteAsync(buffer.AsMemory(0, toWrite)).ConfigureAwait(false);
                remaining -= toWrite;
            }

            await fs.FlushAsync().ConfigureAwait(false);
        }

        // Delete file
        File.Delete(path);
    }

    /// <summary>
    /// AES-GCM single-pass encryption for SSDs.
    /// </summary>
    private static async Task WipeWithAesGcmAsync(string path, int bufferSize)
    {
        string tempPath = path + ".enc";
        byte[] key = RandomNumberGenerator.GetBytes(32); // 256-bit key
        byte[] nonce = RandomNumberGenerator.GetBytes(12); // 96-bit GCM nonce
        byte[] tag = new byte[16];

        try
        {
            using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, useAsync: true);
            using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

            using var aesGcm = new AesGcm(key);

            byte[] buffer = new byte[bufferSize];
            long position = 0;

            int read;
            while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                byte[] ciphertext = new byte[read];
                byte[] tagBuffer = new byte[16];

                aesGcm.Encrypt(nonce, buffer.AsSpan(0, read), ciphertext, tagBuffer, new byte[0]);
                await output.WriteAsync(ciphertext.AsMemory(0, ciphertext.Length));
                position += read;
            }
        }
        finally
        {
            // Zero key and nonce
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(nonce);
        }

        // Delete original file after encryption
        File.Delete(path);

        // Optionally rename encrypted file to original name (so it looks "wiped")
        File.Move(tempPath, path);
    }
}


    private static void FlushFileSystem(string path)
    {
        const uint GENERIC_WRITE = 0x40000000;
        const uint OPEN_EXISTING = 3;
        const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;

        using SafeFileHandle handle = CreateFile(
            path,
            GENERIC_WRITE,
            0,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_WRITE_THROUGH,
            IntPtr.Zero);

        if (!handle.IsInvalid)
        {
            if (!FlushFileBuffers(handle))
            {
                int err = Marshal.GetLastWin32Error();
            }
        }
        else
        {
            int err = Marshal.GetLastWin32Error();
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushFileBuffers(SafeFileHandle hFile);
}