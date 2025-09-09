using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace Password_Vault_V2
{
    internal static class WindowsHello
    {
        internal static async Task<bool> RequestWindowsHelloSignInAsync()
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();

            if (availability != UserConsentVerifierAvailability.Available)
                return false;

            var result = await UserConsentVerifier.RequestVerificationAsync("Please verify your identity");
            return result == UserConsentVerificationResult.Verified;
        }

    }
}
