namespace Password_Vault_V2
{
    partial class ConfirmEmail
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfirmEmail));
            confirmationBox = new GroupBox();
            cancelBtn = new Button();
            timerLbl = new Label();
            confirmCodeBtn = new Button();
            codeTxt = new TextBox();
            confirmCode = new Label();
            confirmationBox.SuspendLayout();
            SuspendLayout();
            // 
            // confirmationBox
            // 
            confirmationBox.Controls.Add(cancelBtn);
            confirmationBox.Controls.Add(timerLbl);
            confirmationBox.Controls.Add(confirmCodeBtn);
            confirmationBox.Controls.Add(codeTxt);
            confirmationBox.Controls.Add(confirmCode);
            confirmationBox.ForeColor = Color.WhiteSmoke;
            confirmationBox.Location = new Point(21, 12);
            confirmationBox.Name = "confirmationBox";
            confirmationBox.Size = new Size(453, 282);
            confirmationBox.TabIndex = 0;
            confirmationBox.TabStop = false;
            confirmationBox.Text = "Confirm Code";
            // 
            // cancelBtn
            // 
            cancelBtn.BackColor = Color.FromArgb(30, 30, 30);
            cancelBtn.FlatAppearance.BorderColor = Color.Cyan;
            cancelBtn.FlatStyle = FlatStyle.Flat;
            cancelBtn.Font = new Font("Century Gothic", 11F);
            cancelBtn.ForeColor = Color.Cyan;
            cancelBtn.Image = (Image)resources.GetObject("cancelBtn.Image");
            cancelBtn.ImageAlign = ContentAlignment.MiddleRight;
            cancelBtn.Location = new Point(4, 171);
            cancelBtn.Name = "cancelBtn";
            cancelBtn.Size = new Size(438, 42);
            cancelBtn.TabIndex = 19;
            cancelBtn.Text = "&Cancel";
            cancelBtn.UseVisualStyleBackColor = false;
            cancelBtn.Click += cancelBtn_Click;
            // 
            // timerLbl
            // 
            timerLbl.AutoSize = true;
            timerLbl.Location = new Point(386, 243);
            timerLbl.Name = "timerLbl";
            timerLbl.Size = new Size(56, 25);
            timerLbl.TabIndex = 18;
            timerLbl.Text = "10:00";
            timerLbl.Visible = false;
            // 
            // confirmCodeBtn
            // 
            confirmCodeBtn.BackColor = Color.FromArgb(30, 30, 30);
            confirmCodeBtn.FlatAppearance.BorderColor = Color.Cyan;
            confirmCodeBtn.FlatStyle = FlatStyle.Flat;
            confirmCodeBtn.Font = new Font("Century Gothic", 11F);
            confirmCodeBtn.ForeColor = Color.Cyan;
            confirmCodeBtn.Image = (Image)resources.GetObject("confirmCodeBtn.Image");
            confirmCodeBtn.ImageAlign = ContentAlignment.MiddleRight;
            confirmCodeBtn.Location = new Point(4, 123);
            confirmCodeBtn.Name = "confirmCodeBtn";
            confirmCodeBtn.Size = new Size(438, 42);
            confirmCodeBtn.TabIndex = 17;
            confirmCodeBtn.Text = "&Confirm Code";
            confirmCodeBtn.UseVisualStyleBackColor = false;
            confirmCodeBtn.Click += confirmCodeBtn_Click;
            // 
            // codeTxt
            // 
            codeTxt.BackColor = Color.FromArgb(30, 30, 30);
            codeTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            codeTxt.ForeColor = Color.White;
            codeTxt.Location = new Point(4, 67);
            codeTxt.Name = "codeTxt";
            codeTxt.Size = new Size(438, 34);
            codeTxt.TabIndex = 15;
            codeTxt.UseSystemPasswordChar = true;
            // 
            // confirmCode
            // 
            confirmCode.AutoSize = true;
            confirmCode.Font = new Font("Century Gothic", 11F);
            confirmCode.Location = new Point(176, 27);
            confirmCode.Name = "confirmCode";
            confirmCode.Size = new Size(73, 25);
            confirmCode.TabIndex = 16;
            confirmCode.Text = "Code";
            confirmCode.TextAlign = ContentAlignment.BottomRight;
            // 
            // ConfirmEmail
            // 
            AcceptButton = confirmCodeBtn;
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(497, 313);
            Controls.Add(confirmationBox);
            DoubleBuffered = true;
            ForeColor = Color.SkyBlue;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConfirmEmail";
            RightToLeft = RightToLeft.No;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Confirm Login";
            confirmationBox.ResumeLayout(false);
            confirmationBox.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox confirmationBox;
        private TextBox codeTxt;
        private Label confirmCode;
        public Button confirmCodeBtn;
        private Label timerLbl;
        public Button cancelBtn;
    }
}
