using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Password_Vault_V2
{
    public partial class PasswordReauthenticate : Form
    {
        public PasswordReauthenticate()
        {
            InitializeComponent();
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string EnteredPassword { get; private set; }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PasswordTxt.Text))
            {
                MessageBox.Show("Please enter a password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EnteredPassword = PasswordTxt.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
