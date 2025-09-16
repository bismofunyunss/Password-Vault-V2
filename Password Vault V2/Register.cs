using System.Buffers;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Security.Cryptography;
using System.Text;
using OtpNet;
using static Password_Vault_V2.Crypto;
using static Password_Vault_V2.FipsCrypto;

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
        byte[] FileKeySalt, byte[] HmacSalt, byte[] DerivedHmacSalt, byte[] KeyHmacSalt,
        byte[] DerivedKeyHmacSalt) GenerateAllSalts()
    {
        var saltSize = FipsEnabled ? SaltSize : CryptoConstants.SaltSize;

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
            CryptoUtilities.RndByteSized(saltSize), // DerivedHmacSalt
            CryptoUtilities.RndByteSized(saltSize), // KeyHmacSalt
            CryptoUtilities.RndByteSized(saltSize) // DerivedKeySalt
        );
    }

    private async Task<DerivedKeys> DeriveKeysAsync(byte[] password,
        (byte[] HashSalt, byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
            byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt, byte[] FileSalt,
            byte[] EncryptionKeySalt, byte[] FileKeySalt, byte[] HmacSalt, byte[] DerivedHmacSalt,
            byte[] KeyHmacSalt, byte[] DerivedKeyHmacSalt) salts)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        if (!FipsEnabled)
        {
            var keys = new DerivedKeys(CryptoConstants.KeySize, CryptoConstants.PasswordHashSize);

            // Password Hash (Argon2id)
            {
                byte[] hash = null!;
                try
                {
                    hash = await HashingMethods.Argon2Id(password, salts.HashSalt, CryptoConstants.PasswordHashSize);
                    hash.AsSpan().CopyTo(keys.PasswordHash.AsSpan());
                }
                finally
                {
                    if (hash != null) CryptographicOperations.ZeroMemory(hash);
                }
            }

            // Password Derived Key -> IntermediateKey (Argon2id -> HKDF)
            {
                byte[] derivedKey = null!;
                byte[] intermediateKey = null!;
                try
                {
                    derivedKey =
                        await HashingMethods.Argon2Id(password, salts.KeyDerivationSalt, CryptoConstants.KeySize);
                    intermediateKey = DeriveAndPin(derivedKey, salts.IntermediateKeySalt,
                        "intermediate key"u8.ToArray(), CryptoConstants.KeySize);

                    // copy into secure buffer
                    intermediateKey.AsSpan().CopyTo(keys.IntermediateKey.AsSpan());
                }
                finally
                {
                    if (derivedKey != null) CryptographicOperations.ZeroMemory(derivedKey);
                    if (intermediateKey != null) CryptographicOperations.ZeroMemory(intermediateKey);
                }
            }

            // Master Key (RNG)
            {
                var masterKey = CryptoUtilities.RndByteSized(CryptoConstants.KeySize);
                try
                {
                    masterKey.AsSpan().CopyTo(keys.MasterKey.AsSpan());
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(masterKey);
                }
            }

            // Derived File Key -> EncryptionKey (Argon2id -> HKDF)
            {
                byte[] derivedFileKey = null!;
                byte[] encryptionKey = null!;
                try
                {
                    derivedFileKey =
                        await HashingMethods.Argon2Id(password, salts.FileKeySalt, CryptoConstants.KeySize);
                    encryptionKey = DeriveAndPin(derivedFileKey, salts.EncryptionKeySalt, "encryption key"u8.ToArray(),
                        CryptoConstants.KeySize);

                    encryptionKey.AsSpan().CopyTo(keys.EncryptionKey.AsSpan());
                }
                finally
                {
                    if (derivedFileKey != null) CryptographicOperations.ZeroMemory(derivedFileKey);
                    if (encryptionKey != null) CryptographicOperations.ZeroMemory(encryptionKey);
                }
            }

            // Derived HMAC Key -> HmacKey (Argon2id -> HKDF)
            {
                byte[] derivedHmacKey = null!;
                byte[] hmacKey = null!;
                try
                {
                    derivedHmacKey =
                        await HashingMethods.Argon2Id(password, salts.DerivedHmacSalt, CryptoConstants.HmacLength);
                    hmacKey = DeriveAndPin(derivedHmacKey, salts.HmacSalt, "hmac key"u8.ToArray(),
                        CryptoConstants.HmacLength);

                    hmacKey.AsSpan().CopyTo(keys.HmacKey.AsSpan());
                }
                finally
                {
                    if (derivedHmacKey != null) CryptographicOperations.ZeroMemory(derivedHmacKey);
                    if (hmacKey != null) CryptographicOperations.ZeroMemory(hmacKey);
                }
            }

            // Key-wrap HMAC (Argon2id -> HKDF) => KwHmacKey
            {
                byte[] keyHmac = null!;
                byte[] wrappedKeyHmac = null!;
                try
                {
                    keyHmac = await HashingMethods.Argon2Id(password, salts.KeyHmacSalt, CryptoConstants.HmacLength);
                    wrappedKeyHmac = DeriveAndPin(keyHmac, salts.DerivedKeyHmacSalt, "kw hmac key"u8.ToArray(),
                        CryptoConstants.HmacLength);

                    wrappedKeyHmac.AsSpan().CopyTo(keys.KwHmacKey.AsSpan());
                }
                finally
                {
                    if (keyHmac != null) CryptographicOperations.ZeroMemory(keyHmac);
                    if (wrappedKeyHmac != null) CryptographicOperations.ZeroMemory(wrappedKeyHmac);
                }
            }

            return keys;
        }

        var fipsKeys = new DerivedKeys(CryptoConstants.KeySize, 32);

        // 1️⃣ Password Hash (PBKDF2-SHA256)
        {
            byte[] hash = null!;
            try
            {
                hash = await Pbkdf2(password, salts.HashSalt, 32);
                hash.AsSpan().CopyTo(fipsKeys.PasswordHash.AsSpan());
            }
            finally
            {
                if (hash != null) CryptographicOperations.ZeroMemory(hash);
            }
        }

        // 2️⃣ Intermediate Key
        {
            byte[] derivedKey = null!;
            byte[] intermediateKey = null!;
            try
            {
                derivedKey = await Pbkdf2(password, salts.KeyDerivationSalt, KeySize);
                intermediateKey = FipsHkdf.DeriveKey(
                    derivedKey,
                    salts.IntermediateKeySalt,
                    "intermediate key"u8.ToArray(),
                    CryptoConstants.KeySize);
                intermediateKey.AsSpan().CopyTo(fipsKeys.IntermediateKey.AsSpan());
            }
            finally
            {
                if (derivedKey != null) CryptographicOperations.ZeroMemory(derivedKey);
                if (intermediateKey != null) CryptographicOperations.ZeroMemory(intermediateKey);
            }
        }

        // 3️⃣ Master Key (random)
        {
            var masterKey = CryptoUtilities.RndByteSized(CryptoConstants.KeySize);
            masterKey.AsSpan().CopyTo(fipsKeys.MasterKey.AsSpan());
        }

        // 4️⃣ Encryption Key
        {
            byte[] derivedFileKey = null!;
            byte[] encryptionKey = null!;
            try
            {
                derivedFileKey = await Pbkdf2(password, salts.FileKeySalt, KeySize);
                encryptionKey = FipsHkdf.DeriveKey(
                    derivedFileKey,
                    salts.EncryptionKeySalt,
                    "encryption key"u8.ToArray(),
                    CryptoConstants.KeySize);
                encryptionKey.AsSpan().CopyTo(fipsKeys.EncryptionKey.AsSpan());
            }
            finally
            {
                if (derivedFileKey != null) CryptographicOperations.ZeroMemory(derivedFileKey);
                if (encryptionKey != null) CryptographicOperations.ZeroMemory(encryptionKey);
            }
        }

        // 5️⃣ HMAC Key
        {
            byte[] derivedHmacKey = null!;
            byte[] hmacKey = null!;
            try
            {
                derivedHmacKey = await Pbkdf2(password, salts.DerivedHmacSalt, KeySize);
                hmacKey = FipsHkdf.DeriveKey(
                    derivedHmacKey,
                    salts.HmacSalt,
                    "hmac key"u8.ToArray(),
                    32); // SHA3-512 would require 64 bytes in non-FIPS
                hmacKey.AsSpan().CopyTo(fipsKeys.HmacKey.AsSpan());
            }
            finally
            {
                if (derivedHmacKey != null) CryptographicOperations.ZeroMemory(derivedHmacKey);
                if (hmacKey != null) CryptographicOperations.ZeroMemory(hmacKey);
            }
        }

        // 6️⃣ Key-Wrap HMAC Key
        {
            byte[] derivedKeyHmac = null!;
            byte[] kwHmacKey = null!;
            try
            {
                derivedKeyHmac = await Pbkdf2(password, salts.KeyHmacSalt, KeySize);
                kwHmacKey = FipsHkdf.DeriveKey(
                    derivedKeyHmac,
                    salts.DerivedKeyHmacSalt,
                    "kw hmac key"u8.ToArray(),
                    32);
                kwHmacKey.AsSpan().CopyTo(fipsKeys.KwHmacKey.AsSpan());
            }
            finally
            {
                if (derivedKeyHmac != null) CryptographicOperations.ZeroMemory(derivedKeyHmac);
                if (kwHmacKey != null) CryptographicOperations.ZeroMemory(kwHmacKey);
            }
        }
        return fipsKeys;
    }


    private byte[] BuildFinalFile(
        (byte[] HashSalt, byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
            byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt, byte[] FileSalt,
            byte[] EncryptionKeySalt, byte[] FileKeySalt, byte[] HmacSalt, byte[] DerivedHmacSalt,
            byte[] KeyHmacSalt,
            byte[] DerivedKeyHmacSalt) salts,
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
            .Concat(salts.KeyHmacSalt)
            .Concat(salts.DerivedKeyHmacSalt)
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
    ///     Safely copies a SecureBuffer to a temporary byte[],
    ///     executes an action, then zeroes the copy.
    /// </summary>
    private static T UseKeyCopy<T>(SecureBuffer secure, Func<byte[], T> action)
    {
        var copy = secure.ToArrayCopy();
        try
        {
            return action(copy);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(copy);
        }
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
            ValidateUsernameAndPassword(username, _passwordBuffer.ToCharArray(), _confirmPasswordBuffer.ToCharArray());

            var salts = GenerateAllSalts();
            IO.CreateUserPath(username);

            using var derivedKeys = await DeriveKeysAsync(password, salts);

            // Encrypt master key with intermediate key
            var encryptedMasterKey = UseKeyCopy(derivedKeys.IntermediateKey, intermediateKeyCopy =>
                EncryptFile(
                    derivedKeys.MasterKey.AsSpan().ToArray(),
                    intermediateKeyCopy,
                    salts.MasterKeyEncryptionSalt).GetAwaiter().GetResult());

            // Compute HMAC over wrapped blob
            var mac = UseKeyCopy(derivedKeys.KwHmacKey, kwHmacKeyCopy =>
            {
                using var h = new HMACSHA256(kwHmacKeyCopy);
                return h.ComputeHash(encryptedMasterKey);
            });

            // Combine wrapped key + HMAC
            var toSeal = new byte[encryptedMasterKey.Length + mac.Length];
            Buffer.BlockCopy(encryptedMasterKey, 0, toSeal, 0, encryptedMasterKey.Length);
            Buffer.BlockCopy(mac, 0, toSeal, encryptedMasterKey.Length, mac.Length);

            // Seal with TPM
            using var wrappedStore = new WrappedAesKeyStore("MyTpmRsaKey");
            var rsaEncryptedBlob = wrappedStore.SealWrappedKey(toSeal);
            CryptographicOperations.ZeroMemory(toSeal);

            using var keyStore = new SoftwareKeyStore(UserFileManager.GetUserFolder(userTxt.Text));
            keyStore.AddNewMasterKey(rsaEncryptedBlob, "Initial key");

            var uuid = Guid.NewGuid().ToByteArray();

            var userFile = IO.BuildUserFile(
                derivedKeys.PasswordHash.AsSpan().ToArray(),
                uuid,
                encryptedMasterKey);

            var encryptedUserFile = UseKeyCopy(derivedKeys.EncryptionKey, encryptionKeyCopy =>
                EncryptFile(
                    userFile,
                    encryptionKeyCopy,
                    salts.FileSalt).GetAwaiter().GetResult());

            var hmac = UseKeyCopy(derivedKeys.HmacKey, hmacKeyCopy =>
                HashingMethods.HmacSha3(encryptedUserFile, hmacKeyCopy));

            var finalFile = BuildFinalFile(salts, uuid, encryptedUserFile);

            var path = UserFileManager.GetUserFilePath(username);
            await IO.WriteFile(path, finalFile);
            File.SetAttributes(path, FileAttributes.ReadOnly);

            CryptographicOperations.ZeroMemory(encryptedMasterKey);
            CryptographicOperations.ZeroMemory(encryptedUserFile);
            CryptographicOperations.ZeroMemory(finalFile);
            CryptographicOperations.ZeroMemory(hmac);
            CryptographicOperations.ZeroMemory(password);
            CryptographicOperations.ZeroMemory(confirmPassword);

            if (userFile != null)
                CryptographicOperations.ZeroMemory(userFile);

            if (mac != null)
                CryptographicOperations.ZeroMemory(mac);
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

            UiController.LogicMethods.EnableUi(userTxt, CreateAccountBtn, passTxt, confirmPassTxt);
            userTxt.Clear();
            passTxt.Clear();
            confirmPassTxt.Clear();
            emailBox.Clear();
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

            var salts = GenerateAllSalts();
            using var keys = await DeriveKeysAsync(password, salts);
            var uuid = Guid.NewGuid().ToByteArray();

            // Wrap master key (RFC 3394)
            var wrappedKey = UseKeyCopy(keys.IntermediateKey, intermediateKeyCopy =>
                AesKeyWrapRfc3394.Wrap(intermediateKeyCopy, keys.MasterKey.AsSpan().ToArray()));

            // Compute HMAC
            var mac = UseKeyCopy(keys.KwHmacKey, kwHmacKeyCopy =>
            {
                using var h = new HMACSHA256(kwHmacKeyCopy);
                return h.ComputeHash(wrappedKey);
            });

            // Combine wrapped key + MAC
            var toSeal = new byte[wrappedKey.Length + mac.Length];
            Buffer.BlockCopy(wrappedKey, 0, toSeal, 0, wrappedKey.Length);
            Buffer.BlockCopy(mac, 0, toSeal, wrappedKey.Length, mac.Length);

            var userFile = IO.BuildUserFile(
                keys.PasswordHash.AsSpan().ToArray(),
                uuid,
                Encoding.UTF8.GetBytes(email),
                wrappedKey);

            var encryptedUserFile = UseKeyCopy(keys.EncryptionKey, encryptionKeyCopy =>
                UseKeyCopy(keys.HmacKey, hmacKeyCopy =>
                    SimpleAesHmac.Encrypt(
                        encryptionKeyCopy,
                        hmacKeyCopy,
                        userFile)));

            var finalFile = BuildFinalFile(salts, uuid, encryptedUserFile);

            var path = UserFileManager.GetUserFilePath(username);

            using var wrappedStore = new WrappedAesKeyStore("MyTpmRsaKey");
            var rsaEncryptedBlob = wrappedStore.SealWrappedKey(toSeal);
            CryptographicOperations.ZeroMemory(toSeal);

            using var keyStore = new SoftwareKeyStore(UserFileManager.GetUserFolder(userTxt.Text));
            keyStore.AddNewMasterKey(rsaEncryptedBlob, "Initial key");

            CryptoUtilities.ClearMemoryNative(wrappedKey, rsaEncryptedBlob, keys.MasterKey.AsSpan().ToArray());

            CryptographicOperations.ZeroMemory(keys.IntermediateKey.AsSpan());
            CryptographicOperations.ZeroMemory(password);
            CryptographicOperations.ZeroMemory(confirmPassword);

            if (userFile != null)
                CryptographicOperations.ZeroMemory(userFile);

            if (mac != null)
                CryptographicOperations.ZeroMemory(mac);

            await IO.WriteFile(path, finalFile);
            File.SetAttributes(path, FileAttributes.ReadOnly);
            LoginAlertManager.RegisterUserEmail(userTxt.Text, emailBox.Text);
            await LoginAlertManager.SendLoginAlertAsync(userTxt.Text);


            CryptographicOperations.ZeroMemory(wrappedKey);
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
            emailBox.Clear();
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

        // Registration
        string username = userTxt.Text.Trim();
        byte[] userSecret = RandomNumberGenerator.GetBytes(20); // 160-bit secret

        try
        {
            // Store securely
            using (var store = new SoftwareKeyStore(UserFileManager.GetUserFolder(username)))
            {
                store.AddTotpSecret(username, userSecret, "Authenticator");
            }

            // Base32 for QR code
            string base32Secret = Base32Encoding.ToString(userSecret).TrimEnd('=').ToUpperInvariant();
            string issuer = "Password Vault";

            // Show QR code for scanning
            using (var verifyForm = new TotpVerify(userSecret, issuer, username, base32Secret))
            {
                if (verifyForm.ShowDialog() != DialogResult.OK)
                {
                    MessageBox.Show("You must verify your Authenticator before continuing.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // do not zero here; form already wiped secret internally
                }
            }


            // Continue with account creation
            var passwordBytes = _passwordBuffer.ToByteArray();
            var confirmPasswordBytes = _confirmPasswordBuffer.ToByteArray();

            try
            {
                if (!FipsEnabled)
                    await RegisterAsync(username, passwordBytes, confirmPasswordBytes);
                else
                    await FipsModeRegisterAsync(username, passwordBytes, confirmPasswordBytes, username);

                // ✅ Update label on success
                outputLbl.Text = "User created successfully.";
                outputLbl.ForeColor = Color.LimeGreen;
                MessageBox.Show(
                    "Registration successful! Do NOT forget your password.",
                    "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                // Update label to idle if registration fails
                outputLbl.Text = "Idle";
                outputLbl.ForeColor = Color.White;
                throw;
            }
            finally
            {
                outputLbl.Text = "Idle...";
                outputLbl.ForeColor = Color.White;
                _passwordBuffer.Dispose();
                _confirmPasswordBuffer.Dispose();
                CryptoUtilities.ClearMemoryNative(passwordBytes, confirmPasswordBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(userSecret);
        }
    }

    private void Register_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); // Clear previous drawings
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
    }

    private void RegisterBox_Enter(object sender, EventArgs e)
    {
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

        public int Size { get; }

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
            if (_buffer is null)
                throw new ObjectDisposedException(nameof(SecureBuffer));
            return _buffer.AsSpan(0, Size);
        }

        /// <summary>
        ///     Returns a copy of the buffer without clearing the original.
        /// </summary>
        public byte[] ToArrayCopy()
        {
            return AsSpan().ToArray();
        }

        /// <summary>
        ///     Returns a copy of the buffer and clears the original.
        ///     Useful for extracting secrets safely.
        /// </summary>
        public byte[] ExtractAndClear()
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
            HmacKey = new SecureBuffer(FipsEnabled ? 32 : CryptoConstants.HmacLength);
            KwHmacKey = new SecureBuffer(FipsEnabled ? 32 : CryptoConstants.HmacLength);
        }

        public SecureBuffer PasswordHash { get; }
        public SecureBuffer IntermediateKey { get; }
        public SecureBuffer MasterKey { get; }
        public SecureBuffer EncryptionKey { get; }
        public SecureBuffer HmacKey { get; }
        public SecureBuffer KwHmacKey { get; }

        public void Dispose()
        {
            PasswordHash.Dispose();
            IntermediateKey.Dispose();
            MasterKey.Dispose();
            EncryptionKey.Dispose();
            HmacKey.Dispose();
            KwHmacKey.Dispose();
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