using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Password_Vault_V2;

public partial class Vault : UserControl
{
    private static CancellationTokenSource _tokenSource = new();
    private static CancellationToken Token => _tokenSource.Token;

    public Vault()
    {
        InitializeComponent();
    }

    private void EnableUi()
    {
        UiController.LogicMethods.EnableUi(AddRowBtn, DeleteRowBtn, SaveVaultBtn);
    }

    private void DisableUi()
    {
        UiController.LogicMethods.DisableUi(AddRowBtn, DeleteRowBtn, SaveVaultBtn);
    }

    private async void StartAnimation()
    {
        await UiController.Animations.AnimateLabel(outputLbl, "Saving vault", Token);
    }

    /// <summary>
    ///     Asynchronously loads the user's vault data into the PassVault DataGridView.
    /// </summary>
    public async void LoadVault()
    {
        try
        {
            var filePath = Authentication.GetUserVault(Authentication.CurrentLoggedInUser);

            if (File.Exists(filePath))
            {
                using var sr = new StreamReader(filePath);
                PassVault.Rows.Clear();

                while (!sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    var values = line?.Split('\t');

                    if (IsBase64(line))
                        throw new ArgumentException("Invalid input text", nameof(line));
                    if (values is { Length: <= 0 })
                        continue;

                    var rowIndex = PassVault.Rows.Add();

                    if (values == null)
                        continue;

                    int index;
                    for (index = 0; index < values.Length; index++)
                        PassVault.Rows[rowIndex].Cells[index].Value = values[index];

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                    GC.WaitForPendingFinalizers();
                }
            }
            else
            {
                throw new Exception("Vault file does not exist.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    [GeneratedRegex(@"^[a-zA-Z0-9\+/]*={0,3}$")]
    private static partial Regex MyRegex();

    private static bool IsBase64(string? str)
    {
        return str != null && MyRegex().IsMatch(str) && str.Length % 4 == 0;
    }

    private void AddRowBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            PassVault.Rows.Add();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    private void DeleteRowBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            if (PassVault.SelectedRows.Count <= 0)
                return;
            var selectedRow = PassVault.SelectedRows[0].Index;

            PassVault.Rows.RemoveAt(selectedRow);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    private async void SaveVaultBtn_Click(object sender, EventArgs e)
    {
        byte[] decryptedPassword = [];
        var handle = GCHandle.Alloc(decryptedPassword, GCHandleType.Pinned);

        try
        {
            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
                "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            StartAnimation();
            DisableUi();

            var filePath = Path.Combine("Password Vault", "Users",
                Authentication.GetUserVault(Authentication.CurrentLoggedInUser));

            await using (var sw = new StreamWriter(filePath))
            {
                sw.NewLine = null;
                sw.AutoFlush = true;
                foreach (DataGridViewRow row in PassVault.Rows)
                {
                    for (var i = 0; i < PassVault.Columns.Count; i++)
                    {
                        row.Cells[i].ValueType = typeof(char[]);
                        sw.Write(row.Cells[i].Value);
                        if (i < PassVault.Columns.Count - 1)
                            await sw.WriteAsync("\t");
                    }

                    await sw.WriteLineAsync();
                }
            }

            decryptedPassword = ProtectedData.Unprotect(Crypto.CryptoConstants.SecurePassword,
                Crypto.CryptoConstants.SecurePasswordSalt, DataProtectionScope.CurrentUser);

            Crypto.CryptoConstants.SecurePassword = decryptedPassword;

            var encryptedVault = await Crypto.EncryptFile(Authentication.CurrentLoggedInUser,
                    Crypto.CryptoConstants.SecurePassword,
                Authentication.GetUserVault(Authentication.CurrentLoggedInUser));

            var encryptedPassword = ProtectedData.Protect(decryptedPassword, Crypto.CryptoConstants.SecurePasswordSalt,
                DataProtectionScope.CurrentUser);

            Crypto.CryptoConstants.SecurePassword = encryptedPassword;

            if (encryptedVault == Array.Empty<byte>())
            {
                Crypto.CryptoUtilities.ClearMemory(decryptedPassword);
                throw new Exception("There was an error while trying to save.");
            }

            var encryptedVaultString = DataConversionHelpers.ByteArrayToBase64String(encryptedVault);
            await File.WriteAllTextAsync(Authentication.GetUserVault(Authentication.CurrentLoggedInUser),
                encryptedVaultString);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            outputLbl.Text = "Vault saved successfully";
            outputLbl.ForeColor = Color.LimeGreen;
            MessageBox.Show("Vault saved successfully.", "Save vault", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            EnableUi();
            outputLbl.Text = "Idle...";
            outputLbl.ForeColor = Color.WhiteSmoke;
        }
        catch (Exception ex)
        {
            Crypto.CryptoUtilities.ClearMemory(decryptedPassword);
            await _tokenSource.CancelAsync();

            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            EnableUi();
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            outputLbl.Text = "Idle...";
            outputLbl.ForeColor = Color.White;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            await _tokenSource.CancelAsync();
            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            Crypto.CryptoUtilities.ClearMemory(decryptedPassword);
            handle.Free();
        }
    }
}