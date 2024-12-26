using System.ComponentModel;

namespace Password_Vault_V2
{
    partial class Encryption
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            FileEncryptDecryptBox = new GroupBox();
            pictureBox1 = new PictureBox();
            WelcomeLabel = new Label();
            PasswordBox = new GroupBox();
            ViewPasswordsCheckbox = new CheckBox();
            confirmPasswordLbl = new Label();
            ConfirmPassword = new TextBox();
            passLbl = new Label();
            CustomPasswordTextBox = new TextBox();
            CustomPasswordCheckBox = new CheckBox();
            FileSizeNumLbl = new Label();
            FileSizeLbl = new Label();
            FileOutputLbl = new Label();
            FileStatusLbl = new Label();
            DecryptBtn = new Button();
            EncryptBtn = new Button();
            ExportFileBtn = new Button();
            ImportFileBtn = new Button();
            FileEncryptDecryptBox.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            PasswordBox.SuspendLayout();
            SuspendLayout();
            // 
            // FileEncryptDecryptBox
            // 
            FileEncryptDecryptBox.BackColor = Color.FromArgb(30, 30, 30);
            FileEncryptDecryptBox.Controls.Add(pictureBox1);
            FileEncryptDecryptBox.Controls.Add(WelcomeLabel);
            FileEncryptDecryptBox.Controls.Add(PasswordBox);
            FileEncryptDecryptBox.Controls.Add(FileSizeNumLbl);
            FileEncryptDecryptBox.Controls.Add(FileSizeLbl);
            FileEncryptDecryptBox.Controls.Add(FileOutputLbl);
            FileEncryptDecryptBox.Controls.Add(FileStatusLbl);
            FileEncryptDecryptBox.Controls.Add(DecryptBtn);
            FileEncryptDecryptBox.Controls.Add(EncryptBtn);
            FileEncryptDecryptBox.Controls.Add(ExportFileBtn);
            FileEncryptDecryptBox.Controls.Add(ImportFileBtn);
            FileEncryptDecryptBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FileEncryptDecryptBox.ForeColor = Color.WhiteSmoke;
            FileEncryptDecryptBox.Location = new Point(3, 3);
            FileEncryptDecryptBox.Name = "FileEncryptDecryptBox";
            FileEncryptDecryptBox.Size = new Size(930, 408);
            FileEncryptDecryptBox.TabIndex = 10;
            FileEncryptDecryptBox.TabStop = false;
            FileEncryptDecryptBox.Text = "File Encryptor / Decryptor";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.sha_256;
            pictureBox1.Location = new Point(786, 263);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(135, 134);
            pictureBox1.TabIndex = 21;
            pictureBox1.TabStop = false;
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(6, 326);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 20;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // PasswordBox
            // 
            PasswordBox.Controls.Add(ViewPasswordsCheckbox);
            PasswordBox.Controls.Add(confirmPasswordLbl);
            PasswordBox.Controls.Add(ConfirmPassword);
            PasswordBox.Controls.Add(passLbl);
            PasswordBox.Controls.Add(CustomPasswordTextBox);
            PasswordBox.Controls.Add(CustomPasswordCheckBox);
            PasswordBox.Font = new Font("Century Gothic", 11F);
            PasswordBox.ForeColor = Color.WhiteSmoke;
            PasswordBox.Location = new Point(7, 132);
            PasswordBox.Name = "PasswordBox";
            PasswordBox.Size = new Size(613, 186);
            PasswordBox.TabIndex = 19;
            PasswordBox.TabStop = false;
            PasswordBox.Text = "Custom Password For Encryption / Decryption";
            // 
            // ViewPasswordsCheckbox
            // 
            ViewPasswordsCheckbox.AutoSize = true;
            ViewPasswordsCheckbox.Location = new Point(321, 99);
            ViewPasswordsCheckbox.Name = "ViewPasswordsCheckbox";
            ViewPasswordsCheckbox.Size = new Size(213, 29);
            ViewPasswordsCheckbox.TabIndex = 8;
            ViewPasswordsCheckbox.Text = "Show Passwords";
            ViewPasswordsCheckbox.UseVisualStyleBackColor = true;
            // 
            // confirmPasswordLbl
            // 
            confirmPasswordLbl.AutoSize = true;
            confirmPasswordLbl.Location = new Point(10, 103);
            confirmPasswordLbl.Name = "confirmPasswordLbl";
            confirmPasswordLbl.Size = new Size(205, 25);
            confirmPasswordLbl.TabIndex = 25;
            confirmPasswordLbl.Text = "Confirm Password";
            // 
            // ConfirmPassword
            // 
            ConfirmPassword.BackColor = Color.FromArgb(30, 30, 30);
            ConfirmPassword.Font = new Font("Century Gothic", 11F);
            ConfirmPassword.ForeColor = Color.White;
            ConfirmPassword.Location = new Point(10, 131);
            ConfirmPassword.Name = "ConfirmPassword";
            ConfirmPassword.Size = new Size(305, 34);
            ConfirmPassword.TabIndex = 6;
            ConfirmPassword.UseSystemPasswordChar = true;
            // 
            // passLbl
            // 
            passLbl.AutoSize = true;
            passLbl.Location = new Point(10, 39);
            passLbl.Name = "passLbl";
            passLbl.Size = new Size(114, 25);
            passLbl.TabIndex = 23;
            passLbl.Text = "Password";
            // 
            // CustomPasswordTextBox
            // 
            CustomPasswordTextBox.BackColor = Color.FromArgb(30, 30, 30);
            CustomPasswordTextBox.Font = new Font("Century Gothic", 11F);
            CustomPasswordTextBox.ForeColor = Color.White;
            CustomPasswordTextBox.Location = new Point(10, 67);
            CustomPasswordTextBox.Name = "CustomPasswordTextBox";
            CustomPasswordTextBox.Size = new Size(305, 34);
            CustomPasswordTextBox.TabIndex = 5;
            CustomPasswordTextBox.UseSystemPasswordChar = true;
            // 
            // CustomPasswordCheckBox
            // 
            CustomPasswordCheckBox.AutoSize = true;
            CustomPasswordCheckBox.Location = new Point(321, 67);
            CustomPasswordCheckBox.Name = "CustomPasswordCheckBox";
            CustomPasswordCheckBox.Size = new Size(274, 29);
            CustomPasswordCheckBox.TabIndex = 7;
            CustomPasswordCheckBox.Text = "Use Custom Password";
            CustomPasswordCheckBox.UseVisualStyleBackColor = true;
            CustomPasswordCheckBox.CheckedChanged += CustomPasswordCheckBox_CheckedChanged;
            // 
            // FileSizeNumLbl
            // 
            FileSizeNumLbl.AutoSize = true;
            FileSizeNumLbl.Font = new Font("Century Gothic", 11F);
            FileSizeNumLbl.Location = new Point(121, 372);
            FileSizeNumLbl.Name = "FileSizeNumLbl";
            FileSizeNumLbl.Size = new Size(24, 25);
            FileSizeNumLbl.TabIndex = 16;
            FileSizeNumLbl.Text = "0";
            // 
            // FileSizeLbl
            // 
            FileSizeLbl.AutoSize = true;
            FileSizeLbl.Font = new Font("Century Gothic", 11F);
            FileSizeLbl.Location = new Point(6, 372);
            FileSizeLbl.Name = "FileSizeLbl";
            FileSizeLbl.Size = new Size(109, 25);
            FileSizeLbl.TabIndex = 15;
            FileSizeLbl.Text = "File Size ::";
            // 
            // FileOutputLbl
            // 
            FileOutputLbl.AutoSize = true;
            FileOutputLbl.Font = new Font("Century Gothic", 11F);
            FileOutputLbl.Location = new Point(91, 351);
            FileOutputLbl.Name = "FileOutputLbl";
            FileOutputLbl.Size = new Size(71, 25);
            FileOutputLbl.TabIndex = 14;
            FileOutputLbl.Text = "Idle...";
            // 
            // FileStatusLbl
            // 
            FileStatusLbl.AutoSize = true;
            FileStatusLbl.Font = new Font("Century Gothic", 11F);
            FileStatusLbl.Location = new Point(7, 351);
            FileStatusLbl.Name = "FileStatusLbl";
            FileStatusLbl.Size = new Size(94, 25);
            FileStatusLbl.TabIndex = 13;
            FileStatusLbl.Text = "Status ::";
            // 
            // DecryptBtn
            // 
            DecryptBtn.BackColor = Color.FromArgb(30, 30, 30);
            DecryptBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            DecryptBtn.FlatStyle = FlatStyle.Flat;
            DecryptBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DecryptBtn.ForeColor = Color.WhiteSmoke;
            DecryptBtn.Image = Properties.Resources.decryption;
            DecryptBtn.ImageAlign = ContentAlignment.MiddleLeft;
            DecryptBtn.Location = new Point(467, 82);
            DecryptBtn.Name = "DecryptBtn";
            DecryptBtn.Size = new Size(454, 44);
            DecryptBtn.TabIndex = 4;
            DecryptBtn.Text = "&Decrypt";
            DecryptBtn.UseVisualStyleBackColor = false;
            DecryptBtn.Click += DecryptBtn_Click;
            // 
            // EncryptBtn
            // 
            EncryptBtn.BackColor = Color.FromArgb(30, 30, 30);
            EncryptBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            EncryptBtn.FlatStyle = FlatStyle.Flat;
            EncryptBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            EncryptBtn.ForeColor = Color.WhiteSmoke;
            EncryptBtn.Image = Properties.Resources.encryption;
            EncryptBtn.ImageAlign = ContentAlignment.MiddleLeft;
            EncryptBtn.Location = new Point(467, 32);
            EncryptBtn.Name = "EncryptBtn";
            EncryptBtn.Size = new Size(454, 44);
            EncryptBtn.TabIndex = 3;
            EncryptBtn.Text = "&Encrypt";
            EncryptBtn.UseVisualStyleBackColor = false;
            EncryptBtn.Click += EncryptBtn_Click;
            // 
            // ExportFileBtn
            // 
            ExportFileBtn.BackColor = Color.FromArgb(30, 30, 30);
            ExportFileBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            ExportFileBtn.FlatStyle = FlatStyle.Flat;
            ExportFileBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ExportFileBtn.ForeColor = Color.WhiteSmoke;
            ExportFileBtn.Image = Properties.Resources.save_file__2_;
            ExportFileBtn.ImageAlign = ContentAlignment.MiddleLeft;
            ExportFileBtn.Location = new Point(6, 82);
            ExportFileBtn.Name = "ExportFileBtn";
            ExportFileBtn.Size = new Size(455, 44);
            ExportFileBtn.TabIndex = 2;
            ExportFileBtn.Text = "&Export File";
            ExportFileBtn.UseVisualStyleBackColor = false;
            ExportFileBtn.Click += ExportFileBtn_Click;
            // 
            // ImportFileBtn
            // 
            ImportFileBtn.BackColor = Color.FromArgb(30, 30, 30);
            ImportFileBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            ImportFileBtn.FlatStyle = FlatStyle.Flat;
            ImportFileBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ImportFileBtn.ForeColor = Color.WhiteSmoke;
            ImportFileBtn.Image = Properties.Resources.open_folder;
            ImportFileBtn.ImageAlign = ContentAlignment.MiddleLeft;
            ImportFileBtn.Location = new Point(7, 32);
            ImportFileBtn.Name = "ImportFileBtn";
            ImportFileBtn.Size = new Size(454, 44);
            ImportFileBtn.TabIndex = 1;
            ImportFileBtn.Text = "&Import File";
            ImportFileBtn.UseVisualStyleBackColor = false;
            this.ImportFileBtn.Click += ImportFileBtn_Click;
            // 
            // Encryption
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(FileEncryptDecryptBox);
            Name = "Encryption";
            Size = new Size(936, 432);
            Load += Encryption_Load;
            FileEncryptDecryptBox.ResumeLayout(false);
            FileEncryptDecryptBox.PerformLayout();
            ((ISupportInitialize)pictureBox1).EndInit();
            PasswordBox.ResumeLayout(false);
            PasswordBox.PerformLayout();
            ResumeLayout(false);
        }

        private GroupBox FileEncryptDecryptBox;
        private Button ImportFileBtn;
        private Button EncryptBtn;
        private Button ExportFileBtn;
        private Button DecryptBtn;
        private Label FileOutputLbl;
        private Label FileStatusLbl;
        private Label FileSizeNumLbl;
        private Label FileSizeLbl;
        private GroupBox PasswordBox;
        private CheckBox ViewPasswordsCheckbox;
        private Label confirmPasswordLbl;
        private Label passLbl;
        private CheckBox CustomPasswordCheckBox;
        public Label WelcomeLabel;
        private PictureBox pictureBox1;
        public TextBox ConfirmPassword;
        public TextBox CustomPasswordTextBox;
    }
    #endregion
}
