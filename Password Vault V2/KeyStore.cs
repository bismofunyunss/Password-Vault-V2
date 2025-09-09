using System.Security.Cryptography;
using System.Text.Json;
using OtpNet;

namespace Password_Vault_V2;

public class SoftwareKeyStore : IDisposable
{
    private readonly string? _jsonPath;
    private readonly List<MasterKeyEntry> _entries = new();
    private readonly List<TotpSecretEntry> _totpEntries = new();
    private bool _disposed;

    public SoftwareKeyStore(string folderPath, string fileName = "keystore.json")
    {
        _jsonPath = Path.Combine(folderPath, fileName);
        if (File.Exists(_jsonPath))
            LoadJson();
    }

    #region MasterKeyEntry & JSON

    public class MasterKeyEntry
    {
        public int Version { get; set; }
        public string WrappedKey { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; } = "";
    }

    private class TotpSecretEntry
    {
        public string Account { get; set; } = string.Empty; // e.g. username or email
        public string WrappedSecret { get; set; } = string.Empty; // DPAPI or AES-encrypted blob
        public DateTime Created { get; set; }
        public string Notes { get; set; } = "";
    }

    public void AddTotpSecret(string account, byte[] secret, string notes = "")
    {
        // Generate new 32-byte salt
        byte[] salt = RandomNumberGenerator.GetBytes(32);

        // Encrypt with DPAPI for current user using the new salt
        byte[] wrapped = ProtectedData.Protect(secret, salt, DataProtectionScope.CurrentUser);

        // Combine salt + wrapped together for storage
        byte[] final = new byte[salt.Length + wrapped.Length];
        Buffer.BlockCopy(salt, 0, final, 0, salt.Length);
        Buffer.BlockCopy(wrapped, 0, final, salt.Length, wrapped.Length);

        _totpEntries.Add(new TotpSecretEntry
        {
            Account = account,
            WrappedSecret = Convert.ToBase64String(final),
            Created = DateTime.UtcNow,
            Notes = notes
        });

        // Wipe sensitive buffers
        CryptographicOperations.ZeroMemory(wrapped);
        CryptographicOperations.ZeroMemory(secret);

        SaveJson();
    }


    public byte[]? GetTotpSecret(string account)
    {
        var entry = _totpEntries.FirstOrDefault(e => e.Account == account);
        if (entry == null) return null;

        try
        {
            byte[] final = Convert.FromBase64String(entry.WrappedSecret);

            // Extract salt (first 32 bytes, for example)
            int saltLength = 32;
            byte[] salt = new byte[saltLength];
            Buffer.BlockCopy(final, 0, salt, 0, saltLength);

            // Extract wrapped secret (rest of buffer)
            int wrappedLength = final.Length - saltLength;
            byte[] wrapped = new byte[wrappedLength];
            Buffer.BlockCopy(final, saltLength, wrapped, 0, wrappedLength);

            // Decrypt with same salt
            return ProtectedData.Unprotect(wrapped, salt, DataProtectionScope.CurrentUser);
        }
        catch
        {
            return null;
        }
    }

    private class StoreModel
    {
        public List<MasterKeyEntry> MasterKeys { get; set; } = new();
        public List<TotpSecretEntry> TotpSecrets { get; set; } = new();
    }

    private void LoadJson()
    {
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

        int newVersion = _entries.Any() ? _entries.Max(e => e.Version) + 1 : 1;

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

    public byte[] RetrieveMasterKey(byte[] kek, int? version = null)
    {
        MasterKeyEntry entry = version.HasValue
            ? _entries.FirstOrDefault(e => e.Version == version.Value)
            : _entries.OrderByDescending(e => e.Version).FirstOrDefault();

        if (entry == null)
            throw new Exception("Master key entry not found.");

        byte[] masterKey =
            FipsCrypto.AesKeyWrapRfc5649.Unwrap(kek, DataConversionHelpers.HexStringToByteArray(entry.WrappedKey));

        CryptographicOperations.ZeroMemory(kek);
        return masterKey; // caller should use MasterKey.SecureKey or similar pinned buffer
    }

    public void RotateMasterKey(byte[] newMasterKey, string notes = "")
    {
        AddNewMasterKey(newMasterKey, notes);
    }

    public MasterKeyEntry GetLatestKey() => _entries.OrderByDescending(e => e.Version).FirstOrDefault();
    public MasterKeyEntry GetKeyByVersion(int version) => _entries.FirstOrDefault(e => e.Version == version);

    #endregion

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
            _totp = new Totp(secretKey, step: 30, totpSize: 6);
        }

        public string GenerateCode()
        {
            return _totp.ComputeTotp(); // generate current 6-digit code
        }

        public bool VerifyCode(string code)
        {
            // Allow ±1 step drift to handle small clock skew
            return _totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
    }

}