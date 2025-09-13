using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using static Password_Vault_V2.Crypto;
using static Password_Vault_V2.Crypto.ParallelCtrEncryptor;
using static Password_Vault_V2.UiController;
using Timer = System.Windows.Forms.Timer;

namespace Password_Vault_V2;

public partial class Encryption : UserControl
{
    private static CancellationTokenSource _encryptAnimationSource = new();
    private static CancellationTokenSource _decryptAnimationSource = new();
    private static CancellationTokenSource _savingFileAnimationSource = new();

    public Encryption()
    {
        InitializeComponent();
    }

    private async void DecryptingAnimation()
    {
        await Animations.AnimateLabel(FileOutputLbl, "Decrypting file", _decryptAnimationSource.Token);
    }

    private async void EncryptingAnimation()
    {
        await Animations.AnimateLabel(FileOutputLbl, "Encrypting file", _encryptAnimationSource.Token);
    }

    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "bytes", "KB", "MB", "GB", "TB", "PB" };
        double len = bytes;
        var order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private async void ImportFileBtn_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
        {
            MessageBox.Show("No user is currently logged in.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
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

            if (fileInfo.Length == 0)
            {
                MessageBox.Show("The file is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Open file safely
            var fileStream = IO.OpenFileStream(selectedFileName);
            if (fileStream == null)
            {
                MessageBox.Show("Unable to open the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            FileVars.Result = fileStream;
            FileVars.FileOpened = true;
            FileVars.LoadedFile = selectedFileName;
            FileVars.FileExtension = fileInfo.Extension.ToLower(CultureInfo.CurrentCulture);
            FileVars.FileSize = fileInfo.Length;
            FileVars.IsEncrypted = false;
            FileVars.IsDecrypted = false;

            UIThreadHelper.SafeInvoke(this, () =>
            {
                FileSizeNumLbl.Text = FormatFileSize(FileVars.FileSize);
                FileOutputLbl.Text = "File opened.";
                FileOutputLbl.ForeColor = Color.LimeGreen;
            });

            MessageBox.Show("File opened successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            MessageBox.Show("An unexpected error occurred while opening the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UIThreadHelper.SafeInvoke(this, () =>
            {
                FileOutputLbl.Text = "Idle...";
                FileOutputLbl.ForeColor = Color.WhiteSmoke;
            });
        }
    }

    private async void ExportFileBtn_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
        {
            MessageBox.Show("No user is currently logged in.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!FileVars.FileOpened || FileVars.Result == null)
        {
            MessageBox.Show("No file is loaded for export.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Timer? animationToken = null;

        try
        {
            using var saveFileDialog = new SaveFileDialog
            {
                FilterIndex = 1,
                ShowHiddenFiles = true,
                CheckFileExists = false,
                CheckPathExists = false,
                RestoreDirectory = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            // Determine extension and filter
            string extension;
            string filter;
            if (FileVars.IsEncrypted)
            {
                extension = ".encrypted";
                filter = "Encrypted files (*.encrypted)|*.encrypted";
            }
            else
            {
                extension = string.IsNullOrEmpty(FileVars.OriginalExtension) ? ".dat" : FileVars.OriginalExtension;
                filter = string.IsNullOrEmpty(FileVars.OriginalExtension)
                    ? "All Files (*.*)|*.*"
                    : $"{extension.TrimStart('.').ToUpper()} files (*{extension})|*{extension}|All Files (*.*)|*.*";
            }

            saveFileDialog.Filter = filter;
            saveFileDialog.DefaultExt = extension;

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = saveFileDialog.FileName;
            if (string.IsNullOrEmpty(Path.GetExtension(selectedFileName)) ||
                !selectedFileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                selectedFileName = Path.ChangeExtension(selectedFileName, extension);
            }

            // Reset stream position before writing
            FileVars.Result.Position = 0;

            await Animations.AnimateLabel(FileStatusLbl, "Saving", _savingFileAnimationSource.Token);

            await IO.WriteFileStreamAsync(selectedFileName, FileVars.Result).ConfigureAwait(false);
            await IO.SecurelyWipeFileAsync(FileVars.LoadedFile);

            UIThreadHelper.SafeInvoke(this, () =>
            {
                FileOutputLbl.Text = "File saved successfully.";
                FileOutputLbl.ForeColor = Color.LimeGreen;
                FileSizeNumLbl.Text = FormatFileSize(0);
                MessageBox.Show("File saved successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            // Cleanup
            FileVars.Result.Dispose();
            FileVars.Result = null;
            FileVars.FileOpened = false;
            FileVars.FileSize = 0;
            FileVars.IsEncrypted = false;
            FileVars.IsDecrypted = false;
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            UIThreadHelper.SafeInvoke(this, () =>
            {
                MessageBox.Show("An unexpected error occurred while saving the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });
        }
        finally
        {
            await ResetSavingAnimationTokenAsync();
            UIThreadHelper.SafeInvoke(this, () =>
            {
                FileOutputLbl.Text = "Idle...";
                FileOutputLbl.ForeColor = Color.WhiteSmoke;
            });
        }
    }

    private async Task<bool> PerformDecryptionAsync(IProgress<double> uiProgress)
    {
        if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
            throw new InvalidOperationException("No user is currently logged in.");

        if (!FileVars.FileOpened || string.IsNullOrEmpty(FileVars.LoadedFile))
            throw new InvalidOperationException("No valid file selected for decryption.");

        if (FileVars.IsDecrypted)
            throw new InvalidOperationException("File already decrypted. Re-encrypt or export to proceed.");

        var input = FileVars.Result ?? throw new InvalidOperationException("No input stream.");
        input.Position = 0;

        // Parse header
        var sigLen = CryptoConstants.FileSignature.Length;
        var saltLen = Settings.Default.FIPS ? 32 : CryptoConstants.SaltSize;
        var headerLen = sigLen + saltLen + 1;

        var header = new byte[headerLen];
        var headerRead = await input.ReadAsync(header, 0, headerLen).ConfigureAwait(false);
        if (headerRead != headerLen)
            throw new InvalidDataException("Failed to read full header.");

        for (var i = 0; i < sigLen; i++)
            if (header[i] != CryptoConstants.FileSignature[i])
                throw new InvalidDataException("File signature mismatch.");

        var salt = new byte[saltLen];
        Buffer.BlockCopy(header, sigLen, salt, 0, saltLen);

        var extLength = header[^1];
        var extBuffer = new byte[extLength];
        var extRead = await input.ReadAsync(extBuffer, 0, extLength).ConfigureAwait(false);
        if (extRead != extLength)
            throw new InvalidDataException("Failed to read extension.");

        FileVars.OriginalExtension = Encoding.UTF8.GetString(extBuffer);
        long encryptedOffset = headerLen + extLength;
        input.Position = encryptedOffset;

        if (encryptedOffset >= input.Length)
            throw new InvalidDataException("No encrypted data found after header.");

        var tempDecryptedPath = Path.GetTempFileName();

        var masterKey = MasterKey.GetKey();
        byte[] fileKey;
        if (!Settings.Default.FIPS)
        {
            fileKey = Crypto.HKDF.HkdfDerivePinned(masterKey, salt, "file key"u8.ToArray(), CryptoConstants.KeySize);
        }
        else
        {
            // IMPORTANT: derive using the same salt read from the file header
            fileKey = FipsCrypto.Hkdf.DeriveKey(masterKey, salt, "file key"u8.ToArray(), CryptoConstants.KeySize);
        }

        try
        {
            await using (var output =
                         new FileStream(tempDecryptedPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                if (!Settings.Default.FIPS)
                {
                    // Non-FIPS cascade decryption
                    await DecryptFile(input, output, fileKey, salt, uiProgress).ConfigureAwait(false);
                }
                else
                {
                    // FIPS AES-GCM decryption: the decryptor expects to find an 8-byte prefix at `input.Position`
                    await FipsCrypto.FipsAesGcmParallel.DecryptFileAesGcmParallelAsync(input, output, fileKey, uiProgress).ConfigureAwait(false);
                }

                await output.FlushAsync().ConfigureAwait(false);
            }

            FileVars.Result?.Dispose();
            FileVars.Result = new FileStream(tempDecryptedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            FileVars.Result.Position = 0;

            return true;
        }
        finally
        {
            CryptoUtilities.ClearMemoryNative(fileKey, salt);
        }
    }



    private static async Task<bool> PerformEncryptionAsync(IProgress<double> progress)
    {
        if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
            throw new InvalidOperationException("No user is currently logged in.");

        if (!FileVars.FileOpened)
            throw new InvalidOperationException("No file is opened. Please open a file before encrypting.");

        if (string.IsNullOrEmpty(FileVars.LoadedFile))
            throw new FileNotFoundException("No file is selected or the file path is empty.");

        if (FileVars.IsEncrypted)
            throw new InvalidOperationException("File is already encrypted. Please decrypt or export it first.");

        var inputStream = FileVars.Result ?? throw new InvalidOperationException("No input stream.");
        inputStream.Position = 0;

        // Peek header to prevent double-encryption
        if (inputStream.Length >= CryptoConstants.FileSignature.Length)
        {
            var header = new byte[CryptoConstants.FileSignature.Length];
            var bytesRead = await inputStream.ReadAsync(header, 0, header.Length).ConfigureAwait(false);
            if (bytesRead != header.Length)
                throw new IOException("Failed to read the full file header.");

            if (header.SequenceEqual(CryptoConstants.FileSignature))
                throw new Exception("File is already encrypted. Unable to encrypt again. Please export the file.");

            inputStream.Position = 0;
        }

        // Choose salt length based on encryption mode
        var saltLength = Settings.Default.FIPS ? 32 : CryptoConstants.SaltSize;
        var salt = CryptoUtilities.RndByteSized(saltLength);
        var masterKey = MasterKey.GetKey();

        byte[] fileKey;
        if (!Settings.Default.FIPS)
        {
            // Non-FIPS: keep your existing derivation
            fileKey = Crypto.HKDF.HkdfDerivePinned(masterKey, salt, "file key"u8.ToArray(), CryptoConstants.KeySize);
        }
        else
        {
            // FIPS: derive key from header salt
            fileKey = FipsCrypto.Hkdf.DeriveKey(masterKey, salt, "file key"u8.ToArray(), CryptoConstants.KeySize);
        }

        try
        {
            // Create final output file immediately for FIPS encryption
            var finalTempPath = Path.GetTempFileName();
            var finalStream = new FileStream(
                finalTempPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                4096);

            // Write header first
            var ext = Path.GetExtension(FileVars.LoadedFile) ?? string.Empty;
            var extBytes = Encoding.UTF8.GetBytes(ext);
            if (extBytes.Length > 255) throw new InvalidOperationException("Extension too long.");
            var extLength = (byte)extBytes.Length;

            await finalStream.WriteAsync(CryptoConstants.FileSignature).ConfigureAwait(false);
            await finalStream.WriteAsync(salt).ConfigureAwait(false);
            await finalStream.WriteAsync(new[] { extLength }, 0, 1).ConfigureAwait(false);
            await finalStream.WriteAsync(extBytes).ConfigureAwait(false);

            if (!Settings.Default.FIPS)
            {
                // Non-FIPS cascade encryption: use a temp stream as before
                var tempEncryptedPath = Path.GetTempFileName();
                await using var tempEncryptedStream = new FileStream(
                    tempEncryptedPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    4096,
                    FileOptions.DeleteOnClose);

                await EncryptFile(inputStream, tempEncryptedStream, fileKey, salt, progress).ConfigureAwait(false);
                tempEncryptedStream.Position = 0;
                await tempEncryptedStream.CopyToAsync(finalStream).ConfigureAwait(false);
            }
            else
            {
                // FIPS AES-GCM encryption: write ciphertext directly after header
                await FipsCrypto.FipsAesGcmParallel.EncryptFileAesGcmParallelAsync(inputStream, finalStream, fileKey, progress).ConfigureAwait(false);
            }

            // Flush & rewind
            await finalStream.FlushAsync();
            finalStream.Position = 0;

            // Dispose old global result
            FileVars.Result?.Dispose();

            // Assign global variable to keep the stream open
            FileVars.Result = finalStream;
        }
        finally
        {
            CryptoUtilities.ClearMemoryNative(fileKey, salt);
        }

        return true;
    }

    private async void DecryptBtn_Click(object sender, EventArgs e)
    {
        if (FileVars.Result == null)
        {
            MessageBox.Show(
                "Please select a file before starting decryption.",
                "No File Selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        DecryptBtn.Enabled = false;
        Timer progressTimer = null;

        try
        {
            UIThreadHelper.SafeInvoke(this, () =>
            {
                MessageBox.Show(
                    "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.\n" +
                    "If using a custom password to decrypt, you MUST enter the same password used during encryption.",
                    "Info",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;

                DecryptingAnimation();
            });

            int currentValue = 0;
            int targetValue = 0;

            progressTimer = new Timer { Interval = 15 };
            progressTimer.Tick += (s, ev) =>
            {
                if (currentValue < targetValue) currentValue++;
                else if (currentValue > targetValue) currentValue--;

                progressBar.Value = currentValue;

                if (currentValue == targetValue) progressTimer.Stop();
            };

            var uiProgress = new Progress<double>(percent =>
            {
                var newTarget = Math.Max(0, Math.Min(100, (int)Math.Round(percent)));
                if (newTarget != targetValue)
                {
                    targetValue = newTarget;
                    if (!progressTimer.Enabled) progressTimer.Start();
                }
            });

            bool success = await PerformDecryptionAsync(uiProgress).ConfigureAwait(false);

            await ResetDecryptAnimationTokenAsync();

            if (success)
            {
                await Task.Delay(2500);

                UIThreadHelper.SafeInvoke(this, () =>
                {
                    FileOutputLbl.Text = "File decrypted.";
                    FileOutputLbl.ForeColor = Color.LimeGreen;

                    MessageBox.Show(
                        "File was decrypted successfully. Don't forget to export and restore its original extension.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    FileSizeNumLbl.Text = FormatFileSize(FileVars.FileSize);
                    FileOutputLbl.Text = "Idle...";
                    FileOutputLbl.ForeColor = Color.WhiteSmoke;

                    FileVars.IsEncrypted = false;
                    FileVars.IsDecrypted = true;
                });
            }
        }
        catch (FileNotFoundException)
        {
            ShowError("The file was not found. Please verify the file path.", "File Error", MessageBoxIcon.Error);
            FileVars.IsDecrypted = false;
        }
        catch (CryptographicException)
        {
            ShowError("Decryption failed. The file may be corrupted or the password is incorrect.", "Decryption Error", MessageBoxIcon.Error);
            FileVars.IsDecrypted = false;
        }
        catch (Exception ex)
        {
            ShowError("An unexpected error occurred during decryption.", "Error", MessageBoxIcon.Error);
            FileVars.IsDecrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            progressTimer?.Stop();
            progressTimer?.Dispose();
            UIThreadHelper.SafeInvoke(this, () => DecryptBtn.Enabled = true);
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

    private static async Task ResetSavingAnimationTokenAsync()
    {
        try
        {
            if (_savingFileAnimationSource != null)
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

    private async void EncryptBtn_Click(object sender, EventArgs e)
    {
        if (FileVars.Result == null)
        {
            MessageBox.Show(
                "Please select a file before starting encryption.",
                "No File Selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        EncryptBtn.Enabled = false;

        Timer progressTimer = null;

        try
        {
            UIThreadHelper.SafeInvoke(this, () =>
            {
                MessageBox.Show(
                    "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);

                EncryptingAnimation();

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
            });

            double currentValue = 0;
            double targetValue = 0;

            progressTimer = new Timer { Interval = 15 };
            progressTimer.Tick += (s, ev) =>
            {
                var delta = (targetValue - currentValue) * 0.1;
                if (Math.Abs(delta) < 0.2)
                {
                    currentValue = targetValue;
                    progressBar.Value = (int)Math.Round(currentValue);
                    progressTimer.Stop();
                }
                else
                {
                    currentValue += delta;
                    progressBar.Value = (int)Math.Round(currentValue);
                }
            };

            var uiProgress = new Progress<double>(percent =>
            {
                targetValue = Math.Max(0, Math.Min(100, percent));
                if (!progressTimer.Enabled) progressTimer.Start();
            });

            bool success = await PerformEncryptionAsync(uiProgress).ConfigureAwait(false);

            await ResetEncryptAnimationTokenAsync();

            if (success)
            {
                await Task.Delay(2500);

                UIThreadHelper.SafeInvoke(this, () =>
                {
                    FileOutputLbl.Text = "File encrypted.";
                    FileOutputLbl.ForeColor = Color.LimeGreen;

                    MessageBox.Show(
                        "File was encrypted successfully. You may now export it.\nTo decrypt, open the encrypted file later.",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    FileSizeNumLbl.Text = FormatFileSize(FileVars.Result.Length);
                    FileOutputLbl.Text = "Idle...";
                    FileOutputLbl.ForeColor = Color.WhiteSmoke;
                    progressBar.Value = 0;
                });

                FileVars.IsEncrypted = true;
                FileVars.IsDecrypted = false;

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            }
        }
        catch (FileNotFoundException ex)
        {
            ShowError("The file was not found. Please verify the file path.", "File Error", MessageBoxIcon.Error);
            FileVars.IsEncrypted = false;
        }
        catch (CryptographicException ex)
        {
            ShowError("An error occurred during encryption. The file may be corrupted.", "Encryption Error", MessageBoxIcon.Error);
            FileVars.IsEncrypted = false;
        }
        catch (Exception ex)
        {
            ShowError("An unexpected error occurred during encryption.", "Error", MessageBoxIcon.Error);
            FileVars.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            progressTimer?.Stop();
            progressTimer?.Dispose();
            UIThreadHelper.SafeInvoke(this, () => EncryptBtn.Enabled = true);
        }
    }

    private void ShowError(string message, string title, MessageBoxIcon icon)
    {
        UIThreadHelper.SafeInvoke(this, () =>
        {
            FileOutputLbl.Text = title;
            FileOutputLbl.ForeColor = Color.Red;
            progressBar.Value = 0;

            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);

            FileOutputLbl.Text = "Idle...";
            FileOutputLbl.ForeColor = Color.WhiteSmoke;
        });
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

    /// <summary>
    ///     Represents static fields and constants used for managing file processing state and UI interactions.
    /// </summary>
    public static class FileVars
    {
        /// <summary>
        ///     Gets or sets the currently loaded file path as a string.
        /// </summary>
        public static string LoadedFile = string.Empty;

        /// <summary>
        ///     Gets or sets the result of the file processing, stored as a stream.
        /// </summary>
        public static Stream? Result;

        /// <summary>
        ///     Gets or sets the current file extension as a string.
        /// </summary>
        public static string FileExtension = string.Empty;

        /// <summary>
        ///     Indicates whether the currently loaded file is encrypted.
        /// </summary>
        public static bool IsEncrypted;

        /// <summary>
        ///     Indicates whether the currently loaded file is decrypted.
        /// </summary>
        public static bool IsDecrypted;

        /// <summary>
        ///     Gets or sets a value indicating whether a file has been opened.
        /// </summary>
        public static bool FileOpened { get; set; }

        /// <summary>
        ///     Gets or sets the size of the currently loaded file in bytes.
        /// </summary>
        public static long FileSize { get; set; }

        /// <summary>
        ///     Gets or sets the original extension of the loaded file, if any.
        /// </summary>
        public static string? OriginalExtension { get; set; }
    }
}