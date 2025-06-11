namespace Password_Vault_V2;

internal static class Program
{
    private static PasswordVault? _mainForm;

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

        // Now start the application
        ApplicationConfiguration.Initialize();
        _mainForm = new PasswordVault();
        Application.Run(_mainForm);
    }

    private static void OnApplicationExit(object? sender, EventArgs e)
    {
        try
        {
            Crypto.MasterKey.Dispose();
            _mainForm?.Vars.VaultControls.PassVault.Rows.Clear();
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
        }
    }
}