using static Password_Vault_V2.Verification;
using Timer = System.Windows.Forms.Timer;

namespace Password_Vault_V2;

public partial class ConfirmEmail : Form
{
    private readonly string _email;
    private readonly VerificationServiceWithTimer _verificationService;
    private string _code = string.Empty;
    private readonly Timer _uiTimer;

    public ConfirmEmail(string email, VerificationServiceWithTimer verificationService, string initialCode)
    {
        InitializeComponent();
        _email = email;
        _verificationService = verificationService;
        _code = initialCode;

        _uiTimer = new Timer { Interval = 1000 };
        _uiTimer.Tick += UiTimer_Tick;
        _uiTimer.Start();
    }

    private async void UiTimer_Tick(object? sender, EventArgs e)
    {
        var timeLeft = _verificationService.GetTimeRemaining(_email);
        if (timeLeft.HasValue)
        {
            timerLbl.Text = timeLeft.Value.ToString(@"mm\:ss");
            timerLbl.ForeColor = timeLeft.Value <= TimeSpan.FromMinutes(1) ? Color.Red : Color.Black;
        }
        else
        {
            timerLbl.Text = "Expired";
            _uiTimer.Stop();

            var result = MessageBox.Show(
                "Code has expired. Would you like to send another code?",
                "Code expired",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                await ResendCodeAsync();
            }
            else
            {
                DialogResult = DialogResult.No;
                MessageBox.Show("Unable to verify email.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _uiTimer.Dispose();
                Close();
            }
        }
    }

    private void confirmCodeBtn_Click(object sender, EventArgs e)
    {
        _uiTimer.Stop();

        if (_verificationService.ValidateCode(_email, _code))
        {
            MessageBox.Show("Email verified.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _verificationService.Validated = true;
            Close();
            return;
        }

        if (!_verificationService.ValidateCode(_email, _code))
        {
            DialogResult = DialogResult.No;
            var result = MessageBox.Show("Unable to verify email. Would you like a new code?", "Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            return;
        }

        else
        {
            DialogResult = DialogResult.No;
            MessageBox.Show("Unable to verify email.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _uiTimer.Dispose();
            Close();
        }
    }

    private async Task ResendCodeAsync()
    {
        bool sent = false;
        while (!sent)
        {
            _code = _verificationService.GenerateCode();
            _verificationService.StoreCode(_email, _code, TimeSpan.FromMinutes(10));

            try
            {
                await _verificationService.SendEmailAsync(_email, _code);
                sent = true;
            }
            catch
            {
                var retry = MessageBox.Show(
                    "An error occurred when trying to resend the code. Would you like to try again?",
                    "Error",
                    MessageBoxButtons.RetryCancel,
                    MessageBoxIcon.Error);

                if (retry == DialogResult.Cancel)
                {
                    DialogResult = DialogResult.No;
                    MessageBox.Show("Unable to verify email.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _uiTimer.Dispose();
                    Close();
                    return;
                }
            }
        }

        // Restart the timer after successfully resending
        _uiTimer.Start();
    }

    private void cancelBtn_Click(object sender, EventArgs e)
    {
        _uiTimer.Stop();
        Close();
    }
}