namespace Password_Vault_V2;

public static class UserLog
{
    private static readonly HttpClient HttpClient = new();
    private static readonly Uri ExternalIp = new("https://api.ipify.org");

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