using System.Buffers.Binary;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Tpm2Lib;
using static Password_Vault_V2.Crypto;
using static Password_Vault_V2.FipsCrypto;

namespace Password_Vault_V2;

public sealed partial class PasswordVault : Form
{
    private readonly SecurePasswordBuffer _passwordBuffer = new();
    private readonly int borderRadius = 20;
    private readonly int borderSize = 4;
    public readonly Variables Vars = new();
    private Color borderColor = Color.FromArgb(128, 128, 255);

    public PasswordVault()
    {
        InitializeComponent();
    }

    private void BtnLogin_Paint(object sender, PaintEventArgs e)
    {
    }

    private void PasswordVault_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); // Clear background before drawing
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        BorderRenderer.DrawSmoothGradientBorder(this, e.Graphics, borderRadius, borderSize, Color.DeepSkyBlue,
            Color.DeepSkyBlue, Color.DeepSkyBlue, Color.DeepSkyBlue);
    }

    #region Variables

    public class Variables
    {
        public int AttemptsRemaining;

        public bool IsDragging;
        public Point Offset;

        public CancellationTokenSource RainbowTokenSource = new();
        public CancellationTokenSource TokenSource = new();

        public Vault VaultControls { get; } = new();
        public Register RegisterControls { get; } = new();
        public Encryption EncryptionControls { get; } = new();
        public FileHash FileHashControls { get; } = new();
        public CryptoSettings CryptoSettingsControls { get; } = new();
        public CancellationToken Token => TokenSource.Token;
        public CancellationToken RainbowLabelToken => RainbowTokenSource.Token;
    }

    #endregion

    #region Methods

    #region Event Handlers

    private async void BtnLogin_Click(object sender, EventArgs e)
    {
        try
        {
            var result = await WindowsHello.RequestWindowsHelloSignInAsync();
            if (!result)
            {
                MessageBox.Show("Windows hello failed to authenticate.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var passwordBytes = ExtractPasswordBytes();
            SaveRememberMeSetting();
            ValidateRemainingAttempts();
            ValidateLoginInputs(passwordBytes);

            ShowLoadingWarning();
            UiController.LogicMethods.DisableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn);

            var userExists = UserFileManager.UserExists(UsernameTxt.Text.Trim());
            await ProcessLogin(userExists);
        }
        catch (Exception ex)
        {
            HandleLoginException(ex);
        }
        finally
        {
            ClearPasswordBuffer();
        }
    }

    #endregion

    #region Core Login Flow

    private async Task ProcessLogin(bool userExists)
    {
        if (userExists)
            await StartLoginProcessAsync();
        else
            throw new Exception("Username does not exist.");
    }

    private async Task StartLoginProcessAsync()
    {
        var cts = PrepareCancellationToken();
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        StartAnimation();

        var passwordBytes = ExtractPasswordBytes();

        // Sensitive working memory
        byte[] decryptedBytes = [],
            passwordDerivedKey = [],
            derivedFileKey = [],
            encryptionKey = [],
            intermediateKey = [],
            decryptedMasterKey = [];

        var handles = CryptoUtilities.PinArrays(passwordBytes, decryptedBytes, derivedFileKey,
            passwordDerivedKey, encryptionKey, intermediateKey, decryptedMasterKey);

        try
        {
            var keyStore = new SoftwareKeyStore(UserFileManager.GetUserFolder(UsernameTxt.Text));

            var useFips = FipsEnabled;

            // Load and parse file
            var userFile = await LoadUserFile(UsernameTxt.Text, useFips);

            var segments = useFips
                ? ExtractFileSegmentsFips(userFile)
                : ExtractFileSegmentsNonFips(userFile);

            // Derive keys
            var keys = useFips
                ? await DeriveKeysFips(passwordBytes, segments)
                : await DeriveKeys(passwordBytes, segments);

            if (!useFips)
                ValidateHmac(segments.EncryptedFile, keys.HmacKey, segments.Hmac);

            // Dont need to validate hmac with FIPS since the internal decrypt will throw on mismatch.

                // Decrypt main file
                decryptedBytes = useFips
                    ? SimpleAesHmac.Decrypt(keys.EncryptionKey, keys.HmacKey, segments.EncryptedFile)
                    : await DecryptFile(segments.EncryptedFile, keys.EncryptionKey, segments.FileSalt);

            // Parse decrypted file
            var parts = ParseUserFile(decryptedBytes);

            // Verify identity
            VerifyUuid(parts[1], segments.Uuid);
            if (!useFips)
                VerifyPassword(passwordBytes, parts[0], segments.HashSalt);
            else
                VerifyPasswordFips(passwordBytes, parts[0], segments.HashSalt);

            // Decrypt master key
            decryptedMasterKey = useFips
                ? keyStore.RetrieveMasterKey(keys.IntermediateKey, 1)
                : await DecryptFile(parts[3], keys.IntermediateKey, segments.MasterKeySalt);

            // Secure master key
            MasterKey.SecureKey(decryptedMasterKey);

            // Finish login
            await HandleLogin();

        }
        finally
        {
            CryptoUtilities.ClearMemoryNative(passwordBytes, decryptedBytes, passwordDerivedKey,
                derivedFileKey, encryptionKey, intermediateKey, decryptedMasterKey);
            CryptoUtilities.FreeArrays(handles);

            StatusOutputLabel.Text = "Idle...";
            StatusOutputLabel.ForeColor = Color.White;
            PasswordTxt.Clear();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
    }

    #endregion

    #region Extraction & Validation Helpers

    private static CancellationToken PrepareCancellationToken()
    {
        var cts = new CancellationTokenSource();
        // Optional: configure timeout or link to external token here
        return cts.Token;
    }

    private byte[] ExtractPasswordBytes()
    {
        return _passwordBuffer.ToByteArray();
    }

    private void SaveRememberMeSetting()
    {
        Settings.Default.userName = RememberMeCheckBox.Checked ? UsernameTxt.Text : string.Empty;
        Settings.Default.Save();
    }

    public static byte[][] ParseUserFile(byte[] userFile)
    {
        if (userFile == null) throw new ArgumentNullException(nameof(userFile));

        var segments = new List<byte[]>();
        int offset = 0;

        while (offset < userFile.Length)
        {
            if (offset + 4 > userFile.Length)
                throw new CryptographicException("Corrupted user file: cannot read segment length.");

            int length = BitConverter.ToInt32(userFile, offset); // little-endian
            offset += 4;

            if (length < 0 || offset + length > userFile.Length)
                throw new CryptographicException("Corrupted user file: invalid segment length.");

            var segment = new byte[length];
            Buffer.BlockCopy(userFile, offset, segment, 0, length);
            segments.Add(segment);
            offset += length;
        }

        return segments.ToArray();
    }


    private void ValidateRemainingAttempts()
    {
        if (!int.TryParse(AttemptsNumberLabel.Text, out Vars.AttemptsRemaining))
            throw new Exception("Unable to parse attempts remaining value.");

        if (Vars.AttemptsRemaining == 0)
            throw new Exception("No attempts remaining. Please restart the program and try again.");
    }

    private void ValidateLoginInputs(byte[] passwordBytes)
    {
        if (string.IsNullOrEmpty(UsernameTxt.Text))
            throw new Exception("Username value was empty.");

        if (passwordBytes == null || passwordBytes.Length == 0)
            throw new Exception("Password array was empty or null.");
    }

    private void ShowLoadingWarning()
    {
        MessageBox.Show(
            "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
            "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }

    #endregion

    #region File & Key Handling

    private async Task<byte[]> LoadUserFile(string username, bool useFips)
    {
        var path = UserFileManager.GetUserFilePath(username);
        var file = await IO.ReadFile(path);

        // Pick correct salt size
        var saltSize = useFips ? 32 : CryptoConstants.SaltSize;

        // Minimum length = 2 salts + UUID
        if (file.Length < saltSize * 2 + CryptoConstants.UuidSize)
            throw new FileNotFoundException("User data is incomplete or corrupted.");

        return file;
    }

    private (byte[] Hmac, byte[] HmacSalt, byte[] DerivedHmacSalt,
            byte[] FileSalt, byte[] FileKeySalt, byte[] EncryptionKeySalt,
            byte[] Uuid, byte[] HashSalt, byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt,
            byte[] KeyDerivationSalt, byte[] IntermediateKeySalt, byte[] EncryptedFile)
       ExtractFileSegmentsNonFips(byte[] file)
    {
        int offset = 0;
        byte[] ReadSegment(int length)
        {
            var segment = new byte[length];
            Buffer.BlockCopy(file, offset, segment, 0, length);
            offset += length;
            return segment;
        }

        var hmac = ReadSegment(CryptoConstants.HmacLength);
        var hmacSalt = ReadSegment(CryptoConstants.SaltSize);
        var derivedHmacSalt = ReadSegment(CryptoConstants.SaltSize);
        var fileSalt = ReadSegment(CryptoConstants.SaltSize);
        var fileKeySalt = ReadSegment(CryptoConstants.SaltSize);
        var encryptionKeySalt = ReadSegment(CryptoConstants.SaltSize);
        var uuid = ReadSegment(CryptoConstants.UuidSize);
        var hashSalt = ReadSegment(CryptoConstants.SaltSize);
        var masterKeyEncryptionSalt = ReadSegment(CryptoConstants.SaltSize);
        var keyDerivationSalt = ReadSegment(CryptoConstants.SaltSize);
        var intermediateKeySalt = ReadSegment(CryptoConstants.SaltSize);
        var masterKeySalt = ReadSegment(CryptoConstants.SaltSize);

        var encryptedFile = new byte[file.Length - offset];
        Buffer.BlockCopy(file, offset, encryptedFile, 0, encryptedFile.Length);

        return (hmac, hmacSalt, derivedHmacSalt, fileSalt, fileKeySalt, encryptionKeySalt,
                uuid, hashSalt, masterKeySalt, masterKeyEncryptionSalt,
                keyDerivationSalt, intermediateKeySalt, encryptedFile);
    }

    private (byte[] Hmac, byte[] HmacSalt, byte[] DerivedHmacSalt,
             byte[] FileSalt, byte[] FileKeySalt, byte[] EncryptionKeySalt,
             byte[] Uuid, byte[] HashSalt, byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt,
             byte[] KeyDerivationSalt, byte[] IntermediateKeySalt, byte[] EncryptedFile)
        ExtractFileSegmentsFips(byte[] file)
    {
        int offset = 0;
        int saltSize = 32; // PBKDF2 FIPS salts

        byte[] ReadSegment(int length)
        {
            var segment = new byte[length];
            Buffer.BlockCopy(file, offset, segment, 0, length);
            offset += length;
            return segment;
        }

        var hmacSalt = ReadSegment(saltSize);
        var derivedHmacSalt = ReadSegment(saltSize);
        var fileSalt = ReadSegment(saltSize);
        var fileKeySalt = ReadSegment(saltSize);
        var encryptionKeySalt = ReadSegment(saltSize);
        var uuid = ReadSegment(CryptoConstants.UuidSize);
        var hashSalt = ReadSegment(saltSize);
        var masterKeyEncryptionSalt = ReadSegment(saltSize);
        var keyDerivationSalt = ReadSegment(saltSize);
        var intermediateKeySalt = ReadSegment(saltSize);

        // The remaining bytes are the HMAC + IV + ciphertext
        var encryptedFile = new byte[file.Length - offset];
        Buffer.BlockCopy(file, offset, encryptedFile, 0, encryptedFile.Length);

        // For FIPS, HMAC is embedded at start of encryptedFile (inside SimpleAesHmac)
        byte[] hmac = null; // handled inside SimpleAesHmac.Decrypt

        return (hmac, hmacSalt, derivedHmacSalt, fileSalt, fileKeySalt, encryptionKeySalt,
            uuid, hashSalt, null, masterKeyEncryptionSalt,
            keyDerivationSalt, intermediateKeySalt, encryptedFile)!;
    }




    private async Task<(byte[] HmacKey, byte[] EncryptionKey, byte[] IntermediateKey)>
        DeriveKeys(byte[] password,
            (byte[] Hmac, byte[] HmacSalt, byte[] DerivedHmacSalt,
                byte[] FileSalt, byte[] FileKeySalt, byte[] EncryptionKeySalt,
                byte[] Uuid, byte[] HashSalt, byte[] MasterKeySalt, byte[] MasterKeyDerivationSalt,
                byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
                byte[] EncryptedFile) segments)
    {
        var passwordDerivedKey =
            await HashingMethods.Argon2Id(password, segments.KeyDerivationSalt, CryptoConstants.KeySize);
        var derivedFileKey = await HashingMethods.Argon2Id(password, segments.FileKeySalt, CryptoConstants.KeySize);
        var derivedHmacKey = await HashingMethods.Argon2Id(password, segments.DerivedHmacSalt, CryptoConstants.KeySize);

        var hmacKey = Crypto.HKDF.HkdfDerivePinned(derivedHmacKey, segments.HmacSalt, "hmac key"u8.ToArray(),
            CryptoConstants.HmacLength);
        var intermediateKey = Crypto.HKDF.HkdfDerivePinned(passwordDerivedKey, segments.IntermediateKeySalt,
            "intermediate key"u8.ToArray(), CryptoConstants.KeySize);
        var encryptionKey = Crypto.HKDF.HkdfDerivePinned(derivedFileKey, segments.EncryptionKeySalt,
            "encryption key"u8.ToArray(), CryptoConstants.KeySize);

        return (hmacKey, encryptionKey, intermediateKey);
    }

    private async Task<(byte[] HmacKey, byte[] EncryptionKey, byte[] IntermediateKey)>
        DeriveKeysFips(byte[] password,
            (byte[] Hmac, byte[] HmacSalt, byte[] DerivedHmacSalt,
                byte[] FileSalt, byte[] FileKeySalt, byte[] EncryptionKeySalt,
                byte[] Uuid, byte[] HashSalt, byte[] MasterKeySalt, byte[] MasterKeyEncryptionSalt,
                byte[] KeyDerivationSalt, byte[] IntermediateKeySalt,
                byte[] EncryptedFile) segments)
    {
        // PBKDF2-HMAC-SHA256 for FIPS compliance
        var passwordDerivedKey =
            Pbkdf2(password, segments.KeyDerivationSalt, CryptoConstants.KeySize);
        var derivedFileKey =
            Pbkdf2(password, segments.FileKeySalt, CryptoConstants.KeySize);
        var derivedHmacKey =
            Pbkdf2(password, segments.DerivedHmacSalt, CryptoConstants.KeySize);

        // HKDF derivations (must be using FIPS-approved HMAC internally)
        var hmacKey = Hkdf.DeriveKey(derivedHmacKey, segments.HmacSalt, "hmac key"u8.ToArray(),
            32);
        var intermediateKey = Hkdf.DeriveKey(passwordDerivedKey, segments.IntermediateKeySalt,
            "intermediate key"u8.ToArray(), CryptoConstants.KeySize);
        var encryptionKey = Hkdf.DeriveKey(derivedFileKey, segments.EncryptionKeySalt,
            "encryption key"u8.ToArray(), CryptoConstants.KeySize);

        return (hmacKey, encryptionKey, intermediateKey);
    }


    private void ValidateHmac(byte[] encryptedFile, byte[] hmacKey, byte[] expectedHmac)
    {
        var calculatedHmac = HashingMethods.HmacSha3(encryptedFile, hmacKey);
        if (!CryptographicOperations.FixedTimeEquals(expectedHmac, calculatedHmac))
            throw new CryptographicException("Authentication tag does not match.");
    }

    private void VerifyUuid(byte[] storedUuid, byte[] fileUuid)
    {
        if (!CryptographicOperations.FixedTimeEquals(storedUuid, fileUuid))
            throw new UnauthorizedAccessException("UUID does not match.");
    }

    private async void VerifyPassword(byte[] password, byte[] storedHash, byte[] hashSalt)
    {
        var derivedHash = await HashingMethods.Argon2Id(password, hashSalt, CryptoConstants.HmacLength);
        if (!CryptoUtilities.ComparePassword(derivedHash, storedHash))
            throw new UnauthorizedAccessException("Invalid password.");
    }

    private async Task VerifyPasswordFips(byte[] password, byte[] storedHash, byte[] hashSalt)
    {
        // PBKDF2-HMAC-SHA512 with high iterations for compliance
        var derivedHash = Pbkdf2(
            password,
            hashSalt,
            32);

        if (!CryptoUtilities.ComparePassword(derivedHash, storedHash))
            throw new UnauthorizedAccessException("Invalid password.");
    }

    private void ValidateHmacFips(byte[] encryptedFile, byte[] hmacKey, byte[] expectedHmac)
    {
        // Use HMAC-SHA256 for FIPS approval
        using var hmac = new HMACSHA256(hmacKey);
        var calculatedHmac = hmac.ComputeHash(encryptedFile);

        if (!CryptographicOperations.FixedTimeEquals(expectedHmac, calculatedHmac))
            throw new CryptographicException("Authentication tag does not match.");
    }

    #endregion

    #region UI & Error Handling

    private void HandleLoginException(Exception ex)
    {
        ErrorLogging.ErrorLog(ex);
        Vars.TokenSource.CancelAsync().Wait();
        Vars.TokenSource = new CancellationTokenSource();

        Vars.AttemptsRemaining--;
        AttemptsNumberLabel.Text = Vars.AttemptsRemaining.ToString();

        StatusOutputLabel.ForeColor = Color.White;
        StatusOutputLabel.Text = "Idle...";
        UiController.LogicMethods.EnableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn);

        MessageBox.Show("An error occured while logging in.", "Error", MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void ClearPasswordBuffer()
    {
        PasswordTxt.Clear();
        _passwordBuffer.Dispose();
    }

    #endregion


    /// <summary>
    ///     Handles UI and internal state recovery after a failed login attempt.
    /// </summary>
    /// <remarks>
    ///     This method performs the following recovery actions:
    ///     <list type="number">
    ///         <item>
    ///             <description>Re-enables UI elements for another login attempt.</description>
    ///         </item>
    ///         <item>
    ///             <description>Updates the status label to reflect the login failure.</description>
    ///         </item>
    ///         <item>
    ///             <description>Cancels and resets the current cancellation token source.</description>
    ///         </item>
    ///         <item>
    ///             <description>Displays an error message to the user.</description>
    ///         </item>
    ///         <item>
    ///             <description>Clears the password textbox and resets the status label to idle.</description>
    ///         </item>
    ///         <item>
    ///             <description>Decrements the remaining login attempts and updates the UI label.</description>
    ///         </item>
    ///     </list>
    ///     All sensitive information from the failed login attempt is cleared from memory or UI elements.
    /// </remarks>
    private async Task HandleLogin()
    {
        var username = UsernameTxt.Text;
        var userFilePath = UserFileManager.GetUserFilePath(username);
        var userVaultPath = UserFileManager.GetUserVault(username);

        if (!File.Exists(userFilePath))
            throw new FileNotFoundException("User file does not exist.");

        var masterKey = MasterKey.GetKey();

        if (masterKey == null)
            throw new InvalidOperationException("Master key not initialized.");

        try
        {
            if (File.Exists(userVaultPath)) Vars.VaultControls.LoadVault();

            // Post-login success
            StatusOutputLabel.ForeColor = Color.LimeGreen;
            StatusOutputLabel.Text = "Access granted";

            if (!Vars.TokenSource.IsCancellationRequested)
                await Vars.TokenSource.CancelAsync();

            await Task.Delay(50); // Optional small buffer to ensure canceled tasks respond

            Vars.TokenSource.Dispose(); // Always dispose before replacing
            Vars.TokenSource = new CancellationTokenSource();

            UserLog.LogUser(username);

            MessageBox.Show("Login successful. Loading vault...", "Login success.",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            UiController.LogicMethods.EnableUi(LogoutBtn);
            WelcomeLabel.Text = $"Welcome, {username}!";

            UiController.LogicMethods.EnableVisibility(
                WelcomeLabel,
                Vars.RegisterControls.WelcomeLabel,
                Vars.VaultControls.WelcomeLabel,
                Vars.EncryptionControls.WelcomeLabel,
                Vars.FileHashControls.WelcomeLabel,
                Vars.CryptoSettingsControls.WelcomeLabel
            );

            var welcomeText = $"Welcome, {username}!";
            Vars.RegisterControls.WelcomeLabel.Text = welcomeText;
            Vars.VaultControls.WelcomeLabel.Text = welcomeText;
            Vars.EncryptionControls.WelcomeLabel.Text = welcomeText;
            Vars.FileHashControls.WelcomeLabel.Text = welcomeText;
            Vars.CryptoSettingsControls.WelcomeLabel.Text = welcomeText;

            if (Vars.RainbowTokenSource.IsCancellationRequested)
                Vars.RainbowTokenSource = new CancellationTokenSource();

            StartRainbowAnimation();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
            CryptoUtilities.ClearMemoryNative(masterKey);
            HandleFailedLogin();
        }
    }

    #region Login Failure Handling

    private async void HandleFailedLogin()
    {
        EnableLoginUi();
        ShowLoginFailureStatus();
        await ResetCancellationTokenAsync();
        PerformMemoryCleanup();
        NotifyLoginFailure();
        ResetLoginInputs();
        DecrementLoginAttempts();
    }

    private void EnableLoginUi()
    {
        UiController.LogicMethods.EnableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn);
    }

    private void ShowLoginFailureStatus()
    {
        StatusOutputLabel.ForeColor = Color.Red;
        StatusOutputLabel.Text = "Login failed.";
    }

    private async Task ResetCancellationTokenAsync()
    {
        await Vars.TokenSource.CancelAsync();
        Vars.TokenSource = new CancellationTokenSource();
    }

    private void PerformMemoryCleanup()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();
    }

    private void NotifyLoginFailure()
    {
        MessageBox.Show("Log in failed! Please recheck your login credentials and try again.",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void ResetLoginInputs()
    {
        PasswordTxt.Clear();
        StatusOutputLabel.ForeColor = Color.WhiteSmoke;
        StatusOutputLabel.Text = "Idle...";
    }

    private void DecrementLoginAttempts()
    {
        Vars.AttemptsRemaining--;
        AttemptsNumberLabel.Text = Vars.AttemptsRemaining.ToString();
    }

    #endregion

    #region Animations

    private async void StartAnimation()
    {
        await UiController.Animations.AnimateLabel(StatusOutputLabel, "Logging in", Vars.Token);
    }

    private async void StartRainbowAnimation()
    {
        try
        {
            await Task.WhenAll(GetRainbowAnimationTasks());
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
        }
    }

    private Task[] GetRainbowAnimationTasks()
    {
        return new[]
        {
            UiController.Animations.RainbowLabel(WelcomeLabel, Vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(Vars.RegisterControls.WelcomeLabel, Vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(Vars.VaultControls.WelcomeLabel, Vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(Vars.EncryptionControls.WelcomeLabel, Vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(Vars.FileHashControls.WelcomeLabel, Vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(Vars.CryptoSettingsControls.WelcomeLabel, Vars.RainbowLabelToken)
        };
    }

    #endregion

    #region Form Lifecycle

    private void PasswordVault_Load(object sender, EventArgs e)
    {
        try
        {
            InitializeUiVisibility();
            LoadCryptoSettings();
            HandleFipsMode();
            LoadUserPreferences();
        }
        catch (Exception ex)
        {
            ShowError(ex);
            ErrorLogging.ErrorLog(ex);
        }
    }

    private void InitializeUiVisibility()
    {
        UiController.LogicMethods.DisableVisibility(
            WelcomeLabel,
            Vars.RegisterControls.WelcomeLabel,
            Vars.VaultControls.WelcomeLabel,
            Vars.EncryptionControls.WelcomeLabel,
            Vars.FileHashControls.WelcomeLabel,
            Vars.CryptoSettingsControls.WelcomeLabel);
    }

    private void LoadCryptoSettings()
    {
        CryptoSettings.Iterations = Settings.Default.Iterations;
        CryptoSettings.MemSize = Settings.Default.MemorySize;
        CryptoSettings.Parallelism = Settings.Default.Parallelism;
    }

    private void HandleFipsMode()
    {
        if (!Settings.Default.FIPS) return;

        MessageBox.Show("Starting in FIPS mode!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Vars.CryptoSettingsControls.FipsModeCheckbox.Checked = true;
        FipsEnabled = true;
    }

    private void LoadUserPreferences()
    {
        if (string.IsNullOrEmpty(Settings.Default.userName))
        {
            UsernameTxt.Text = string.Empty;
            RememberMeCheckBox.Checked = false;
        }
        else
        {
            UsernameTxt.Text = Settings.Default.userName;
            RememberMeCheckBox.Checked = true;
            UsernameTxt.Select();
        }
    }

    private void PasswordVault_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (Encryption.FileVars.Result == null) return;

        try
        {
            Encryption.FileVars.Result.Dispose();
            Encryption.FileVars.Result = null;
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
        }
    }

    #endregion

    #region Logout

    private async void LogoutBtn_Click(object sender, EventArgs e)
    {
        try
        {
            EnsureUserIsLoggedIn();
            ClearVaultData();
            DisposeSensitiveData();
            EnableLoginControls();
            HideWelcomeLabels();
            await ResetRainbowAnimationAsync();
            ClearPasswordInput();
            ShowLogoutSuccess();
        }
        catch (Exception ex)
        {
            ShowError(ex);
            ErrorLogging.ErrorLog(ex);
        }
    }

    private void EnsureUserIsLoggedIn()
    {
        if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
            throw new InvalidOperationException("No user is currently logged in.");
    }

    private void ClearVaultData()
    {
        Vars.VaultControls.PassVault.Rows.Clear();
        Vars.VaultControls.SaveVaultBtn.Enabled = true;
        UserFileManager.CurrentLoggedInUser = string.Empty;
    }

    private void DisposeSensitiveData()
    {
        MasterKey.Dispose();
        MasterKey.Reset();
        _passwordBuffer.Dispose();
    }

    private void EnableLoginControls()
    {
        UiController.LogicMethods.EnableUi(BtnLogin, UsernameTxt, PasswordTxt, RememberMeCheckBox);
    }

    private void HideWelcomeLabels()
    {
        UiController.LogicMethods.DisableVisibility(
            WelcomeLabel,
            Vars.RegisterControls.WelcomeLabel,
            Vars.VaultControls.WelcomeLabel,
            Vars.EncryptionControls.WelcomeLabel,
            Vars.FileHashControls.WelcomeLabel,
            Vars.CryptoSettingsControls.WelcomeLabel);
    }

    private async Task ResetRainbowAnimationAsync()
    {
        if (Vars.RainbowTokenSource == null) return;

        await Vars.RainbowTokenSource.CancelAsync();
        Vars.RainbowTokenSource.Dispose();
        Vars.RainbowTokenSource = new CancellationTokenSource();
    }

    private void ClearPasswordInput()
    {
        PasswordTxt.Clear();
    }

    private void ShowLogoutSuccess()
    {
        MessageBox.Show("User successfully logged out.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    #endregion

    #region Common

    private void ShowError(Exception ex)
    {
        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    #endregion

    #endregion

    #region WindowAnimations

    [DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(UnmanagedType.Bool)]
    private static extern bool AnimateWindow(IntPtr hWnd, int dwTime, AnimateWindowFlags flags);


    [Flags]
    public enum AnimateWindowFlags
    {
        AwVerPositive = 0x00000004,
        AwHide = 0x00010000,
        AwSlide = 0x00040000
    }

    private void MinimizeIcon_MouseEnter(object sender, EventArgs e)
    {
        MinimizeIcon.BackColor = Color.LightSkyBlue;
    }

    private void MinimizeIcon_MouseLeave(object sender, EventArgs e)
    {
        MinimizeIcon.BackColor = Color.FromArgb(30, 30, 30);
    }

    private void ShutdownIcon_MouseEnter(object sender, EventArgs e)
    {
        ShutdownIcon.BackColor = Color.LightSkyBlue;
    }

    private void ShutdownIcon_MouseLeave(object sender, EventArgs e)
    {
        ShutdownIcon.BackColor = Color.FromArgb(30, 30, 30);
    }

    private void MinimizeIcon_Click(object sender, EventArgs e)
    {
        MinimizeIcon.BackColor = Color.DeepSkyBlue;
        AnimateWindow(Handle, 300,
            AnimateWindowFlags.AwHide | AnimateWindowFlags.AwSlide | AnimateWindowFlags.AwVerPositive);
        WindowState = FormWindowState.Minimized;
        ShutdownIcon.BackColor = Color.FromArgb(30, 30, 30);
    }

    private void ShutdownIcon_Click(object sender, EventArgs e)
    {
        ShutdownIcon.BackColor = Color.DeepSkyBlue;
        var result = MessageBox.Show("Are you sure you want to close the application?", "Confirm",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
        if (result == DialogResult.OK)
            Environment.Exit(0);
        ShutdownIcon.BackColor = Color.FromArgb(30, 30, 30);
    }

    #endregion

    #region NavigationButtons

    private void VaultBtn_Click(object sender, EventArgs e)
    {
        SidePanelMarker.Height = VaultBtn.Height;
        SidePanelMarker.Top = VaultBtn.Top;
        LoginGroupBox.Visible = false;
        Vars.RegisterControls.Visible = false;
        Vars.FileHashControls.Visible = false;
        Vars.EncryptionControls.Visible = false;
        Vars.CryptoSettingsControls.Visible = false;
        Size = Size with { Height = 530 };
        Size = Size with { Width = 895 };
        Vars.VaultControls.Location = new Point(200, 55);
        Vars.VaultControls.Visible = true;
        Controls.Add(Vars.VaultControls);
    }

    private void LoginBtn_Click(object sender, EventArgs e)
    {
        SidePanelMarker.Height = LoginBtn.Height;
        SidePanelMarker.Top = LoginBtn.Top;
        LoginGroupBox.Visible = true;
        Controls.Add(LoginGroupBox);
        Size = Size with { Height = 514 };
        Size = Size with { Width = 849 };
        Vars.VaultControls.Visible = false;
        Vars.RegisterControls.Visible = false;
        Vars.EncryptionControls.Visible = false;
        Vars.FileHashControls.Visible = false;
        Vars.CryptoSettingsControls.Visible = false;
        AcceptButton = BtnLogin;
    }

    private void RegisterBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 530 };
        Size = Size with { Width = 696 };
        Vars.RegisterControls.Location = new Point(210, 45);
        SidePanelMarker.Height = RegisterBtn.Height;
        SidePanelMarker.Top = RegisterBtn.Top;
        LoginGroupBox.Visible = false;
        Vars.VaultControls.Visible = false;
        Vars.RegisterControls.Visible = true;
        Vars.EncryptionControls.Visible = false;
        Vars.FileHashControls.Visible = false;
        Vars.CryptoSettingsControls.Visible = false;
        Controls.Add(Vars.RegisterControls);
        Vars.RegisterControls.ParentForm!.AcceptButton = Vars.RegisterControls.CreateAccountBtn;
        Vars.RegisterControls.userTxt.Select();
    }

    private void EncryptionBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 375 };
        Size = Size with { Width = 1200 };
        LoginGroupBox.Visible = false;
        Vars.VaultControls.Visible = false;
        Vars.RegisterControls.Visible = false;
        Vars.FileHashControls.Visible = false;
        Vars.CryptoSettingsControls.Visible = false;
        Vars.EncryptionControls.Location = new Point(220, 60);
        SidePanelMarker.Height = EncryptionBtn.Height;
        SidePanelMarker.Top = EncryptionBtn.Top;
        Vars.EncryptionControls.Visible = true;
        Controls.Add(Vars.EncryptionControls);
    }

    private void FileHashBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 490 };
        Size = Size with { Width = 1185 };
        SidePanelMarker.Height = FileHashBtn.Height;
        SidePanelMarker.Top = FileHashBtn.Top;
        LoginGroupBox.Visible = false;
        Vars.VaultControls.Visible = false;
        Vars.RegisterControls.Visible = false;
        Vars.EncryptionControls.Visible = false;
        Vars.CryptoSettingsControls.Visible = false;
        Vars.FileHashControls.Location = new Point(175, 0);
        Vars.FileHashControls.Visible = true;
        Controls.Add(Vars.FileHashControls);
    }

    private void CryptoSettingsBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 450 };
        Size = Size with { Width = 685 };
        SidePanelMarker.Height = CryptoSettingsBtn.Height;
        SidePanelMarker.Top = CryptoSettingsBtn.Top;
        LoginGroupBox.Visible = false;
        Vars.VaultControls.Visible = false;
        Vars.RegisterControls.Visible = false;
        Vars.EncryptionControls.Visible = false;
        Vars.FileHashControls.Visible = false;
        Vars.CryptoSettingsControls.Location = new Point(200, 50);
        Vars.CryptoSettingsControls.Visible = true;
        Controls.Add(Vars.CryptoSettingsControls);
    }

    #endregion

    #region DragForm

    private void PasswordVault_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        Vars.IsDragging = true;
        Vars.Offset = e.Location;
    }

    private void PasswordVault_MouseMove(object sender, MouseEventArgs e)
    {
        if (!Vars.IsDragging)
            return;

        var newLocation = PointToScreen(new Point(e.X, e.Y));
        newLocation.Offset(-Vars.Offset.X, -Vars.Offset.Y);
        Location = newLocation;
    }

    private void PasswordVault_MouseUp(object sender, MouseEventArgs e)
    {
        Vars.IsDragging = false;
    }

    private void TopPanelBar_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        Vars.IsDragging = true;
        Vars.Offset = e.Location;
    }

    private void TopPanelBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!Vars.IsDragging)
            return;

        var newLocation = PointToScreen(new Point(e.X, e.Y));
        newLocation.Offset(-Vars.Offset.X, -Vars.Offset.Y);
        Location = newLocation;
    }

    private void TopPanelBar_MouseUp(object sender, MouseEventArgs e)
    {
        Vars.IsDragging = false;
    }

    #endregion

    #region TextboxBehavior

    private void PasswordTxt_KeyPress(object sender, KeyPressEventArgs e)
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

    private void PasswordTxt_KeyDown(object sender, KeyEventArgs e)
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

    private void UpdateMaskedText()
    {
        PasswordTxt.Text = new string('●', _passwordBuffer.Length);
        PasswordTxt.SelectionStart = PasswordTxt.Text.Length; // Move caret to end
    }

    #endregion TextboxBehavior
}