using System.ComponentModel;
using System.Diagnostics;
using System.Security;
using System.Text;

namespace Password_Vault_V2;

public partial class Register : UserControl
{
    private static CancellationTokenSource _cancellationTokenSource = new();
    private static readonly CancellationToken CancellationToken = _cancellationTokenSource.Token;

    public Register()
    {
        InitializeComponent();
    }

    public static bool CheckPasswordValidity(IReadOnlyCollection<char> password,
        IReadOnlyCollection<char>? password2 = null)
    {
        if (password.Count is 24 or > 120)
            return false;

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return false;

        if (password.Any(char.IsWhiteSpace) || (password2 != null &&
                                                (password2.Any(char.IsWhiteSpace) ||
                                                 !password.SequenceEqual(password2))))
            return false;

        return password.Any(char.IsSymbol) || password.Any(char.IsPunctuation);
    }


    /// <summary>
    ///     Asynchronously creates a user account with specified username and password.
    /// </summary>
    private async Task CreateAccountAsync()
    {
        if (ShowPasswordCheckBox.Checked)
            ShowPasswordCheckBox.Checked = false;

        var userName = userTxt.Text;

        var userExists = Authentication.UserExists(userName);

        if (SecurePassword != null)
        {
            var passwordBytes = Crypto.ConversionMethods.ToByteArray(SecurePassword);
            if (SecureConfirmPassword != null)
            {
                var confirmPasswordBytes = Crypto.ConversionMethods.ToByteArray(SecureConfirmPassword);

                try
                {
                    if (!userExists)
                    {
                        DisableUi();

                        await RegisterAsync(userName, Encoding.UTF8.GetBytes(passTxt.Text), Encoding.UTF8.GetBytes(confirmPassTxt.Text));
                    }
                    else
                    {
                        throw new ArgumentException("Username already exists.", nameof(userName));
                    }
                }
                catch (Exception)
                {
                    await _cancellationTokenSource.CancelAsync();

                    if (_cancellationTokenSource.IsCancellationRequested)
                        _cancellationTokenSource = new CancellationTokenSource();

                    outputLbl.Text = "Idle...";
                    outputLbl.ForeColor = Color.White;
                    throw;
                }
                finally
                {
                    var arrays = new[]
                    {
                        passwordBytes, confirmPasswordBytes
                    };

                    Crypto.CryptoUtilities.ClearMemory(arrays);
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
                }
            }
        }
    }

    private void DisableUi()
    {
        foreach (Control c in RegisterBox.Controls)
        {
            if (c == userLbl || c == passLbl || c == confirmPassLbl || c == statusLbl || c == outputLbl ||
                c == WelcomeLabel)
                continue;
            c.Enabled = false;
        }
    }

    private void EnableUi()
    {
        foreach (Control c in RegisterBox.Controls)
            c.Enabled = true;
    }

    /// <summary>
    ///     Asynchronously registers a user with the specified username and password,
    ///     encrypts user data, and displays relevant messages to the user.
    /// </summary>
    /// <param name="username">The username of the user to be registered.</param>
    /// <param name="password">The password of the user to be registered.</param>
    /// <param name="confirmPassword">The confirmation of the user's password.</param>
    private async Task RegisterAsync(string username, byte[] password, byte[] confirmPassword)
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;

        StartAnimation();

        var passwordChars = Encoding.UTF8.GetChars(password);
        var confirmPasswordChars = Encoding.UTF8.GetChars(confirmPassword);

        ValidateUsernameAndPassword(username, ref passwordChars, ref confirmPasswordChars);

        var userName = userTxt.Text;

        var userDirectory = CreateDirectoryIfNotExists(Path.Combine("Password Vault", "Users", userName));

        var userFile = Path.Combine(userDirectory, $"{userName}.user");
        var userSalt = Path.Combine(userDirectory, $"{userName}.salt");

        var salt = Crypto.CryptoUtilities.RndByteSized(Crypto.CryptoConstants.SaltSize);
        var hashedPassword = await Crypto.HashingMethods.Argon2Id(password, salt, 32);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

        if (hashedPassword == null)
            throw new Exception("Value was null or empty.");

        var saltString = DataConversionHelpers.ByteArrayToBase64String(salt);

        Crypto.CryptoConstants.Hash = hashedPassword;

        await File.WriteAllTextAsync(userFile,
            $"User:\n{username}\nHash:\n{DataConversionHelpers.ByteArrayToHexString(hashedPassword)}");

        await File.WriteAllTextAsync(userSalt, saltString);

        var encrypted = await Crypto.EncryptFile(username, password, Authentication.GetUserFilePath(username));

        if (encrypted == Array.Empty<byte>())
            throw new InvalidOperationException("Value returned null or empty.");

        await File.WriteAllTextAsync(userFile, DataConversionHelpers.ByteArrayToBase64String(encrypted));

        Crypto.CryptoUtilities.ClearMemory(hashedPassword);

        outputLbl.Text = "Account created";
        outputLbl.ForeColor = Color.LimeGreen;

        if (_cancellationTokenSource.IsCancellationRequested)
            _cancellationTokenSource = new CancellationTokenSource();

        await _cancellationTokenSource.CancelAsync();

        MessageBox.Show("Registration successful! Make sure you do NOT forget your password or you will lose access " +
                        "to all of your files.", "Registration Complete", MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        EnableUi();

        var arrays = new[]
        {
            password, confirmPassword
        };
        Crypto.CryptoUtilities.ClearMemory(arrays);

        var charArrays = new[]
        {
            passwordChars, confirmPasswordChars
        };

        Crypto.CryptoUtilities.ClearMemory(charArrays);

        outputLbl.Text = "Idle...";
        outputLbl.ForeColor = Color.White;

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
        GC.WaitForPendingFinalizers();

        userTxt.Clear();
        passTxt.Clear();
        confirmPassTxt.Clear();
    }


    /// <summary>
    ///     Validates the provided username and password for registration.
    /// </summary>
    /// <param name="userName">The username to be validated.</param>
    /// <param name="password">The password to be validated.</param>
    /// <param name="password2">The confirmation password to be validated.</param>
    /// <exception cref="ArgumentException">
    ///     Thrown if the username or password does not meet the specified criteria.
    /// </exception>
    private static void ValidateUsernameAndPassword(string userName, ref char[] password, ref char[] password2)
    {
        if (!userName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == ' '))
            throw new ArgumentException(
                "Value contains illegal characters. Valid characters are letters, digits, underscores, and spaces.",
                nameof(userName));

        if (string.IsNullOrEmpty(userName) || userName.Length > 20)
            throw new ArgumentException("Invalid username.", nameof(userName));

        if (password == Array.Empty<char>())
            throw new ArgumentException("Invalid password.", nameof(password));

        if (!CheckPasswordValidity(password, password2))
            throw new Exception(
                "Password must contain between 24 and 120 characters. It also must include:" +
                " 1.) At least one uppercase letter." +
                " 2.) At least one lowercase letter." +
                " 3.) At least one number." +
                " 4.) At least one special character." +
                " 5.) Must not contain any spaces." +
                " 6.) Both passwords must match.");
    }

    private static string CreateDirectoryIfNotExists(string directoryPath)
    {
        var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            directoryPath);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        return fullPath;
    }

    private async void CreateAccountBtn_Click(object sender, EventArgs e)
    {
        MessageBox.Show(
            @"Do NOT close the program while loading. This may cause corrupted data that is NOT recoverable.", @"Info",
            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        try
        {
            await CreateAccountAsync();
        }
        catch (Exception ex)
        {
            EnableUi();
            ErrorLogging.ErrorLog(ex);
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void StartAnimation()
    {
        await UiController.Animations.AnimateLabel(outputLbl, "Creating account", CancellationToken);
    }

    private void ShowPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        passTxt.UseSystemPasswordChar = !ShowPasswordCheckBox.Checked;
        confirmPassTxt.UseSystemPasswordChar = !ShowPasswordCheckBox.Checked;
    }

    #region TextboxBehavior

    [Browsable(false)] public SecureString? SecurePassword { get; } = new();
    [Browsable(false)] public SecureString? SecureConfirmPassword { get; } = new();

    public void ClearPassword()
    {
        passTxt.Clear();
        SecurePassword?.Clear();
        SecureConfirmPassword?.Clear();
        confirmPassTxt.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
            SecurePassword?.Dispose();
            SecureConfirmPassword?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void passTxt_TextChanged(object sender, EventArgs e)
    {
        if (SecurePassword == null)
            return;
        SecurePassword.Clear();
        foreach (var c in passTxt.Text) SecurePassword.AppendChar(c);
    }

    private void confirmPassTxt_TextChanged(object sender, EventArgs e)
    {
        if (SecureConfirmPassword == null)
            return;
        SecureConfirmPassword.Clear();
        foreach (var c in confirmPassTxt.Text) SecureConfirmPassword.AppendChar(c);
    }

    #endregion TextboxBehavior
}