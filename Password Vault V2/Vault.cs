using System.Text.RegularExpressions;

namespace Password_Vault_V2;

public partial class Vault : UserControl
{
    public Vault()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Represents static fields and constants used for file processing and UI interactions.
    /// </summary>
    public static class FileProcessingConstants
    {
        public const int MaximumFileSize = 1_000_000_000;
        public static char[] PasswordArray = [];
        public static string LoadedFile = string.Empty;
        public static string LoadedFileToHash = string.Empty;
        public static string Result = string.Empty;
        public static bool IsAnimating { get; set; }
        public static bool FileOpened { get; set; }
        public static long FileSize { get; set; }

        public static readonly ToolTip Tooltip = new();
    }

    private void EnableUi()
    {
        foreach (Control c in this.Controls)
            c.Enabled = true;
    }

    private void DisableUi()
    {
        foreach (Control c in this.Controls)
            c.Enabled = false;
    }

    /// <summary>
    /// Initiates the animation for encryption.
    /// </summary>
    private async void StartAnimationEncryption()
    {
        FileProcessingConstants.IsAnimating = true;
        await AnimateLabelEncrypt();
    }

    /// <summary>
    /// Initiates the animation for decryption.
    /// </summary>
    private async void StartAnimationDecryption()
    {
        FileProcessingConstants.IsAnimating = true;
        await AnimateLabelDecrypt();
    }

    /// <summary>
    /// Asynchronously animates the label to indicate file encryption progress.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AnimateLabelEncrypt()
    {
        while (FileProcessingConstants.IsAnimating)
        {
            outputLbl.Text = @"Encrypting file";
            for (var i = 0; i < 4; i++)
            {
                outputLbl.Text += @".";
                await Task.Delay(400);
            }
        }
    }

    /// <summary>
    /// Asynchronously animates the label to indicate file decryption progress.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task AnimateLabelDecrypt()
    {
        while (FileProcessingConstants.IsAnimating)
        {
            outputLbl.Text = @"Decrypting file";
            for (var i = 0; i < 4; i++)
            {
                outputLbl.Text += @".";
                await Task.Delay(400);
            }
        }
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

                    if (values != null)
                        for (var i = 0; i < values.Length; i++)
                            PassVault.Rows[rowIndex].Cells[i].Value = values[i];
                }
            }
            else
            {
                MessageBox.Show("Vault file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Crypto.CryptoUtilities.ClearMemory(FileProcessingConstants.PasswordArray);
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
    }

    private void DeleteRowBtn_Click(object sender, EventArgs e)
    {

    }

    private async void SaveVaultBtn_Click(object sender, EventArgs e)
    {
        try
        {
            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
                "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

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

            var encryptedVault = await Crypto.EncryptFile(Authentication.CurrentLoggedInUser,
                Crypto.CryptoConstants.SecurePassword,
                Authentication.GetUserVault(Authentication.CurrentLoggedInUser));

            if (encryptedVault == Array.Empty<byte>())
                throw new ArgumentException("Value was empty.", nameof(encryptedVault));

            var encryptedVaultString = DataConversionHelpers.ByteArrayToBase64String(encryptedVault);
            await File.WriteAllTextAsync(Authentication.GetUserVault(Authentication.CurrentLoggedInUser),
                encryptedVaultString);


            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            FileProcessingConstants.IsAnimating = false;
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
            EnableUi();
            FileProcessingConstants.IsAnimating = false;
            Crypto.CryptoUtilities.ClearMemory(FileProcessingConstants.PasswordArray);

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }
}