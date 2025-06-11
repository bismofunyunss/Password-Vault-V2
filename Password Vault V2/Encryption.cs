using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using static Password_Vault_V2.Crypto;

namespace Password_Vault_V2;

public partial class Encryption : UserControl
{
    private static CancellationTokenSource _encryptAnimationSource = new();
    private static CancellationTokenSource _decryptAnimationSource = new();
    private static readonly byte[] MasterKey = Crypto.MasterKey.GetKey();

    public Encryption()
    {
        InitializeComponent();
    }

    private async void DecryptingAnimation()
    {
        await UiController.Animations.AnimateLabel(FileOutputLbl, "Decrypting file", _decryptAnimationSource.Token);
    }

    private async void EncryptingAnimation()
    {
        await UiController.Animations.AnimateLabel(FileOutputLbl, "Encrypting file", _encryptAnimationSource.Token);
    }

    private async void ImportFileBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
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
                throw new Exception("File size exceeds the maximum allowed limit.");

            FileProcessingConstants.FileOpened = true;
            FileProcessingConstants.LoadedFile = selectedFileName;
            FileProcessingConstants.Result = await IO.ReadFile(selectedFileName) ??
                                             throw new InvalidOperationException("Unable to read file.");

            // Validate that the file has been read correctly
            if (FileProcessingConstants.Result.Length == 0)
                throw new Exception("The file is empty or cannot be read.");

            FileProcessingConstants.FileExtension = fileInfo.Extension.ToLower(CultureInfo.CurrentCulture);
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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
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
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            if (!FileProcessingConstants.FileOpened)
                throw new InvalidOperationException("No file is opened.");

            using var saveFileDialog = new SaveFileDialog();

            // Determine file extension and filter
            string? extension;
            string filter;

            if (FileProcessingConstants.IsEncrypted)
            {
                extension = ".encrypted";
                filter = "Encrypted files (*.encrypted)|*.encrypted";
            }
            else
            {
                // Use original extension from decrypted metadata if available
                extension = FileProcessingConstants.OriginalExtension;
                if (!string.IsNullOrWhiteSpace(extension) && extension.StartsWith("."))
                    extension = $".{FileProcessingConstants.FileExtension}";

                var extNoDot = extension!.TrimStart('.');
                filter = $"{extNoDot.ToUpper()} files (*{extension})|*{extension}|All Files (*.*)|*.*";
            }

            saveFileDialog.Filter = filter;
            saveFileDialog.DefaultExt = extension;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.ShowHiddenFiles = true;
            saveFileDialog.CheckFileExists = false;
            saveFileDialog.CheckPathExists = false;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = saveFileDialog.FileName;

            // Ensure proper extension if user didn't add one manually
            if (string.IsNullOrEmpty(Path.GetExtension(selectedFileName)))
                selectedFileName = Path.ChangeExtension(selectedFileName, extension);
            else if (!selectedFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                selectedFileName = Path.ChangeExtension(selectedFileName, extension);

            if (FileProcessingConstants.Result.Length == 0)
                throw new InvalidOperationException("There is no data to write to the file.");

            await IO.WriteFile(selectedFileName, FileProcessingConstants.Result);

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
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        }
    }

    /// <summary>
    /// Handles the decryption of an encrypted file when the Decrypt button is clicked.
    /// </summary>
    /// <param name="sender">The source of the event (Decrypt button).</param>
    /// <param name="e">Event data associated with the button click.</param>
    /// <remarks>
    /// Performs several validations before decrypting the file, including checking whether a file is loaded, 
    /// whether it has already been decrypted, and whether a user is logged in.
    /// 
    /// If decryption is successful, updates the UI and internal flags accordingly. 
    /// Displays informative message boxes to guide the user. 
    /// Logs errors with full exception details for debugging.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no user is logged in, no file is opened, or the file has already been decrypted.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the file path is null or empty.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when decryption fails due to cryptographic issues, such as an incorrect password.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when the encrypted file is corrupted or metadata is missing.
    /// </exception>
    private async void DecryptBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable. " +
                "If using a custom password to decrypt, you MUST enter the same password used during encryption.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            DecryptBtn.Enabled = false;

            if (!FileProcessingConstants.FileOpened)
                throw new InvalidOperationException("No file is currently opened. Please open a file first.");

            if (string.IsNullOrEmpty(FileProcessingConstants.LoadedFile))
                throw new FileNotFoundException("No file is selected or the selected file path is empty.");

            if (FileProcessingConstants.IsDecrypted)
                throw new InvalidOperationException(
                    "The file has already been decrypted. You cannot decrypt it again. Please either encrypt or export it.");

            DecryptingAnimation();

            var data = FileProcessingConstants.Result;
            var signature = CryptoConstants.FileSignature;
            var saltSize = CryptoConstants.SaltSize;
            var headerLength = signature.Length + saltSize + 1; // signature + salt + extension length byte

            if (data.Length < headerLength)
                throw new Exception("Encrypted file is corrupted or missing required metadata.");

            // Validate file signature
            var actualSignature = data.AsSpan(0, signature.Length);
            if (!actualSignature.SequenceEqual(signature))
                throw new Exception("Invalid file signature. This file may not be a valid encrypted file.");

            // Extract HKDF salt for file key derivation
            var saltStart = signature.Length;
            var fileHkdfSalt = data.AsSpan(saltStart, saltSize).ToArray();

            // Read extension length
            var extLenIndex = saltStart + saltSize;
            var extensionLength = data[extLenIndex];

            // Ensure there's enough data for extension bytes
            if (data.Length < extLenIndex + 1 + extensionLength)
                throw new Exception("File is missing extension metadata or is corrupted.");

            var extBytesIndex = extLenIndex + 1;
            var extensionBytes = data.AsSpan(extBytesIndex, extensionLength).ToArray();
            var originalExtension = Encoding.UTF8.GetString(extensionBytes);
            FileProcessingConstants.OriginalExtension = originalExtension;

            // Extract encrypted content
            var encryptedStart = extBytesIndex + extensionLength;
            var encryptedContent = data.AsSpan(encryptedStart).ToArray();

            // Derive the file key using MasterKey + extracted salt
            var fileKey = Crypto.HKDF.HkdfDerivePinned(MasterKey, fileHkdfSalt, "file key"u8.ToArray(),
                CryptoConstants.KeySize);

            // Decrypt using the derived file key
            var decryptedFile = await DecryptFile(encryptedContent, fileKey, fileHkdfSalt);

            await Task.Delay(3000, _decryptAnimationSource.Token);
            await ResetDecryptAnimationTokenAsync();

            if (decryptedFile.Length == 0)
                throw new Exception("Decryption failed: resulting file is empty or corrupted.");

            FileProcessingConstants.Result = decryptedFile;

            FileOutputLbl.Text = "File decrypted.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show(
                "File was decrypted successfully. Don't forget to export and restore its original extension.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            FileSizeNumLbl.Text = $"{FileProcessingConstants.Result.Length:#,0} bytes";
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            FileProcessingConstants.IsEncrypted = false;
            FileProcessingConstants.IsDecrypted = true;

            // Clear sensitive memory
            CryptoUtilities.ClearMemoryNative(fileKey, fileHkdfSalt);
        }
        catch (OperationCanceledException)
        {
            // Ignored
        }
        catch (CryptographicException ex)
        {
            await ResetDecryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error decrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show("Unable to load vault. Recheck your password and vault file.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            FileProcessingConstants.IsDecrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (FileNotFoundException ex)
        {
            await ResetDecryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error decrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show("No file is selected. Or the file path was empty.", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            FileProcessingConstants.IsDecrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (InvalidOperationException ex)
        {
            await ResetDecryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error decrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show(
                "An invalid operation error has occured. Make sure you have a file opened, and that it hasn't already been decrypted.",
                "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            FileProcessingConstants.IsDecrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            await ResetDecryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error decrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show("An unexpected error has occured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            FileProcessingConstants.IsDecrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            DecryptBtn.Enabled = true;
        }
    }

    private static async Task ResetDecryptAnimationTokenAsync()
    {
        try
        {
            if (_decryptAnimationSource != null)
            {
                await _decryptAnimationSource.CancelAsync();
                _decryptAnimationSource.Dispose();
            }
        }
        catch
        {
            /* Ignore cleanup exceptions */
        }
        finally
        {
            _decryptAnimationSource = new CancellationTokenSource();
        }
    }

    /// <summary>
    /// Checks the header of a file to determine if it has already been encrypted.
    /// </summary>
    /// <param name="file">The full path to the file to check.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method opens the specified file and reads its initial bytes to compare them against the expected file signature.
    /// If the signature matches, it indicates the file has already been encrypted, and an <see cref="Exception"/> is thrown.
    /// </remarks>
    /// <exception cref="IOException">
    /// Thrown if the method fails to read the full header from the file.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown if the file appears to already be encrypted based on the signature.
    /// </exception>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     await CheckHeader("C:\\path\\to\\file.dat");
    /// }
    /// catch (Exception ex)
    /// {
    ///     MessageBox.Show(ex.Message);
    /// }
    /// </code>
    /// </example>

    private static async void CheckHeader(string file)
    {
        await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (fs.Length >= CryptoConstants.FileSignature.Length)
        {
            var header = new byte[CryptoConstants.FileSignature.Length];
            var bytesRead = await fs.ReadAsync(header);

            if (bytesRead != header.Length)
                throw new IOException("Failed to read the full file header.");

            if (header.SequenceEqual(CryptoConstants.FileSignature))
                throw new Exception("File is already encrypted. Unable to encrypt again. Please export the file.");
        }
    }

    /// <summary>
    /// Handles the click event for the Encrypt button and performs encryption on the currently loaded file.
    /// </summary>
    /// <param name="sender">The source of the event, typically the Encrypt button.</param>
    /// <param name="e">The event data associated with the button click.</param>
    /// <remarks>
    /// This method validates that a user is logged in and a file is selected, then performs file encryption using 
    /// a derived key and a random salt. It embeds metadata such as the original file extension into the encrypted output.
    /// 
    /// On success, it updates the UI with a success message and sets encryption state flags. On failure, it provides 
    /// user-friendly error messages depending on the specific exception caught. Sensitive data is cleared from memory
    /// and the UI is reset regardless of success or failure.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no user is logged in, no file is opened, or the file has already been encrypted.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the selected file path is null, empty, or the file cannot be found.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when a cryptographic error occurs during key derivation or encryption.
    /// </exception>
    /// <exception cref="CryptographicOperationException">
    /// Thrown when encryption fails or the output is invalid.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown when an unexpected error occurs during the encryption process.
    /// </exception>

    private async void EncryptBtn_Click(object sender, EventArgs e)
    {
        byte[] fileKey = [];
        byte[] fileHkdfSalt = [];

        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.\n\n" +
                "If using a custom password to encrypt with, MAKE SURE YOU REMEMBER THE PASSWORD!\n" +
                "You will NOT be able to decrypt the file without it. It is separate from your login password.",
                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            EncryptBtn.Enabled = false;

            if (!FileProcessingConstants.FileOpened)
                throw new InvalidOperationException("No file is opened. Please open a file before encrypting.");

            if (string.IsNullOrEmpty(FileProcessingConstants.LoadedFile))
                throw new FileNotFoundException("No file is selected or the file path is empty.");

            if (FileProcessingConstants.IsEncrypted)
                throw new InvalidOperationException("File is already encrypted. Please decrypt or export it first.");

            CheckHeader(FileProcessingConstants.LoadedFile);

            EncryptingAnimation();

            var input = await IO.ReadFile(FileProcessingConstants.LoadedFile);
            fileHkdfSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);

            fileKey = Crypto.HKDF.HkdfDerivePinned(
                MasterKey, fileHkdfSalt, "file key"u8.ToArray(), CryptoConstants.KeySize);

            if (input != null)
            {
                var encryptedFile = await EncryptFile(input, fileKey, fileHkdfSalt);

                CryptoUtilities.ClearMemoryNative(input);

                if (encryptedFile.Length == 0)
                    throw new CryptographicException("Encryption failed. The resulting file was empty.");

                var originalExtension = Path.GetExtension(FileProcessingConstants.LoadedFile);
                var extensionBytes = Encoding.UTF8.GetBytes(originalExtension);

                if (extensionBytes.Length > 255)
                    throw new InvalidOperationException("File extension is too long to store.");

                byte extensionLength = (byte)extensionBytes.Length;

                // Final file format: [Signature][Salt][ExtLen][Ext][Encrypted]
                encryptedFile =
                [
                    ..CryptoConstants.FileSignature,
                ..fileHkdfSalt,
                extensionLength,
                ..extensionBytes,
                ..encryptedFile
                ];

                await Task.Delay(3000, _encryptAnimationSource.Token);
                await ResetEncryptAnimationTokenAsync();

                FileProcessingConstants.Result = encryptedFile;
            }

            FileOutputLbl.Text = "File encrypted.";
            FileOutputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show(
                "File was encrypted successfully. You may now export it.\nTo decrypt, open the encrypted file later.",
                "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            FileSizeNumLbl.Text = $"{FileProcessingConstants.Result.Length:#,0} bytes";
            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;

            FileProcessingConstants.IsEncrypted = true;
            FileProcessingConstants.IsDecrypted = false;
        }
        catch (OperationCanceledException)
        {
            // Operation was canceled, no action needed.
        }
        catch (InvalidOperationException ex)
        {
            await ResetEncryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show(ex.Message, "Invalid Operation", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            FileProcessingConstants.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (FileNotFoundException ex)
        {
            await ResetEncryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show("The file was not found. Please verify the file path.", "File Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileProcessingConstants.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (CryptographicException ex)
        {
            await ResetEncryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show("An error has occured when trying to encrypt the file.", "Encryption Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileProcessingConstants.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            await ResetEncryptAnimationTokenAsync();

            FileOutputLbl.Text = "Error encrypting file.";
            FileOutputLbl.ForeColor = Color.Red;

            MessageBox.Show("An unexpected error occurred during encryption.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            FileProcessingConstants.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            CryptoUtilities.ClearMemoryNative(fileKey, fileHkdfSalt);
            EncryptBtn.Enabled = true;
        }
    }


    // Utility method to reset the animation token
    private static async Task ResetEncryptAnimationTokenAsync()
    {
        try
        {
            if (_encryptAnimationSource != null)
            {
                await _encryptAnimationSource.CancelAsync();
                _encryptAnimationSource.Dispose();
            }
        }
        catch
        {
            /* Ignore cleanup issues */
        }
        finally
        {
            _encryptAnimationSource = new CancellationTokenSource();
        }
    }

    private void FileEncryptDecryptBox_Enter(object sender, EventArgs e)
    {
    }

    /// <summary>
    /// Represents static fields and constants used for managing file processing state and UI interactions.
    /// </summary>
    private static class FileProcessingConstants
    {
        /// <summary>
        /// Gets the maximum allowed file size for processing, in bytes (2,000,000,000 bytes).
        /// </summary>
        public const int MaximumFileSize = 2_000_000_000;

        /// <summary>
        /// Gets or sets the currently loaded file path as a string.
        /// </summary>
        public static string LoadedFile = string.Empty;

        /// <summary>
        /// Gets or sets the result of the file processing, stored as a byte array.
        /// </summary>
        public static byte[] Result = [];

        /// <summary>
        /// Gets or sets the current file extension as a string.
        /// </summary>
        public static string FileExtension = string.Empty;

        /// <summary>
        /// Indicates whether the currently loaded file is encrypted.
        /// </summary>
        public static bool IsEncrypted;

        /// <summary>
        /// Indicates whether the currently loaded file is decrypted.
        /// </summary>
        public static bool IsDecrypted;

        /// <summary>
        /// Gets or sets a value indicating whether a file has been opened.
        /// </summary>
        public static bool FileOpened { get; set; }

        /// <summary>
        /// Gets or sets the size of the currently loaded file in bytes.
        /// </summary>
        public static long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the original extension of the loaded file, if any.
        /// </summary>
        public static string? OriginalExtension { get; set; }
    }

}