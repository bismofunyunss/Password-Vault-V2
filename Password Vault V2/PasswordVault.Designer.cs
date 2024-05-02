namespace Password_Vault_V2
{
    partial class PasswordVault
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PasswordVault));
            SidePanelMenu = new Panel();
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
            PasswordTxt = new TextBox();
            BtnLogin = new Button();
            label1 = new Label();
            label3 = new Label();
            AttemptsRemainingLabel = new Label();
            AttemptsNumberLabel = new Label();
            StatusLabel = new Label();
            StatusOutputLabel = new Label();
            LoginGroupBox = new GroupBox();
            LogoutBtn = new Button();
            ShowPasswordCheckBox = new CheckBox();
            RememberMeCheckBox = new CheckBox();
            SidePanelMenu.SuspendLayout();
            TopPanelBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MinimizeIcon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ShutdownIcon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            LoginGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // SidePanelMenu
            // 
            SidePanelMenu.BackColor = Color.FromArgb(30, 30, 30);
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
            SidePanelMenu.Size = new Size(206, 492);
            SidePanelMenu.TabIndex = 0;
            // 
            // SeparatePanel
            // 
            SeparatePanel.BackColor = Color.Cyan;
            SeparatePanel.Location = new Point(198, 0);
            SeparatePanel.Name = "SeparatePanel";
            SeparatePanel.Size = new Size(8, 492);
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
            RegisterBtn.TabIndex = 10;
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
            FileHashBtn.TabIndex = 9;
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
            EncryptionBtn.TabIndex = 8;
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
            VaultBtn.TabIndex = 7;
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
            TopPanelBar.Size = new Size(597, 53);
            TopPanelBar.TabIndex = 1;
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
            MinimizeIcon.Location = new Point(493, 0);
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
            ShutdownIcon.Location = new Point(545, 0);
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
            WelcomeLabel.Location = new Point(6, 362);
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
            UsernameTxt.Size = new Size(493, 34);
            UsernameTxt.TabIndex = 6;
            // 
            // PasswordTxt
            // 
            PasswordTxt.BackColor = Color.FromArgb(30, 30, 30);
            PasswordTxt.ForeColor = Color.White;
            PasswordTxt.Location = new Point(80, 130);
            PasswordTxt.Name = "PasswordTxt";
            PasswordTxt.Size = new Size(493, 34);
            PasswordTxt.TabIndex = 7;
            PasswordTxt.UseSystemPasswordChar = true;
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
            BtnLogin.Size = new Size(493, 42);
            BtnLogin.TabIndex = 8;
            BtnLogin.Text = "&Login";
            BtnLogin.UseVisualStyleBackColor = false;
            BtnLogin.Click += BtnLogin_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(80, 25);
            label1.Name = "label1";
            label1.Size = new Size(121, 25);
            label1.TabIndex = 9;
            label1.Text = "Username";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Century Gothic", 11F);
            label3.Location = new Point(75, 102);
            label3.Name = "label3";
            label3.Size = new Size(114, 25);
            label3.TabIndex = 10;
            label3.Text = "Password";
            // 
            // AttemptsRemainingLabel
            // 
            AttemptsRemainingLabel.AutoSize = true;
            AttemptsRemainingLabel.Font = new Font("Century Gothic", 11F);
            AttemptsRemainingLabel.Location = new Point(289, 383);
            AttemptsRemainingLabel.Name = "AttemptsRemainingLabel";
            AttemptsRemainingLabel.Size = new Size(250, 25);
            AttemptsRemainingLabel.TabIndex = 11;
            AttemptsRemainingLabel.Text = "Attempts Remaining ::";
            // 
            // AttemptsNumberLabel
            // 
            AttemptsNumberLabel.AutoSize = true;
            AttemptsNumberLabel.Font = new Font("Century Gothic", 11F);
            AttemptsNumberLabel.Location = new Point(545, 383);
            AttemptsNumberLabel.Name = "AttemptsNumberLabel";
            AttemptsNumberLabel.Size = new Size(24, 25);
            AttemptsNumberLabel.TabIndex = 12;
            AttemptsNumberLabel.Text = "3";
            // 
            // StatusLabel
            // 
            StatusLabel.AutoSize = true;
            StatusLabel.Font = new Font("Century Gothic", 11F);
            StatusLabel.Location = new Point(6, 383);
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
            StatusOutputLabel.Location = new Point(82, 383);
            StatusOutputLabel.Name = "StatusOutputLabel";
            StatusOutputLabel.Size = new Size(71, 25);
            StatusOutputLabel.TabIndex = 14;
            StatusOutputLabel.Text = "Idle...";
            // 
            // LoginGroupBox
            // 
            LoginGroupBox.BackColor = Color.FromArgb(30, 30, 30);
            LoginGroupBox.Controls.Add(LogoutBtn);
            LoginGroupBox.Controls.Add(WelcomeLabel);
            LoginGroupBox.Controls.Add(ShowPasswordCheckBox);
            LoginGroupBox.Controls.Add(RememberMeCheckBox);
            LoginGroupBox.Controls.Add(StatusOutputLabel);
            LoginGroupBox.Controls.Add(StatusLabel);
            LoginGroupBox.Controls.Add(AttemptsNumberLabel);
            LoginGroupBox.Controls.Add(AttemptsRemainingLabel);
            LoginGroupBox.Controls.Add(label3);
            LoginGroupBox.Controls.Add(label1);
            LoginGroupBox.Controls.Add(BtnLogin);
            LoginGroupBox.Controls.Add(PasswordTxt);
            LoginGroupBox.Controls.Add(UsernameTxt);
            LoginGroupBox.Controls.Add(pictureBox3);
            LoginGroupBox.Controls.Add(pictureBox1);
            LoginGroupBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LoginGroupBox.ForeColor = Color.White;
            LoginGroupBox.Location = new Point(212, 59);
            LoginGroupBox.Name = "LoginGroupBox";
            LoginGroupBox.Size = new Size(579, 421);
            LoginGroupBox.TabIndex = 6;
            LoginGroupBox.TabStop = false;
            LoginGroupBox.Text = "Login";
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
            LogoutBtn.Size = new Size(493, 48);
            LogoutBtn.TabIndex = 17;
            LogoutBtn.Text = "&Logout";
            LogoutBtn.UseVisualStyleBackColor = false;
            LogoutBtn.Click += LogoutBtn_Click;
            // 
            // ShowPasswordCheckBox
            // 
            ShowPasswordCheckBox.AutoSize = true;
            ShowPasswordCheckBox.Font = new Font("Century Gothic", 11F);
            ShowPasswordCheckBox.Location = new Point(75, 266);
            ShowPasswordCheckBox.Name = "ShowPasswordCheckBox";
            ShowPasswordCheckBox.Size = new Size(204, 29);
            ShowPasswordCheckBox.TabIndex = 16;
            ShowPasswordCheckBox.Text = "Show Password";
            ShowPasswordCheckBox.UseVisualStyleBackColor = true;
            ShowPasswordCheckBox.CheckedChanged += ShowPasswordCheckBox_CheckedChanged;
            // 
            // RememberMeCheckBox
            // 
            RememberMeCheckBox.AutoSize = true;
            RememberMeCheckBox.Font = new Font("Century Gothic", 11F);
            RememberMeCheckBox.Location = new Point(371, 266);
            RememberMeCheckBox.Name = "RememberMeCheckBox";
            RememberMeCheckBox.Size = new Size(202, 29);
            RememberMeCheckBox.TabIndex = 15;
            RememberMeCheckBox.Text = "Remember Me";
            RememberMeCheckBox.UseVisualStyleBackColor = true;
            // 
            // PasswordVault
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(803, 492);
            Controls.Add(LoginGroupBox);
            Controls.Add(TopPanelBar);
            Controls.Add(SidePanelMenu);
            FormBorderStyle = FormBorderStyle.None;
            Name = "PasswordVault";
            StartPosition = FormStartPosition.CenterScreen;
            Load += PasswordVault_Load;
            MouseDown += PasswordVault_MouseDown;
            MouseMove += PasswordVault_MouseMove;
            MouseUp += PasswordVault_MouseUp;
            SidePanelMenu.ResumeLayout(false);
            TopPanelBar.ResumeLayout(false);
            TopPanelBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MinimizeIcon).EndInit();
            ((System.ComponentModel.ISupportInitialize)ShutdownIcon).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
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
        private TextBox PasswordTxt;
        private Button BtnLogin;
        private Label label1;
        private Label label3;
        private Label AttemptsRemainingLabel;
        private Label AttemptsNumberLabel;
        private Label StatusLabel;
        private Label StatusOutputLabel;
        private GroupBox LoginGroupBox;
        private Label WelcomeLabel;
        private CheckBox ShowPasswordCheckBox;
        private CheckBox RememberMeCheckBox;
        private Button LogoutBtn;
        private Panel SeparatePanel;
    }
}
