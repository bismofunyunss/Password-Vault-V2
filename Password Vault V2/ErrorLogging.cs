namespace Password_Vault_V2;

public static class ErrorLogging
{
    private static readonly string LogFileName = "ErrorLog.txt";
    public static void ErrorLog(Exception ex)
    {
        try
        {
            using var writer = File.AppendText(LogFileName);
            writer.AutoFlush = true;
            LogExceptionDetails(writer, ex);

            // If there's an inner exception, log it
            if (ex.InnerException != null)
            {
                writer.WriteLine("Inner Exception:");
                LogExceptionDetails(writer, ex.InnerException);
            }
        }
        catch (IOException ioException)
        {
            HandleLoggingError($"Error logging failed due to I/O exception: {ioException.Message}");
        }
        catch (Exception logException)
        {
            HandleLoggingError($"Error logging failed with an unexpected exception: {logException.Message}");
        }
    }

    private static void LogExceptionDetails(TextWriter writer, Exception ex)
    {
        writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"Exception Type: {ex.GetType().FullName}");
        writer.WriteLine($"Message: {ex.Message}");
        writer.WriteLine($"Stack Trace: {ex.StackTrace}");
        writer.WriteLine();
    }

    private static void HandleLoggingError(string errorMessage)
    {
        MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}