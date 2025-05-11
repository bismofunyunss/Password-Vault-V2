namespace Password_Vault_V2
{
    partial class PasswordReauthenticate
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            label2 = new Label();
            PasswordTxt = new TextBox();
            ConfirmPassTxt = new TextBox();
            BtnConfirm = new Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.White;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(114, 25);
            label1.TabIndex = 0;
            label1.Text = "Password";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.White;
            label2.Location = new Point(12, 80);
            label2.Name = "label2";
            label2.Size = new Size(210, 25);
            label2.TabIndex = 1;
            label2.Text = "Re-enter Password";
            // 
            // PasswordTxt
            // 
            PasswordTxt.BackColor = Color.FromArgb(30, 30, 30);
            PasswordTxt.Location = new Point(12, 37);
            PasswordTxt.Name = "PasswordTxt";
            PasswordTxt.Size = new Size(400, 31);
            PasswordTxt.TabIndex = 2;
            PasswordTxt.UseSystemPasswordChar = true;
            // 
            // ConfirmPassTxt
            // 
            ConfirmPassTxt.BackColor = Color.FromArgb(30, 30, 30);
            ConfirmPassTxt.Location = new Point(12, 108);
            ConfirmPassTxt.Name = "ConfirmPassTxt";
            ConfirmPassTxt.Size = new Size(400, 31);
            ConfirmPassTxt.TabIndex = 3;
            ConfirmPassTxt.UseSystemPasswordChar = true;
            // 
            // BtnConfirm
            // 
            BtnConfirm.BackColor = Color.FromArgb(30, 30, 30);
            BtnConfirm.FlatStyle = FlatStyle.Flat;
            BtnConfirm.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            BtnConfirm.ForeColor = Color.White;
            BtnConfirm.Image = Properties.Resources.enter;
            BtnConfirm.ImageAlign = ContentAlignment.MiddleLeft;
            BtnConfirm.Location = new Point(12, 145);
            BtnConfirm.Name = "BtnConfirm";
            BtnConfirm.Size = new Size(400, 42);
            BtnConfirm.TabIndex = 4;
            BtnConfirm.Text = "&Confirm";
            BtnConfirm.UseVisualStyleBackColor = false;
            BtnConfirm.Click += BtnConfirm_Click;
            // 
            // PasswordReauthenticate
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(424, 210);
            Controls.Add(BtnConfirm);
            Controls.Add(ConfirmPassTxt);
            Controls.Add(PasswordTxt);
            Controls.Add(label2);
            Controls.Add(label1);
            Name = "PasswordReauthenticate";
            Text = "PasswordReauthenticate";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox PasswordTxt;
        private TextBox ConfirmPassTxt;
        private Button BtnConfirm;
    }
}