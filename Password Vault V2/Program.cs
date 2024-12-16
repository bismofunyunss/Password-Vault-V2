using Secure_Password_Vault;
using System.Diagnostics;

namespace Password_Vault_V2;

internal static class Program
{
    [STAThread]
    static async Task Main()
    {
        try
        {
            var checksTask = Task.Run(() => Checks());
            ApplicationConfiguration.Initialize();
            Application.Run(new PasswordVault());
            await checksTask;
        }
        catch (Exception ex)
        {
            MessageBox.Show("A critical error occurred, and the application must close.",
                "Critical Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.ExitThread();
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Performs continuous checks and terminates the process after a random delay.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task Checks()
    {
        while (true)
        {
            var result = await AntiTamper.PerformChecks();

            if (result)
                break;
        }

        await Task.Delay(Crypto.CryptoUtilities.BoundedInt(5000, 15000));
        Application.ExitThread();
        Environment.Exit(0); 
    }
}