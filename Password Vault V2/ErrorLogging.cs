namespace Password_Vault_V2;

public static class ErrorLogging
{
    /// <summary>
    /// The name of the file where error logs will be stored.
    /// </summary>
    private static readonly string LogFileName = "ErrorLog.txt";

    /// <summary>
    /// Logs the provided exception and any inner exception to the error log file.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <remarks>
    /// Logs include the exception type, message, stack trace, and timestamp.
    /// If the logging process fails, an error message is shown via a message box.
    /// </remarks>
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

    /// <summary>
    /// Writes detailed information about an exception to the provided text writer.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter"/> to write to.</param>
    /// <param name="ex">The exception whose details are to be logged.</param>
    private static void LogExceptionDetails(TextWriter writer, Exception ex)
    {
        writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"Exception Type: {ex.GetType().FullName}");
        writer.WriteLine($"Message: {ex.Message}");
        writer.WriteLine($"Stack Trace: {ex.StackTrace}");
        writer.WriteLine();
    }

    /// <summary>
    /// Displays a message box with the specified error message if logging fails.
    /// </summary>
    /// <param name="errorMessage">The error message to display.</param>
    private static void HandleLoggingError(string errorMessage)
    {
        MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}