using System.Collections.Concurrent;
using System.Security.Cryptography;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Timer = System.Threading.Timer;

namespace Password_Vault_V2;

public abstract class Verification
{
    public class VerificationServiceWithTimer : IDisposable
    {
        private readonly Timer _cleanupTimer;
        private readonly ConcurrentDictionary<string, CodeEntry> _codes = new();
        internal bool Validated;

        // Run cleanup every minute to clear expired codes
        public VerificationServiceWithTimer()
        {
            _cleanupTimer = new Timer(_ => CleanupExpiredCodes(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
            foreach (var entry in _codes.Values) entry.CancellationTokenSource?.Dispose();
        }

        private void CleanupExpiredCodes()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _codes)
                if (kvp.Value.Expiry <= now)
                    _codes.TryRemove(kvp.Key, out _);
        }

        public string GenerateCode()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            var val = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
            val = val % 900000 + 100000;
            return val.ToString();
        }

        public void StoreCode(string email, string code, TimeSpan validFor)
        {
            var cts = new CancellationTokenSource(validFor);
            var entry = new CodeEntry
            {
                Code = code,
                Expiry = DateTime.UtcNow.Add(validFor),
                CancellationTokenSource = cts
            };
            _codes[email] = entry;

            // When token cancels, remove the code automatically
            cts.Token.Register(() => _codes.TryRemove(email, out _));
        }

        public bool ValidateCode(string email, string code)
        {
            if (_codes.TryGetValue(email, out var entry))
                if (DateTime.UtcNow <= entry.Expiry && entry.Code == code)
                {
                    _codes.TryRemove(email, out _);
                    entry.CancellationTokenSource.Cancel();
                    return true;
                }

            return false;
        }

        public TimeSpan? GetTimeRemaining(string email)
        {
            if (_codes.TryGetValue(email, out var entry))
            {
                var timeLeft = entry.Expiry - DateTime.UtcNow;
                if (timeLeft > TimeSpan.Zero) return timeLeft;
            }

            return null;
        }

        // Send email via Gmail SMTP + OAuth2 (using Google.Apis.Auth & MailKit)
        public async Task SendEmailAsync(string toEmail, string code)
        {
            using var stream = new FileStream("GmailClient.json", FileMode.Open, FileAccess.Read);
            var credPath = "token.json";

            try
            {
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { "https://www.googleapis.com/auth/gmail.send" }, // <-- change scope
                    "user", // generic ID for the token store
                    CancellationToken.None,
                    new FileDataStore(credPath, true));

                var accessToken = await credential.GetAccessTokenForRequestAsync();

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Password Vault", "vaultdev10@gmail.com"));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Your Verification Code";
                message.Body = new TextPart("plain")
                {
                    Text = $"Your verification code is: {code}. It expires in 10 minutes."
                };

                using var client = new SmtpClient();
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                // use the actual Gmail address explicitly
                var oauth2 = new SaslMechanismOAuth2("vaultdev10@gmail.com", accessToken);
                await client.AuthenticateAsync(oauth2);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private class CodeEntry
        {
            public string Code { get; init; }
            public DateTime Expiry { get; init; }
            public CancellationTokenSource? CancellationTokenSource { get; init; }
        }
    }
}