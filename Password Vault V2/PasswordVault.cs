using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA2208

namespace Password_Vault_V2;

public partial class PasswordVault : Form
{
    private readonly Variables _vars = new();

    public PasswordVault()
    {
        InitializeComponent();
    }

    #region Variables

    private class Variables
    {
        public readonly CancellationTokenSource TokenSource = new();
        public int AttemptsRemaining;
        public GCHandle Handle;

        // Other Variables
        public bool IsDragging;
        public Point Offset;
        public byte[] PasswordBytes = [];

        public CancellationTokenSource RainbowTokenSource = new();

        // UI Controls
        public Vault VaultControls { get; } = new();
        public Register RegisterControls { get; } = new();
        public Encryption EncryptionControls { get; } = new();
        public FileHash FileHashControls { get; } = new();

        // Properties for CancellationToken
        public CancellationToken Token => TokenSource.Token;
        public CancellationToken RainbowLabelToken => RainbowTokenSource.Token;
    }

    #endregion

    #region Methods

    private void EnableUi()
    {
        UsernameTxt.Enabled = true;
        PasswordTxt.Enabled = true;
        BtnLogin.Enabled = true;
        LogoutBtn.Enabled = true;
        LoginBtn.Enabled = true;
        RegisterBtn.Enabled = true;
        EncryptionBtn.Enabled = true;
        VaultBtn.Enabled = true;
        FileHashBtn.Enabled = true;
    }

    private void DisableUi()
    {
        UsernameTxt.Enabled = false;
        PasswordTxt.Enabled = false;
        BtnLogin.Enabled = false;
        LogoutBtn.Enabled = false;
        LoginBtn.Enabled = false;
        RegisterBtn.Enabled = false;
        EncryptionBtn.Enabled = false;
        VaultBtn.Enabled = false;
        FileHashBtn.Enabled = false;
    }

    private async void BtnLogin_Click(object sender, EventArgs e)
    {
        _vars.PasswordBytes = Encoding.UTF8.GetBytes(PasswordTxt.Text);

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

        var canParse = int.TryParse(AttemptsNumberLabel.Text, out _vars.AttemptsRemaining);

        if (canParse)
        {
            if (_vars.AttemptsRemaining == 0)
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
            if (UsernameTxt.Text == string.Empty)
                throw new ArgumentException("Value was empty.", nameof(UsernameTxt));

            if (_vars.PasswordBytes.Length == 0)
                throw new ArgumentException("Value was empty.", nameof(_vars.PasswordBytes));

            MessageBox.Show(
                "Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.",
                "Info", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            UiController.LogicMethods.DisableUi(UsernameTxt, PasswordTxt, BtnLogin, LogoutBtn, LoginBtn, RegisterBtn,
                EncryptionBtn, VaultBtn, FileHashBtn);

            var userExists = Authentication.UserExists(UsernameTxt.Text);

            await ProcessLogin(userExists);
        }
        catch (Exception ex)
        {
            Crypto.CryptoUtilities.ClearMemory(_vars.PasswordBytes);
            ErrorLogging.ErrorLog(ex);
            StatusOutputLabel.Text = "Login failed.";
            StatusOutputLabel.ForeColor = Color.Red;
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _vars.AttemptsRemaining--;
            AttemptsNumberLabel.Text = _vars.AttemptsRemaining.ToString();
            StatusOutputLabel.ForeColor = Color.White;
            StatusOutputLabel.Text = "Idle...";
            EnableUi();
        }
    }

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

    private async Task StartLoginProcessAsync()
    {
        StartAnimation();

        Authentication.CurrentLoggedInUser = UsernameTxt.Text;

        if (ShowPasswordCheckBox.Checked)
            ShowPasswordCheckBox.Checked = false;

        var decryptedBytes = await Crypto.DecryptFile(Authentication.CurrentLoggedInUser, _vars.PasswordBytes,
            Authentication.GetUserFilePath(Authentication.CurrentLoggedInUser));

        if (decryptedBytes == Array.Empty<byte>())
            throw new ArgumentException("Value was empty.", nameof(decryptedBytes));

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        var decryptedText = DataConversionHelpers.ByteArrayToString(decryptedBytes);
        await File.WriteAllTextAsync(Authentication.GetUserFilePath(Authentication.CurrentLoggedInUser), decryptedText);

        var salt = Authentication.GetUserSalt(UsernameTxt.Text);

        Authentication.GetUserInfo(UsernameTxt.Text);

        var hashedInput = await Crypto.HashingMethods.Argon2Id(_vars.PasswordBytes, salt, 32);

        var encryptedBytes = await Crypto.EncryptFile(Authentication.CurrentLoggedInUser, _vars.PasswordBytes,
            Authentication.GetUserFilePath(Authentication.CurrentLoggedInUser));

        if (encryptedBytes == Array.Empty<byte>())
            throw new ArgumentException("Value was empty.", nameof(decryptedBytes));

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        var encryptedText = DataConversionHelpers.ByteArrayToBase64String(encryptedBytes);
        await File.WriteAllTextAsync(Authentication.GetUserFilePath(Authentication.CurrentLoggedInUser), encryptedText);

        if (hashedInput == Array.Empty<byte>())
            throw new ArgumentException("Value was empty.", nameof(hashedInput));

        var loginSuccessful = await Crypto.CryptoUtilities.ComparePassword(hashedInput, Crypto.CryptoConstants.Hash);

        Crypto.CryptoUtilities.ClearMemory(Crypto.CryptoConstants.Hash, hashedInput);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        switch (loginSuccessful)
        {
            case true:
                await HandleLogin();
                break;
            case false:
                HandleFailedLogin();
                break;
        }
    }

    private void UserDoesNotExist()
    {
        EnableUi();
        Crypto.CryptoUtilities.ClearMemory(_vars.PasswordBytes);
        StatusOutputLabel.ForeColor = Color.WhiteSmoke;
        StatusOutputLabel.Text = @"Idle...";
        throw new ArgumentException("Username does not exist.", nameof(UsernameTxt));
    }

    /// <summary>
    ///     Handles actions and processes for a successful login.
    /// </summary>
    private async Task HandleLogin()
    {
        if (!File.Exists(Authentication.GetUserFilePath(UsernameTxt.Text)))
            return;

        _vars.Handle = GCHandle.Alloc(_vars.PasswordBytes, GCHandleType.Pinned);

        Crypto.CryptoConstants.SecurePasswordSalt = Crypto.CryptoUtilities.RndByteSized(128);
        try
        {
            if (File.Exists(Authentication.GetUserVault(UsernameTxt.Text)))
            {
                var decryptedVault = await Crypto.DecryptFile(UsernameTxt.Text,
                    _vars.PasswordBytes, Authentication.GetUserVault(UsernameTxt.Text));

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                if (decryptedVault == Array.Empty<byte>())
                    throw new ArgumentException("Value returned empty or null", nameof(decryptedVault));

                await File.WriteAllTextAsync(Authentication.GetUserVault(UsernameTxt.Text),
                    DataConversionHelpers.ByteArrayToString(decryptedVault));

                _vars.VaultControls.LoadVault();

                var encryptedBytes = await Crypto.EncryptFile(UsernameTxt.Text, _vars.PasswordBytes,
                    Authentication.GetUserVault(UsernameTxt.Text));

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

                if (encryptedBytes == Array.Empty<byte>())
                    throw new ArgumentException("Value returned empty or null.", nameof(encryptedBytes));

                await File.WriteAllTextAsync(Authentication.GetUserVault(UsernameTxt.Text),
                    DataConversionHelpers.ByteArrayToBase64String(encryptedBytes));

                StatusOutputLabel.ForeColor = Color.LimeGreen;
                StatusOutputLabel.Text = "Access granted";
                await _vars.TokenSource.CancelAsync();
                UserLog.LogUser(Authentication.CurrentLoggedInUser);

                Crypto.CryptoConstants.SecurePassword = ProtectedData.Protect(Crypto.CryptoConstants.SecurePassword,
                    Crypto.CryptoConstants.SecurePasswordSalt, DataProtectionScope.CurrentUser);

                MessageBox.Show("Login successful. Loading vault...", "Login success.",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                EnableUi();
                UsernameTxt.Enabled = false;
                PasswordTxt.Enabled = false;
                BtnLogin.Enabled = false;
                ShowPasswordCheckBox.Enabled = false;
                RememberMeCheckBox.Enabled = false;
                LogoutBtn.Enabled = true;
                WelcomeLabel.Text = @$"Welcome, {Authentication.CurrentLoggedInUser}!";
                PasswordTxt.Clear();
                Crypto.CryptoUtilities.ClearMemory(_vars.PasswordBytes);

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
                UiController.LogicMethods.EnableVisibility(WelcomeLabel, _vars.RegisterControls.WelcomeLabel,
                    _vars.VaultControls.WelcomeLabel,
                    _vars.EncryptionControls.WelcomeLabel, _vars.FileHashControls.WelcomeLabel);

                return;
            }

            Crypto.CryptoConstants.SecurePassword = ProtectedData.Protect(Crypto.CryptoConstants.SecurePassword,
                Crypto.CryptoConstants.SecurePasswordSalt, DataProtectionScope.CurrentUser);

            StatusOutputLabel.ForeColor = Color.LimeGreen;
            StatusOutputLabel.Text = "Access granted";
            await _vars.TokenSource.CancelAsync();
            UserLog.LogUser(Authentication.CurrentLoggedInUser);

            EnableUi();
            UsernameTxt.Enabled = false;
            PasswordTxt.Enabled = false;
            BtnLogin.Enabled = false;
            ShowPasswordCheckBox.Enabled = false;
            RememberMeCheckBox.Enabled = false;
            LogoutBtn.Enabled = true;
            Crypto.CryptoUtilities.ClearMemory(_vars.PasswordBytes);

            MessageBox.Show("Login successful. Loading vault...", "Login success.",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            WelcomeLabel.Text = $"Welcome, {Authentication.CurrentLoggedInUser}!";
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            UiController.LogicMethods.EnableVisibility(WelcomeLabel, _vars.RegisterControls.WelcomeLabel,
                _vars.VaultControls.WelcomeLabel,
                _vars.EncryptionControls.WelcomeLabel, _vars.FileHashControls.WelcomeLabel);

            _vars.RegisterControls.WelcomeLabel.Text = $"Welcome, {Authentication.CurrentLoggedInUser}!";
            _vars.VaultControls.WelcomeLabel.Text = $"Welcome, {Authentication.CurrentLoggedInUser}!";
            _vars.EncryptionControls.WelcomeLabel.Text = $"Welcome, {Authentication.CurrentLoggedInUser}!";
            _vars.FileHashControls.WelcomeLabel.Text = $"Welcome, {Authentication.CurrentLoggedInUser}!";
        }
        finally
        {
            _vars.Handle.Free();
        }

        if (_vars.RainbowTokenSource.IsCancellationRequested)
            _vars.RainbowTokenSource = new CancellationTokenSource();

        StartRainbowAnimation();
    }


    private void HandleFailedLogin()
    {
        Crypto.CryptoUtilities.ClearMemory(_vars.PasswordBytes.Length * sizeof(byte),
            _vars.Handle.AddrOfPinnedObject());
        EnableUi();
        StatusOutputLabel.ForeColor = Color.Red;
        StatusOutputLabel.Text = @"Login failed.";
        MessageBox.Show(@"Log in failed! Please recheck your login credentials and try again.", @"Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        PasswordTxt.Clear();
        StatusOutputLabel.ForeColor = Color.WhiteSmoke;
        StatusOutputLabel.Text = @"Idle...";
        _vars.AttemptsRemaining--;
        AttemptsNumberLabel.Text = _vars.AttemptsRemaining.ToString();
    }

    private async void StartAnimation()
    {
        await UiController.Animations.AnimateLabel(StatusOutputLabel, "Logging in", _vars.Token);
    }

    private async void StartRainbowAnimation()
    {
        var rainbowTasks = new[] { UiController.Animations.RainbowLabel(WelcomeLabel, _vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(_vars.RegisterControls.WelcomeLabel, _vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(_vars.VaultControls.WelcomeLabel, _vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(_vars.EncryptionControls.WelcomeLabel, _vars.RainbowLabelToken),
            UiController.Animations.RainbowLabel(_vars.FileHashControls.WelcomeLabel, _vars.RainbowLabelToken)
        };

        foreach (var t in rainbowTasks)
            await Task.Run(() => t);
    }

    private void PasswordVault_Load(object sender, EventArgs e)
    {
        UiController.LogicMethods.DisableVisibility(WelcomeLabel, _vars.RegisterControls, _vars.VaultControls,
            _vars.EncryptionControls, _vars.FileHashControls);
    }

    private void ShowPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        PasswordTxt.UseSystemPasswordChar = !ShowPasswordCheckBox.Checked;
    }

    private async void LogoutBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("There is no user currently logged in.");

            Authentication.CurrentLoggedInUser = string.Empty;
            Crypto.CryptoUtilities.ClearMemory(Crypto.CryptoConstants.SecurePassword);
            UiController.LogicMethods.EnableUi(BtnLogin, UsernameTxt, PasswordTxt, ShowPasswordCheckBox,
                RememberMeCheckBox);

            if (_vars.RainbowTokenSource.IsCancellationRequested)
            {
                _vars.RainbowTokenSource = new CancellationTokenSource();
                await _vars.RainbowTokenSource.CancelAsync();
            }
            else
            {
                await _vars.RainbowTokenSource.CancelAsync();
            }

            MessageBox.Show("User successfully logged out.", "Success", MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            UiController.LogicMethods.DisableVisibility(WelcomeLabel, _vars.RegisterControls.WelcomeLabel,
                _vars.VaultControls.WelcomeLabel,
                _vars.EncryptionControls.WelcomeLabel, _vars.FileHashControls.WelcomeLabel);
            StatusOutputLabel.Text = "Idle...";
            StatusOutputLabel.ForeColor = Color.White;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region WindowAnimations

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AnimateWindow(IntPtr hWnd, int dwTime, AnimateWindowFlags flags);

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
        _vars.RegisterControls.Visible = false;
        _vars.FileHashControls.Visible = false;
        _vars.EncryptionControls.Visible = false;
        Size = Size with { Height = 510 };
        Size = Size with { Width = 885 };
        _vars.VaultControls.Location = new Point(200, 55);
        _vars.VaultControls.Visible = true;
        Controls.Add(_vars.VaultControls);
    }

    private void LoginBtn_Click(object sender, EventArgs e)
    {
        SidePanelMarker.Height = LoginBtn.Height;
        SidePanelMarker.Top = LoginBtn.Top;
        LoginGroupBox.Visible = true;
        Controls.Add(LoginGroupBox);
        Size = Size with { Height = 492 };
        Size = Size with { Width = 803 };
        _vars.VaultControls.Visible = false;
        _vars.RegisterControls.Visible = false;
        _vars.EncryptionControls.Visible = false;
        _vars.FileHashControls.Visible = false;
    }

    private void RegisterBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 504 };
        Size = Size with { Width = 670 };
        _vars.RegisterControls.Location = new Point(200, 45);
        SidePanelMarker.Height = RegisterBtn.Height;
        SidePanelMarker.Top = RegisterBtn.Top;
        LoginGroupBox.Visible = false;
        _vars.VaultControls.Visible = false;
        _vars.RegisterControls.Visible = true;
        _vars.EncryptionControls.Visible = false;
        _vars.FileHashControls.Visible = false;
        Controls.Add(_vars.RegisterControls);
    }

    private void EncryptionBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 485 };
        Size = Size with { Width = 1158 };
        LoginGroupBox.Visible = false;
        _vars.VaultControls.Visible = false;
        _vars.RegisterControls.Visible = false;
        _vars.FileHashControls.Visible = false;
        _vars.EncryptionControls.Location = new Point(215, 60);
        SidePanelMarker.Height = EncryptionBtn.Height;
        SidePanelMarker.Top = EncryptionBtn.Top;
        _vars.EncryptionControls.Visible = true;
        Controls.Add(_vars.EncryptionControls);
    }

    private void FileHashBtn_Click(object sender, EventArgs e)
    {
        Size = Size with { Height = 380 };
        Size = Size with { Width = 1158 };
        LoginGroupBox.Visible = false;
        _vars.VaultControls.Visible = false;
        _vars.RegisterControls.Visible = false;
        _vars.EncryptionControls.Visible = false;
        _vars.FileHashControls.Location = new Point(178, -10);
        SidePanelMarker.Height = FileHashBtn.Height;
        SidePanelMarker.Top = FileHashBtn.Top;
        _vars.FileHashControls.Visible = true;
        Controls.Add(_vars.FileHashControls);
    }

    #endregion

    #region DragForm

    private void PasswordVault_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        _vars.IsDragging = true;
        _vars.Offset = e.Location;
    }

    private void PasswordVault_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_vars.IsDragging)
            return;

        var newLocation = PointToScreen(new Point(e.X, e.Y));
        newLocation.Offset(-_vars.Offset.X, -_vars.Offset.Y);
        Location = newLocation;
    }

    private void PasswordVault_MouseUp(object sender, MouseEventArgs e)
    {
        _vars.IsDragging = false;
    }

    private void TopPanelBar_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        _vars.IsDragging = true;
        _vars.Offset = e.Location;
    }

    private void TopPanelBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_vars.IsDragging)
            return;

        var newLocation = PointToScreen(new Point(e.X, e.Y));
        newLocation.Offset(-_vars.Offset.X, -_vars.Offset.Y);
        Location = newLocation;
    }

    private void TopPanelBar_MouseUp(object sender, MouseEventArgs e)
    {
        _vars.IsDragging = false;
    }

    #endregion

}