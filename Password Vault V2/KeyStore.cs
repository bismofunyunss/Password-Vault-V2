using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OtpNet;

namespace Password_Vault_V2;

public class SoftwareKeyStore : IDisposable
{
    private readonly List<MasterKeyEntry> _entries = new();
    private readonly string? _jsonPath;
    private readonly List<TotpSecretEntry> _totpEntries = new();
    private bool _disposed;

    public SoftwareKeyStore(string folderPath, string fileName = "keystore.json")
    {
        _jsonPath = Path.Combine(folderPath, fileName);
        if (File.Exists(_jsonPath))
            LoadJson();
    }

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            _entries.Clear();
            _disposed = true;
        }
    }

    #endregion

    public class TwoFactorAuth
    {
        private readonly Totp _totp;
        internal bool synced;

        public TwoFactorAuth(byte[] secretKey)
        {
            // 30-second expiry, 6-digit codes
            _totp = new Totp(secretKey);
        }

        public string GenerateCode()
        {
            return _totp.ComputeTotp(); // generate current 6-digit code
        }

        public bool VerifyCode(string code)
        {
            // Allow ±1 step drift to handle small clock skew
            return _totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
        }
    }

    #region MasterKeyEntry & JSON

    public class MasterKeyEntry
    {
        public int Version { get; set; }
        public string WrappedKey { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; } = "";
    }

    public void AddTotpSecret(string account, byte[] secret, string notes = "")
    {
        if (secret == null || secret.Length != 20)
            throw new ArgumentException("TOTP secret must be exactly 20 bytes.", nameof(secret));

        // Optional: generate 32-byte DPAPI salt
        var salt = RandomNumberGenerator.GetBytes(32);

        var secretClone = (byte[])secret.Clone();

        // Encrypt secret with DPAPI for current user
        var wrapped = ProtectedData.Protect(secretClone, salt, DataProtectionScope.CurrentUser);

        try
        {
            // Store salt + wrapped bytes together
            var combined = new byte[salt.Length + wrapped.Length];
            Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
            Buffer.BlockCopy(wrapped, 0, combined, salt.Length, wrapped.Length);

            _totpEntries.Add(new TotpSecretEntry
            {
                Account = account,
                WrappedSecret = Convert.ToBase64String(combined),
                Created = DateTime.UtcNow,
                Notes = notes
            });

            SaveJson(); // your existing method to persist _totpEntries
        }
        finally
        {
            CryptographicOperations.ZeroMemory(secretClone);
            CryptographicOperations.ZeroMemory(wrapped);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    public byte[]? GetTotpSecret(string account)
    {
        var entry = _totpEntries.FirstOrDefault(e => e.Account == account);
        if (entry == null) return null;

        try
        {
            var combined = Convert.FromBase64String(entry.WrappedSecret);

            int saltLength = 32;
            var salt = new byte[saltLength];
            var wrapped = new byte[combined.Length - saltLength];

            Buffer.BlockCopy(combined, 0, salt, 0, saltLength);
            Buffer.BlockCopy(combined, saltLength, wrapped, 0, wrapped.Length);

            return ProtectedData.Unprotect(wrapped, salt, DataProtectionScope.CurrentUser);
        }
        catch { return null; }
    }

    private class TotpSecretEntry
{
    public string Account { get; set; } = string.Empty;
    public string WrappedSecret { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string Notes { get; set; } = "";
}


private class StoreModel
    {
        public List<MasterKeyEntry> MasterKeys { get; set; } = new();
        public List<TotpSecretEntry> TotpSecrets { get; set; } = new();
    }

    private void LoadJson()
    {
        _entries.Clear();
        _totpEntries.Clear();

        if (!File.Exists(_jsonPath))
            return;

        var json = File.ReadAllText(_jsonPath);
        var loaded = JsonSerializer.Deserialize<StoreModel>(json);
        if (loaded != null)
        {
            _entries.AddRange(loaded.MasterKeys);
            _totpEntries.AddRange(loaded.TotpSecrets);
        }
    }

    private void SaveJson()
    {
        var dir = Path.GetDirectoryName(_jsonPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var model = new StoreModel
        {
            MasterKeys = _entries,
            TotpSecrets = _totpEntries
        };

        File.WriteAllText(
            _jsonPath,
            JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true
            })
        );
    }

    #endregion

    #region Key Management

    public void AddNewMasterKey(byte[] wrappedKey, string notes = "")
    {
        if (wrappedKey == null || wrappedKey.Length == 0)
            throw new ArgumentNullException(nameof(wrappedKey));

        var newVersion = _entries.Any() ? _entries.Max(e => e.Version) + 1 : 1;

        _entries.Add(new MasterKeyEntry
        {
            Version = newVersion,
            Timestamp = DateTime.UtcNow,
            WrappedKey = DataConversionHelpers.ByteArrayToHexString(wrappedKey),
            Notes = notes
        });

        SaveJson();

        // Zero sensitive buffers
        CryptographicOperations.ZeroMemory(wrappedKey);
    }

    public byte[] RetrieveMasterKey(byte[] kek, byte[] hmacKey, int? version = null)
    {
        var entry = version.HasValue
            ? _entries.FirstOrDefault(e => e.Version == version.Value)
            : _entries.OrderByDescending(e => e.Version).FirstOrDefault();

        if (entry == null)
            throw new Exception("Master key entry not found.");

        // entry.WrappedKey = TPM-RSA-wrapped RFC5649 blob
        using var wrappedAesKey = new FipsCrypto.WrappedAesKeyStore("MyTpmRsaKey");

        // Pass RSA blob and KEK
        var masterKey = wrappedAesKey.UnsealKey(
            DataConversionHelpers.HexStringToByteArray(entry.WrappedKey),
            kek, hmacKey);

        CryptographicOperations.ZeroMemory(kek);
        return masterKey;
    }

    public void RotateMasterKey(byte[] newMasterKey, string notes = "")
    {
        AddNewMasterKey(newMasterKey, notes);
    }

    public MasterKeyEntry GetLatestKey()
    {
        return _entries.OrderByDescending(e => e.Version).FirstOrDefault();
    }

    public MasterKeyEntry GetKeyByVersion(int version)
    {
        return _entries.FirstOrDefault(e => e.Version == version);
    }

    #endregion
}