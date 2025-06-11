namespace Password_Vault_V2;

public static class UserLog
{
    /// <summary>
    /// A shared <see cref="HttpClient"/> instance used for HTTP requests.
    /// </summary>
    private static readonly HttpClient HttpClient = new();

    /// <summary>
    /// The URI used to fetch the external IP address of the user.
    /// </summary>
    private static readonly Uri ExternalIp = new("https://api.ipify.org");

    /// <summary>
    /// Logs the specified user's login time and external IP address to a text file.
    /// </summary>
    /// <param name="userName">The username of the user who logged in.</param>
    /// <remarks>
    /// The IP address is retrieved using an external API call to <c>https://api.ipify.org</c>.
    /// If logging fails, an error message is displayed and the exception is logged.
    /// </remarks>
    public static void LogUser(string userName)
    {
        try
        {
            File.AppendAllText("UserLog.txt",
                $"Username: {userName} logged in using IP: {HttpClient.GetStringAsync(ExternalIp).Result} {DateTime.Now}\n");
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(e);
        }
    }
}