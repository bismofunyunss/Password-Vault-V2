namespace Password_Vault_V2;

public static class UserFileManager
{
    /// <summary>
    /// Gets or sets the username of the currently logged-in user.
    /// </summary>
    public static string CurrentLoggedInUser { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full file path of the user file for the specified username.
    /// </summary>
    /// <param name="userName">The username to get the user file path for.</param>
    /// <returns>The full path to the user's .user file in the local application data directory.</returns>
    public static string GetUserFilePath(string userName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Password Vault",
            "Users",
            userName, $"{userName}.user");
    }

    /// <summary>
    /// Gets the full file path of the vault file for the specified username.
    /// </summary>
    /// <param name="userName">The username to get the vault file path for.</param>
    /// <returns>The full path to the user's .vault file in the local application data directory.</returns>
    public static string GetUserVault(string userName)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Password Vault",
            "Users", userName, $"{userName}.vault");
    }

    /// <summary>
    /// Checks if a user file exists for the specified username.
    /// </summary>
    /// <param name="userName">The username to check existence for.</param>
    /// <returns><c>true</c> if the user file exists; otherwise, <c>false</c>.</returns>
    public static bool UserExists(string userName)
    {
        var path = GetUserFilePath(userName);
        return File.Exists(path);
    }
}