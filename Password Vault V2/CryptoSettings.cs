using System.Drawing.Drawing2D;

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
        AutoPopDelay = 0,
        InitialDelay = 0,
        ToolTipIcon = ToolTipIcon.Info,
        Active = true,
        AutomaticDelay = 1000,
        IsBalloon = true,
        ShowAlways = true
    };

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
    }

    /// <summary>
    /// Displays a tooltip when the value of the iterations number box changes.
    /// Provides context about the impact of iteration count on CPU usage.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void IterationsNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Iterations = (int)IterationsNumberBox.Value;
        MemSize = (double)MemorySizeNumberBox.Value * MemConstant / Math.Pow(1024, 2);
        Parallelism = (int)ParallelismNumberBox.Value;
        Settings.Default.Iterations = Iterations;
        Settings.Default.MemorySize = MemSize;
        Settings.Default.Parallelism = Parallelism;
        Settings.Default.Save();
    }

    /// <summary>
    /// Displays a tooltip when the value of the parallelism number box changes.
    /// Guides the user on setting the parallelism based on CPU core count.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void ParallelismNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Iterations = (int)IterationsNumberBox.Value;
        MemSize = (double)MemorySizeNumberBox.Value * MemConstant / Math.Pow(1024, 2);
        Parallelism = (int)ParallelismNumberBox.Value;
        Settings.Default.Iterations = Iterations;
        Settings.Default.MemorySize = MemSize;
        Settings.Default.Parallelism = Parallelism;
        Settings.Default.Save();
    }

    /// <summary>
    /// Displays a tooltip when the value of the memory size number box changes.
    /// Advises the user on setting a memory size that balances security and system performance.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void MemorySizeNumberBox_ValueChanged(object sender, EventArgs e)
    {
        Iterations = (int)IterationsNumberBox.Value;
        MemSize = (double)MemorySizeNumberBox.Value * MemConstant / Math.Pow(1024, 2);
        Parallelism = (int)ParallelismNumberBox.Value;
        Settings.Default.Iterations = Iterations;
        Settings.Default.MemorySize = MemSize;
        Settings.Default.Parallelism = Parallelism;
        Settings.Default.Save();
    }

    private void FipsModeCheckbox_MouseHover(object sender, EventArgs e)
    {
        Tip.SetToolTip(FipsModeCheckbox, "Enable FIPS certified algorithms.");
    }

    private void FipsModeCheckbox_CheckedChanged(object sender, EventArgs e)
    {
        if (FipsModeCheckbox.Checked)
        {
            Settings.Default.FIPS = true;
            Settings.Default.Save();
            IterationsNumberBox.Minimum = 10000;
            IterationsNumberBox.Maximum = 2000000;
            IterationsNumberBox.Increment = 10000;
            IterationsNumberBox.Value = 10000;
            MemorySizeNumberBox.Enabled = false;
            ParallelismNumberBox.Enabled = false;
        }
        else
        {
            Settings.Default.FIPS = false;
            Settings.Default.Save();
            IterationsNumberBox.Minimum = 1;
            IterationsNumberBox.Maximum = 100;
            IterationsNumberBox.Increment = 1;
            IterationsNumberBox.Value = 1;
            MemorySizeNumberBox.Enabled = true;
            ParallelismNumberBox.Enabled = true;
        }
    }

    private void CryptoSettings_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.Clear(this.BackColor);  // Clear previous drawings
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
    }
}