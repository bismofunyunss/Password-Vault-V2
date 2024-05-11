namespace Password_Vault_V2;

public partial class FileHash : UserControl
{
    private static string _fileToHash = string.Empty;

    public FileHash()
    {
        InitializeComponent();
    }

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

    private async void CalculateHashBtn_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
                throw new Exception("No user is currently logged in.");

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

    private void HashImportFile_Click(object sender, EventArgs e)
    {
        try
        {
            if (Authentication.CurrentLoggedInUser == string.Empty)
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