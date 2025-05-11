using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;

namespace Password_Vault_V2;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new PasswordVault());
    }
}