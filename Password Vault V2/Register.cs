using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Password_Vault_V2;

public partial class Register : UserControl
{
    public Register()
    {
        InitializeComponent();
    }

    private static bool _isAnimating;

    public static bool CheckPasswordValidity(IReadOnlyCollection<char> password, IReadOnlyCollection<char>? password2 = null)
    {
        if (password.Count is 24 or > 120)
            return false;

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return false;

        if (password.Any(char.IsWhiteSpace) || (password2 != null && (password2.Any(char.IsWhiteSpace) || !password.SequenceEqual(password2))))
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

        var passwordBytes = Encoding.UTF8.GetBytes(passTxt.Text);
        var confirmPasswordBytes = Encoding.UTF8.GetBytes(confirmPassTxt.Text);

        var pinnedPassword = GCHandle.Alloc(passwordBytes, GCHandleType.Pinned);

        var pinnedConfirmPassword = GCHandle.Alloc(confirmPasswordBytes, GCHandleType.Pinned);

        var size = passwordBytes.Length;

        var finalSize = size * sizeof(byte);

        var userExists = Authentication.UserExists(userName);

        try
        {
            if (!userExists)
            {
                DisableUi();

                await RegisterAsync(userName, passwordBytes, confirmPasswordBytes);

            }
            else
            {
                throw new ArgumentException("Username already exists.", nameof(userName));
            }
        }
        finally
        {
            Crypto.CryptoUtilities.ClearMemory(finalSize, pinnedPassword.AddrOfPinnedObject(), finalSize, pinnedConfirmPassword.AddrOfPinnedObject());
            pinnedPassword.Free();
            pinnedConfirmPassword.Free();
        }
    }

    private void DisableUi()
    {
        foreach (Control c in RegisterBox.Controls)
        {
            if (c == userLbl || c == passLbl || c == confirmPassLbl || c == statusLbl || c == outputLbl)
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
        var confirmChars = Encoding.UTF8.GetChars(confirmPassword);
        ValidateUsernameAndPassword(username, ref passwordChars, ref confirmChars);

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
        _isAnimating = false;

        MessageBox.Show("Registration successful! Make sure you do NOT forget your password or you will lose access " +
                        "to all of your files.", "Registration Complete", MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        EnableUi();
        userTxt.Clear();
        Crypto.CryptoUtilities.ClearMemory(passTxt.Text, confirmPassTxt.Text);
        passTxt.Clear();
        confirmPassTxt.Clear();
        Crypto.CryptoUtilities.ClearMemory(password);
        Crypto.CryptoUtilities.ClearMemory(confirmPassword);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
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

            outputLbl.ForeColor = Color.WhiteSmoke;
            _isAnimating = false;
            ErrorLogging.ErrorLog(ex);

            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            outputLbl.Text = "Idle...";
        }
    }

    private async void StartAnimation()
    {
        _isAnimating = true;
        await AnimateLabel();
    }

    private async Task AnimateLabel()
    {
        while (_isAnimating)
        {
            outputLbl.Text = @"Creating account";

            for (var i = 0; i < 4; i++)
            {
                outputLbl.Text += @".";
                await Task.Delay(400);
            }
        }
    }

    private void ShowPasswordCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        passTxt.UseSystemPasswordChar = !ShowPasswordCheckBox.Checked;
        confirmPassTxt.UseSystemPasswordChar = !ShowPasswordCheckBox.Checked;
    }
}