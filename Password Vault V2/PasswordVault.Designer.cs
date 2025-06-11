using System.ComponentModel;

namespace Password_Vault_V2
{
    partial class PasswordVault
    {
        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(PasswordVault));
            SidePanelMenu = new Panel();
            CryptoSettingsBtn = new Button();
            SeparatePanel = new Panel();
            LoginPanel = new Panel();
            RegisterBtn = new Button();
            FileHashBtn = new Button();
            EncryptionBtn = new Button();
            VaultBtn = new Button();
            SidePanelMarker = new Panel();
            LoginBtn = new Button();
            TopPanelBar = new Panel();
            label2 = new Label();
            MinimizeIcon = new PictureBox();
            ShutdownIcon = new PictureBox();
            WelcomeLabel = new Label();
            pictureBox1 = new PictureBox();
            pictureBox3 = new PictureBox();
            UsernameTxt = new TextBox();
            BtnLogin = new Button();
            UsernameLabel = new Label();
            PasswordLabel = new Label();
            AttemptsRemainingLabel = new Label();
            AttemptsNumberLabel = new Label();
            StatusLabel = new Label();
            StatusOutputLabel = new Label();
            LoginGroupBox = new GroupBox();
            PasswordTxt = new TextBox();
            LogoutBtn = new Button();
            RememberMeCheckBox = new CheckBox();
            SidePanelMenu.SuspendLayout();
            TopPanelBar.SuspendLayout();
            ((ISupportInitialize)MinimizeIcon).BeginInit();
            ((ISupportInitialize)ShutdownIcon).BeginInit();
            ((ISupportInitialize)pictureBox1).BeginInit();
            ((ISupportInitialize)pictureBox3).BeginInit();
            LoginGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // SidePanelMenu
            // 
            SidePanelMenu.BackColor = Color.FromArgb(30, 30, 30);
            SidePanelMenu.Controls.Add(CryptoSettingsBtn);
            SidePanelMenu.Controls.Add(SeparatePanel);
            SidePanelMenu.Controls.Add(LoginPanel);
            SidePanelMenu.Controls.Add(RegisterBtn);
            SidePanelMenu.Controls.Add(FileHashBtn);
            SidePanelMenu.Controls.Add(EncryptionBtn);
            SidePanelMenu.Controls.Add(VaultBtn);
            SidePanelMenu.Controls.Add(SidePanelMarker);
            SidePanelMenu.Controls.Add(LoginBtn);
            SidePanelMenu.Dock = DockStyle.Left;
            SidePanelMenu.Location = new Point(0, 0);
            SidePanelMenu.Name = "SidePanelMenu";
            SidePanelMenu.Size = new Size(206, 514);
            SidePanelMenu.TabIndex = 0;
            // 
            // CryptoSettingsBtn
            // 
            CryptoSettingsBtn.FlatAppearance.BorderSize = 0;
            CryptoSettingsBtn.FlatStyle = FlatStyle.Flat;
            CryptoSettingsBtn.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            CryptoSettingsBtn.ForeColor = Color.White;
            CryptoSettingsBtn.Image = (Image)resources.GetObject("CryptoSettingsBtn.Image");
            CryptoSettingsBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CryptoSettingsBtn.Location = new Point(19, 308);
            CryptoSettingsBtn.Name = "CryptoSettingsBtn";
            CryptoSettingsBtn.Size = new Size(170, 53);
            CryptoSettingsBtn.TabIndex = 12;
            CryptoSettingsBtn.TabStop = false;
            CryptoSettingsBtn.Text = "     Settings";
            CryptoSettingsBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
            CryptoSettingsBtn.UseVisualStyleBackColor = true;
            CryptoSettingsBtn.Click += CryptoSettingsBtn_Click;
            // 
            // SeparatePanel
            // 
            SeparatePanel.BackColor = Color.Cyan;
            SeparatePanel.Location = new Point(198, 0);
            SeparatePanel.Name = "SeparatePanel";
            SeparatePanel.Size = new Size(8, 514);
            SeparatePanel.TabIndex = 11;
            // 
            // LoginPanel
            // 
            LoginPanel.Location = new Point(206, 59);
            LoginPanel.Name = "LoginPanel";
            LoginPanel.Size = new Size(597, 341);
            LoginPanel.TabIndex = 7;
            // 
            // RegisterBtn
            // 
            RegisterBtn.FlatAppearance.BorderSize = 0;
            RegisterBtn.FlatStyle = FlatStyle.Flat;
            RegisterBtn.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            RegisterBtn.ForeColor = Color.White;
            RegisterBtn.Image = (Image)resources.GetObject("RegisterBtn.Image");
            RegisterBtn.ImageAlign = ContentAlignment.MiddleLeft;
            RegisterBtn.Location = new Point(19, 74);
            RegisterBtn.Name = "RegisterBtn";
            RegisterBtn.Size = new Size(173, 53);
            RegisterBtn.TabIndex = 7;
            RegisterBtn.TabStop = false;
            RegisterBtn.Text = "     Register";
            RegisterBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
            RegisterBtn.UseVisualStyleBackColor = true;
            RegisterBtn.Click += RegisterBtn_Click;
            // 
            // FileHashBtn
            // 
            FileHashBtn.FlatAppearance.BorderSize = 0;
            FileHashBtn.FlatStyle = FlatStyle.Flat;
            FileHashBtn.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FileHashBtn.ForeColor = Color.White;
            FileHashBtn.Image = (Image)resources.GetObject("FileHashBtn.Image");
            FileHashBtn.ImageAlign = ContentAlignment.MiddleLeft;
            FileHashBtn.Location = new Point(22, 248);
            FileHashBtn.Name = "FileHashBtn";
            FileHashBtn.Size = new Size(170, 53);
            FileHashBtn.TabIndex = 10;
            FileHashBtn.TabStop = false;
            FileHashBtn.Text = "     File Hash";
            FileHashBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
            FileHashBtn.UseVisualStyleBackColor = true;
            FileHashBtn.Click += FileHashBtn_Click;
            // 
            // EncryptionBtn
            // 
            EncryptionBtn.FlatAppearance.BorderSize = 0;
            EncryptionBtn.FlatStyle = FlatStyle.Flat;
            EncryptionBtn.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            EncryptionBtn.ForeColor = Color.White;
            EncryptionBtn.Image = (Image)resources.GetObject("EncryptionBtn.Image");
            EncryptionBtn.ImageAlign = ContentAlignment.MiddleLeft;
            EncryptionBtn.Location = new Point(19, 189);
            EncryptionBtn.Name = "EncryptionBtn";
            EncryptionBtn.Size = new Size(173, 53);
            EncryptionBtn.TabIndex = 9;
            EncryptionBtn.TabStop = false;
            EncryptionBtn.Text = "     Encryption";
            EncryptionBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
            EncryptionBtn.UseVisualStyleBackColor = true;
            EncryptionBtn.Click += EncryptionBtn_Click;
            // 
            // VaultBtn
            // 
            VaultBtn.FlatAppearance.BorderSize = 0;
            VaultBtn.FlatStyle = FlatStyle.Flat;
            VaultBtn.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            VaultBtn.ForeColor = Color.White;
            VaultBtn.Image = (Image)resources.GetObject("VaultBtn.Image");
            VaultBtn.ImageAlign = ContentAlignment.MiddleLeft;
            VaultBtn.Location = new Point(19, 133);
            VaultBtn.Name = "VaultBtn";
            VaultBtn.Size = new Size(173, 53);
            VaultBtn.TabIndex = 8;
            VaultBtn.TabStop = false;
            VaultBtn.Text = "     Vault";
            VaultBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
            VaultBtn.UseVisualStyleBackColor = true;
            VaultBtn.Click += VaultBtn_Click;
            // 
            // SidePanelMarker
            // 
            SidePanelMarker.BackColor = Color.Cyan;
            SidePanelMarker.Location = new Point(3, 9);
            SidePanelMarker.Name = "SidePanelMarker";
            SidePanelMarker.Size = new Size(10, 56);
            SidePanelMarker.TabIndex = 6;
            // 
            // LoginBtn
            // 
            LoginBtn.FlatAppearance.BorderSize = 0;
            LoginBtn.FlatStyle = FlatStyle.Flat;
            LoginBtn.Font = new Font("Century Gothic", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LoginBtn.ForeColor = Color.White;
            LoginBtn.Image = (Image)resources.GetObject("LoginBtn.Image");
            LoginBtn.ImageAlign = ContentAlignment.MiddleLeft;
            LoginBtn.Location = new Point(19, 15);
            LoginBtn.Name = "LoginBtn";
            LoginBtn.Size = new Size(173, 53);
            LoginBtn.TabIndex = 6;
            LoginBtn.TabStop = false;
            LoginBtn.Text = "     Login";
            LoginBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
            LoginBtn.UseVisualStyleBackColor = true;
            LoginBtn.Click += LoginBtn_Click;
            // 
            // TopPanelBar
            // 
            TopPanelBar.BackColor = Color.FromArgb(30, 30, 30);
            TopPanelBar.Controls.Add(label2);
            TopPanelBar.Controls.Add(MinimizeIcon);
            TopPanelBar.Controls.Add(ShutdownIcon);
            TopPanelBar.Dock = DockStyle.Top;
            TopPanelBar.Location = new Point(206, 0);
            TopPanelBar.Name = "TopPanelBar";
            TopPanelBar.Size = new Size(643, 53);
            TopPanelBar.TabIndex = 11;
            TopPanelBar.MouseDown += TopPanelBar_MouseDown;
            TopPanelBar.MouseMove += TopPanelBar_MouseMove;
            TopPanelBar.MouseUp += TopPanelBar_MouseUp;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Century Gothic", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.DeepSkyBlue;
            label2.Location = new Point(6, 9);
            label2.Name = "label2";
            label2.Size = new Size(191, 30);
            label2.TabIndex = 5;
            label2.Text = "Password Vault";
            // 
            // MinimizeIcon
            // 
            MinimizeIcon.BackColor = Color.FromArgb(30, 30, 30);
            MinimizeIcon.Dock = DockStyle.Right;
            MinimizeIcon.Image = (Image)resources.GetObject("MinimizeIcon.Image");
            MinimizeIcon.Location = new Point(539, 0);
            MinimizeIcon.Name = "MinimizeIcon";
            MinimizeIcon.Size = new Size(52, 53);
            MinimizeIcon.SizeMode = PictureBoxSizeMode.Zoom;
            MinimizeIcon.TabIndex = 3;
            MinimizeIcon.TabStop = false;
            MinimizeIcon.Click += MinimizeIcon_Click;
            MinimizeIcon.MouseEnter += MinimizeIcon_MouseEnter;
            MinimizeIcon.MouseLeave += MinimizeIcon_MouseLeave;
            // 
            // ShutdownIcon
            // 
            ShutdownIcon.BackColor = Color.FromArgb(30, 30, 30);
            ShutdownIcon.Dock = DockStyle.Right;
            ShutdownIcon.Image = (Image)resources.GetObject("ShutdownIcon.Image");
            ShutdownIcon.Location = new Point(591, 0);
            ShutdownIcon.Name = "ShutdownIcon";
            ShutdownIcon.Size = new Size(52, 53);
            ShutdownIcon.SizeMode = PictureBoxSizeMode.Zoom;
            ShutdownIcon.TabIndex = 4;
            ShutdownIcon.TabStop = false;
            ShutdownIcon.Click += ShutdownIcon_Click;
            ShutdownIcon.MouseEnter += ShutdownIcon_MouseEnter;
            ShutdownIcon.MouseLeave += ShutdownIcon_MouseLeave;
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(6, 383);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 6;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(6, 107);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(68, 54);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 5;
            pictureBox1.TabStop = false;
            // 
            // pictureBox3
            // 
            pictureBox3.Image = (Image)resources.GetObject("pictureBox3.Image");
            pictureBox3.Location = new Point(6, 30);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(68, 54);
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.TabIndex = 4;
            pictureBox3.TabStop = false;
            // 
            // UsernameTxt
            // 
            UsernameTxt.BackColor = Color.FromArgb(30, 30, 30);
            UsernameTxt.ForeColor = Color.White;
            UsernameTxt.Location = new Point(80, 53);
            UsernameTxt.Name = "UsernameTxt";
            UsernameTxt.Size = new Size(525, 34);
            UsernameTxt.TabIndex = 0;
            // 
            // BtnLogin
            // 
            BtnLogin.BackColor = Color.FromArgb(30, 30, 30);
            BtnLogin.FlatStyle = FlatStyle.Flat;
            BtnLogin.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnLogin.Image = Properties.Resources.enter;
            BtnLogin.ImageAlign = ContentAlignment.MiddleLeft;
            BtnLogin.Location = new Point(80, 166);
            BtnLogin.Name = "BtnLogin";
            BtnLogin.Size = new Size(525, 42);
            BtnLogin.TabIndex = 3;
            BtnLogin.Text = "&Login";
            BtnLogin.UseVisualStyleBackColor = false;
            BtnLogin.Click += BtnLogin_Click;
            // 
            // UsernameLabel
            // 
            UsernameLabel.AutoSize = true;
            UsernameLabel.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            UsernameLabel.Location = new Point(80, 15);
            UsernameLabel.Name = "UsernameLabel";
            UsernameLabel.Size = new Size(121, 25);
            UsernameLabel.TabIndex = 9;
            UsernameLabel.Text = "Username";
            // 
            // PasswordLabel
            // 
            PasswordLabel.AutoSize = true;
            PasswordLabel.Font = new Font("Century Gothic", 11F);
            PasswordLabel.Location = new Point(80, 90);
            PasswordLabel.Name = "PasswordLabel";
            PasswordLabel.Size = new Size(114, 25);
            PasswordLabel.TabIndex = 10;
            PasswordLabel.Text = "Password";
            // 
            // AttemptsRemainingLabel
            // 
            AttemptsRemainingLabel.AutoSize = true;
            AttemptsRemainingLabel.Font = new Font("Century Gothic", 11F);
            AttemptsRemainingLabel.Location = new Point(315, 404);
            AttemptsRemainingLabel.Name = "AttemptsRemainingLabel";
            AttemptsRemainingLabel.Size = new Size(250, 25);
            AttemptsRemainingLabel.TabIndex = 11;
            AttemptsRemainingLabel.Text = "Attempts Remaining ::";
            // 
            // AttemptsNumberLabel
            // 
            AttemptsNumberLabel.AutoSize = true;
            AttemptsNumberLabel.Font = new Font("Century Gothic", 11F);
            AttemptsNumberLabel.Location = new Point(571, 404);
            AttemptsNumberLabel.Name = "AttemptsNumberLabel";
            AttemptsNumberLabel.Size = new Size(24, 25);
            AttemptsNumberLabel.TabIndex = 12;
            AttemptsNumberLabel.Text = "3";
            // 
            // StatusLabel
            // 
            StatusLabel.AutoSize = true;
            StatusLabel.Font = new Font("Century Gothic", 11F);
            StatusLabel.Location = new Point(6, 404);
            StatusLabel.Name = "StatusLabel";
            StatusLabel.Size = new Size(94, 25);
            StatusLabel.TabIndex = 13;
            StatusLabel.Text = "Status ::";
            // 
            // StatusOutputLabel
            // 
            StatusOutputLabel.AutoSize = true;
            StatusOutputLabel.Font = new Font("Century Gothic", 11F);
            StatusOutputLabel.ForeColor = Color.White;
            StatusOutputLabel.Location = new Point(104, 404);
            StatusOutputLabel.Name = "StatusOutputLabel";
            StatusOutputLabel.Size = new Size(71, 25);
            StatusOutputLabel.TabIndex = 14;
            StatusOutputLabel.Text = "Idle...";
            // 
            // LoginGroupBox
            // 
            LoginGroupBox.BackColor = Color.FromArgb(30, 30, 30);
            LoginGroupBox.Controls.Add(PasswordTxt);
            LoginGroupBox.Controls.Add(LogoutBtn);
            LoginGroupBox.Controls.Add(WelcomeLabel);
            LoginGroupBox.Controls.Add(RememberMeCheckBox);
            LoginGroupBox.Controls.Add(StatusOutputLabel);
            LoginGroupBox.Controls.Add(StatusLabel);
            LoginGroupBox.Controls.Add(AttemptsNumberLabel);
            LoginGroupBox.Controls.Add(AttemptsRemainingLabel);
            LoginGroupBox.Controls.Add(PasswordLabel);
            LoginGroupBox.Controls.Add(UsernameLabel);
            LoginGroupBox.Controls.Add(BtnLogin);
            LoginGroupBox.Controls.Add(UsernameTxt);
            LoginGroupBox.Controls.Add(pictureBox3);
            LoginGroupBox.Controls.Add(pictureBox1);
            LoginGroupBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LoginGroupBox.ForeColor = Color.White;
            LoginGroupBox.Location = new Point(221, 59);
            LoginGroupBox.Name = "LoginGroupBox";
            LoginGroupBox.Size = new Size(611, 443);
            LoginGroupBox.TabIndex = 6;
            LoginGroupBox.TabStop = false;
            LoginGroupBox.Text = "Login";
            // 
            // PasswordTxt
            // 
            PasswordTxt.BackColor = Color.FromArgb(30, 30, 30);
            PasswordTxt.ForeColor = Color.White;
            PasswordTxt.Location = new Point(80, 126);
            PasswordTxt.Name = "PasswordTxt";
            PasswordTxt.Size = new Size(525, 34);
            PasswordTxt.TabIndex = 2;
            PasswordTxt.KeyDown += PasswordTxt_KeyDown;
            PasswordTxt.KeyPress += PasswordTxt_KeyPress;
            // 
            // LogoutBtn
            // 
            LogoutBtn.BackColor = Color.FromArgb(30, 30, 30);
            LogoutBtn.FlatStyle = FlatStyle.Flat;
            LogoutBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LogoutBtn.Image = (Image)resources.GetObject("LogoutBtn.Image");
            LogoutBtn.ImageAlign = ContentAlignment.MiddleLeft;
            LogoutBtn.Location = new Point(80, 212);
            LogoutBtn.Name = "LogoutBtn";
            LogoutBtn.Size = new Size(525, 42);
            LogoutBtn.TabIndex = 4;
            LogoutBtn.Text = "&Logout";
            LogoutBtn.UseVisualStyleBackColor = false;
            LogoutBtn.Click += LogoutBtn_Click;
            // 
            // RememberMeCheckBox
            // 
            RememberMeCheckBox.AutoSize = true;
            RememberMeCheckBox.Font = new Font("Century Gothic", 11F);
            RememberMeCheckBox.Location = new Point(80, 260);
            RememberMeCheckBox.Name = "RememberMeCheckBox";
            RememberMeCheckBox.Size = new Size(202, 29);
            RememberMeCheckBox.TabIndex = 6;
            RememberMeCheckBox.Text = "Remember Me";
            RememberMeCheckBox.UseVisualStyleBackColor = true;
            // 
            // PasswordVault
            // 
            AcceptButton = BtnLogin;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(849, 514);
            ControlBox = false;
            Controls.Add(LoginGroupBox);
            Controls.Add(TopPanelBar);
            Controls.Add(SidePanelMenu);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "PasswordVault";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Password Vault";
            Load += PasswordVault_Load;
            MouseDown += PasswordVault_MouseDown;
            MouseMove += PasswordVault_MouseMove;
            MouseUp += PasswordVault_MouseUp;
            SidePanelMenu.ResumeLayout(false);
            TopPanelBar.ResumeLayout(false);
            TopPanelBar.PerformLayout();
            ((ISupportInitialize)MinimizeIcon).EndInit();
            ((ISupportInitialize)ShutdownIcon).EndInit();
            ((ISupportInitialize)pictureBox1).EndInit();
            ((ISupportInitialize)pictureBox3).EndInit();
            LoginGroupBox.ResumeLayout(false);
            LoginGroupBox.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel SidePanelMenu;
        private Panel TopPanelBar;
        private PictureBox MinimizeIcon;
        private PictureBox ShutdownIcon;
        private Label label2;
        private Button LoginBtn;
        private Button VaultBtn;
        private Panel SidePanelMarker;
        private Button FileHashBtn;
        private Button EncryptionBtn;
        private Button RegisterBtn;
        private Panel LoginPanel;
        private PictureBox pictureBox1;
        private PictureBox pictureBox3;
        private TextBox UsernameTxt;
        private Button BtnLogin;
        private Label UsernameLabel;
        private Label PasswordLabel;
        private Label AttemptsRemainingLabel;
        private Label AttemptsNumberLabel;
        private Label StatusLabel;
        private Label StatusOutputLabel;
        private GroupBox LoginGroupBox;
        private Label WelcomeLabel;
        private CheckBox RememberMeCheckBox;
        private Button LogoutBtn;
        private Panel SeparatePanel;
        private TextBox PasswordTxt;
        private Button CryptoSettingsBtn;
    }
}
