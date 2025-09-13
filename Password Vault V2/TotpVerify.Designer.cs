namespace Password_Vault_V2
{
    internal partial class TotpVerify
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TotpVerify));
            QRCodeImg = new PictureBox();
            VerificationBox = new GroupBox();
            confirmBtn = new Button();
            codetxt = new TextBox();
            verificationCodeLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)QRCodeImg).BeginInit();
            VerificationBox.SuspendLayout();
            SuspendLayout();
            // 
            // QRCodeImg
            // 
            QRCodeImg.Location = new Point(12, 25);
            QRCodeImg.Name = "QRCodeImg";
            QRCodeImg.Size = new Size(301, 249);
            QRCodeImg.TabIndex = 0;
            QRCodeImg.TabStop = false;
            // 
            // VerificationBox
            // 
            VerificationBox.BackColor = Color.FromArgb(30, 30, 30);
            VerificationBox.Controls.Add(confirmBtn);
            VerificationBox.Controls.Add(codetxt);
            VerificationBox.Controls.Add(verificationCodeLabel);
            VerificationBox.ForeColor = Color.White;
            VerificationBox.Location = new Point(12, 289);
            VerificationBox.Name = "VerificationBox";
            VerificationBox.Size = new Size(301, 189);
            VerificationBox.TabIndex = 1;
            VerificationBox.TabStop = false;
            VerificationBox.Text = "Verification";
            // 
            // confirmBtn
            // 
            confirmBtn.BackColor = Color.FromArgb(30, 30, 30);
            confirmBtn.FlatStyle = FlatStyle.Flat;
            confirmBtn.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            confirmBtn.ForeColor = Color.White;
            confirmBtn.ImageAlign = ContentAlignment.MiddleLeft;
            confirmBtn.Location = new Point(6, 104);
            confirmBtn.Name = "confirmBtn";
            confirmBtn.Size = new Size(289, 42);
            confirmBtn.TabIndex = 4;
            confirmBtn.Text = "Enter";
            confirmBtn.UseVisualStyleBackColor = false;
            confirmBtn.Click += confirmBtn_Click;
            // 
            // codetxt
            // 
            codetxt.BackColor = Color.FromArgb(30, 30, 30);
            codetxt.ForeColor = Color.White;
            codetxt.Location = new Point(6, 67);
            codetxt.Name = "codetxt";
            codetxt.Size = new Size(289, 31);
            codetxt.TabIndex = 1;
            // 
            // verificationCodeLabel
            // 
            verificationCodeLabel.AutoSize = true;
            verificationCodeLabel.Location = new Point(6, 39);
            verificationCodeLabel.Name = "verificationCodeLabel";
            verificationCodeLabel.Size = new Size(54, 25);
            verificationCodeLabel.TabIndex = 0;
            verificationCodeLabel.Text = "Code";
            // 
            // TotpVerify
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(325, 501);
            Controls.Add(VerificationBox);
            Controls.Add(QRCodeImg);
            DoubleBuffered = true;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "TotpVerify";
            Text = "Authenticator";
            ((System.ComponentModel.ISupportInitialize)QRCodeImg).EndInit();
            VerificationBox.ResumeLayout(false);
            VerificationBox.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox QRCodeImg;
        private GroupBox VerificationBox;
        private Label verificationCodeLabel;
        private TextBox codetxt;
        private Button confirmBtn;
    }
}
