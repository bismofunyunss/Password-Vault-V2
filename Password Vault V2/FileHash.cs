namespace Password_Vault_V2;

public sealed partial class FileHash : UserControl
{
    private static string _fileToHash = string.Empty;

    public FileHash()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Asynchronously computes the SHA3 hash of the specified file's contents.
    /// </summary>
    /// <param name="file">The full path to the file to hash.</param>
    /// <returns>A lowercase hexadecimal string representing the file's SHA3 hash.</returns>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the caller does not have the required permission.</exception>
    private static async Task<string> CalculateHash(string file)
    {
        using var ms = new MemoryStream();
        await using (var fs = new FileStream(file, FileMode.Open,
                         FileAccess.Read))
        {
            await fs.CopyToAsync(ms);
        }

        var hashBytes = Crypto.HashingMethods.Sha3Hash(ms.ToArray());
        var hashHexString = DataConversionHelpers.ByteArrayToHexString(hashBytes).ToLower();

        return hashHexString;
    }

    /// <summary>
    /// Handles the click event for the Calculate Hash button. Computes and displays
    /// the SHA3 hash of the previously selected file.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private async void CalculateHashBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new InvalidOperationException("No user is currently logged in.");

            var result = await CalculateHash(_fileToHash);
            hashoutputtxt.Text = result;
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            hashoutputtxt.Text = string.Empty;
        }
    }

    /// <summary>
    /// Handles the click event for the Import File button. Opens a file dialog for the user
    /// to select a file and stores the selected file path for hashing.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void HashImportFile_Click(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(UserFileManager.CurrentLoggedInUser))
                throw new Exception("No user is currently logged in.");

            using var openFileDialog = new OpenFileDialog();
            openFileDialog.FilterIndex = 1;
            openFileDialog.ShowHiddenFiles = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.InitialDirectory =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var selectedFileName = openFileDialog.FileName;

            _fileToHash = selectedFileName;

            if (string.IsNullOrEmpty(selectedFileName))
                return;

            var fileName = Path.GetFileName(selectedFileName);
            filenamelbl.Text = $@"File Name: {fileName}";
        }
        catch (Exception ex)
        {
            ErrorLogging.ErrorLog(ex);
            MessageBox.Show(ex.Message, @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}