using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Password_Vault_V2.Crypto;

namespace Password_Vault_V2;

public partial class Encryption : UserControl
{
    private static CancellationTokenSource _encryptAnimationSource = new();
    private static readonly CancellationToken EncryptAnimationToken = _encryptAnimationSource.Token;
    private static CancellationTokenSource _decryptAnimationSource = new();
    private static readonly CancellationToken DecryptAnimationToken = _decryptAnimationSource.Token;
    private static readonly byte[] FileSignature = "v1.0"u8.ToArray(); // 4 bytes

    public Encryption()
    {
        InitializeComponent();
    }

    private async void DecryptingAnimation()
    {
        await UiController.Animations.AnimateLabel(FileOutputLbl, "Decrypting file", DecryptAnimationToken);
    }

    private async void EncryptingAnimation()
    {
        await UiController.Animations.AnimateLabel(FileOutputLbl, "Encrypting file", EncryptAnimationToken);
    }

    private async void ImportFileBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Authentication.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*",
                Title = "Select a file to encrypt/decrypt.",
                FilterIndex = 1,
                ShowHiddenFiles = true,
                CheckFileExists = true,
                CheckPathExists = true,
                RestoreDirectory = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = openFileDialog.FileName;
            var fileInfo = new FileInfo(selectedFileName);

            if (fileInfo.Length > FileProcessingConstants.MaximumFileSize)
                throw new ArgumentException("File size exceeds the maximum allowed limit.");

            FileProcessingConstants.FileOpened = true;
            FileProcessingConstants.LoadedFile = selectedFileName;
            FileProcessingConstants.Result = await File.ReadAllBytesAsync(selectedFileName);

            // Validate that the file has been read correctly
            if (FileProcessingConstants.Result.Length == 0)
                throw new InvalidOperationException("The file is empty or cannot be read.");

            FileProcessingConstants.FileExtension = fileInfo.Extension.ToLower();
            FileProcessingConstants.FileSize = (int)fileInfo.Length;

            FileSizeNumLbl.Text = $"{fileInfo.Length:#,0} bytes";
            FileOutputLbl.Text = "File opened.";
            FileOutputLbl.ForeColor = Color.LimeGreen;
            FileProcessingConstants.IsDecrypted = false;
            FileProcessingConstants.IsEncrypted = false;

            MessageBox.Show("File opened successfully.", "Opened successfully", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show("Access to the file was denied.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred while opening the file.", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
    }


    private async void ExportFileBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Authentication.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new InvalidOperationException("No file is opened.");

            using var saveFileDialog = new SaveFileDialog();

            if (FileProcessingConstants.IsEncrypted)
            {
                saveFileDialog.Filter = "Encrypted files (*.encrypted)|*.encrypted";
                saveFileDialog.DefaultExt = ".encrypted";
                saveFileDialog.FilterIndex = 1;
            }
            else
            {
                saveFileDialog.Filter =
                    $"{FileProcessingConstants.FileExtension.ToUpper()} files (*.{FileProcessingConstants.FileExtension})|*.{FileProcessingConstants.FileExtension}|All Files (*.*)|*.*";
                saveFileDialog.DefaultExt = $".{FileProcessingConstants.FileExtension}";
                saveFileDialog.FilterIndex = 1; // Default to your file type, not "All Files"
            }

            saveFileDialog.ShowHiddenFiles = true;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = false;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = saveFileDialog.FileName;

            // Ensure file extension is correct
            if (string.IsNullOrEmpty(Path.GetExtension(selectedFileName)))
                selectedFileName = Path.ChangeExtension(selectedFileName, FileProcessingConstants.FileExtension);

            // Check if encrypted file is saved with the correct extension
            if (FileProcessingConstants.IsEncrypted &&
                !selectedFileName.EndsWith(".encrypted", StringComparison.OrdinalIgnoreCase))
                selectedFileName = Path.ChangeExtension(selectedFileName, ".encrypted");

            if (FileProcessingConstants.Result.Length == 0)
                throw new InvalidOperationException("There is no data to write to the file.");

            await using var fs = new FileStream(selectedFileName, FileMode.Create, FileAccess.Write);
            await fs.WriteAsync(FileProcessingConstants.Result.AsMemory(0, FileProcessingConstants.Result.Length));

            FileOutputLbl.Text = "File saved successfully.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show("File saved successfully.", "Saved successfully", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Cleanup state
            FileProcessingConstants.FileOpened = false;
            FileProcessingConstants.Result = [];
            FileSizeNumLbl.Text = "0 bytes";
            FileProcessingConstants.IsDecrypted = false;
            FileProcessingConstants.IsEncrypted = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred while saving the file.", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
    }

    private async void DecryptBtn_Click(object sender, EventArgs e)
    {
        byte[] customPassword = null;
        byte[] confirmPassword = null;
        byte[] decryptedPassword = null;
        GCHandle? handle = null;

        try
        {
            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable. " +
                "If using a custom password to decrypt, you MUST enter the same password used during encryption.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (string.IsNullOrEmpty(Authentication.CurrentLoggedInUser))
                throw new Exception("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new Exception("No file is opened.");

            if (string.IsNullOrEmpty(FileProcessingConstants.LoadedFile))
                throw new Exception("No file is selected.");

            if (FileProcessingConstants.IsDecrypted)
                throw new Exception("File is already decrypted. Unable to decrypt again." +
                                    "Either encrypt the file or export file.");

            DecryptingAnimation();

            var data = FileProcessingConstants.Result;
            var signature = FileSignature;
            var saltSize = CryptoConstants.SaltSize;
            var expectedHeaderLength = signature.Length + saltSize;

            if (data.Length < expectedHeaderLength)
                throw new Exception("Encrypted file is corrupted or missing required metadata.");

            // Validate signature
            var actualSignature = data.AsSpan(0, signature.Length);
            if (!actualSignature.SequenceEqual(signature))
                throw new Exception("Invalid file signature. This file may not be a valid encrypted file.");

            // Extract Argon2 salt
            var argonSalt = data.AsSpan(signature.Length, saltSize).ToArray();

            // Extract actual encrypted content
            var encryptedContent = data.AsSpan(expectedHeaderLength).ToArray();
            FileProcessingConstants.Result = encryptedContent;

            // Decrypt stored login password
            decryptedPassword = ProtectedData.Unprotect(
                CryptoConstants.SecurePassword,
                CryptoConstants.SecurePasswordSalt,
                DataProtectionScope.CurrentUser);

            handle = GCHandle.Alloc(decryptedPassword, GCHandleType.Pinned);

            var decryptedFile = await DecryptFile(
                Authentication.CurrentLoggedInUser,
                FileProcessingConstants.Result,
                decryptedPassword,
                argonSalt);

            if (decryptedFile.Length == 0)
                throw new Exception("There was an error while decrypting.");

            // Re-protect password
            var encryptedPassword = ProtectedData.Protect(
                decryptedPassword,
                CryptoConstants.SecurePasswordSalt,
                DataProtectionScope.CurrentUser);

            CryptoConstants.SecurePassword = encryptedPassword;

            await _decryptAnimationSource.CancelAsync();
            if (_decryptAnimationSource.IsCancellationRequested)
                _decryptAnimationSource = new CancellationTokenSource();

            FileProcessingConstants.Result = decryptedFile;

            FileOutputLbl.Text = "File decrypted.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show(
                "File was decrypted successfully. Don't forget to export and restore its original extension.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            var size = (long)FileProcessingConstants.Result.Length;
            FileSizeNumLbl.Text = $"{size:#,0} bytes";

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            FileProcessingConstants.IsEncrypted = false;
            FileProcessingConstants.IsDecrypted = true;
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            CustomPasswordTextBox.Clear();
            ConfirmPassword.Clear();

            CryptoUtilities.ClearMemory(customPassword);
            CryptoUtilities.ClearMemory(confirmPassword);

            FileOutputLbl.Text = "Error decrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            await _decryptAnimationSource.CancelAsync();
            if (_decryptAnimationSource.IsCancellationRequested)
                _decryptAnimationSource = new CancellationTokenSource();

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            if (handle is { IsAllocated: true })
                handle.Value.Free();

            if (customPassword != null) CryptoUtilities.ClearMemory(customPassword);
            if (confirmPassword != null) CryptoUtilities.ClearMemory(confirmPassword);
            if (decryptedPassword != null) CryptoUtilities.ClearMemory(decryptedPassword);
        }
    }

    private async void EncryptBtn_Click(object sender, EventArgs e)
    {
        byte[] customPassword = null;
        byte[] confirmPassword = null;
        byte[] decryptedPassword = null;
        GCHandle? handle = null;

        try
        {
            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable. " +
                "If using a custom password to encrypt with, MAKE SURE YOU REMEMBER THE PASSWORD! You will NOT be able to " +
                "decrypt the file without the password. It is separate than the password you use to login with.",
                "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (string.IsNullOrEmpty(Authentication.CurrentLoggedInUser))
                throw new Exception("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new Exception("No file is opened.");

            if (string.IsNullOrEmpty(FileProcessingConstants.LoadedFile))
                return;

            if (FileProcessingConstants.IsEncrypted)
                throw new Exception("File is already encrypted. Unable to encrypt again." +
                                    " Either decrypt the file or export file.");

            // Early check: Is file already encrypted based on header?
            await using (var fs = new FileStream(FileProcessingConstants.LoadedFile, FileMode.Open, FileAccess.Read,
                             FileShare.Read))
            {
                if (fs.Length >= FileSignature.Length)
                {
                    var header = new byte[FileSignature.Length];
                    var bytesRead = fs.Read(header, 0, FileSignature.Length);

                    if (bytesRead != FileSignature.Length)
                        throw new IOException("Failed to read the full file header.");

                    if (header.SequenceEqual(FileSignature))
                        throw new Exception(
                            "File is already encrypted. Unable to encrypt again. You will need to export the file.");
                }
            }

            EncryptingAnimation();

            decryptedPassword = ProtectedData.Unprotect(
                CryptoConstants.SecurePassword,
                CryptoConstants.SecurePasswordSalt,
                DataProtectionScope.CurrentUser);

            handle = GCHandle.Alloc(decryptedPassword, GCHandleType.Pinned);

            var input = await File.ReadAllBytesAsync(FileProcessingConstants.LoadedFile);
            var argonSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);

            var encryptedFile = await EncryptFile(
                Authentication.CurrentLoggedInUser,
                input,
                decryptedPassword,
                argonSalt);

            if (encryptedFile.Length == 0)
                throw new Exception("There was an error while encrypting.");

            var encryptedPassword = ProtectedData.Protect(
                decryptedPassword,
                CryptoConstants.SecurePasswordSalt,
                DataProtectionScope.CurrentUser);

            CryptoConstants.SecurePassword = encryptedPassword;

            // Add signature + salt to encrypted file
            encryptedFile = [.. FileSignature, .. argonSalt, .. encryptedFile];

            await _encryptAnimationSource.CancelAsync();
            if (_encryptAnimationSource.IsCancellationRequested)
                _encryptAnimationSource = new CancellationTokenSource();

            FileProcessingConstants.Result = encryptedFile;

            FileOutputLbl.Text = "File encrypted.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show(
                "File was encrypted successfully. You may now export the encrypted file. To decrypt, you will open the encrypted file.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            var size = (long)FileProcessingConstants.Result.Length;
            FileSizeNumLbl.Text = $"{size:#,0} bytes";

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            FileProcessingConstants.IsEncrypted = true;
            FileProcessingConstants.IsDecrypted = false;
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            CustomPasswordTextBox.Clear();
            ConfirmPassword.Clear();

            await _encryptAnimationSource.CancelAsync();
            if (_encryptAnimationSource.IsCancellationRequested)
                _encryptAnimationSource = new CancellationTokenSource();

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            if (handle is { IsAllocated: true })
                handle.Value.Free();

            if (customPassword != null) CryptoUtilities.ClearMemory(customPassword);
            if (confirmPassword != null) CryptoUtilities.ClearMemory(confirmPassword);
            if (decryptedPassword != null) CryptoUtilities.ClearMemory(decryptedPassword);
        }
    }


    private void CustomPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        if (CustomPasswordCheckBox.Checked)
        {
            CustomPasswordTextBox.Enabled = true;
            ConfirmPassword.Enabled = true;
        }
        else
        {
            CustomPasswordTextBox.Enabled = false;
            ConfirmPassword.Enabled = false;
        }
    }

    /// <summary>
    ///     Represents static fields and constants used for file processing and UI interactions.
    /// </summary>
    private static class FileProcessingConstants
    {
        public const int MaximumFileSize = 1_000_000_000;
        public static string LoadedFile = string.Empty;
        public static byte[] Result = [];
        public static string FileExtension = string.Empty;
        public static bool IsEncrypted;
        public static bool IsDecrypted;
        public static bool FileOpened { get; set; }
        public static long FileSize { get; set; }
    }
}