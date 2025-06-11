using System.Diagnostics;
using System.Runtime.InteropServices;
using static Password_Vault_V2.Crypto;

namespace Password_Vault_V2;

public sealed partial class PasswordVault : Form
{
    private readonly SecurePasswordBuffer _passwordBuffer = new();
    public readonly Variables Vars = new();


    public PasswordVault()
    {
        InitializeComponent();
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

    /// <summary>
    /// Handles the click event for the login button. Validates input, handles login logic,
    /// manages UI state, and displays messages based on success or failure.
    /// </summary>
    /// <param name="sender">The source of the event (typically the login button).</param>
    /// <param name="e">The event data associated with the click event.</param>
    /// <remarks>
    /// This method performs the following actions:
    /// <list type="bullet">
    /// <item>Converts the password buffer to a byte array.</item>
    /// <item>Stores or clears the username in application settings based on the "Remember Me" checkbox.</item>
    /// <item>Parses and checks remaining login attempts, preventing login if exhausted.</item>
    /// <item>Validates input fields and shows a warning about not closing the application while loading.</item>
    /// <item>Disables the UI during login processing.</item>
    /// <item>Calls <c>UserFileManager.UserExists</c> and passes result to <c>ProcessLogin</c>.</item>
    /// <item>Handles and logs exceptions, updates the UI with failure status, decrements attempt counter, and re-enables controls.</item>
    /// <item>Clears the password field, disposes of password buffer, and performs aggressive garbage collection in <c>finally</c>.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown if the username is empty, the password array is null/empty, or if the remaining attempts value is unparsable.
    /// </exception>
    private async void BtnLogin_Click(object sender, EventArgs e)
    {
        var passwordBytesArray = _passwordBuffer.ToByteArray();

        switch (RememberMeCheckBox.Checked)
        {
            case true:
                Settings.Default.userName = UsernameTxt.Text;
                Settings.Default.Save();
                break;
            case false:
                Settings.Default.userName = string.Empty;
                Settings.Default.Save();
                break;
        }

        var canParse = int.TryParse(AttemptsNumberLabel.Text, out Vars.AttemptsRemaining);

        if (canParse)
        {
            if (Vars.AttemptsRemaining == 0)
            {
                MessageBox.Show("No attempts remaining. Please restart the program and try again.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        else
        {
            throw new Exception("Unable to parse attempts remaining value.");
        }

        try
        {
            if (string.IsNullOrEmpty(UsernameTxt.Text))
                throw new Exception("Username value was empty.");
            if (passwordBytesArray == null || passwordBytesArray.Length == 0)
                throw new Exception("Password array was empty or null.");

            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            UiController.LogicMethods.DisableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn);

            var userExists = UserFileManager.UserExists(UsernameTxt.Text.Trim());

            await ProcessLogin(userExists);
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            await Vars.TokenSource.CancelAsync();

            if (Vars.TokenSource.IsCancellationRequested)
            {
                Vars.TokenSource.Dispose();
                Vars.TokenSource = new CancellationTokenSource();
            }

            StatusOutputLabel.Text = "Login failed.";
            StatusOutputLabel.ForeColor = Color.Red;

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            Vars.AttemptsRemaining--;
            AttemptsNumberLabel.Text = Vars.AttemptsRemaining.ToString();
            StatusOutputLabel.ForeColor = Color.White;
            StatusOutputLabel.Text = "Idle...";
            UiController.LogicMethods.EnableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn);
        }
        finally
        {
            PasswordTxt.Clear();
            _passwordBuffer.Dispose();
        }
    }

    /// <summary>
    /// Processes the login attempt based on whether the user exists.
    /// </summary>
    /// <param name="userExists">
    /// A boolean value indicating whether the user exists in the system.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// If <paramref name="userExists"/> is <c>true</c>, the method starts the asynchronous login process.
    /// If <paramref name="userExists"/> is <c>false</c>, it triggers a response indicating that the user does not exist.
    /// </remarks>
    private async Task ProcessLogin(bool userExists)
    {
        switch (userExists)
        {
            case true:
                await StartLoginProcessAsync();
                break;
            case false:
                UserDoesNotExist();
                break;
        }
    }

    /// <summary>
    /// Parses a decrypted user file into its component byte array segments.
    /// Each segment is expected to be prefixed by a 4-byte integer indicating the segment's length.
    /// <br/><br/>
    /// <b>Example usage:</b>
    /// <code>
    /// byte[] decrypted = DecryptFile(...);
    /// byte[][] parts = ParseUserFile(decrypted);
    /// </code>
    /// </summary>
    /// <param name="decryptedBytes">
    /// A byte array representing the decrypted contents of a user file. The file must consist of
    /// sequential blocks prefixed with 4-byte length headers.
    /// </param>
    /// <returns>
    /// An array of <see cref="byte[]"/> where each entry represents one segment from the original byte stream.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if a declared length exceeds the remaining data, indicating a malformed or truncated file.
    /// </exception>
    private static byte[][] ParseUserFile(byte[] decryptedBytes)
    {
        var parts = new List<byte[]>();
        using var ms = new MemoryStream(decryptedBytes);
        using var reader = new BinaryReader(ms);

        while (ms.Position < ms.Length)
        {
            var length = reader.ReadInt32();
            var part = reader.ReadBytes(length);
            parts.Add(part);
        }

        return [.. parts];
    }

    /// <summary>
    /// Begins the secure login process by reading and validating a user's encrypted file,
    /// verifying the password, and loading cryptographic parameters into memory.
    /// <br/><br/>
    /// <b>Example usage:</b>
    /// <code>
    /// await StartLoginProcessAsync();
    /// </code>
    /// </summary>
    /// <remarks>
    /// This method performs several sequential steps:
    /// <list type="number">
    /// <item>Initializes the cancellation token if needed and sets process priority.</item>
    /// <item>Starts a UI animation and extracts the password bytes.</item>
    /// <item>Reads the encrypted user file from disk and parses out salts, UUIDs, and encrypted data.</item>
    /// <item>Uses Argon2id to derive the file decryption key and decrypts the user file.</item>
    /// <item>Validates password hash and UUID integrity checks to ensure data hasn't been tampered with.</item>
    /// <item>Derives a password-based key to decrypt the master key used for future cryptographic operations.</item>
    /// <item>If successful, updates application state and securely stores the master key in memory.</item>
    /// </list>
    /// Sensitive buffers are securely cleared in the <c>finally</c> block to prevent memory disclosure.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown if password validation or file integrity checks (UUID/HKDF salt mismatch) fail.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the user file is missing critical cryptographic segments.
    /// </exception>
    /// <exception cref="Exception">
    /// May throw other exceptions for unexpected conditions, such as null/empty password or memory issues.
    /// </exception>
    private async Task StartLoginProcessAsync()
    {
        if (Vars.TokenSource.IsCancellationRequested)
            Vars.TokenSource = new CancellationTokenSource();

        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
        StartAnimation();

        var passwordBytes = _passwordBuffer.ToByteArray();

        // Sensitive working memory
        byte[]
            derivedHash = [],
            decryptedBytes = [],
            uuidBytes = [];
        byte[]
            userFile = [],
            fileKey = [],
            passwordDerivedKey = [],
            decryptedMasterKey = [];

        var handles = CryptoUtilities.PinArrays(passwordBytes, decryptedBytes, fileKey,
            passwordDerivedKey, decryptedMasterKey);

        try
        {
            var userName = UsernameTxt.Text;

            // Step 1: Read user file
            userFile = await File.ReadAllBytesAsync(UserFileManager.GetUserFilePath(userName));
            if (userFile.Length < CryptoConstants.SaltSize * 2 + CryptoConstants.UuidSize)
                throw new FileNotFoundException("User data is incomplete or corrupted.");

            var offset = 0;

            // Extract userFileSalt
            var userFileSalt = new byte[CryptoConstants.SaltSize];
            Buffer.BlockCopy(userFile, offset, userFileSalt, 0, userFileSalt.Length);
            offset += userFileSalt.Length;

            // Extract UUID
            uuidBytes = new byte[CryptoConstants.UuidSize];
            Buffer.BlockCopy(userFile, offset, uuidBytes, 0, uuidBytes.Length);
            offset += uuidBytes.Length;

            // Extract hkdfSalt
            var
                hkdfSalt = new byte[CryptoConstants.SaltSize];
            Buffer.BlockCopy(userFile, offset, hkdfSalt, 0, hkdfSalt.Length);
            offset += hkdfSalt.Length;

            // Extract encryptedUserFile
            var encryptedLength = userFile.Length - offset;
            var encryptedUserFile = new byte[encryptedLength];
            Buffer.BlockCopy(userFile, offset, encryptedUserFile, 0, encryptedLength);

            UserCryptoParameters.HkdfSalt = hkdfSalt;

            // Step 2: Derive fileKey
            fileKey = await HashingMethods.Argon2Id(passwordBytes, userFileSalt, CryptoConstants.KeySize);

            // Step 3: Decrypt user file
            decryptedBytes = await DecryptFile(encryptedUserFile, fileKey, hkdfSalt);

            // Step 4: Parse decrypted data
            var parts = ParseUserFile(decryptedBytes);

            UserCryptoParameters.HashSalt = parts[0];
            UserCryptoParameters.UUID = parts[1];
            UserCryptoParameters.HkdfSalt = parts[2];
            if (!UserCryptoParameters.HkdfSalt.SequenceEqual(hkdfSalt))
                throw new UnauthorizedAccessException("HKDF salt mismatch, file integrity compromised.");
            UserCryptoParameters.KeyDerivationSalt = parts[3];
            UserCryptoParameters.IntermediateKeySalt = parts[4];
            UserCryptoParameters.MasterKeySalt = parts[5];
            UserCryptoParameters.PasswordHash = parts[6]; // <-- Use index 6 here
            UserCryptoParameters.EncryptedMasterKey = parts[7]; // <-- And index 7 here


            // Final check
            if (!UserCryptoParameters.UUID.SequenceEqual(uuidBytes))
                throw new UnauthorizedAccessException("UUID mismatch, user file corrupted.");

            var path = UserFileManager.GetUserFilePath(userName);
            File.SetAttributes(path, FileAttributes.None);

            // Verify password hash
            derivedHash = await HashingMethods.Argon2Id(passwordBytes, UserCryptoParameters.HashSalt, 64);

            if (!CryptoUtilities.ComparePassword(derivedHash, UserCryptoParameters.PasswordHash))
            {
                await Task.Delay(300); // Slow down brute-force attacks
                throw new UnauthorizedAccessException("Invalid password.");
            }

            // Set current user
            UserFileManager.CurrentLoggedInUser = userName;

            passwordDerivedKey = await HashingMethods.Argon2Id(passwordBytes,
                UserCryptoParameters.KeyDerivationSalt, CryptoConstants.KeySize);

            decryptedMasterKey = await DecryptFile(UserCryptoParameters.EncryptedMasterKey, passwordDerivedKey, hkdfSalt);

            MasterKey.SecureKey(decryptedMasterKey); // Store securely in memory

            // Successful login logic
            await HandleLogin();
        }

        finally
        {
#pragma warning disable 
            // Sanitize sensitive memory
            CryptoUtilities.ClearMemoryNative(uuidBytes, derivedHash, fileKey,
                passwordBytes, decryptedBytes, passwordDerivedKey,
                decryptedMasterKey, userFile, UserCryptoParameters.EncryptedMasterKey, UserCryptoParameters.HashSalt,
                UserCryptoParameters.IntermediateKeySalt, UserCryptoParameters.KeyDerivationSalt,
                UserCryptoParameters.MasterKeySalt, UserCryptoParameters.PasswordHash,
                UserCryptoParameters.UUID, UserCryptoParameters.UserFileSalt, UserCryptoParameters.HkdfSalt);
            CryptoUtilities.FreeArrays(handles);
#pragma warning restore
            // UI Cleanup
            StatusOutputLabel.Text = "Idle...";
            StatusOutputLabel.ForeColor = Color.White;
            PasswordTxt.Clear();

            // Force garbage collection
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// Handles the case when a user does not exist in the system.
    /// </summary>
    /// <exception cref="Exception">
    /// Thrown to indicate that the specified username does not exist.
    /// </exception>
    /// <remarks>
    /// This method throws a generic <see cref="Exception"/> with a message indicating that
    /// the username was not found. It is typically used to halt further login processing.
    /// </remarks>
    private static void UserDoesNotExist()
    {
        throw new Exception("Username does not exist.");
    }

    /// <summary>
    /// Handles UI and internal state recovery after a failed login attempt.
    /// </summary>
    /// <remarks>
    /// This method performs the following recovery actions:
    /// <list type="number">
    /// <item><description>Re-enables UI elements for another login attempt.</description></item>
    /// <item><description>Updates the status label to reflect the login failure.</description></item>
    /// <item><description>Cancels and resets the current cancellation token source.</description></item>
    /// <item><description>Displays an error message to the user.</description></item>
    /// <item><description>Clears the password textbox and resets the status label to idle.</description></item>
    /// <item><description>Decrements the remaining login attempts and updates the UI label.</description></item>
    /// </list>
    /// All sensitive information from the failed login attempt is cleared from memory or UI elements.
    /// </remarks>
    private async Task HandleLogin()
    {
        var username = UsernameTxt.Text;
        var userFilePath = UserFileManager.GetUserFilePath(username);
        var userVaultPath = UserFileManager.GetUserVault(username);

        if (!File.Exists(userFilePath))
            throw new FileNotFoundException("User file does not exist.");

        var masterKey = MasterKey.GetKey();

        if (masterKey is null)
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
            HandleFailedLogin();
        }
    }

    /// <summary>
    /// Handles UI and state recovery after a failed login attempt.
    /// </summary>
    /// <remarks>
    /// This method performs several post-failure recovery steps:
    /// <list type="number">
    /// <item>Re-enables login UI controls for retry.</item>
    /// <item>Updates the status label to indicate failure.</item>
    /// <item>Cancels the current cancellation token and prepares a new one.</item>
    /// <item>Displays an error message to the user.</item>
    /// <item>Clears the password textbox and resets the status label.</item>
    /// <item>Decrements the remaining login attempt counter and updates the UI.</item>
    /// </list>
    /// All sensitive data from the failed login attempt is cleared or reset appropriately.
    /// </remarks>
    private async void HandleFailedLogin()
    {
        // Enable input and login-related UI elements
        UiController.LogicMethods.EnableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn);

        // Show immediate failure status
        StatusOutputLabel.ForeColor = Color.Red;
        StatusOutputLabel.Text = "Login failed.";

        // Cancel current token and prepare a new one
        await Vars.TokenSource.CancelAsync();
        Vars.TokenSource = new CancellationTokenSource();

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();

        // Notify user
        MessageBox.Show(
            "Log in failed! Please recheck your login credentials and try again.",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error
        );

        // Clear sensitive input
        PasswordTxt.Clear();

        // Reset status label to idle
        StatusOutputLabel.ForeColor = Color.WhiteSmoke;
        StatusOutputLabel.Text = "Idle...";

        // Decrease attempts and update display
        Vars.AttemptsRemaining--;
        AttemptsNumberLabel.Text = Vars.AttemptsRemaining.ToString();
    }

    /// <summary>
    /// Starts the login status label animation indicating that the login process is underway.
    /// </summary>
    /// <remarks>
    /// This method asynchronously animates the <c>StatusOutputLabel</c> with the message "Logging in"
    /// using a cancellation token to allow interruption if the login process is canceled.
    /// </remarks>
    private async void StartAnimation()
    {
        await UiController.Animations.AnimateLabel(StatusOutputLabel, "Logging in", Vars.Token);
    }

    /// <summary>
    /// Starts asynchronous rainbow-colored text animations on all welcome labels across different UI sections.
    /// </summary>
    /// <remarks>
    /// This method launches parallel label animations using the <see cref="UiController.Animations.RainbowLabel"/> method
    /// for the main welcome label and additional welcome labels in the Register, Vault, Encryption, and File Hash control sections.
    /// If any exception occurs during the animations, it is caught and logged using <see cref="ErrorLogging.ErrorLog(Exception)"/>.
    /// </remarks>
    /// <exception cref="Exception">
    /// Any unexpected exception during label animation will be caught and logged internally.
    /// </exception>
    private async void StartRainbowAnimation()
    {
        try
        {
            var rainbowTasks = new[]
            {
                UiController.Animations.RainbowLabel(WelcomeLabel, Vars.RainbowLabelToken),
                UiController.Animations.RainbowLabel(Vars.RegisterControls.WelcomeLabel, Vars.RainbowLabelToken),
                UiController.Animations.RainbowLabel(Vars.VaultControls.WelcomeLabel, Vars.RainbowLabelToken),
                UiController.Animations.RainbowLabel(Vars.EncryptionControls.WelcomeLabel, Vars.RainbowLabelToken),
                UiController.Animations.RainbowLabel(Vars.FileHashControls.WelcomeLabel, Vars.RainbowLabelToken)
            };

            await Task.WhenAll(rainbowTasks);
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
        }
    }

    /// <summary>
    /// Handles the <c>Load</c> event for the <c>PasswordVault</c> form.
    /// Initializes UI visibility, cryptographic settings, and user preferences.
    /// </summary>
    /// <param name="sender">The source of the event, typically the form.</param>
    /// <param name="e">An instance of <see cref="EventArgs"/> containing the event data.</param>
    /// <remarks>
    /// This method performs the following initialization steps:
    /// <list type="number">
    /// <item><description>Hides all welcome labels across different control sections.</description></item>
    /// <item><description>Initializes cryptographic settings using application defaults.</description></item>
    /// <item><description>Checks the stored username setting and sets the login fields accordingly.</description></item>
    /// <item><description>If no username is stored, clears the username field and disables "Remember Me."</description></item>
    /// <item><description>If a username is stored, populates it and checks the "Remember Me" checkbox.</description></item>
    /// </list>
    /// All exceptions are caught and logged, and an error message is displayed to the user.
    /// </remarks>
    /// <exception cref="Exception">
    /// Any unhandled exception during initialization will be caught and logged internally.
    /// </exception>
    private void PasswordVault_Load(object sender, EventArgs e)
    {
        try
        {
            UiController.LogicMethods.DisableVisibility(WelcomeLabel, Vars.RegisterControls.WelcomeLabel,
                Vars.VaultControls.WelcomeLabel,
                Vars.EncryptionControls.WelcomeLabel, Vars.FileHashControls.WelcomeLabel);

           // PasswordTxt.PasswordChar = '●';
            CryptoSettings.Iterations = Settings.Default.Iterations;
            CryptoSettings.MemSize = Settings.Default.MemorySize;
            CryptoSettings.Parallelism = Settings.Default.Parallelism;

            if (Settings.Default.userName == string.Empty)
            {
                UsernameTxt.Text = string.Empty;
                RememberMeCheckBox.Checked = false;
                return;
            }

            UsernameTxt.Text = Settings.Default.userName;
            RememberMeCheckBox.Checked = true;
            UsernameTxt.Select();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    /// <summary>
    /// Handles the <c>Click</c> event of the <c>LogoutBtn</c> button.
    /// Performs logout operations including state cleanup, UI reset, and cancellation of background tasks.
    /// </summary>
    /// <param name="sender">The source of the event, typically the <c>LogoutBtn</c> control.</param>
    /// <param name="e">An instance of <see cref="EventArgs"/> containing the event data.</param>
    /// <remarks>
    /// This method performs the following operations:
    /// <list type="number">
    /// <item><description>Verifies that a user is currently logged in.</description></item>
    /// <item><description>Clears the vault contents and resets the current user state.</description></item>
    /// <item><description>Disposes of cryptographic key material and secure memory buffers.</description></item>
    /// <item><description>Re-enables login UI elements for future login attempts.</description></item>
    /// <item><description>Cancels and resets the rainbow label animation task.</description></item>
    /// <item><description>Clears the password input field and displays a logout confirmation.</description></item>
    /// <item><description>Hides all welcome labels and resets the login status label.</description></item>
    /// </list>
    /// If an error occurs during the logout process, it is caught, logged, and displayed to the user.
    /// </remarks>
    /// <exception cref="Exception">
    /// Thrown when attempting to log out without any user currently logged in.
    /// </exception>
    private async void LogoutBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            Vars.VaultControls.PassVault.Rows.Clear();
            Vars.VaultControls.SaveVaultBtn.Enabled = true;
            UserFileManager.CurrentLoggedInUser = string.Empty;
            MasterKey.Dispose();
            _passwordBuffer.Dispose();
            UiController.LogicMethods.EnableUi(BtnLogin, UsernameTxt, PasswordTxt, RememberMeCheckBox);

            if (Vars.RainbowTokenSource != null)
            {
                await Vars.RainbowTokenSource.CancelAsync();
                Vars.RainbowTokenSource.Dispose();
                Vars.RainbowTokenSource = new CancellationTokenSource();
            }

            PasswordTxt.Clear();

            MessageBox.Show("User successfully logged out.", "Success", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            UiController.LogicMethods.DisableVisibility(WelcomeLabel, Vars.RegisterControls.WelcomeLabel,
                Vars.VaultControls.WelcomeLabel,
                Vars.EncryptionControls.WelcomeLabel, Vars.FileHashControls.WelcomeLabel);
            StatusOutputLabel.Text = "Idle...";
            StatusOutputLabel.ForeColor = Color.White;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
        }
    }

    #endregion

    #region WindowAnimations

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AnimateWindow(IntPtr hWnd, int dwTime, AnimateWindowFlags flags);

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
        Size = Size with { Height = 510 };
        Size = Size with { Width = 885 };
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
        Size = Size with { Height = 504 };
        Size = Size with { Width = 685 };
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
        // 973, 289
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
        Size = Size with { Height = 438 };
        Size = Size with { Width = 1160 };
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
        Size = Size with { Height = 500 };
        Size = Size with { Width = 675 };
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
}

#endregion TextboxBehavior