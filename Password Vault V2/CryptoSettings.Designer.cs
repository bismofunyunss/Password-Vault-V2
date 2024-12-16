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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CryptoSettings));
            statusLbl = new Label();
            outputLbl = new Label();
            WelcomeLabel = new Label();
            CryptoBox = new GroupBox();
            MemoryAmountLbl = new Label();
            MemorySizeNumberBox = new NumericUpDown();
            ParallelismLbl = new Label();
            ParallelismNumberBox = new NumericUpDown();
            IterationsLbl = new Label();
            IterationsNumberBox = new NumericUpDown();
            SaveBtn = new Button();
            CryptoBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)MemorySizeNumberBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ParallelismNumberBox).BeginInit();
            ((System.ComponentModel.ISupportInitialize)IterationsNumberBox).BeginInit();
            SuspendLayout();
            // 
            // statusLbl
            // 
            statusLbl.AutoSize = true;
            statusLbl.Font = new Font("Century Gothic", 11F);
            statusLbl.Location = new Point(6, 397);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(94, 25);
            statusLbl.TabIndex = 8;
            statusLbl.Text = "Status ::";
            // 
            // outputLbl
            // 
            outputLbl.AutoSize = true;
            outputLbl.Font = new Font("Century Gothic", 11F);
            outputLbl.Location = new Point(104, 397);
            outputLbl.Name = "outputLbl";
            outputLbl.Size = new Size(71, 25);
            outputLbl.TabIndex = 9;
            outputLbl.Text = "Idle...";
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(6, 372);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 12;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // CryptoBox
            // 
            CryptoBox.BackColor = Color.FromArgb(30, 30, 30);
            CryptoBox.Controls.Add(MemoryAmountLbl);
            CryptoBox.Controls.Add(MemorySizeNumberBox);
            CryptoBox.Controls.Add(ParallelismLbl);
            CryptoBox.Controls.Add(ParallelismNumberBox);
            CryptoBox.Controls.Add(IterationsLbl);
            CryptoBox.Controls.Add(IterationsNumberBox);
            CryptoBox.Controls.Add(SaveBtn);
            CryptoBox.Controls.Add(WelcomeLabel);
            CryptoBox.Controls.Add(outputLbl);
            CryptoBox.Controls.Add(statusLbl);
            CryptoBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            CryptoBox.ForeColor = Color.WhiteSmoke;
            CryptoBox.Location = new Point(13, 3);
            CryptoBox.Name = "CryptoBox";
            CryptoBox.Size = new Size(446, 436);
            CryptoBox.TabIndex = 1;
            CryptoBox.TabStop = false;
            CryptoBox.Text = "Cryptography Settings";
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
            // SaveBtn
            // 
            SaveBtn.BackColor = Color.FromArgb(30, 30, 30);
            SaveBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            SaveBtn.FlatStyle = FlatStyle.Flat;
            SaveBtn.Font = new Font("Century Gothic", 11F);
            SaveBtn.ForeColor = Color.WhiteSmoke;
            SaveBtn.Image = (Image)resources.GetObject("SaveBtn.Image");
            SaveBtn.ImageAlign = ContentAlignment.MiddleLeft;
            SaveBtn.Location = new Point(6, 238);
            SaveBtn.Name = "SaveBtn";
            SaveBtn.Size = new Size(434, 44);
            SaveBtn.TabIndex = 13;
            SaveBtn.Text = "&Save Settings";
            SaveBtn.UseVisualStyleBackColor = false;
            SaveBtn.Click += SaveBtn_Click;
            // 
            // CryptoSettings
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(CryptoBox);
            Name = "CryptoSettings";
            Size = new Size(470, 449);
            Load += CryptoSettings_Load;
            CryptoBox.ResumeLayout(false);
            CryptoBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)MemorySizeNumberBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)ParallelismNumberBox).EndInit();
            ((System.ComponentModel.ISupportInitialize)IterationsNumberBox).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label statusLbl;
        private Label outputLbl;
        public Label WelcomeLabel;
        private GroupBox CryptoBox;
        public Button SaveBtn;
        private Label IterationsLbl;
        private Label ParallelismLbl;
        private Label MemoryAmountLbl;
        private NumericUpDown IterationsNumberBox;
        private NumericUpDown ParallelismNumberBox;
        private NumericUpDown MemorySizeNumberBox;
    }
}
