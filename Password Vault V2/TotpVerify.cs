using System.Security.Cryptography;
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

        // Clean Base32 secret for QR code
        var secretBase32Clean = secretBase32.TrimEnd('=').ToUpperInvariant();

        // Build otpauth URL for Google Authenticator
        var otpauthUrl = $"otpauth://totp/{issuer}:{user}" +
                         $"?secret={secretBase32Clean}" +
                         $"&issuer={issuer}" +
                         $"&digits=6" +
                         $"&period=30" +
                         $"&algorithm=SHA1";

        // Generate QR code
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpauthUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);

        var qrPixelCount = Math.Min(QRCodeImg.Width, QRCodeImg.Height);
        var pixelsPerModule = qrPixelCount / qrCodeData.ModuleMatrix.Count;

        var qrBitmap = qrCode.GetGraphic(pixelsPerModule);

        var finalBitmap = new Bitmap(QRCodeImg.Width, QRCodeImg.Height);
        using (var g = Graphics.FromImage(finalBitmap))
        {
            g.Clear(Color.FromArgb(30, 30, 30));
            var xOffset = (finalBitmap.Width - qrBitmap.Width) / 2;
            var yOffset = (finalBitmap.Height - qrBitmap.Height) / 2;
            g.DrawImage(qrBitmap, xOffset, yOffset, qrBitmap.Width, qrBitmap.Height);
        }

        QRCodeImg.Image = finalBitmap;
        QRCodeImg.SizeMode = PictureBoxSizeMode.Normal;

        // Ensure cancellation if form closed
        FormClosing += (s, e) =>
        {
            if (DialogResult != DialogResult.OK)
                DialogResult = DialogResult.Cancel;
        };
    }

    private void confirmBtn_Click(object sender, EventArgs e)
    {
        try
        {
            var totp = new Totp(secret, 30, totpSize: 6);

            var entered = new string(codetxt.Text.Where(char.IsDigit).ToArray());

            if (totp.VerifyTotp(entered, out _, new VerificationWindow(1, 1)))
            {
                MessageBox.Show("Authenticator setup successful!", "Success", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Invalid code. Please try again.", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            // Wipe internal secret after verification attempt
            CryptographicOperations.ZeroMemory(secret);
        }
    }
}