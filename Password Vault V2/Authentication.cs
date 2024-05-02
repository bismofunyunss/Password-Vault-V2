namespace Password_Vault_V2;

public static class Authentication
{
    public static string CurrentLoggedInUser { get; set; } = string.Empty;

    public static string GetUserFilePath(string userName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Password Vault",
            "Users", userName, $"{userName}.user");
    }

    public static string GetUserVault(string userName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Password Vault",
            "Users", userName, $"{userName}.vault");
    }

    public static bool UserExists(string userName)
    {
        var path = GetUserFilePath(userName);
        return File.Exists(path);
    }

    /// <summary>
    ///     Retrieves the AES and ChaCha salts associated with a user.
    /// </summary>
    /// <param name="userName">The username of the user.</param>
    /// <returns>
    ///     A tuple containing the AES salt and ChaCha salt as byte arrays.
    ///     If an error occurs, returns empty byte arrays.
    /// </returns>
    /// <remarks>
    ///     The salts are retrieved from files stored in the local application data folder.
    ///     The file is named "{userName}.salt".
    /// </remarks>
    public static byte[] GetUserSalt(string userName)
    {
        // Construct the path to the user's salt file
        var userSaltFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Password Vault", "Users", userName, $"{userName}.salt");

        // Read salt value from file asynchronously and convert it to a byte array
        var salt = DataConversionHelpers.Base64StringToByteArray(File.ReadAllText(userSaltFilePath));

        return salt;
    }

    /// <summary>
    ///     Asynchronously retrieves user information from a file and updates CryptoConstants.Hash.
    /// </summary>
    /// <param name="userName">The user name for which information is retrieved.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    /// <exception cref="IOException">Thrown if the file specified by the user name does not exist.</exception>
    /// <remarks>
    ///     This method constructs a file path using the provided user name and reads information
    ///     from the file. It searches for the line containing "User:" and converts the hexadecimal
    ///     string from the subsequent line to a byte array, updating CryptoConstants.Hash.
    ///     Any exceptions during file reading or processing are logged and rethrown.
    /// </remarks>
    public static void GetUserInfo(string userName)
    {
        var path = GetUserFilePath(userName);

        if (!File.Exists(path))
            throw new IOException("File does not exist.");

        var lines = File.ReadAllLines(path);

        // Find the line containing "User:"
        var index = Array.IndexOf(lines, "User:");
        if (index == -1)
            return;

        // Convert the hexadecimal string to a byte array and assign it to CryptoConstants.Hash
        Crypto.CryptoConstants.Hash = DataConversionHelpers.HexStringToByteArray(lines[index + 3]);
    }
}