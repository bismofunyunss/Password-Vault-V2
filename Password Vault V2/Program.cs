using System.Runtime.InteropServices;

namespace Password_Vault_V2;

internal static class Program
{
    private static PasswordVault? _mainForm;
    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

    private const int DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;
    [STAThread]
    private static void Main()
    {
        // Initialize global handlers first
        Application.ThreadException += (sender, args) =>
        {
            try
            {
                Crypto.MasterKey.Dispose();
                _mainForm?.Vars.VaultControls.PassVault.Rows.Clear();
            }
            catch (Exception e)
            {
                ErrorLogging.ErrorLog(e);
            }

            if (args.Exception is { } ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ErrorLogging.ErrorLog(new Exception("Unhandled non-Exception object thrown."));
                MessageBox.Show("An unknown error occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            try
            {
                Crypto.MasterKey.Dispose();
                _mainForm?.Vars.VaultControls.PassVault.Rows.Clear();
            }
            catch (Exception e)
            {
                ErrorLogging.ErrorLog(e);
            }

            if (args.ExceptionObject is Exception ex)
            {
                ErrorLogging.ErrorLog(ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ErrorLogging.ErrorLog(new Exception("Unhandled non-Exception object thrown."));
                MessageBox.Show("An unknown error occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        Application.ApplicationExit += OnApplicationExit;
        SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Now start the application
        _mainForm = new PasswordVault();
        Application.Run(_mainForm);
    }

    private static void OnApplicationExit(object? sender, EventArgs e)
    {
        try
        {
            Crypto.MasterKey.Dispose();
            _mainForm?.Vars.VaultControls.PassVault.Rows.Clear();
            FipsCrypto.FipsEnabled = false;
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
        }
    }
}