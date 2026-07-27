using System;
using System.Linq;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Computes <see cref="Users.User_Object.Authentication_Source"/>'s human-readable value from
    /// a user's stored <see cref="Users.User_Object.External_Provider_Code"/> plus the currently configured
    /// OIDC/SAML providers - kept independent of any application-cache gateway so it can be called from
    /// any layer (Engine_Library, Library, ...) that already has an <see cref="Authentication_Configuration"/>
    /// in hand </summary>
    public static class Authentication_Source_Helper
    {
        /// <summary> Get the human-readable authentication source description for a user </summary>
        /// <param name="ExternalProviderCode"> User's <see cref="Users.User_Object.External_Provider_Code"/>;
        /// NULL/empty means a native SobekCM account </param>
        /// <param name="Config"> Current authentication configuration (Oidc/Saml provider lists) </param>
        /// <returns> "Registered" for a native account; "OpenID (Label)" / "SAML (Label)" for a federated
        /// account, using the matching provider's Display_Label; or the raw provider code if it no longer
        /// matches any configured OIDC/SAML provider (e.g. removed/renamed since the user last signed in) </returns>
        public static string Get_Authentication_Source(string ExternalProviderCode, Authentication_Configuration Config)
        {
            if (String.IsNullOrEmpty(ExternalProviderCode))
                return "Registered";

            Oidc_Configuration oidcMatch = Config?.Oidc?.FirstOrDefault(provider => String.Equals(provider.Provider_Code, ExternalProviderCode, StringComparison.OrdinalIgnoreCase));
            if (oidcMatch != null)
                return "OpenID (" + oidcMatch.Display_Label + ")";

            Saml_Configuration samlMatch = Config?.Saml?.FirstOrDefault(provider => String.Equals(provider.Provider_Code, ExternalProviderCode, StringComparison.OrdinalIgnoreCase));
            if (samlMatch != null)
                return "SAML (" + samlMatch.Display_Label + ")";

            return ExternalProviderCode;
        }
    }
}
