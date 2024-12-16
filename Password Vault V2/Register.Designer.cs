using System.ComponentModel;

namespace Password_Vault_V2
{
    partial class Register
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(Register));
            userLbl = new Label();
            userTxt = new TextBox();
            passLbl = new Label();
            passTxt = new TextBox();
            confirmPassLbl = new Label();
            confirmPassTxt = new TextBox();
            CreateAccountBtn = new Button();
            statusLbl = new Label();
            outputLbl = new Label();
            ShowPasswordCheckBox = new CheckBox();
            RegisterBox = new GroupBox();
            WelcomeLabel = new Label();
            RegisterBox.SuspendLayout();
            SuspendLayout();
            // 
            // userLbl
            // 
            userLbl.AutoSize = true;
            userLbl.Font = new Font("Century Gothic", 11F);
            userLbl.Location = new Point(6, 26);
            userLbl.Name = "userLbl";
            userLbl.Size = new Size(121, 25);
            userLbl.TabIndex = 0;
            userLbl.Text = "Username";
            // 
            // userTxt
            // 
            userTxt.BackColor = Color.FromArgb(30, 30, 30);
            userTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            userTxt.ForeColor = Color.White;
            userTxt.Location = new Point(6, 54);
            userTxt.Name = "userTxt";
            userTxt.Size = new Size(438, 34);
            userTxt.TabIndex = 1;
            // 
            // passLbl
            // 
            passLbl.AutoSize = true;
            passLbl.Font = new Font("Century Gothic", 11F);
            passLbl.Location = new Point(6, 87);
            passLbl.Name = "passLbl";
            passLbl.Size = new Size(114, 25);
            passLbl.TabIndex = 2;
            passLbl.Text = "Password";
            // 
            // passTxt
            // 
            passTxt.BackColor = Color.FromArgb(30, 30, 30);
            passTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            passTxt.ForeColor = Color.White;
            passTxt.Location = new Point(6, 115);
            passTxt.Name = "passTxt";
            passTxt.Size = new Size(438, 34);
            passTxt.TabIndex = 2;
            passTxt.UseSystemPasswordChar = true;
            passTxt.TextChanged += passTxt_TextChanged;
            // 
            // confirmPassLbl
            // 
            confirmPassLbl.AutoSize = true;
            confirmPassLbl.Font = new Font("Century Gothic", 11F);
            confirmPassLbl.Location = new Point(6, 152);
            confirmPassLbl.Name = "confirmPassLbl";
            confirmPassLbl.Size = new Size(205, 25);
            confirmPassLbl.TabIndex = 4;
            confirmPassLbl.Text = "Confirm Password";
            // 
            // confirmPassTxt
            // 
            confirmPassTxt.BackColor = Color.FromArgb(30, 30, 30);
            confirmPassTxt.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            confirmPassTxt.ForeColor = Color.White;
            confirmPassTxt.Location = new Point(6, 180);
            confirmPassTxt.Name = "confirmPassTxt";
            confirmPassTxt.Size = new Size(438, 34);
            confirmPassTxt.TabIndex = 3;
            confirmPassTxt.UseSystemPasswordChar = true;
            confirmPassTxt.TextChanged += confirmPassTxt_TextChanged;
            // 
            // CreateAccountBtn
            // 
            CreateAccountBtn.BackColor = Color.FromArgb(30, 30, 30);
            CreateAccountBtn.FlatAppearance.BorderColor = Color.WhiteSmoke;
            CreateAccountBtn.FlatStyle = FlatStyle.Flat;
            CreateAccountBtn.Font = new Font("Century Gothic", 11F);
            CreateAccountBtn.ForeColor = Color.WhiteSmoke;
            CreateAccountBtn.Image = (Image)resources.GetObject("CreateAccountBtn.Image");
            CreateAccountBtn.ImageAlign = ContentAlignment.MiddleLeft;
            CreateAccountBtn.Location = new Point(6, 220);
            CreateAccountBtn.Name = "CreateAccountBtn";
            CreateAccountBtn.Size = new Size(438, 44);
            CreateAccountBtn.TabIndex = 4;
            CreateAccountBtn.Text = "&Create Account";
            CreateAccountBtn.UseVisualStyleBackColor = false;
            CreateAccountBtn.Click += CreateAccountBtn_Click;
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
            // ShowPasswordCheckBox
            // 
            ShowPasswordCheckBox.AutoSize = true;
            ShowPasswordCheckBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ShowPasswordCheckBox.Location = new Point(234, 281);
            ShowPasswordCheckBox.Name = "ShowPasswordCheckBox";
            ShowPasswordCheckBox.Size = new Size(204, 29);
            ShowPasswordCheckBox.TabIndex = 5;
            ShowPasswordCheckBox.Text = "Show Password";
            ShowPasswordCheckBox.UseVisualStyleBackColor = true;
            ShowPasswordCheckBox.CheckedChanged += ShowPasswordCheckBox_CheckedChanged;
            // 
            // RegisterBox
            // 
            RegisterBox.BackColor = Color.FromArgb(30, 30, 30);
            RegisterBox.Controls.Add(WelcomeLabel);
            RegisterBox.Controls.Add(ShowPasswordCheckBox);
            RegisterBox.Controls.Add(outputLbl);
            RegisterBox.Controls.Add(statusLbl);
            RegisterBox.Controls.Add(CreateAccountBtn);
            RegisterBox.Controls.Add(confirmPassTxt);
            RegisterBox.Controls.Add(confirmPassLbl);
            RegisterBox.Controls.Add(passTxt);
            RegisterBox.Controls.Add(passLbl);
            RegisterBox.Controls.Add(userTxt);
            RegisterBox.Controls.Add(userLbl);
            RegisterBox.Font = new Font("Century Gothic", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            RegisterBox.ForeColor = Color.WhiteSmoke;
            RegisterBox.Location = new Point(12, 12);
            RegisterBox.Name = "RegisterBox";
            RegisterBox.Size = new Size(450, 434);
            RegisterBox.TabIndex = 0;
            RegisterBox.TabStop = false;
            RegisterBox.Text = "Register Account";
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
            // Register
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(RegisterBox);
            Name = "Register";
            Size = new Size(474, 458);
            RegisterBox.ResumeLayout(false);
            RegisterBox.PerformLayout();
            ResumeLayout(false);
        }

        private Label userLbl;
        public TextBox userTxt;
        private Label passLbl;
        private TextBox passTxt;
        private Label confirmPassLbl;
        private TextBox confirmPassTxt;
        private Label statusLbl;
        private Label outputLbl;
        private CheckBox ShowPasswordCheckBox;
        private GroupBox RegisterBox;
        public Label WelcomeLabel;
        public Button CreateAccountBtn;
    }

    #endregion
}

