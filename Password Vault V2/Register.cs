using System.Buffers;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using OtpNet;
using static Password_Vault_V2.Crypto;

namespace Password_Vault_V2;

public sealed partial class Register : UserControl
{
    private static CancellationTokenSource? _cancellationTokenSource = new();
    private readonly SecurePasswordBuffer _confirmPasswordBuffer = new();
    private readonly SecurePasswordBuffer _passwordBuffer = new();
    private Task? _animationTask;


    public Register()
    {
        InitializeComponent();
    }

    private static (byte[] HashSalt, byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
        byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt, byte[] FileSalt, byte[] EncryptionKeySalt,
        byte[] FileKeySalt, byte[] HmacSalt, byte[] DerivedHmacSalt) GenerateAllSalts()
    {
        var saltSize = FipsCrypto.FipsEnabled ? FipsCrypto.SaltSize : CryptoConstants.SaltSize;

        return (
            CryptoUtilities.RndByteSized(saltSize), // HashSalt
            CryptoUtilities.RndByteSized(saltSize), // KeyDerivationSalt
            CryptoUtilities.RndByteSized(saltSize), // IntermediateKeySalt
            CryptoUtilities.RndByteSized(saltSize), // MasterKeySalt
            CryptoUtilities.RndByteSized(saltSize), // MasterKeyEncryptionSalt
            CryptoUtilities.RndByteSized(saltSize), // FileSalt
            CryptoUtilities.RndByteSized(saltSize), // EncryptionKeySalt
            CryptoUtilities.RndByteSized(saltSize), // FileKeySalt
            CryptoUtilities.RndByteSized(saltSize), // HmacSalt
            CryptoUtilities.RndByteSized(saltSize) // DerivedHmacSalt
        );
    }

    private async Task<DerivedKeys> DeriveKeysAsync(byte[] password,
        (byte[] HashSalt, byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
            byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt, byte[] FileSalt,
            byte[] EncryptionKeySalt, byte[] FileKeySalt, byte[] HmacSalt, byte[] DerivedHmacSalt) salts)
    {
        if (!FipsCrypto.FipsEnabled)
        {
            var keys = new DerivedKeys(CryptoConstants.KeySize, CryptoConstants.PasswordHashSize);

            // Password Hash
            {
                var hash = await HashingMethods.Argon2Id(password, salts.HashSalt, CryptoConstants.PasswordHashSize);
                hash.AsSpan().CopyTo(keys.PasswordHash.AsSpan());
                CryptographicOperations.ZeroMemory(hash);
            }

            // Password Derived Key (intermediate key)
            {
                var derivedKey =
                    await HashingMethods.Argon2Id(password, salts.KeyDerivationSalt, CryptoConstants.KeySize);
                var intermediateKey = DeriveAndPin(derivedKey, salts.IntermediateKeySalt,
                    "intermediate key"u8.ToArray(), CryptoConstants.KeySize);
                derivedKey.AsSpan().Clear();
                intermediateKey.AsSpan().CopyTo(keys.IntermediateKey.AsSpan());
            }

            // Master Key
            {
                var masterKey = CryptoUtilities.RndByteSized(CryptoConstants.KeySize);
                masterKey.AsSpan().CopyTo(keys.MasterKey.AsSpan());
            }

            // Derived File Key & Encryption Key
            {
                var derivedFileKey =
                    await HashingMethods.Argon2Id(password, salts.FileKeySalt, CryptoConstants.KeySize);
                var encryptionKey = DeriveAndPin(derivedFileKey, salts.EncryptionKeySalt, "encryption key"u8.ToArray(),
                    CryptoConstants.KeySize);
                derivedFileKey.AsSpan().Clear();
                encryptionKey.AsSpan().CopyTo(keys.EncryptionKey.AsSpan());
            }

            // Derived HMAC Key & HMAC Key
            {
                var derivedHmacKey =
                    await HashingMethods.Argon2Id(password, salts.DerivedHmacSalt, CryptoConstants.KeySize);
                var hmacKey = DeriveAndPin(derivedHmacKey, salts.HmacSalt, "hmac key"u8.ToArray(),
                    CryptoConstants.HmacLength);
                derivedHmacKey.AsSpan().Clear();
                hmacKey.AsSpan().CopyTo(keys.HmacKey.AsSpan());
            }
            return keys;
        }

        var fipsKeys = new DerivedKeys(CryptoConstants.KeySize, 32);

        // Password Hash
        {
            var hash = FipsCrypto.Pbkdf2(password, salts.HashSalt, 32);
            hash.AsSpan().CopyTo(fipsKeys.PasswordHash.AsSpan());
            CryptographicOperations.ZeroMemory(hash);
        }

        // Password Derived Key (intermediate key)
        {
            var derivedKey =
                FipsCrypto.Pbkdf2(password, salts.KeyDerivationSalt, FipsCrypto.KeySize);
            var intermediateKey = FipsCrypto.Hkdf.DeriveKey(derivedKey, salts.IntermediateKeySalt,
                "intermediate key"u8.ToArray(), CryptoConstants.KeySize);
            CryptographicOperations.ZeroMemory(derivedKey);
            intermediateKey.AsSpan().CopyTo(fipsKeys.IntermediateKey.AsSpan());
        }

        // Master Key
        {
            var masterKey = CryptoUtilities.RndByteSized(CryptoConstants.KeySize);
            masterKey.AsSpan().CopyTo(fipsKeys.MasterKey.AsSpan());
        }

        // Derived File Key & Encryption Key
        {
            var derivedFileKey =
                FipsCrypto.Pbkdf2(password, salts.FileKeySalt, FipsCrypto.KeySize);
            var encryptionKey = FipsCrypto.Hkdf.DeriveKey(derivedFileKey, salts.EncryptionKeySalt,
                "encryption key"u8.ToArray(),
                CryptoConstants.KeySize);
            derivedFileKey.AsSpan().Clear();
            encryptionKey.AsSpan().CopyTo(fipsKeys.EncryptionKey.AsSpan());
        }

        // Derived HMAC Key & HMAC Key
        {
            var derivedHmacKey =
                FipsCrypto.Pbkdf2(password, salts.DerivedHmacSalt, FipsCrypto.KeySize);
            var hmacKey = FipsCrypto.Hkdf.DeriveKey(derivedHmacKey, salts.HmacSalt, "hmac key"u8.ToArray(),
                32);
            derivedHmacKey.AsSpan().Clear();
            hmacKey.AsSpan().CopyTo(fipsKeys.HmacKey.AsSpan());
        }
        return fipsKeys;
    }

    private byte[] BuildFinalFile(
        (byte[] HashSalt, byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
            byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt, byte[] FileSalt,
            byte[] EncryptionKeySalt, byte[] FileKeySalt, byte[] HmacSalt, byte[] DerivedHmacSalt) salts,
        byte[] uuid,
        byte[] encryptedUserFile)
    {
        return salts.HmacSalt
            .Concat(salts.DerivedHmacSalt)
            .Concat(salts.FileSalt)
            .Concat(salts.FileKeySalt)
            .Concat(salts.EncryptionKeySalt)
            .Concat(uuid) // use the same UUID from registration
            .Concat(salts.HashSalt)
            .Concat(salts.MasterKeyEncryptionSalt)
            .Concat(salts.KeyDerivationSalt)
            .Concat(salts.IntermediateKeySalt)
            .Concat(encryptedUserFile)
            .ToArray();
    }


    /// <summary>
    ///     Validates a password by checking length, character composition, and optional confirmation match.
    /// </summary>
    /// <param name="password">The primary password to validate, as a collection of characters.</param>
    /// <param name="password2">
    ///     An optional second password to compare for equality with the first. If provided, the two must match exactly
    ///     and must not contain whitespace.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the password meets all security requirements:
    ///     length between 22 and 120 characters, contains at least one uppercase letter, one lowercase letter,
    ///     one digit, and at least one symbol or punctuation character, and no whitespace. Also returns <c>true</c>
    ///     only if <paramref name="password2" /> is <c>null</c> or matches <paramref name="password" /> exactly.
    ///     Otherwise, returns <c>false</c>.
    /// </returns>
    private static bool CheckPasswordValidity(IReadOnlyCollection<char> password,
        IReadOnlyCollection<char>? password2 = null)
    {
        if (password is { Count: < 22 or > 120 })
            return false;

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return false;

        if (password.Any(char.IsWhiteSpace) || (password2 != null &&
                                                (password2.Any(char.IsWhiteSpace) ||
                                                 !password.SequenceEqual(password2))))
            return false;

        return password.Any(char.IsSymbol) || password.Any(char.IsPunctuation);
    }

    /// <summary>
    ///     Validates the provided username and password for registration.
    /// </summary>
    /// <param name="userName">The username to be validated.</param>
    /// <param name="password">The password to be validated.</param>
    /// <param name="password2">The confirmation password to be validated.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if the username or password does not meet the specified criteria.
    /// </exception>
    private static void ValidateUsernameAndPassword(string userName, char[] password, char[] password2)
    {
        if (!userName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == ' '))
            throw new ArgumentException(
                "Value contains illegal characters. Valid characters are letters, digits, underscores, and spaces.",
                nameof(userName));

        if (string.IsNullOrEmpty(userName) || userName.Length > 20)
            throw new ArgumentException("Invalid username.", nameof(userName));

        if (password == Array.Empty<char>())
            throw new ArgumentException("Invalid password.", nameof(password));

        if (!CheckPasswordValidity(password, password2))
            throw new Exception(
                "Password must contain between 22 and 120 characters. It also must include:" +
                " 1.) At least one uppercase letter." +
                " 2.) At least one lowercase letter." +
                " 3.) At least one number." +
                " 4.) At least one special character." +
                " 5.) Must not contain any spaces." +
                " 6.) Both passwords must match.");
    }


    /// <summary>
    ///     Starts the label animation indicating that account creation is in progress.
    /// </summary>
    /// <remarks>
    ///     Cancels any previously running animation by signaling its cancellation token,
    ///     then creates a new <see cref="CancellationTokenSource" /> and begins animating
    ///     the label with the text "Creating account".
    /// </remarks>
    private void StartAnimation()
    {
        _cancellationTokenSource?.Cancel(); // Cancel any previous animation
        _cancellationTokenSource = new CancellationTokenSource();
        _animationTask =
            UiController.Animations.AnimateLabel(outputLbl, "Creating account", _cancellationTokenSource.Token);
    }

    /// <summary>
    ///     Stops the label animation started by <see cref="StartAnimation" /> asynchronously.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation of canceling the animation
    ///     and waiting for it to complete.
    /// </returns>
    /// <remarks>
    ///     Attempts to cancel the current animation using the associated cancellation token,
    ///     waits for the animation task to complete, handles expected and unexpected exceptions,
    ///     and disposes of the cancellation token source.
    /// </remarks>
    /// <exception cref="Exception">
    ///     Logs and shows a message box for any unexpected exceptions during cancellation or awaiting the animation task.
    /// </exception>
    private async Task StopAnimationAsync()
    {
        if (_cancellationTokenSource is { IsCancellationRequested: false })
            try
            {
                await _cancellationTokenSource.CancelAsync();
            }
            catch (Exception ex)
            {
                // Optional: log if needed
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ErrorLogging.ErrorLog(ex);
            }

        if (_animationTask != null)
            try
            {
                await _animationTask;
            }
            catch (OperationCanceledException)
            {
                // Expected, no need to log
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Unexpected error
                ErrorLogging.ErrorLog(ex);
            }

        _animationTask = null;

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    /// <summary>
    ///     Registers a new user by generating salts and keys, encrypting the master key,
    ///     building a secure user file, and writing it to disk.
    ///     All sensitive data is securely wiped from memory upon completion.
    /// </summary>
    /// <param name="username">The username to register.</param>
    /// <param name="password">The user password in byte[] form.</param>
    /// <param name="confirmPassword">The confirmation password in byte[] form.</param>
    private async Task RegisterAsync(string username, byte[] password, byte[] confirmPassword)
    {
        UiController.LogicMethods.DisableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        StartAnimation();

        try
        {
            // Validate inputs (use your existing validation)
            ValidateUsernameAndPassword(username, _passwordBuffer.ToCharArray(), _confirmPasswordBuffer.ToCharArray());

            // Generate all salts needed for derivation and encryption
            var salts = GenerateAllSalts();

            IO.CreateUserPath(username);

            // Securely derive keys & hashes in a disposable container
            using var derivedKeys = await DeriveKeysAsync(password, salts);

            // Encrypt master key with intermediate key
            var encryptedMasterKey = await EncryptFile(
                derivedKeys.MasterKey.AsSpan().ToArray(),
                derivedKeys.IntermediateKey.AsSpan().ToArray(),
                salts.MasterKeyEncryptionSalt);

            var keyVersion = 1;

            var uuid = Guid.NewGuid().ToByteArray();
            // Build and encrypt user file
            var userFile = IO.BuildUserFile(
                derivedKeys.PasswordHash.AsSpan().ToArray(),
                uuid,
                Encoding.UTF8.GetBytes(emailTxt.Text), encryptedMasterKey);

            var encryptedUserFile = await EncryptFile(
                userFile,
                derivedKeys.EncryptionKey.AsSpan().ToArray(),
                salts.FileSalt);

            // Calculate HMAC
            var hmac = HashingMethods.HmacSha3(encryptedUserFile, derivedKeys.HmacKey.ToArrayAndClear());

            // Assemble final file (concatenate all needed metadata and encrypted data)
            var finalFile = BuildFinalFile(salts, uuid, encryptedUserFile);

            var path = UserFileManager.GetUserFilePath(username);
            await IO.WriteFile(path, finalFile);
            File.SetAttributes(path, FileAttributes.ReadOnly);

            outputLbl.Text = "User created successfully.";
            outputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show(
                "Registration successful! Make sure you do NOT forget your password or you will lose access to all of your files.",
                "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Zero sensitive byte arrays after use
            CryptographicOperations.ZeroMemory(encryptedMasterKey);
            CryptographicOperations.ZeroMemory(encryptedUserFile);
            CryptographicOperations.ZeroMemory(finalFile);
            CryptographicOperations.ZeroMemory(hmac);
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred when creating account.", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            try
            {
                await StopAnimationAsync();
            }
            catch (Exception animationEx)
            {
                MessageBox.Show(animationEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ErrorLogging.ErrorLog(animationEx);
            }

            // Clear UI & buffers safely
            UiController.LogicMethods.EnableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
            outputLbl.Text = "Idle...";
            outputLbl.ForeColor = Color.White;

            userTxt.Clear();
            passTxt.Clear();
            confirmPassTxt.Clear();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
    }

    private async Task FipsModeRegisterAsync(string username, byte[] password, byte[] confirmPassword, string email)
    {
        UiController.LogicMethods.DisableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
        StartAnimation();

        try
        {
            ValidateUsernameAndPassword(username, _passwordBuffer.ToCharArray(), _confirmPasswordBuffer.ToCharArray());

            // 1️⃣ Generate all salts
            var salts = GenerateAllSalts();

            // 2️⃣ Derive keys
            using var keys = await DeriveKeysAsync(password, salts);

            // 3️⃣ Generate a UUID once
            var uuid = Guid.NewGuid().ToByteArray();

            // 4️⃣ Encrypt master key using IntermediateKey + MasterKeySalt
            var encryptedMasterKey = FipsCrypto.AesKeyWrapRfc5649.Wrap(
                keys.IntermediateKey.AsSpan().ToArray(), keys.MasterKey.AsSpan().ToArray());


            // 5️⃣ Build user file: password hash || UUID || email || encrypted master key
            var userFile = IO.BuildUserFile(
                keys.PasswordHash.AsSpan().ToArray(),
                uuid,
                Encoding.UTF8.GetBytes(email),
                encryptedMasterKey);

            // 6️⃣ Encrypt user file with AES-HMAC (single pass)
            var encryptedUserFile = FipsCrypto.SimpleAesHmac.Encrypt(
                keys.EncryptionKey.AsSpan().ToArray(),
                keys.HmacKey.AsSpan().ToArray(),
                userFile);

            // 7️⃣ Assemble final file with metadata + encryptedUserFile
            var finalFile = BuildFinalFile(
                salts,
                uuid,
                encryptedUserFile);

            // 8️⃣ Write to disk
            var path = UserFileManager.GetUserFilePath(username);

            using var keyStore = new SoftwareKeyStore(UserFileManager.GetUserFolder(username));

            // 3. Add new master key
            keyStore.AddNewMasterKey(encryptedMasterKey, "Initial key");

            CryptographicOperations.ZeroMemory(encryptedMasterKey);
            CryptographicOperations.ZeroMemory(keys.IntermediateKey.AsSpan());

            await IO.WriteFile(path, finalFile);
            File.SetAttributes(path, FileAttributes.ReadOnly);

            outputLbl.Text = "User created successfully.";
            outputLbl.ForeColor = Color.LimeGreen;

            MessageBox.Show(
                "Registration successful! Do NOT forget your password.",
                "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Zero sensitive memory
            CryptographicOperations.ZeroMemory(encryptedMasterKey);
            CryptographicOperations.ZeroMemory(encryptedUserFile);
            CryptographicOperations.ZeroMemory(finalFile);
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred during registration.", "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
        finally
        {
            await StopAnimationAsync();
            UiController.LogicMethods.EnableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
            userTxt.Clear();
            passTxt.Clear();
            confirmPassTxt.Clear();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
    }


    /// <summary>
    ///     Handles the click event of the <c>CreateAccountBtn</c> button. Initiates the user account registration process,
    ///     displaying a warning about potential data corruption, converting secure password buffers to byte arrays,
    ///     and invoking <see cref="RegisterAsync" />. Ensures sensitive data is disposed and cleared from memory.
    /// </summary>
    /// <param name="sender">The source of the event, typically the <c>CreateAccountBtn</c> control.</param>
    /// <param name="e">An <see cref="EventArgs" /> that contains the event data.</param>
    /// <remarks>
    ///     This method warns the user not to close the application during registration, securely handles password data,
    ///     logs any exceptions, and ensures that sensitive data is cleaned up regardless of success or failure.
    /// </remarks>
    private async void CreateAccountBtn_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(userTxt.Text))
            throw new Exception("Username textbox was empty.");
        if (_passwordBuffer.Length == 0)
            throw new Exception("Password textbox was empty.");
        if (_confirmPasswordBuffer.Length == 0)
            throw new Exception("Confirm password textbox was empty.");

        MessageBox.Show(
            "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
            "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

        var username = userTxt.Text.Trim();

        // Generate TOTP secret
        var userSecret = RandomNumberGenerator.GetBytes(20);

        // Store TOTP secret securely
        using var store = new SoftwareKeyStore(UserFileManager.GetUserFolder(username));
        store.AddTotpSecret(username, userSecret, "Authenticator");

        // Generate QR code
        string issuer = "Password Vault";
        string otpauthUrl = $"otpauth://totp/{issuer}:{username}?secret={Base32Encoding.ToString(userSecret)}&issuer={issuer}&digits=6";

        using var verifyForm = new TotpVerify(userSecret, issuer, username, Base32Encoding.ToString(userSecret));
        if (verifyForm.ShowDialog() != DialogResult.OK)
        {
            MessageBox.Show(
                "You must successfully verify your Authenticator before continuing.",
                "Verification Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            CryptographicOperations.ZeroMemory(userSecret);
            return; // stop account creation
        }

        // Wipe sensitive memory
        CryptographicOperations.ZeroMemory(userSecret);

        // Continue with account creation
        var passwordBytes = _passwordBuffer.ToByteArray();
        var confirmPasswordBytes = _confirmPasswordBuffer.ToByteArray();

        try
        {
            if (!FipsCrypto.FipsEnabled)
                await RegisterAsync(username, passwordBytes, confirmPasswordBytes);
            else
                await FipsModeRegisterAsync(username, passwordBytes, confirmPasswordBytes, username);
        }
        finally
        {
            _passwordBuffer.Dispose();
            _confirmPasswordBuffer.Dispose();
            CryptoUtilities.ClearMemoryNative(passwordBytes, confirmPasswordBytes);
        }

        MessageBox.Show("Account created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }


    private void Register_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); // Clear previous drawings
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
    }

    public sealed class SecureBuffer : IDisposable
    {
        private byte[]? _buffer;

        public SecureBuffer(int size)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(size);
            _buffer.AsSpan(0, size).Clear(); // Clear on rent
            Size = size;
        }

        private int Size { get; }

        public void Dispose()
        {
            if (_buffer != null)
            {
                CryptographicOperations.ZeroMemory(_buffer.AsSpan(0, Size));
                ArrayPool<byte>.Shared.Return(_buffer, true);
                _buffer = null;
            }
        }

        public Span<byte> AsSpan()
        {
            if (_buffer is null) throw new ObjectDisposedException(nameof(SecureBuffer));
            return _buffer.AsSpan(0, Size);
        }

        public byte[] ToArrayAndClear()
        {
            var copy = AsSpan().ToArray();
            CryptographicOperations.ZeroMemory(AsSpan());
            return copy;
        }
    }

    public sealed class DerivedKeys : IDisposable
    {
        public DerivedKeys(int keySize, int hashSize)
        {
            PasswordHash = new SecureBuffer(hashSize);
            IntermediateKey = new SecureBuffer(keySize);
            MasterKey = new SecureBuffer(keySize);
            EncryptionKey = new SecureBuffer(keySize);
            HmacKey = new SecureBuffer(FipsCrypto.FipsEnabled ? 32 : CryptoConstants.HmacLength);
        }

        public SecureBuffer PasswordHash { get; }
        public SecureBuffer IntermediateKey { get; }
        public SecureBuffer MasterKey { get; }
        public SecureBuffer EncryptionKey { get; }
        public SecureBuffer HmacKey { get; }

        public void Dispose()
        {
            PasswordHash.Dispose();
            IntermediateKey.Dispose();
            MasterKey.Dispose();
            EncryptionKey.Dispose();
            HmacKey.Dispose();
        }
    }

    #region TextboxBehavior

    private void UpdateMaskedText()
    {
        passTxt.Text = new string('●', _passwordBuffer.Length);
        passTxt.SelectionStart = passTxt.Text.Length; // Move caret to end
        confirmPassTxt.Text = new string('●', _confirmPasswordBuffer.Length);
        confirmPassTxt.SelectionStart = confirmPassTxt.Text.Length; // Move caret to end
    }

    private void passTxt_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar >= 32 && e.KeyChar <= 126) // Printable ASCII
        {
            _passwordBuffer.Add((byte)e.KeyChar);
            UpdateMaskedText();
            e.Handled = true; // Prevent actual char from showing
        }
        else
        {
            e.Handled = true; // Block others
        }
    }

    private void passTxt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Back && _passwordBuffer.Length > 0)
        {
            _passwordBuffer.RemoveAt(_passwordBuffer.Length - 1);
            UpdateMaskedText();
            e.Handled = true; // Prevent default backspace (removes actual char)
        }
        else if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            // Handle submit if needed
        }
    }

    private void confirmPassTxt_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar >= 32 && e.KeyChar <= 126) // Printable ASCII
        {
            _confirmPasswordBuffer.Add((byte)e.KeyChar);
            UpdateMaskedText();
            e.Handled = true; // Prevent actual char from showing
        }
        else
        {
            e.Handled = true; // Block others
        }
    }

    private void confirmPassTxt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Back && _passwordBuffer.Length > 0)
        {
            _confirmPasswordBuffer.RemoveAt(_confirmPasswordBuffer.Length - 1);
            UpdateMaskedText();
            e.Handled = true; // Prevent default backspace (removes actual char)
        }
        else if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            // Handle submit if needed
        }
    }

    #endregion TextboxBehavior
}