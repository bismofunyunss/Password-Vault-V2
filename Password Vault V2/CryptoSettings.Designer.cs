namespace Password_Vault_V2
{
    partial class CryptoSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            WelcomeLabel = new Label();
            CryptoBox = new GroupBox();
            FipsModeCheckbox = new CheckBox();
            MemoryAmountLbl = new Label();
            MemorySizeNumberBox = new NumericUpDown();
            ParallelismLbl = new Label();
            ParallelismNumberBox = new NumericUpDown();
            IterationsLbl = new Label();
            IterationsNumberBox = new NumericUpDown();
            PasswordControls = new GroupBox();
            UpdatePassBtn = new Button();
            confirmPassTxt = new TextBox();
            confirmPassLbl = new Label();
            NewPassTxt = new TextBox();
            passLbl = new Label();
            OldPasswordTxt = new TextBox();
            userLbl = new Label();
            CryptoBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MemorySizeNumberBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ParallelismNumberBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)IterationsNumberBox).BeginInit();
            PasswordControls.SuspendLayout();
            SuspendLayout();
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(7, 307);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 12;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // CryptoBox
            // 
            CryptoBox.BackColor = Color.FromArgb(30, 30, 30);
            CryptoBox.Controls.Add(FipsModeCheckbox);
            CryptoBox.Controls.Add(MemoryAmountLbl);
            CryptoBox.Controls.Add(MemorySizeNumberBox);
            CryptoBox.Controls.Add(ParallelismLbl);
            CryptoBox.Controls.Add(ParallelismNumberBox);
            CryptoBox.Controls.Add(IterationsLbl);
            CryptoBox.Controls.Add(IterationsNumberBox);
            CryptoBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            CryptoBox.ForeColor = Color.WhiteSmoke;
            CryptoBox.Location = new Point(15, 28);
            CryptoBox.Name = "CryptoBox";
            CryptoBox.Size = new Size(461, 344);
            CryptoBox.TabIndex = 1;
            CryptoBox.TabStop = false;
            CryptoBox.Text = "Cryptography Settings";
            // 
            // FipsModeCheckbox
            // 
            FipsModeCheckbox.AutoSize = true;
            FipsModeCheckbox.Location = new Point(289, 297);
            FipsModeCheckbox.Name = "FipsModeCheckbox";
            FipsModeCheckbox.Size = new Size(151, 29);
            FipsModeCheckbox.TabIndex = 20;
            FipsModeCheckbox.Text = "FIPS Mode";
            FipsModeCheckbox.UseVisualStyleBackColor = true;
            FipsModeCheckbox.CheckedChanged += FipsModeCheckbox_CheckedChanged;
            FipsModeCheckbox.MouseHover += FipsModeCheckbox_MouseHover;
            // 
            // MemoryAmountLbl
            // 
            MemoryAmountLbl.AutoSize = true;
            MemoryAmountLbl.Font = new Font("Century Gothic", 11F);
            MemoryAmountLbl.Location = new Point(6, 170);
            MemoryAmountLbl.Name = "MemoryAmountLbl";
            MemoryAmountLbl.Size = new Size(203, 25);
            MemoryAmountLbl.TabIndex = 19;
            MemoryAmountLbl.Text = "Memory Size / GB";
            // 
            // MemorySizeNumberBox
            // 
            MemorySizeNumberBox.BackColor = Color.FromArgb(30, 30, 30);
            MemorySizeNumberBox.DecimalPlaces = 1;
            MemorySizeNumberBox.ForeColor = SystemColors.InactiveBorder;
            MemorySizeNumberBox.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            MemorySizeNumberBox.Location = new Point(6, 198);
            MemorySizeNumberBox.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
            MemorySizeNumberBox.Minimum = new decimal(new int[] { 5, 0, 0, 65536 });
            MemorySizeNumberBox.Name = "MemorySizeNumberBox";
            MemorySizeNumberBox.Size = new Size(434, 34);
            MemorySizeNumberBox.TabIndex = 18;
            MemorySizeNumberBox.ThousandsSeparator = true;
            MemorySizeNumberBox.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            MemorySizeNumberBox.ValueChanged += MemorySizeNumberBox_ValueChanged;
            // 
            // ParallelismLbl
            // 
            ParallelismLbl.AutoSize = true;
            ParallelismLbl.Font = new Font("Century Gothic", 11F);
            ParallelismLbl.Location = new Point(6, 105);
            ParallelismLbl.Name = "ParallelismLbl";
            ParallelismLbl.Size = new Size(129, 25);
            ParallelismLbl.TabIndex = 17;
            ParallelismLbl.Text = "Parallelism";
            // 
            // ParallelismNumberBox
            // 
            ParallelismNumberBox.BackColor = Color.FromArgb(30, 30, 30);
            ParallelismNumberBox.ForeColor = SystemColors.InactiveBorder;
            ParallelismNumberBox.Location = new Point(6, 133);
            ParallelismNumberBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            ParallelismNumberBox.Name = "ParallelismNumberBox";
            ParallelismNumberBox.Size = new Size(434, 34);
            ParallelismNumberBox.TabIndex = 16;
            ParallelismNumberBox.Value = new decimal(new int[] { 1, 0, 0, 0 });
            ParallelismNumberBox.ValueChanged += ParallelismNumberBox_ValueChanged;
            // 
            // IterationsLbl
            // 
            IterationsLbl.AutoSize = true;
            IterationsLbl.Font = new Font("Century Gothic", 11F);
            IterationsLbl.Location = new Point(6, 40);
            IterationsLbl.Name = "IterationsLbl";
            IterationsLbl.Size = new Size(110, 25);
            IterationsLbl.TabIndex = 15;
            IterationsLbl.Text = "Iterations";
            // 
            // IterationsNumberBox
            // 
            IterationsNumberBox.BackColor = Color.FromArgb(30, 30, 30);
            IterationsNumberBox.ForeColor = SystemColors.InactiveBorder;
            IterationsNumberBox.Location = new Point(6, 68);
            IterationsNumberBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            IterationsNumberBox.Name = "IterationsNumberBox";
            IterationsNumberBox.Size = new Size(434, 34);
            IterationsNumberBox.TabIndex = 14;
            IterationsNumberBox.Value = new decimal(new int[] { 1, 0, 0, 0 });
            IterationsNumberBox.ValueChanged += IterationsNumberBox_ValueChanged;
            // 
            // PasswordControls
            // 
            PasswordControls.BackColor = Color.FromArgb(30, 30, 30);
            PasswordControls.Controls.Add(UpdatePassBtn);
            PasswordControls.Controls.Add(confirmPassTxt);
            PasswordControls.Controls.Add(confirmPassLbl);
            PasswordControls.Controls.Add(NewPassTxt);
            PasswordControls.Controls.Add(passLbl);
            PasswordControls.Controls.Add(OldPasswordTxt);
            PasswordControls.Controls.Add(userLbl);
            PasswordControls.Controls.Add(WelcomeLabel);
            PasswordControls.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            PasswordControls.ForeColor = Color.WhiteSmoke;
            PasswordControls.Location = new Point(15, 393);
            PasswordControls.Name = "PasswordControls";
            PasswordControls.Size = new Size(461, 338);
            PasswordControls.TabIndex = 2;
            PasswordControls.TabStop = false;
            PasswordControls.Text = "Password Settings";
            // 
            // UpdatePassBtn
            // 
            UpdatePassBtn.BackColor = Color.FromArgb(30, 30, 30);
            UpdatePassBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            UpdatePassBtn.FlatStyle = FlatStyle.Flat;
            UpdatePassBtn.Font = new Font("Century Gothic", 11F);
            UpdatePassBtn.ForeColor = Color.WhiteSmoke;
            UpdatePassBtn.ImageAlign = ContentAlignment.MiddleLeft;
            UpdatePassBtn.Location = new Point(4, 225);
            UpdatePassBtn.Name = "UpdatePassBtn";
            UpdatePassBtn.Size = new Size(438, 44);
            UpdatePassBtn.TabIndex = 23;
            UpdatePassBtn.Text = "&Update Password";
            UpdatePassBtn.UseVisualStyleBackColor = false;
            UpdatePassBtn.Click += UpdatePassBtn_Click;
            // 
            // confirmPassTxt
            // 
            confirmPassTxt.BackColor = Color.FromArgb(30, 30, 30);
            confirmPassTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            confirmPassTxt.ForeColor = Color.White;
            confirmPassTxt.Location = new Point(4, 185);
            confirmPassTxt.Name = "confirmPassTxt";
            confirmPassTxt.Size = new Size(438, 34);
            confirmPassTxt.TabIndex = 21;
            confirmPassTxt.UseSystemPasswordChar = true;
            // 
            // confirmPassLbl
            // 
            confirmPassLbl.AutoSize = true;
            confirmPassLbl.Font = new Font("Century Gothic", 11F);
            confirmPassLbl.Location = new Point(6, 157);
            confirmPassLbl.Name = "confirmPassLbl";
            confirmPassLbl.Size = new Size(205, 25);
            confirmPassLbl.TabIndex = 22;
            confirmPassLbl.Text = "Confirm Password";
            // 
            // NewPassTxt
            // 
            NewPassTxt.BackColor = Color.FromArgb(30, 30, 30);
            NewPassTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            NewPassTxt.ForeColor = Color.White;
            NewPassTxt.Location = new Point(2, 120);
            NewPassTxt.Name = "NewPassTxt";
            NewPassTxt.Size = new Size(438, 34);
            NewPassTxt.TabIndex = 19;
            // 
            // passLbl
            // 
            passLbl.AutoSize = true;
            passLbl.Font = new Font("Century Gothic", 11F);
            passLbl.Location = new Point(6, 92);
            passLbl.Name = "passLbl";
            passLbl.Size = new Size(170, 25);
            passLbl.TabIndex = 20;
            passLbl.Text = "New Password";
            // 
            // OldPasswordTxt
            // 
            OldPasswordTxt.BackColor = Color.FromArgb(30, 30, 30);
            OldPasswordTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            OldPasswordTxt.ForeColor = Color.White;
            OldPasswordTxt.Location = new Point(4, 55);
            OldPasswordTxt.Name = "OldPasswordTxt";
            OldPasswordTxt.Size = new Size(438, 34);
            OldPasswordTxt.TabIndex = 18;
            // 
            // userLbl
            // 
            userLbl.AutoSize = true;
            userLbl.Font = new Font("Century Gothic", 11F);
            userLbl.Location = new Point(6, 27);
            userLbl.Name = "userLbl";
            userLbl.Size = new Size(160, 25);
            userLbl.TabIndex = 17;
            userLbl.Text = "Old Password";
            // 
            // CryptoSettings
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(PasswordControls);
            Controls.Add(CryptoBox);
            Name = "CryptoSettings";
            Size = new Size(487, 749);
            Load += CryptoSettings_Load;
            Paint += CryptoSettings_Paint;
            CryptoBox.ResumeLayout(false);
            CryptoBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MemorySizeNumberBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)ParallelismNumberBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)IterationsNumberBox).EndInit();
            PasswordControls.ResumeLayout(false);
            PasswordControls.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        public Label WelcomeLabel;
        private GroupBox CryptoBox;
        private Label IterationsLbl;
        private Label ParallelismLbl;
        private Label MemoryAmountLbl;
        private NumericUpDown IterationsNumberBox;
        private NumericUpDown ParallelismNumberBox;
        private NumericUpDown MemorySizeNumberBox;
        internal CheckBox FipsModeCheckbox;
        private GroupBox PasswordControls;
        public Button UpdatePassBtn;
        private TextBox confirmPassTxt;
        private Label confirmPassLbl;
        private TextBox NewPassTxt;
        private Label passLbl;
        public TextBox OldPasswordTxt;
        private Label userLbl;
    }
}
