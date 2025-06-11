namespace Password_Vault_V2;

public static class UiController
{
    internal static class Animations
    {
        /// <summary>
        ///     Asynchronously animates the text color of a label to create a rainbow effect.
        /// </summary>
        /// <param name="label">The label to animate.</param>
        /// <param name="token"></param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RainbowLabel(Control label, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    label.ForeColor = Color.FromArgb(
                        Crypto.CryptoUtilities.BoundedInt(0, 255),
                        Crypto.CryptoUtilities.BoundedInt(0, 255),
                        Crypto.CryptoUtilities.BoundedInt(0, 255)
                    );

                    await Task.Delay(125, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when token is cancelled
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ErrorLogging.ErrorLog(ex);
            }
        }


        /// <summary>
        ///     Asynchronously animates the specified <see cref="Label" /> by appending a varying number
        ///     of dots to the given text in a looping pattern (e.g., "Creating account.", "Creating account..").
        /// </summary>
        /// <param name="label">The <see cref="Label" /> control to animate.</param>
        /// <param name="text">The base text to display before the animated dots.</param>
        /// <param name="token">
        ///     A <see cref="CancellationToken" /> used to request cancellation of the animation.
        /// </param>
        /// <returns>A <see cref="Task" /> that represents the asynchronous animation operation.</returns>
        /// <remarks>
        ///     The method continuously appends 1 to 4 dots to the <paramref name="text" /> at 400ms intervals
        ///     unless cancellation is requested. It handles <see cref="OperationCanceledException" /> silently,
        ///     and logs unexpected exceptions via <see cref="ErrorLogging.ErrorLog(Exception)" />.
        /// </remarks>
        /// <example>
        ///     <code>
        /// var cts = new CancellationTokenSource();
        /// await UiController.Animations.AnimateLabel(myLabel, "Loading", cts.Token);
        /// </code>
        /// </example>
        /// <exception cref="Exception">
        ///     Displays a message box and logs unexpected exceptions that occur during animation.
        /// </exception>
        public static async Task AnimateLabel(Label label, string text, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                    for (var i = 0; i < 4; i++)
                    {
                        if (token.IsCancellationRequested)
                            return;

                        label.Text = text + new string('.', i + 1);
                        await Task.Delay(400, token);
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

    internal static class LogicMethods
        {
        /// <summary>
        /// Enables the specified UI controls by setting their <see cref="Control.Enabled"/> property to <c>true</c>.
        /// </summary>
        /// <param name="c">An array of <see cref="Control"/> objects to enable.</param>
        /// <remarks>
        /// Useful for re-enabling user interface elements after a disabled state, such as after a background operation completes.
        /// </remarks>
        public static void EnableUi(params Control[] c)
        {
            foreach (var control in c) control.Enabled = true;
        }

        /// <summary>
        /// Disables the specified UI controls by setting their <see cref="Control.Enabled"/> property to <c>false</c>.
        /// </summary>
        /// <param name="c">An array of <see cref="Control"/> objects to disable.</param>
        /// <remarks>
        /// Useful for preventing user interaction during background operations or validation checks.
        /// </remarks>
        public static void DisableUi(params Control[] c)
        {
            foreach (var control in c) control.Enabled = false;
        }

        /// <summary>
        /// Makes the specified UI controls visible by setting their <see cref="Control.Visible"/> property to <c>true</c>.
        /// </summary>
        /// <param name="c">An array of <see cref="Control"/> objects to show.</param>
        /// <remarks>
        /// Use this method to programmatically reveal UI components based on user actions or application state.
        /// </remarks>
        public static void EnableVisibility(params Control[] c)
        {
            foreach (var control in c) control.Visible = true;
        }

        /// <summary>
        /// Hides the specified UI controls by setting their <see cref="Control.Visible"/> property to <c>false</c>.
        /// </summary>
        /// <param name="c">An array of <see cref="Control"/> objects to hide.</param>
        /// <remarks>
        /// Use this method to dynamically hide UI components that should not be shown in a given context.
        /// </remarks>
        public static void DisableVisibility(params Control[] c)
        {
            foreach (var control in c) control.Visible = false;
        }
    }
}