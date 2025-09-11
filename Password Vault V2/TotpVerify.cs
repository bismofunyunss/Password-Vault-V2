using OtpNet;
using QRCoder;

namespace Password_Vault_V2;

internal partial class TotpVerify : Form
{
    private readonly byte[] secret;

    public TotpVerify(byte[] generatedSecret, string issuer, string user, string secretBase32)
    {
        InitializeComponent();
        secret = generatedSecret;

        // Build QR code
        string otpauthUrl = $"otpauth://totp/{issuer}:{user}?secret={secretBase32}&issuer={issuer}&digits=6";
        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
        QRCode qrCode = new QRCode(qrCodeData);
        Bitmap qrCodeImage = qrCode.GetGraphic(4);
        QRCodeImg.Image = qrCodeImage;

        // Ensure that if the user closes the form without success, we treat it as failure
        this.FormClosing += (s, e) =>
        {
            if (DialogResult != DialogResult.OK)
                DialogResult = DialogResult.Cancel;
        };
    }

    private void confirmBtn_Click(object sender, EventArgs e)
    {
        var totp = new Totp(secret);
        var entered = codetxt.Text.Trim();

        if (totp.VerifyTotp(entered, out _))
        {
            MessageBox.Show("Authenticator setup successful!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("Invalid code. Please try again.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            // Form stays open, user can retry
        }
    }
}
