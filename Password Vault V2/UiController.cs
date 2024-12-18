namespace Password_Vault_V2;

public static class UiController
{
    public static class Animations
    {
        /// <summary>
        ///     Asynchronously animates the text color of a label to create a rainbow effect.
        /// </summary>
        /// <param name="label">The label to animate.</param>
        /// <param name="token"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RainbowLabel(Control label, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
                try
                {
                    label.ForeColor = Color.FromArgb(
                        Crypto.CryptoUtilities.BoundedInt(0, 255),
                        Crypto.CryptoUtilities.BoundedInt(0, 255),
                        Crypto.CryptoUtilities.BoundedInt(0, 255)
                    );

                    await Task.Delay(125, token); // Use RainbowLabelToken for delay
                }
                catch (OperationCanceledException)
                {
                    // Ignore.
                }
                catch (Exception ex)
                {
                    ErrorLogging.ErrorLog(ex);
                    return;
                }
        }

        public static async Task AnimateLabel(Label label, string text, CancellationToken token)
        {
            try
            {
                while (true)
                {
                    label.Text = text;
                    for (var i = 0; i < 4; i++)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        label.Text += @".";
                        await Task.Delay(400, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error has occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ErrorLogging.ErrorLog(ex);
            }
        }
    }

public static class LogicMethods
    {
        public static void EnableUi(params Control[] c)
        {
            foreach (var control in c) control.Enabled = true;
        }

        public static void DisableUi(params Control[] c)
        {
            foreach (var control in c) control.Enabled = false;
        }

        public static void EnableVisibility(params Control[] c)
        {
            foreach (var control in c) control.Visible = true;
        }

        public static void DisableVisibility(params Control[] c)
        {
            foreach (var control in c) control.Visible = false;
        }
    }
}