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

            FileVars.FileOpened = true;
            FileVars.LoadedFile = selectedFileName;
            FileVars.Result = IO.OpenFileStream(selectedFileName);

            // Validate that the file has been read correctly
            if (FileVars.Result.Length == 0)
                throw new Exception("The file is empty.");

            FileVars.FileExtension = fileInfo.Extension.ToLower(CultureInfo.CurrentCulture);
            FileVars.FileSize = fileInfo.Length;

            FileSizeNumLbl.Text = FormatFileSize(FileVars.FileSize);
            FileOutputLbl.Text = "File opened.";
            FileOutputLbl.ForeColor = Color.LimeGreen;
            FileVars.IsDecrypted = false;
            FileVars.IsEncrypted = false;

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

            if (!FileVars.FileOpened)
                throw new InvalidOperationException("No file is opened.");

            using var saveFileDialog = new SaveFileDialog();

            // Determine file extension and filter
            string? extension;
            string filter;

            if (FileVars.IsEncrypted)
            {
                extension = ".encrypted";
                filter = "Encrypted files (*.encrypted)|*.encrypted";
            }
            else
            {
                // Use original extension from decrypted metadata if available
                extension = FileVars.OriginalExtension;
                if (string.IsNullOrEmpty(extension))
                {
                    filter = "All Files (*.*)|*.*";
                }
                else
                {
                    var extNoDot = extension.TrimStart('.');
                    filter = $"{extNoDot.ToUpper()} files (*{extension})|*{extension}|All Files (*.*)|*.*";
                }
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

            if (FileVars.Result?.Length == 0)
                throw new InvalidOperationException("There is no data to write to the file.");

            FileVars.Result.Position = 0;
            await Animations.AnimateLabel(FileStatusLbl, "Saving", _savingFileAnimationSource.Token);
            await IO.WriteFileStreamAsync(selectedFileName, FileVars.Result).ConfigureAwait(false);
            await IO.SecurelyWipeFileAsync(FileVars.LoadedFile);

            UIThreadHelper.SafeInvoke(FileOutputLbl, () =>
            {
                FileOutputLbl.Text = "File saved successfully.";
                FileOutputLbl.ForeColor = Color.LimeGreen;
            });


            UIThreadHelper.SafeInvoke(this,
                () =>
                {
                    MessageBox.Show(this, "File saved successfully.", "Saved successfully", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                });

            // Cleanup state
            FileVars.FileOpened = false;
            FileVars.Result?.Dispose();

            UIThreadHelper.SafeInvoke(FileSizeNumLbl,
                () => { FileSizeNumLbl.Text = FormatFileSize(0); });

            FileVars.Result = null;
            FileVars.FileSize = 0;
            FileVars.IsDecrypted = false;
            FileVars.IsEncrypted = false;
        }
        catch (Exception ex)
        {
            UIThreadHelper.SafeInvoke(this,
                () => { MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); });
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            await ResetSavingAnimationTokenAsync();
            UIThreadHelper.SafeInvoke(FileOutputLbl, () =>
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
        try
        {
            UIThreadHelper.SafeInvoke(this, () =>
            {
                MessageBox.Show(
                    "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.\n" +
                    "If using a custom password to decrypt, you MUST enter the same password used during encryption.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
                DecryptBtn.Enabled = false;
                DecryptingAnimation();
            });

            var currentValue = 0;
            var targetValue = 0;
            var timer = new Timer
            {
                Interval = 15
            };

            timer.Tick += (s, e) =>
            {
                if (currentValue < targetValue)
                    currentValue++;
                else if (currentValue > targetValue)
                    currentValue--;

                progressBar.Value = currentValue;

                if (currentValue == targetValue)
                    timer.Stop();
            };

            var uiProgress = new Progress<double>(percent =>
            {
                var newTarget = (int)Math.Round(percent);
                newTarget = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, newTarget));

                if (newTarget != targetValue)
                {
                    targetValue = newTarget;
                    if (!timer.Enabled)
                        timer.Start();
                }
            });


            var success = await PerformDecryptionAsync(uiProgress).ConfigureAwait(false);

            await ResetDecryptAnimationTokenAsync();

            if (success)
            {
                await Task.Delay(2500);
                await ResetDecryptAnimationTokenAsync();

                UIThreadHelper.SafeInvoke(this, () =>
                {
                    FileOutputLbl.Text = "File decrypted.";
                    FileOutputLbl.ForeColor = Color.LimeGreen;

                    MessageBox.Show(
                        "File was decrypted successfully. Don't forget to export and restore its original extension.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    FileSizeNumLbl.Text = FormatFileSize(FileVars.FileSize);
                    FileOutputLbl.Text = "Idle...";
                    FileOutputLbl.ForeColor = Color.WhiteSmoke;

                    FileVars.IsEncrypted = false;
                    FileVars.IsDecrypted = true;
                });
            }
        }
        catch (Exception ex)
        {
            await ResetDecryptAnimationTokenAsync();

            UIThreadHelper.SafeInvoke(this, () =>
            {
                FileOutputLbl.Text = "Error decrypting file.";
                FileOutputLbl.ForeColor = Color.Red;

                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                FileOutputLbl.Text = "Idle...";
                FileOutputLbl.ForeColor = Color.WhiteSmoke;
                progressBar.Value = 0;
                FileVars.IsDecrypted = false;
            });

            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
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
        try
        {
            UIThreadHelper.SafeInvoke(this, () =>
            {
                MessageBox.Show(
                    "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                EncryptBtn.Enabled = false;
                EncryptingAnimation();

                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0; // Reset progress bar at start
            });

            if (FileVars.Result == null)
                throw new InvalidOperationException("No input file loaded for encryption.");

            double currentValue = 0;
            double targetValue = 0;


            var timer = new Timer { Interval = 15 };
            timer.Tick += (s, e) =>
            {
                var delta = (targetValue - currentValue) * 0.1;
                if (Math.Abs(delta) < 0.2)
                {
                    currentValue = targetValue;
                    progressBar.Value = (int)Math.Round(currentValue);
                    timer.Stop();
                }
                else
                {
                    currentValue += delta;
                    progressBar.Value = (int)Math.Round(currentValue);
                }
            };

            // This is now Progress<long>, so we're passed raw byte counts:
            var uiProgress = new Progress<double>(percent =>
            {
                targetValue = percent;
                if (!timer.Enabled) timer.Start();
            });


            var success = await PerformEncryptionAsync(uiProgress).ConfigureAwait(false);

            if (success)
            {
                await Task.Delay(2500);

                UIThreadHelper.SafeInvoke(this, () =>
                {
                    FileOutputLbl.Text = "File encrypted.";
                    FileOutputLbl.ForeColor = Color.LimeGreen;
                });

                await ResetEncryptAnimationTokenAsync();

                UIThreadHelper.SafeInvoke(this, () =>
                {
                    MessageBox.Show(
                        "File was encrypted successfully. You may now export it.\nTo decrypt, open the encrypted file later.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                UIThreadHelper.SafeInvoke(this, () =>
                {
                    FileSizeNumLbl.Text = FormatFileSize(FileVars.Result.Length);
                    FileOutputLbl.Text = "Idle...";
                    FileOutputLbl.ForeColor = Color.WhiteSmoke;
                    progressBar.Value = 0; // Reset progress bar when done
                });

                FileVars.IsEncrypted = true;
                FileVars.IsDecrypted = false;
            }
        }
        catch (InvalidOperationException ex)
        {
            await ResetEncryptAnimationTokenAsync();
            ShowError("Error encrypting file.", ex.Message, MessageBoxIcon.Warning);
            FileVars.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (FileNotFoundException ex)
        {
            await ResetEncryptAnimationTokenAsync();
            ShowError("Error encrypting file.", "The file was not found. Please verify the file path.",
                MessageBoxIcon.Error);
            FileVars.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (CryptographicException ex)
        {
            await ResetEncryptAnimationTokenAsync();
            ShowError("Error encrypting file.", "An error has occurred when trying to encrypt the file.",
                MessageBoxIcon.Error);
            FileVars.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        catch (Exception ex)
        {
            await ResetEncryptAnimationTokenAsync();
            ShowError("Error encrypting file.", "An unexpected error occurred during encryption.\n" + ex.Message,
                MessageBoxIcon.Error);
            FileVars.IsEncrypted = false;
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            UIThreadHelper.SafeInvoke(this, () => { EncryptBtn.Enabled = true; });
        }
    }

    // Helper to show error on UI safely
    private void ShowError(string title, string message, MessageBoxIcon icon)
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