using System.Security.Cryptography;
using System.Text;
using static Password_Vault_V2.Crypto;

namespace Password_Vault_V2;

public partial class Vault : UserControl
{
    private static readonly CancellationTokenSource _tokenSource = new();

    public Vault()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Enables the UI controls related to vault management.
    /// </summary>
    private void EnableUi()
    {
        UiController.LogicMethods.EnableUi(AddRowBtn, DeleteRowBtn, SaveVaultBtn);
    }

    /// <summary>
    /// Disables the UI controls related to vault management.
    /// </summary>
    private void DisableUi()
    {
        UiController.LogicMethods.DisableUi(AddRowBtn, DeleteRowBtn, SaveVaultBtn);
    }

    /// <summary>
    /// Encrypts the vault data by deriving a vault key using a randomly generated salt with HKDF,
    /// then encrypting the vault bytes and prepending the salt to the encrypted output.
    /// </summary>
    /// <param name="vaultBytes">The raw vault data bytes to encrypt.</param>
    /// <param name="masterKey">The master key used for HKDF key derivation.</param>
    /// <returns>A task that represents the asynchronous encryption operation.
    /// The task result contains the encrypted vault data prefixed with the salt.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="vaultBytes"/> or <paramref name="masterKey"/> is null.</exception>
    private static async Task<byte[]> EncryptVaultWithSalt(byte[] vaultBytes, byte[] masterKey)
    {
        var hkdfSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);

        // Derive vault key using the per-vault salt
        var vaultKey = Crypto.HKDF.HkdfDerivePinned(masterKey, hkdfSalt, "vault key"u8.ToArray(), CryptoConstants.KeySize);

        // Pass hkdfSalt to EncryptFile
        var encryptedVault = await EncryptFile(vaultBytes, vaultKey, hkdfSalt);

        // Prepend salt to encrypted vault
        var result = hkdfSalt.Concat(encryptedVault).ToArray();

        CryptoUtilities.ClearMemoryNative(vaultKey, vaultBytes, encryptedVault);

        return result;
    }

    /// <summary>
    /// Decrypts the encrypted vault data by extracting the salt, deriving the vault key with HKDF,
    /// and decrypting the ciphertext.
    /// </summary>
    /// <param name="encryptedVault">The encrypted vault data, with the salt prepended.</param>
    /// <param name="masterKey">The master key used for HKDF key derivation.</param>
    /// <returns>A task that represents the asynchronous decryption operation.
    /// The task result contains the decrypted vault bytes.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <paramref name="encryptedVault"/> is shorter than the expected salt size.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="encryptedVault"/> or <paramref name="masterKey"/> is null.</exception>
    private static async Task<byte[]> DecryptVaultWithSalt(byte[] encryptedVault, byte[] masterKey)
    {
        if (encryptedVault is { Length: < CryptoConstants.SaltSize })
            throw new InvalidOperationException("Encrypted data is too short to contain a valid salt.");

        var hkdfSalt = new byte[CryptoConstants.SaltSize];
        Buffer.BlockCopy(encryptedVault, 0, hkdfSalt, 0, hkdfSalt.Length);

        var ciphertextLength = encryptedVault.Length - hkdfSalt.Length;
        var ciphertext = new byte[ciphertextLength];
        Buffer.BlockCopy(encryptedVault, hkdfSalt.Length, ciphertext, 0, ciphertextLength);

        // Derive vault key using same salt
        var vaultKey = Crypto.HKDF.HkdfDerivePinned(masterKey, hkdfSalt, "vault key"u8.ToArray(), CryptoConstants.KeySize);

        // Pass salt to DecryptFile
        var decryptedBytes = await DecryptFile(ciphertext, vaultKey, hkdfSalt);

        CryptoUtilities.ClearMemoryNative(vaultKey, ciphertext);

        return decryptedBytes;
    }

    /// <summary>
    /// Loads and decrypts the current user's vault file into the password vault UI table.
    /// </summary>
    /// <remarks>
    /// This method reads the encrypted vault file from disk, decrypts it using the master key,
    /// and populates the <c>PassVault</c> DataGridView with the decrypted data.
    /// </remarks>
    /// <exception cref="FileNotFoundException">Thrown if the vault file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the vault cannot be decrypted or is empty.</exception>
    /// <exception cref="CryptographicException">Thrown if a cryptographic operation fails during decryption.</exception>
    /// <example>
    /// <code>
    /// LoadVault();
    /// </code>
    /// </example>
    public async void LoadVault()
    {
        try
        {
            var filePath = UserFileManager.GetUserVault(UserFileManager.CurrentLoggedInUser);
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Vault file does not exist.");

            var encryptedVault = await IO.ReadFile(filePath);

            var masterKey = MasterKey.GetKey();

            var decryptedVaultBytes = await DecryptVaultWithSalt(encryptedVault, masterKey);

            CryptoUtilities.ClearMemoryNative(encryptedVault);

            if (decryptedVaultBytes == null || decryptedVaultBytes.Length == 0)
                throw new InvalidOperationException("Unable to decrypt vault.");

            var decryptedText = Encoding.UTF8.GetString(decryptedVaultBytes);
            CryptoUtilities.ClearMemoryNative(decryptedVaultBytes);

            var lines = decryptedText.Split(["\r\n", "\n"], StringSplitOptions.None);
            PassVault.Rows.Clear();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var values = line.Split('\t');
                if (values.Length == 0)
                    continue;

                var rowIndex = PassVault.Rows.Add();
                for (var i = 0; i < values.Length; i++)
                    PassVault.Rows[rowIndex].Cells[i].Value = values[i];
            }
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show("An error occured when loading vault file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        catch (CryptographicException ex)
        {
            MessageBox.Show("A cryptographic error occured when loading vault file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occured when loading vault file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    /// <summary>
    /// Handles the click event for the Add Row button.
    /// </summary>
    /// <param name="sender">The source of the event, typically the AddRowBtn.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    /// <remarks>
    /// Adds a new row to the <c>PassVault</c> DataGridView. 
    /// Throws an error if no user is currently logged in.
    /// </remarks>
    /// <exception cref="Exception">Thrown when no user is logged in.</exception>
    private void AddRowBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new Exception("No user is currently logged in.");

            PassVault.Rows.Add();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    /// <summary>
    /// Handles the click event for the Delete Row button.
    /// </summary>
    /// <param name="sender">The source of the event, typically the DeleteRowBtn.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    /// <remarks>
    /// Deletes the currently selected row in the <c>PassVault</c> DataGridView.
    /// Throws an error if no user is logged in.
    /// </remarks>
    /// <exception cref="Exception">Thrown when no user is logged in.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid row index is selected.</exception>
    private void DeleteRowBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
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

    /// <summary>
    /// Handles the click event for the Save Vault button.
    /// </summary>
    /// <param name="sender">The source of the event, typically the SaveVaultBtn.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    /// <remarks>
    /// Serializes the contents of the <c>PassVault</c> DataGridView,
    /// encrypts it using the master key, and saves it to the user’s vault file path.
    /// Displays warnings and disables UI elements to prevent corruption during the save process.
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown when no user is logged in or when encryption or file writing fails.
    /// </exception>
    private async void SaveVaultBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new Exception("There is no user currently logged in.");

            MessageBox.Show(
                "Do NOT close the program while saving. This may cause corrupted data that is NOT recoverable. You may only save once per login." +
                "You will need to log back in in order to load vault contents.",
                "Info", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new Exception("No user is currently logged in.");

            DisableUi();

            var vaultPlaintext = SerializeVaultToText();
            var vaultBytes = Encoding.UTF8.GetBytes(vaultPlaintext);

            var masterKey = MasterKey.GetKey();

            var encryptedVault = await EncryptVaultWithSalt(vaultBytes, masterKey);

            if (encryptedVault == null || encryptedVault.Length == 0)
                throw new Exception("Encryption failed. Vault not saved.");

            var userVaultPath = UserFileManager.GetUserVault(UserFileManager.CurrentLoggedInUser);
            await IO.WriteFile(userVaultPath, encryptedVault);

            CryptoUtilities.ClearMemoryNative(vaultBytes);
            CryptoUtilities.ClearMemoryNative(encryptedVault);

            outputLbl.Text = "Vault saved successfully";
            outputLbl.ForeColor = Color.LimeGreen;
            MessageBox.Show("Vault saved successfully.", "Save vault", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            EnableUi();
            outputLbl.Text = "Idle...";
            outputLbl.ForeColor = Color.WhiteSmoke;
            SaveVaultBtn.Enabled = false;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        }
    }

    /// <summary>
    /// Serializes the contents of the <c>PassVault</c> DataGridView into a tab-delimited string format.
    /// </summary>
    /// <returns>
    /// A <see cref="string"/> containing the serialized vault data, with each row separated by a newline
    /// and each cell separated by a tab character.
    /// </returns>
    /// <remarks>
    /// This method skips the placeholder row used for adding new entries (<see cref="DataGridViewRow.IsNewRow"/>).
    /// </remarks>
    private string SerializeVaultToText()
    {
        var sb = new StringBuilder();

        foreach (DataGridViewRow row in PassVault.Rows)
        {
            if (row.IsNewRow) continue;

            var cells = row.Cells.Cast<DataGridViewCell>()
                .Select(cell => cell.Value?.ToString() ?? string.Empty);

            sb.AppendLine(string.Join("\t", cells));
        }

        return sb.ToString();
    }
}