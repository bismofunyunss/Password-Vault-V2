using System.ComponentModel;
using System.Security;

namespace Password_Vault_V2;

[DesignerCategory("Code")]
public partial class PasswordBox : TextBox
{
    public PasswordBox()
    {
        InitializeComponent();
        textBox2.UseSystemPasswordChar = true;
        SecurePassword = new SecureString();
        TextChanged += PasswordBox_TextChanged;
    }

    [Browsable(false)]
    public SecureString? SecurePassword { get; }

    private void PasswordBox_TextChanged(object? sender, EventArgs e)
    {
        if (SecurePassword == null)
            return;
        SecurePassword.Clear();
        foreach (var c in Text) SecurePassword.AppendChar(c);
    }

    public void ClearPassword()
    {
        textBox2.Clear();
        SecurePassword?.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SecurePassword?.Dispose();
        }
        base.Dispose(disposing);
    }
}