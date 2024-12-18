namespace Password_Vault_V2;

public partial class CryptoSettings : UserControl
{
    public CryptoSettings()
    {
        InitializeComponent();
    }
    public static int Iterations;
    public static double MemSize;
    public static int Parallelism;
    private static readonly double MemConstant = Math.Pow(1024, 2);
    private static readonly ToolTip Tip = new()
    {
        AutoPopDelay = 5000,
        InitialDelay = 1000,
        ToolTipIcon = ToolTipIcon.Info,
        Active = true,
        AutomaticDelay = 1000,
        IsBalloon = true,
    };
    private CancellationTokenSource _tokenSource = new();
    public CancellationToken Token => _tokenSource.Token;
    private async void SaveBtn_Click(object sender, EventArgs e)
    {
        try
        {
            MessageBox.Show("Saving settings...", "Saving", MessageBoxButtons.OK, MessageBoxIcon.Information);
            outputLbl.Text = "Saving settings";
            AnimateLabel();
            Iterations = (int)IterationsNumberBox.Value;
            MemSize = (double)MemorySizeNumberBox.Value * MemConstant / Math.Pow(1024, 2);
            Parallelism = (int)ParallelismNumberBox.Value;
            Settings.Default.Iterations = Iterations;
            Settings.Default.MemorySize = MemSize;
            Settings.Default.Parallelism = Parallelism;
            Settings.Default.Save();

            await Task.Delay(3000, Token);
            await _tokenSource.CancelAsync();

            if (_tokenSource.IsCancellationRequested)
                _tokenSource = new CancellationTokenSource();
            outputLbl.ForeColor = Color.LimeGreen;
            outputLbl.Text = @"Saved Successfully";
            MessageBox.Show("Settings saved successfully.", "Success", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            outputLbl.ForeColor = Color.WhiteSmoke;
            outputLbl.Text = @"Idle...";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            ErrorLogging.ErrorLog(ex);
            outputLbl.ForeColor = Color.WhiteSmoke;
            outputLbl.Text = @"Idle...";
        }
    }

    private async void AnimateLabel()
    {
        await UiController.Animations.AnimateLabel(outputLbl, "Saving Settings", Token);
    }

    private void CryptoSettings_Load(object sender, EventArgs e)
    {
        // Validate Iterations
        if (Settings.Default.Iterations >= IterationsNumberBox.Minimum && Settings.Default.Iterations <= IterationsNumberBox.Maximum)
            IterationsNumberBox.Value = Settings.Default.Iterations;
        else
            IterationsNumberBox.Value = IterationsNumberBox.Minimum;

        // Validate MemorySize
        if ((decimal)Settings.Default.MemorySize >= MemorySizeNumberBox.Minimum && (decimal)Settings.Default.MemorySize <= MemorySizeNumberBox.Maximum)
            MemorySizeNumberBox.Value = (decimal)Settings.Default.MemorySize;
        else
            MemorySizeNumberBox.Value = MemorySizeNumberBox.Minimum;

        // Validate Parallelism
        if (Settings.Default.Parallelism >= ParallelismNumberBox.Minimum && Settings.Default.Parallelism <= ParallelismNumberBox.Maximum)
            ParallelismNumberBox.Value = Settings.Default.Parallelism;
        else
            ParallelismNumberBox.Value = ParallelismNumberBox.Minimum;

        MessageBox.Show("Make sure you save your settings, they won't apply unless you save them.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void IterationsNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Tip.SetToolTip(IterationsNumberBox, "A higher iteration count will create a more secure hash with the tradeoff of having more of a " +
            "strain on your CPU.");
    }

    private void ParallelismNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Tip.SetToolTip(ParallelismNumberBox, "This value should be the amount of cores your CPU has multiplied by 2. For example, if your" +
            " CPU had 12 cores, multiply that by 2 to get 24. Therefore this value should be 24. This value is dependent on your CPU.");
    }

    private void MemorySizeNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Tip.SetToolTip(MemorySizeNumberBox, "A higher memory amount will create a stronger hash at the expense of putting a strain on " +
            "your PC. Do not exceed the max amount of RAM that your system has. For example, if you have 16GB of RAM, this value should be" +
            " 12GB or less.");
    }
}