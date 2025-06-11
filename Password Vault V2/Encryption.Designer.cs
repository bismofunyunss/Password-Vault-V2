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
            SuspendLayout();
            // 
            // FileEncryptDecryptBox
            // 
            FileEncryptDecryptBox.BackColor = Color.FromArgb(30, 30, 30);
            FileEncryptDecryptBox.Controls.Add(pictureBox1);
            FileEncryptDecryptBox.Controls.Add(WelcomeLabel);
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
            FileEncryptDecryptBox.Size = new Size(954, 292);
            FileEncryptDecryptBox.TabIndex = 10;
            FileEncryptDecryptBox.TabStop = false;
            FileEncryptDecryptBox.Text = "File Encryptor / Decryptor";
            FileEncryptDecryptBox.Enter += FileEncryptDecryptBox_Enter;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.sha_256;
            pictureBox1.Location = new Point(786, 132);
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
            WelcomeLabel.Location = new Point(6, 191);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 20;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // FileSizeNumLbl
            // 
            FileSizeNumLbl.AutoSize = true;
            FileSizeNumLbl.Font = new Font("Century Gothic", 11F);
            FileSizeNumLbl.Location = new Point(121, 241);
            FileSizeNumLbl.Name = "FileSizeNumLbl";
            FileSizeNumLbl.Size = new Size(89, 25);
            FileSizeNumLbl.TabIndex = 16;
            FileSizeNumLbl.Text = "0 bytes";
            // 
            // FileSizeLbl
            // 
            FileSizeLbl.AutoSize = true;
            FileSizeLbl.Font = new Font("Century Gothic", 11F);
            FileSizeLbl.Location = new Point(6, 241);
            FileSizeLbl.Name = "FileSizeLbl";
            FileSizeLbl.Size = new Size(109, 25);
            FileSizeLbl.TabIndex = 15;
            FileSizeLbl.Text = "File Size ::";
            // 
            // FileOutputLbl
            // 
            FileOutputLbl.AutoSize = true;
            FileOutputLbl.Font = new Font("Century Gothic", 11F);
            FileOutputLbl.Location = new Point(104, 216);
            FileOutputLbl.Name = "FileOutputLbl";
            FileOutputLbl.Size = new Size(71, 25);
            FileOutputLbl.TabIndex = 14;
            FileOutputLbl.Text = "Idle...";
            // 
            // FileStatusLbl
            // 
            FileStatusLbl.AutoSize = true;
            FileStatusLbl.Font = new Font("Century Gothic", 11F);
            FileStatusLbl.Location = new Point(6, 216);
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
            ImportFileBtn.Click += ImportFileBtn_Click;
            // 
            // Encryption
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(FileEncryptDecryptBox);
            Name = "Encryption";
            Size = new Size(973, 308);
            FileEncryptDecryptBox.ResumeLayout(false);
            FileEncryptDecryptBox.PerformLayout();
            ((ISupportInitialize)pictureBox1).EndInit();
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
        public Label WelcomeLabel;
        private PictureBox pictureBox1;
    }
    #endregion
}
