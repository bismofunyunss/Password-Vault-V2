using System.ComponentModel;

namespace Password_Vault_V2
{
    sealed partial class Register
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
            RegisterBox = new GroupBox();
            emailBox = new TextBox();
            emailLbl = new Label();
            pictureBox1 = new PictureBox();
            WelcomeLabel = new Label();
            button1 = new Button();
            RegisterBox.SuspendLayout();
            ((ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // userLbl
            // 
            userLbl.AutoSize = true;
            userLbl.Font = new Font("Century Gothic", 11F);
            userLbl.Location = new Point(8, 28);
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
            passLbl.Location = new Point(8, 89);
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
            passTxt.KeyDown += passTxt_KeyDown;
            passTxt.KeyPress += passTxt_KeyPress;
            // 
            // confirmPassLbl
            // 
            confirmPassLbl.AutoSize = true;
            confirmPassLbl.Font = new Font("Century Gothic", 11F);
            confirmPassLbl.Location = new Point(8, 154);
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
            confirmPassTxt.KeyDown += confirmPassTxt_KeyDown;
            confirmPassTxt.KeyPress += confirmPassTxt_KeyPress;
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
            CreateAccountBtn.Location = new Point(6, 285);
            CreateAccountBtn.Name = "CreateAccountBtn";
            CreateAccountBtn.Size = new Size(438, 44);
            CreateAccountBtn.TabIndex = 5;
            CreateAccountBtn.Text = "&Create Account";
            CreateAccountBtn.UseVisualStyleBackColor = false;
            CreateAccountBtn.Click += CreateAccountBtn_Click;
            // 
            // statusLbl
            // 
            statusLbl.AutoSize = true;
            statusLbl.Font = new Font("Century Gothic", 11F);
            statusLbl.Location = new Point(8, 457);
            statusLbl.Name = "statusLbl";
            statusLbl.Size = new Size(94, 25);
            statusLbl.TabIndex = 8;
            statusLbl.Text = "Status ::";
            // 
            // outputLbl
            // 
            outputLbl.AutoSize = true;
            outputLbl.Font = new Font("Century Gothic", 11F);
            outputLbl.Location = new Point(106, 457);
            outputLbl.Name = "outputLbl";
            outputLbl.Size = new Size(71, 25);
            outputLbl.TabIndex = 9;
            outputLbl.Text = "Idle...";
            // 
            // RegisterBox
            // 
            RegisterBox.BackColor = Color.FromArgb(30, 30, 30);
            RegisterBox.Controls.Add(button1);
            RegisterBox.Controls.Add(emailBox);
            RegisterBox.Controls.Add(emailLbl);
            RegisterBox.Controls.Add(pictureBox1);
            RegisterBox.Controls.Add(WelcomeLabel);
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
            RegisterBox.Location = new Point(12, 16);
            RegisterBox.Name = "RegisterBox";
            RegisterBox.Padding = new Padding(5);
            RegisterBox.Size = new Size(467, 546);
            RegisterBox.TabIndex = 0;
            RegisterBox.TabStop = false;
            RegisterBox.Text = "Register Account";
            RegisterBox.Enter += RegisterBox_Enter;
            // 
            // emailBox
            // 
            emailBox.BackColor = Color.FromArgb(30, 30, 30);
            emailBox.ForeColor = Color.White;
            emailBox.Location = new Point(8, 245);
            emailBox.Name = "emailBox";
            emailBox.Size = new Size(436, 34);
            emailBox.TabIndex = 16;
            // 
            // emailLbl
            // 
            emailLbl.AutoSize = true;
            emailLbl.Font = new Font("Century Gothic", 11F);
            emailLbl.Location = new Point(8, 217);
            emailLbl.Name = "emailLbl";
            emailLbl.Size = new Size(71, 25);
            emailLbl.TabIndex = 15;
            emailLbl.Text = "Email";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(322, 407);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(122, 131);
            pictureBox1.TabIndex = 13;
            pictureBox1.TabStop = false;
            // 
            // WelcomeLabel
            // 
            WelcomeLabel.AutoSize = true;
            WelcomeLabel.Font = new Font("Century Gothic", 11F);
            WelcomeLabel.ForeColor = Color.White;
            WelcomeLabel.Location = new Point(6, 432);
            WelcomeLabel.Name = "WelcomeLabel";
            WelcomeLabel.Size = new Size(169, 25);
            WelcomeLabel.TabIndex = 12;
            WelcomeLabel.Text = "Welcome, null";
            // 
            // Register
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(RegisterBox);
            DoubleBuffered = true;
            Name = "Register";
            Size = new Size(496, 565);
            Paint += Register_Paint;
            RegisterBox.ResumeLayout(false);
            RegisterBox.PerformLayout();
            ((ISupportInitialize)pictureBox1).EndInit();
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
        private GroupBox RegisterBox;
        public Label WelcomeLabel;
        public Button CreateAccountBtn;
        private PictureBox pictureBox1;
        private Label emailLbl;
        private TextBox emailBox;
        public Button button1;
    }

    #endregion
}

