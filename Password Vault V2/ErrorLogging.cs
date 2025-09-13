using System;
using System.IO;
using System.Text.RegularExpressions;

public static class ErrorLogging
{
    private static readonly object _lock = new object();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyAppName", "Logs");

    /// <summary>
    /// Logs an exception and all inner exceptions to a daily rotating log file.
    /// Messages and stack traces are sanitized to remove newlines and file paths.
    /// </summary>
    public static void ErrorLog(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);

            string logFilePath = Path.Combine(LogDirectory, $"ErrorLog_{DateTime.Now:yyyy-MM-dd}.txt");

            lock (_lock)
            {
                using var writer = new StreamWriter(logFilePath, append: true);
                writer.WriteLine(new string('-', 80));
                WriteException(writer, ex);

                // Recursively log all inner exceptions
                Exception inner = ex.InnerException;
                while (inner != null)
                {
                    writer.WriteLine("Inner Exception:");
                    WriteException(writer, inner);
                    inner = inner.InnerException;
                }
            }
        }
        catch
        {
            // Silent failure; optionally write to Debug output
            System.Diagnostics.Debug.WriteLine("Error logging failed.");
        }
    }

    /// <summary>
    /// Writes sanitized exception details to the writer.
    /// Removes newlines and replaces file paths with <path>.
    /// </summary>
    private static void WriteException(TextWriter writer, Exception ex)
    {
        writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"Exception Type: {ex.GetType().FullName}");
        writer.WriteLine($"Message: {SanitizeText(ex.Message)}");
        writer.WriteLine($"Stack Trace: {SanitizeText(ex.StackTrace)}");
        writer.WriteLine();
    }

    /// <summary>
    /// Sanitizes exception text to remove newlines and replace file paths.
    /// </summary>
    private static string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove newlines
        text = text.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ");

        // Replace Windows-style file paths: C:\...\file.ext → <path>
        text = Regex.Replace(text, @"[a-zA-Z]:\\[^\s]*", "<path>");

        return text;
    }
}


