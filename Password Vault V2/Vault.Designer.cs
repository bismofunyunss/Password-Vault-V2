﻿namespace Password_Vault_V2
{
    partial class Vault
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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Vault));
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            vaultBox = new GroupBox();
            WelcomeLabel = new Label();
            outputLbl = new Label();
            statusLbl = new Label();
            PassVault = new DataGridView();
            SaveVaultBtn = new Button();
            AddRowBtn = new Button();
            DeleteRowBtn = new Button();
            Description = new DataGridViewTextBoxColumn();
            Email = new DataGridViewTextBoxColumn();
            Username = new DataGridViewTextBoxColumn();
            Password = new DataGridViewTextBoxColumn();
            vaultBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PassVault).BeginInit();
            SuspendLayout();
            // 
            // vaultBox
            // 
            vaultBox.BackColor = Color.FromArgb(30, 30, 30);
            vaultBox.Controls.Add(WelcomeLabel);
            vaultBox.Controls.Add(outputLbl);
            vaultBox.Controls.Add(statusLbl);
            vaultBox.Controls.Add(PassVault);
            vaultBox.Controls.Add(SaveVaultBtn);
            vaultBox.Controls.Add(AddRowBtn);
            vaultBox.Controls.Add(DeleteRowBtn);
            vaultBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            vaultBox.ForeColor = Color.WhiteSmoke;
            vaultBox.Location = new Point(17, 3);
            vaultBox.Name = "vaultBox";
            vaultBox.Size = new Size(656, 438);
            vaultBox.TabIndex = 9;
            vaultBox.TabStop = false;
            vaultBox.Text = "Vault";
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(6, 375);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 13;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // outputLbl
            // 
            outputLbl.AutoSize = true;
            outputLbl.Font = new Font("Century Gothic", 11F);
            outputLbl.Location = new Point(90, 400);
            outputLbl.Name = "outputLbl";
            outputLbl.Size = new Size(71, 25);
            outputLbl.TabIndex = 12;
            outputLbl.Text = "Idle...";
            // 
            // statusLbl
            // 
            statusLbl.AutoSize = true;
            statusLbl.Font = new Font("Century Gothic", 11F);
            statusLbl.Location = new Point(6, 400);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(94, 25);
            statusLbl.TabIndex = 11;
            statusLbl.Text = "Status ::";
            // 
            // PassVault
            // 
            PassVault.AllowUserToAddRows = false;
            PassVault.AllowUserToResizeColumns = false;
            PassVault.AllowUserToResizeRows = false;
            PassVault.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            PassVault.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            PassVault.BackgroundColor = Color.FromArgb(30, 30, 30);
            PassVault.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle1.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle1.ForeColor = Color.WhiteSmoke;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle1.SelectionForeColor = Color.WhiteSmoke;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            PassVault.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            PassVault.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            PassVault.Columns.AddRange(new DataGridViewColumn[] { Description, Email, Username, Password });
            PassVault.EnableHeadersVisualStyles = false;
            PassVault.GridColor = Color.WhiteSmoke;
            PassVault.Location = new Point(6, 29);
            PassVault.MultiSelect = false;
            PassVault.Name = "PassVault";
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle6.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            dataGridViewCellStyle6.ForeColor = Color.WhiteSmoke;
            dataGridViewCellStyle6.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.True;
            PassVault.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
            PassVault.RowHeadersWidth = 62;
            PassVault.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            PassVault.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            PassVault.ShowCellErrors = false;
            PassVault.ShowCellToolTips = false;
            PassVault.ShowEditingIcon = false;
            PassVault.ShowRowErrors = false;
            PassVault.Size = new Size(642, 163);
            PassVault.TabIndex = 0;
            // 
            // SaveVaultBtn
            // 
            SaveVaultBtn.BackColor = Color.FromArgb(30, 30, 30);
            SaveVaultBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            SaveVaultBtn.FlatStyle = FlatStyle.Flat;
            SaveVaultBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            SaveVaultBtn.ForeColor = Color.WhiteSmoke;
            SaveVaultBtn.Image = (Image)resources.GetObject("SaveVaultBtn.Image");
            SaveVaultBtn.ImageAlign = ContentAlignment.MiddleLeft;
            SaveVaultBtn.Location = new Point(6, 298);
            SaveVaultBtn.Name = "SaveVaultBtn";
            SaveVaultBtn.Size = new Size(642, 44);
            SaveVaultBtn.TabIndex = 7;
            SaveVaultBtn.Text = "&Save Vault";
            SaveVaultBtn.UseVisualStyleBackColor = false;
            SaveVaultBtn.Click += SaveVaultBtn_Click;
            // 
            // AddRowBtn
            // 
            AddRowBtn.BackColor = Color.FromArgb(30, 30, 30);
            AddRowBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            AddRowBtn.FlatStyle = FlatStyle.Flat;
            AddRowBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            AddRowBtn.ForeColor = Color.WhiteSmoke;
            AddRowBtn.Image = (Image)resources.GetObject("AddRowBtn.Image");
            AddRowBtn.ImageAlign = ContentAlignment.MiddleLeft;
            AddRowBtn.Location = new Point(6, 198);
            AddRowBtn.Name = "AddRowBtn";
            AddRowBtn.Size = new Size(642, 44);
            AddRowBtn.TabIndex = 5;
            AddRowBtn.Text = "&Add New Row";
            AddRowBtn.UseVisualStyleBackColor = false;
            AddRowBtn.Click += AddRowBtn_Click;
            // 
            // DeleteRowBtn
            // 
            DeleteRowBtn.BackColor = Color.FromArgb(30, 30, 30);
            DeleteRowBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            DeleteRowBtn.FlatStyle = FlatStyle.Flat;
            DeleteRowBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DeleteRowBtn.ForeColor = Color.WhiteSmoke;
            DeleteRowBtn.Image = (Image)resources.GetObject("DeleteRowBtn.Image");
            DeleteRowBtn.ImageAlign = ContentAlignment.MiddleLeft;
            DeleteRowBtn.Location = new Point(6, 248);
            DeleteRowBtn.Name = "DeleteRowBtn";
            DeleteRowBtn.Size = new Size(642, 44);
            DeleteRowBtn.TabIndex = 6;
            DeleteRowBtn.Text = "&Delete Row";
            DeleteRowBtn.UseVisualStyleBackColor = false;
            DeleteRowBtn.Click += DeleteRowBtn_Click;
            // 
            // Description
            // 
            dataGridViewCellStyle2.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.Font = new Font("Times New Roman", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = Color.White;
            dataGridViewCellStyle2.SelectionBackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle2.SelectionForeColor = Color.White;
            Description.DefaultCellStyle = dataGridViewCellStyle2;
            Description.HeaderText = "Description";
            Description.MinimumWidth = 8;
            Description.Name = "Description";
            // 
            // Email
            // 
            dataGridViewCellStyle3.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle3.Font = new Font("Times New Roman", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = Color.Gold;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle3.SelectionForeColor = Color.Gold;
            Email.DefaultCellStyle = dataGridViewCellStyle3;
            Email.HeaderText = "Email";
            Email.MinimumWidth = 8;
            Email.Name = "Email";
            // 
            // Username
            // 
            dataGridViewCellStyle4.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle4.Font = new Font("Times New Roman", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle4.ForeColor = Color.Gold;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle4.SelectionForeColor = Color.Gold;
            Username.DefaultCellStyle = dataGridViewCellStyle4;
            Username.HeaderText = "Username";
            Username.MinimumWidth = 8;
            Username.Name = "Username";
            // 
            // Password
            // 
            dataGridViewCellStyle5.BackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle5.Font = new Font("Times New Roman", 11F, FontStyle.Bold, GraphicsUnit.Point, 0);
            dataGridViewCellStyle5.ForeColor = Color.Gold;
            dataGridViewCellStyle5.SelectionBackColor = SystemColors.ControlDarkDark;
            dataGridViewCellStyle5.SelectionForeColor = Color.Gold;
            Password.DefaultCellStyle = dataGridViewCellStyle5;
            Password.HeaderText = "Password";
            Password.MinimumWidth = 8;
            Password.Name = "Password";
            // 
            // Vault
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(vaultBox);
            Name = "Vault";
            Size = new Size(691, 487);
            vaultBox.ResumeLayout(false);
            vaultBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PassVault).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Label outputLbl;
        private Label statusLbl;
        private Button SaveVaultBtn;
        private Button AddRowBtn;
        private Button DeleteRowBtn;
        public GroupBox vaultBox;
        public DataGridView PassVault;
        public Label WelcomeLabel;
        private DataGridViewTextBoxColumn Description;
        private DataGridViewTextBoxColumn Email;
        private DataGridViewTextBoxColumn Username;
        private DataGridViewTextBoxColumn Password;
    }
}