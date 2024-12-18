using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Password_Vault_V2;

public partial class Encryption : UserControl
{
    private static CancellationTokenSource _tokenSource = new();
    private static readonly CancellationToken Token = _tokenSource.Token;

    public Encryption()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Represents static fields and constants used for file processing and UI interactions.
    /// </summary>
    private static class FileProcessingConstants
    {
        public const int MaximumFileSize = 1_000_000_000;
        public static byte[] PasswordArray = [];
        public static byte[] EncryptedPassword = [];
        public static byte[] DecryptedPassword = [];
        public static string LoadedFile = string.Empty;
        public static byte[] Result = [];
        public static string FileExtension = string.Empty;

        public static bool FileOpened { get; set; }
        public static long FileSize { get; set; }
    }

    private async void DecryptingAnimation()
    {
        await UiController.Animations.AnimateLabel(FileOutputLbl, "Decrypting file", Token);
    }

    private async void EncryptingAnimation()
    {
        await UiController.Animations.AnimateLabel(FileOutputLbl, "Encrypting file", Token);
    }

    private async void ExportFileBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new Exception("No file is opened.");

            using var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = $"Encrypted Files (*.encrypted)|*.encrypted|{FileProcessingConstants.FileExtension.ToUpper()}" +
                $" Files (*.{FileProcessingConstants.FileExtension})|*.{FileProcessingConstants.FileExtension}";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.ShowHiddenFiles = true;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = false;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.InitialDirectory =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = saveFileDialog.FileName;

            if (FileProcessingConstants.Result.Length == 0)
                return;

            await using (var fs = new FileStream(selectedFileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var fileBytes = FileProcessingConstants.Result;
                await fs.WriteAsync(fileBytes, 0, fileBytes.Length);
            }

            FileOutputLbl.Text = "File saved successfully.";
            FileOutputLbl.ForeColor = Color.LimeGreen;
            MessageBox.Show("File saved successfully.", "Saved successfully", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            FileProcessingConstants.Result = [];

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
        catch (Exception ex)
        {
            FileOutputLbl.Text = "Error saving file.";
            FileOutputLbl.ForeColor = Color.Red;
            ErrorLogging.ErrorLog(ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
    }

    private async void DecryptBtn_Click(object sender, EventArgs e)
    {
        var handle = GCHandle.Alloc(FileProcessingConstants.PasswordArray, GCHandleType.Pinned);
        byte[] customPassword = [];
        byte[] confirmPassword = [];

        try
        {
            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable. " +
                "If using a custom password to encrypt with, MAKE SURE YOU REMEMBER THE PASSWORD! You will NOT be able to " +
                "decrypt the file without the password. It is separate than the password you use to login with.",
                "Info",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new Exception("No file is opened.");

            if (FileProcessingConstants.LoadedFile == string.Empty)
                return;

            DecryptingAnimation();

            FileProcessingConstants.PasswordArray = ProtectedData.Unprotect(Crypto.CryptoConstants.SecurePassword,
                Crypto.CryptoConstants.SecurePasswordSalt, DataProtectionScope.CurrentUser);

            if (CustomPasswordCheckBox.Checked)
            {
                customPassword = Encoding.UTF8.GetBytes(CustomPasswordTextBox.Text);
                confirmPassword = Encoding.UTF8.GetBytes(ConfirmPassword.Text);

                if (!customPassword.SequenceEqual(confirmPassword))
                    throw new Exception("Both passwords must match.");

                FileProcessingConstants.PasswordArray = customPassword;
            }
            
            var decryptedFile =
                await Crypto.DecryptNonTxt(Authentication.CurrentLoggedInUser, FileProcessingConstants.LoadedFile,
                    FileProcessingConstants.PasswordArray);

            var encryptedPassword = ProtectedData.Protect(FileProcessingConstants.PasswordArray, Crypto.CryptoConstants.SecurePasswordSalt,
    DataProtectionScope.CurrentUser);

            Crypto.CryptoConstants.SecurePassword = encryptedPassword;

            if (decryptedFile == Array.Empty<byte>())
                throw new Exception("There was an error while decrypting.");

            FileProcessingConstants.Result = decryptedFile;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            FileOutputLbl.Text = "File decrypted.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            await _tokenSource.CancelAsync();
            Crypto.CryptoUtilities.ClearMemory(FileProcessingConstants.DecryptedPassword);

            MessageBox.Show(
                "File was decrypted successfully. Don't forget to export the file and change the extension to the original one.",
                "Success", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            var size = (long)FileProcessingConstants.Result.Length;
            var fileSize = size.ToString("#,0");
            FileSizeNumLbl.Text = $"{fileSize} bytes";

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
        catch (OperationCanceledException ex)
        {
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            if (CustomPasswordTextBox.Text != string.Empty)
            {
                CustomPasswordTextBox.Clear();
                ConfirmPassword.Clear();
            }

            var arrays = new[]
            {
                customPassword, confirmPassword, FileProcessingConstants.PasswordArray, FileProcessingConstants.DecryptedPassword
            };
            Crypto.CryptoUtilities.ClearMemory(arrays);

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;
            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            await _tokenSource.CancelAsync();
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            handle.Free();
            var arrays = new[]
            {
                customPassword, confirmPassword, FileProcessingConstants.PasswordArray, FileProcessingConstants.DecryptedPassword
            };
            Crypto.CryptoUtilities.ClearMemory(arrays);
        }
    }

    private void ImportFileBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            using var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files(*.*) | *.*";
            openFileDialog.Title = "Select a file to encrypt.";
            openFileDialog.FilterIndex = 1;
            openFileDialog.ShowHiddenFiles = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory =
               Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = openFileDialog.FileName;
            var fileInfo = new FileInfo(selectedFileName);

            FileProcessingConstants.FileOpened = true;
            FileProcessingConstants.LoadedFile = selectedFileName;

            FileProcessingConstants.FileExtension = fileInfo.Extension.ToLower();

#pragma warning disable CA2208

            if (string.IsNullOrEmpty(selectedFileName))
                throw new ArgumentException("Value was empty.", nameof(selectedFileName));

            if (fileInfo.Length > FileProcessingConstants.MaximumFileSize)
                throw new ArgumentException("File size is too large.", nameof(FileProcessingConstants.FileSize));

#pragma warning restore

            FileProcessingConstants.FileSize = (int)fileInfo.Length;

            var fileSize = fileInfo.Length.ToString("#,0");
            FileSizeNumLbl.Text = $"{fileSize} bytes";

            FileOutputLbl.Text = "File opened.";
            FileOutputLbl.ForeColor = Color.LimeGreen;
            MessageBox.Show("File opened successfully.", "Opened successfully", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            FileProcessingConstants.Result = [];
        }
        catch (AccessViolationException ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FileSizeNumLbl.Text = "0";
            FileOutputLbl.Text = "Error loading file.";
            FileOutputLbl.ForeColor = Color.Red;
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FileSizeNumLbl.Text = "0";
            FileOutputLbl.Text = "Error loading file.";
            FileOutputLbl.ForeColor = Color.Red;
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            ErrorLogging.ErrorLog(ex);
        }
    }

    private async void EncryptBtn_Click(object sender, EventArgs e)
    {
        var handle = GCHandle.Alloc(FileProcessingConstants.PasswordArray, GCHandleType.Pinned);
        byte[] customPassword = [];
        byte[] confirmPassword = [];

        try
        {
            MessageBox.Show(
            "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable. " +
            "If using a custom password to encrypt with, MAKE SURE YOU REMEMBER THE PASSWORD! You will NOT be able to " +
            "decrypt the file without the password. It is separate than the password you use to login with.",
            "Info",
            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new Exception("No file is opened.");

            if (FileProcessingConstants.LoadedFile == string.Empty)
                return;

            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            EncryptingAnimation();

            FileProcessingConstants.DecryptedPassword = ProtectedData.Unprotect(FileProcessingConstants.PasswordArray,
     Crypto.CryptoConstants.SecurePasswordSalt, DataProtectionScope.CurrentUser);

            FileProcessingConstants.PasswordArray = FileProcessingConstants.DecryptedPassword;

            if (CustomPasswordCheckBox.Checked)
            {
                customPassword = Encoding.UTF8.GetBytes(CustomPasswordTextBox.Text);
                confirmPassword = Encoding.UTF8.GetBytes(ConfirmPassword.Text);

                if (!customPassword.SequenceEqual(confirmPassword))
                    throw new Exception("Both passwords must match.");

                FileProcessingConstants.PasswordArray = customPassword;
            }


            var encryptedFile =
                await Crypto.EncryptNonTxt(Authentication.CurrentLoggedInUser, FileProcessingConstants.LoadedFile,
                    FileProcessingConstants.PasswordArray);

            FileProcessingConstants.EncryptedPassword = ProtectedData.Protect(FileProcessingConstants.DecryptedPassword, Crypto.CryptoConstants.SecurePasswordSalt,
DataProtectionScope.CurrentUser);

            FileProcessingConstants.PasswordArray = FileProcessingConstants.EncryptedPassword;

            if (encryptedFile == Array.Empty<byte>())
                throw new Exception("There was an error while decrypting.");

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            FileProcessingConstants.Result = encryptedFile;

            FileOutputLbl.Text = "File encrypted.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            await _tokenSource.CancelAsync();
            Crypto.CryptoUtilities.ClearMemory(FileProcessingConstants.DecryptedPassword);

            MessageBox.Show(
                "File was encrypted successfully. You MUST export the file and import it again to decrypt it.",
                "Success", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            var size = (long)FileProcessingConstants.Result.Length;
            var fileSize = size.ToString("#,0");
            FileSizeNumLbl.Text = $"{fileSize} bytes";

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
        catch (Exception ex)
        {
            if (CustomPasswordTextBox.Text != string.Empty)
            {
                CustomPasswordTextBox.Clear();
                ConfirmPassword.Clear();
            }

            var arrays = new[]
            {
                customPassword, confirmPassword, FileProcessingConstants.PasswordArray, FileProcessingConstants.DecryptedPassword
            };
            Crypto.CryptoUtilities.ClearMemory(arrays);

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;
            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();

            await _tokenSource.CancelAsync();
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            handle.Free();
            var arrays = new[]
            {
                customPassword, confirmPassword, FileProcessingConstants.PasswordArray, FileProcessingConstants.DecryptedPassword
            };
            Crypto.CryptoUtilities.ClearMemory(arrays);
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

    private void Encryption_Load(object sender, EventArgs e)
    {
        FileProcessingConstants.PasswordArray = Crypto.CryptoConstants.SecurePassword;
    }
}