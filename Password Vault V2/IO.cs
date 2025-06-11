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

        if (fs.Length > 2_000_000_000)
            throw new OutOfMemoryException("File size exceeds the maximum allowed limit.");

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
}