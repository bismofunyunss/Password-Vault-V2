using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CredentialManagement;

public static class LoginAlertManager
{
    private static readonly string UserDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Password Vault", "UserData");

    static LoginAlertManager()
    {
        if (!Directory.Exists(UserDataFolder))
            Directory.CreateDirectory(UserDataFolder);
    }

    /// <summary>
    /// Registers a user's email. Overwrites if already exists.
    /// </summary>
    public static void RegisterUserEmail(string username, string email)
    {
        var path = Path.Combine(UserDataFolder, $"{username}.json");
        var data = new UserLoginData { Email = email };
        File.WriteAllText(path, JsonSerializer.Serialize(data));
    }

    /// <summary>
    /// Sends a login alert email to the user's registered email with IP/location/machine info.
    /// </summary>
    public static async Task SendLoginAlertAsync(string username)
    {
        var data = GetUserData(username);
        if (data == null || string.IsNullOrWhiteSpace(data.Email))
            throw new InvalidOperationException("User email not registered.");

        string ip = await GetExternalIpAsync();
        var geo = await GetGeoInfoAsync(ip);

        string machine = Environment.MachineName;
        string os = Environment.OSVersion.ToString();
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");

        // Detect unusual login
        bool suspicious = data.LastIp != null && (data.LastIp != ip || data.LastLocation != geo.Summary);

        string message =
            $"User: {username} logged in.\n" +
            $"IP: {ip}\n" +
            $"Location: {geo.City}, {geo.Region}, {geo.Country}, ISP: {geo.ISP}" +
            (suspicious ? "\n⚠️ Unusual login location detected!" : "") + "\n" +
            $"Machine: {machine}\nOS: {os}\nTime: {time}";

        await SendEmailAsync(data.Email, "Login Alert", message);

        // Save current login info for next comparison
        data.LastIp = ip;
        data.LastLocation = geo.Summary;
        SaveUserData(username, data);
    }

    private static async Task<string> GetExternalIpAsync()
    {
        using var http = new HttpClient();
        return (await http.GetStringAsync("https://api.ipify.org")).Trim();
    }

    private static async Task<GeoInfo> GetGeoInfoAsync(string ip)
    {
        try
        {
            using var http = new HttpClient();
            string url = $"http://ip-api.com/json/{ip}";
            var json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string city = root.GetProperty("city").GetString();
            string region = root.GetProperty("regionName").GetString();
            string country = root.GetProperty("country").GetString();
            string isp = root.GetProperty("isp").GetString();

            return new GeoInfo
            {
                City = city,
                Region = region,
                Country = country,
                ISP = isp,
                Summary = $"{city}, {region}, {country}"
            };
        }
        catch
        {
            return new GeoInfo
            {
                City = "Unknown",
                Region = "Unknown",
                Country = "Unknown",
                ISP = "Unknown",
                Summary = "Unknown"
            };
        }
    }

    private static async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        // Retrieve VaultDev10 credentials from Windows Credential Manager
        var (smtpUser, smtpPass) = GetVaultDev10Credentials();

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(smtpUser, "Password Vault"),
            Subject = subject,
            Body = message
        };
        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }

    private static (string Username, string Password) GetVaultDev10Credentials()
    {
        using var cred = new Credential { Target = "MyAppSMTP" }; // Name used when saving credentials
        if (!cred.Load())
            throw new InvalidOperationException("VaultDev10 credentials not found in Windows Credential Manager.");
        return (cred.Username, cred.Password);
    }

    private static UserLoginData GetUserData(string username)
    {
        var path = Path.Combine(UserDataFolder, $"{username}.json");
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<UserLoginData>(json);
    }

    private static void SaveUserData(string username, UserLoginData data)
    {
        var path = Path.Combine(UserDataFolder, $"{username}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
    }

    private class UserLoginData
    {
        public string Email { get; set; }
        public string LastIp { get; set; }
        public string LastLocation { get; set; }
    }

    private class GeoInfo
    {
        public string City { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string ISP { get; set; }
        public string Summary { get; set; }
    }
}

