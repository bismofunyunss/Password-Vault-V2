using System.Diagnostics;
using System.Runtime.InteropServices;
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
    ///     Securely clears sensitive data from memory by zeroing out provided byte and char arrays using pinned memory.
    ///     Also resets the internal cancellation token source.
    /// </summary>
    /// <param name="password">The user's password in byte array form.</param>
    /// <param name="confirmPassword">The confirmation password in byte array form.</param>
    /// <param name="passwordChars">The user's password in char array form.</param>
    /// <param name="confirmPasswordChars">The confirmation password in char array form.</param>
    /// <param name="passwordHash">The derived password hash to clear from memory.</param>
    /// <param name="passwordDerivedKey">The key derived from the password to clear from memory.</param>
    /// <param name="uuid">The UUID used in the user file to clear from memory.</param>
    /// <param name="encryptedMasterKey">The encrypted master key to be cleared from memory.</param>
    /// <param name="encryptionKey">The key used for encrypting the user file to clear from memory.</param>
    /// <param name="hashSalt">The salt used for hashing the password.</param>
    /// <param name="userFileSalt">The salt used for deriving the user file encryption key.</param>
    /// <param name="intermediateKeySalt">The salt used for deriving the intermediate key.</param>
    /// <param name="masterKeySalt">The salt used for deriving the master key.</param>
    /// <returns>A task that represents the asynchronous cleanup operation.</returns>
    /// <remarks>
    ///     This method uses <see cref="GCHandle" /> to pin each array and zeroes out the memory to reduce the risk of
    ///     sensitive data lingering in memory. It should be called in a final block after sensitive operations complete.
    /// </remarks>
    private static async Task CleanupSecurely(
        byte[]? password, byte[]? confirmPassword, char[]? passwordChars, char[]? confirmPasswordChars,
        byte[]? passwordHash, byte[]? passwordDerivedKey, byte[]? uuid, byte[]? encryptedMasterKey,
        byte[]? encryptionKey,
        byte[]? hashSalt, byte[]? userFileSalt, byte[]? intermediateKeySalt, byte[]? masterKeySalt)
    {
        static void ClearWithGCHandle<T>(params T[]?[] arrays) where T : unmanaged
        {
            foreach (var arr in arrays)
            {
                if (arr == null) continue;

                var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
                try
                {
                    var ptr = handle.AddrOfPinnedObject();
                    var byteSize = Buffer.ByteLength(arr);
                    for (var i = 0; i < byteSize; i++)
                        Marshal.WriteByte(ptr, i, 0);
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        // Clear byte[] and char[] arrays safely
        ClearWithGCHandle(password, confirmPassword, passwordHash, passwordDerivedKey,
            uuid, encryptedMasterKey, encryptionKey,
            hashSalt, userFileSalt, intermediateKeySalt, masterKeySalt);

        ClearWithGCHandle(passwordChars, confirmPasswordChars);

        // Reset cancellation token
        if (_cancellationTokenSource is not null)
        {
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }


    /// <summary>
    /// Starts the label animation indicating that account creation is in progress.
    /// </summary>
    /// <remarks>
    /// Cancels any previously running animation by signaling its cancellation token,
    /// then creates a new <see cref="CancellationTokenSource"/> and begins animating
    /// the label with the text "Creating account".
    /// </remarks>
    private void StartAnimation()
    {
        _cancellationTokenSource?.Cancel(); // Cancel any previous animation
        _cancellationTokenSource = new CancellationTokenSource();
        _animationTask =
            UiController.Animations.AnimateLabel(outputLbl, "Creating account", _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Stops the label animation started by <see cref="StartAnimation"/> asynchronously.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation of canceling the animation
    /// and waiting for it to complete.
    /// </returns>
    /// <remarks>
    /// Attempts to cancel the current animation using the associated cancellation token,
    /// waits for the animation task to complete, handles expected and unexpected exceptions,
    /// and disposes of the cancellation token source.
    /// </remarks>
    /// <exception cref="Exception">
    /// Logs and shows a message box for any unexpected exceptions during cancellation or awaiting the animation task.
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

        char[]? passwordChars = null, confirmPasswordChars = null;

        byte[]? passwordHash = null, passwordDerivedKey = null;
        byte[]? encryptedMasterKey = null;
        byte[]? encryptionKey = null;
        byte[]? hashSalt = null, userFileSalt = null, masterKeySalt = null, intermediateKeySalt = null;
        byte[]? uuid = null;

        try
        {
            passwordChars = _passwordBuffer.ToCharArray();
            confirmPasswordChars = _confirmPasswordBuffer.ToCharArray();
            ValidateUsernameAndPassword(username, passwordChars, confirmPasswordChars);

            // Generate salts and UUID
            hashSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);
            userFileSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);
            masterKeySalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);
            intermediateKeySalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);
            var keyDerivationSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);
            var hkdfSalt = CryptoUtilities.RndByteSized(CryptoConstants.SaltSize);
            uuid = Guid.NewGuid().ToByteArray();

            UserCryptoParameters.HkdfSalt = hkdfSalt;
            IO.CreateUserPath(username);

            // Key derivation
            passwordHash = await HashingMethods.Argon2Id(password, hashSalt, CryptoConstants.PasswordHashSize);
            var intermediateKey = await HashingMethods.Argon2Id(password, intermediateKeySalt, CryptoConstants.KeySize);
            var masterKey = HKDF.HkdfDerivePinned(intermediateKey, masterKeySalt, "master key"u8.ToArray(),
                CryptoConstants.KeySize);

            passwordDerivedKey = await HashingMethods.Argon2Id(password, keyDerivationSalt, CryptoConstants.KeySize);
            encryptedMasterKey = await EncryptFile(masterKey, passwordDerivedKey, hkdfSalt);

            // Build and encrypt user file
            var userFile = IO.BuildUserFile(hashSalt, uuid, hkdfSalt, keyDerivationSalt, intermediateKeySalt,
                masterKeySalt, passwordHash, encryptedMasterKey);
            encryptionKey = await HashingMethods.Argon2Id(password, userFileSalt, CryptoConstants.KeySize);
            var encryptedUserFile = await EncryptFile(userFile, encryptionKey, hkdfSalt);

            var finalFile = userFileSalt.Concat(uuid).Concat(hkdfSalt).Concat(encryptedUserFile).ToArray();
            var path = UserFileManager.GetUserFilePath(username);

            await IO.WriteFile(path, finalFile);
            File.SetAttributes(path, FileAttributes.ReadOnly);

            MessageBox.Show(
                "Registration successful! Make sure you do NOT forget your password or you will lose access to all of your files.",
                "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            try
            {
                await CleanupSecurely(
                    password, confirmPassword,
                    passwordChars, confirmPasswordChars,
                    passwordHash, passwordDerivedKey,
                    uuid, encryptedMasterKey, encryptionKey,
                    hashSalt, userFileSalt, intermediateKeySalt, masterKeySalt);

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                GC.WaitForPendingFinalizers();
            }
            catch (Exception cleanupEx)
            {
                MessageBox.Show(cleanupEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ErrorLogging.ErrorLog(cleanupEx);
            }

            try
            {
                UiController.LogicMethods.EnableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
                outputLbl.Text = "Idle...";
                outputLbl.ForeColor = Color.White;
                userTxt.Clear();
                passTxt.Clear();
                confirmPassTxt.Clear();
            }
            catch (Exception uiEx)
            {
                MessageBox.Show(uiEx.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ErrorLogging.ErrorLog(uiEx);
            }
        }
    }

    /// <summary>
    /// Handles the click event of the <c>CreateAccountBtn</c> button. Initiates the user account registration process,
    /// displaying a warning about potential data corruption, converting secure password buffers to byte arrays, 
    /// and invoking <see cref="RegisterAsync"/>. Ensures sensitive data is disposed and cleared from memory.
    /// </summary>
    /// <param name="sender">The source of the event, typically the <c>CreateAccountBtn</c> control.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    /// <remarks>
    /// This method warns the user not to close the application during registration, securely handles password data,
    /// logs any exceptions, and ensures that sensitive data is cleaned up regardless of success or failure.
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
            @"Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
            @"Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

        var passwordBytes = _passwordBuffer.ToByteArray();
        var confirmPasswordBytes = _confirmPasswordBuffer.ToByteArray();

        try
        {
            await RegisterAsync(userTxt.Text.Trim(), _passwordBuffer.ToByteArray(),
                _confirmPasswordBuffer.ToByteArray());
        }
        catch (Exception ex)
        {
            UiController.LogicMethods.EnableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
            ErrorLogging.ErrorLog(ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _passwordBuffer.Dispose();
            _confirmPasswordBuffer.Dispose();
            CryptoUtilities.ClearMemoryNative(passwordBytes, confirmPasswordBytes);
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
            _confirmPasswordBuffer.RemoveAt(_passwordBuffer.Length - 1);
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