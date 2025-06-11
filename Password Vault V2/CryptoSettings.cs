namespace Password_Vault_V2;

public partial class CryptoSettings : UserControl
{
    public CryptoSettings()
    {
        InitializeComponent();
    }
    /// <summary>
    /// Gets or sets the number of iterations used for the key derivation function.
    /// </summary>
    public static int Iterations;

    /// <summary>
    /// Gets or sets the memory size (in MB) used for the key derivation function.
    /// </summary>
    public static double MemSize;

    /// <summary>
    /// Gets or sets the degree of parallelism used for the key derivation function.
    /// </summary>
    public static int Parallelism;

    /// <summary>
    /// Represents the constant multiplier used to convert megabytes to bytes.
    /// </summary>
    private static readonly double MemConstant = Math.Pow(1024, 2);

    /// <summary>
    /// Represents the tooltip configuration used for displaying help hints on the form.
    /// </summary>
    private static readonly ToolTip Tip = new()
    {
        AutoPopDelay = 5000,
        InitialDelay = 1000,
        ToolTipIcon = ToolTipIcon.Info,
        Active = true,
        AutomaticDelay = 1000,
        IsBalloon = true,
    };


    /// <summary>
    /// A cancellation token source used to cancel asynchronous operations.
    /// </summary>
    private CancellationTokenSource _tokenSource = new();

    /// <summary>
    /// Gets the current cancellation token for ongoing async operations.
    /// </summary>
    private CancellationToken Token => _tokenSource.Token;

    /// <summary>
    /// Handles the click event for the Save button.
    /// Saves the cryptographic settings and updates the UI accordingly.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
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

            await Task.Delay(3000, Token).ConfigureAwait(false);
            await _tokenSource.CancelAsync().ConfigureAwait(false);

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

    /// <summary>
    /// Animates the output label with a "Saving Settings" message.
    /// </summary>
    private async void AnimateLabel()
    {
        await UiController.Animations.AnimateLabel(outputLbl, "Saving Settings", Token).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the load event of the CryptoSettings form.
    /// Initializes the input fields with saved values or their minimum allowed values.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
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

    /// <summary>
    /// Displays a tooltip when the value of the iterations number box changes.
    /// Provides context about the impact of iteration count on CPU usage.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void IterationsNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Tip.SetToolTip(IterationsNumberBox, "A higher iteration count will create a more secure hash with the tradeoff of having more of a " +
            "strain on your CPU.");
    }

    /// <summary>
    /// Displays a tooltip when the value of the parallelism number box changes.
    /// Guides the user on setting the parallelism based on CPU core count.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void ParallelismNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Tip.SetToolTip(ParallelismNumberBox, "This value should be the amount of cores your CPU has multiplied by 2. For example, if your" +
            " CPU had 12 cores, multiply that by 2 to get 24. Therefore this value should be 24. This value is dependent on your CPU.");
    }

    /// <summary>
    /// Displays a tooltip when the value of the memory size number box changes.
    /// Advises the user on setting a memory size that balances security and system performance.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void MemorySizeNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Tip.SetToolTip(MemorySizeNumberBox, "A higher memory amount will create a stronger hash at the expense of putting a strain on " +
            "your PC. Do not exceed the max amount of RAM that your system has. For example, if you have 16GB of RAM, this value should be" +
            " 12GB or less.");
    }
}