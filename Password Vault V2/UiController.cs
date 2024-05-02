using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Password_Vault_V2
{
    public static class UiController
    {
        public static class Animations
        {
            /// <summary>
            /// Asynchronously animates the text color of a label to create a rainbow effect.
            /// </summary>
            /// <param name="label">The label to animate.</param>
            /// <returns>A task representing the asynchronous operation.</returns>
            public static async Task RainbowLabel(Control label, CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        label.ForeColor = Color.FromArgb(
                            Crypto.CryptoUtilities.BoundedInt(0, 255),
                            Crypto.CryptoUtilities.BoundedInt(0, 255),
                            Crypto.CryptoUtilities.BoundedInt(0, 255)
                        );

                        await Task.Delay(125, token); // Use RainbowLabelToken for delay
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }

            public static async Task AnimateLabel(Label label, string text, CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        label.Text = text;
                        for (var i = 0; i < 4; i++)
                        {
                            label.Text += @".";
                            await Task.Delay(400, token);
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
            }
        }

        public static class LogicMethods
        {
            public static void EnableUi(params Control[] c)
            {
                foreach (var control in c)
                {
                    control.Enabled = true;
                }
            }

            public static void DisableUi(params Control[] c)
            {
                foreach (var control in c)
                {
                    control.Enabled = false;
                }
            }

            public static void EnableVisibility(params Control[] c)
            {
                foreach (var control in c)
                {
                    control.Visible = true;
                }
            }

            public static void DisableVisibility(params Control[] c)
            {
                foreach (var control in c)
                {
                    control.Visible = false;
                }
            }
        }
    }
}
