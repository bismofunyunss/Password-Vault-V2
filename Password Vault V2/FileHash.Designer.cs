namespace Password_Vault_V2
{
    partial class FileHash
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
            hashbox = new GroupBox();
            pictureBox1 = new PictureBox();
            WelcomeLabel = new Label();
            filenamelbl = new Label();
            CalculateHashBtn = new Button();
            hashoutputlbl = new Label();
            hashoutputtxt = new TextBox();
            HashImportFile = new Button();
            hashbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // hashbox
            // 
            hashbox.BackColor = Color.FromArgb(30, 30, 30);
            hashbox.Controls.Add(pictureBox1);
            hashbox.Controls.Add(WelcomeLabel);
            hashbox.Controls.Add(filenamelbl);
            hashbox.Controls.Add(CalculateHashBtn);
            hashbox.Controls.Add(hashoutputlbl);
            hashbox.Controls.Add(hashoutputtxt);
            hashbox.Controls.Add(HashImportFile);
            hashbox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            hashbox.ForeColor = Color.WhiteSmoke;
            hashbox.Location = new Point(39, 73);
            hashbox.Name = "hashbox";
            hashbox.Size = new Size(930, 358);
            hashbox.TabIndex = 12;
            hashbox.TabStop = false;
            hashbox.Text = "File Hash Calculator";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.data_encryption__1_;
            pictureBox1.Location = new Point(790, 215);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(134, 128);
            pictureBox1.TabIndex = 22;
            pictureBox1.TabStop = false;
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(6, 296);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 21;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // filenamelbl
            // 
            filenamelbl.AutoSize = true;
            filenamelbl.Font = new Font("Century Gothic", 11F);
            filenamelbl.ForeColor = Color.WhiteSmoke;
            filenamelbl.Location = new Point(7, 321);
            filenamelbl.Name = "filenamelbl";
            filenamelbl.Size = new Size(175, 25);
            filenamelbl.TabIndex = 20;
            filenamelbl.Text = "File Name: N/A";
            // 
            // CalculateHashBtn
            // 
            CalculateHashBtn.BackColor = Color.FromArgb(30, 30, 30);
            CalculateHashBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            CalculateHashBtn.FlatStyle = FlatStyle.Flat;
            CalculateHashBtn.Font = new Font("Century Gothic", 11F);
            CalculateHashBtn.ForeColor = Color.WhiteSmoke;
            CalculateHashBtn.Image = Properties.Resources.cryptography__1_;
            CalculateHashBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CalculateHashBtn.Location = new Point(7, 116);
            CalculateHashBtn.Name = "CalculateHashBtn";
            CalculateHashBtn.Size = new Size(917, 44);
            CalculateHashBtn.TabIndex = 19;
            CalculateHashBtn.Text = "&Calculate Hash";
            CalculateHashBtn.UseVisualStyleBackColor = false;
            CalculateHashBtn.Click += CalculateHashBtn_Click;
            // 
            // hashoutputlbl
            // 
            hashoutputlbl.AutoSize = true;
            hashoutputlbl.Location = new Point(6, 40);
            hashoutputlbl.Name = "hashoutputlbl";
            hashoutputlbl.Size = new Size(146, 25);
            hashoutputlbl.TabIndex = 18;
            hashoutputlbl.Text = "Hash Output";
            // 
            // hashoutputtxt
            // 
            hashoutputtxt.BackColor = Color.FromArgb(30, 30, 30);
            hashoutputtxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            hashoutputtxt.ForeColor = Color.White;
            hashoutputtxt.Location = new Point(10, 77);
            hashoutputtxt.Name = "hashoutputtxt";
            hashoutputtxt.ReadOnly = true;
            hashoutputtxt.Size = new Size(914, 34);
            hashoutputtxt.TabIndex = 6;
            // 
            // HashImportFile
            // 
            HashImportFile.BackColor = Color.FromArgb(30, 30, 30);
            HashImportFile.FlatAppearance.BorderColor = Color.WhiteSmoke;
            HashImportFile.FlatStyle = FlatStyle.Flat;
            HashImportFile.Font = new Font("Century Gothic", 11F);
            HashImportFile.ForeColor = Color.WhiteSmoke;
            HashImportFile.Image = Properties.Resources.open_folder;
            HashImportFile.ImageAlign = ContentAlignment.MiddleLeft;
            HashImportFile.Location = new Point(6, 165);
            HashImportFile.Name = "HashImportFile";
            HashImportFile.Size = new Size(918, 44);
            HashImportFile.TabIndex = 5;
            HashImportFile.Text = "&Import File";
            HashImportFile.UseVisualStyleBackColor = false;
            HashImportFile.Click += HashImportFile_Click;
            // 
            // FileHash
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(hashbox);
            Name = "FileHash";
            Size = new Size(1008, 554);
            hashbox.ResumeLayout(false);
            hashbox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox hashbox;
        private Label filenamelbl;
        private Button CalculateHashBtn;
        private Label hashoutputlbl;
        private TextBox hashoutputtxt;
        private Button HashImportFile;
        public Label WelcomeLabel;
        private PictureBox pictureBox1;
    }
}
