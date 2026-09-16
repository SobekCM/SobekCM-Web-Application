#region Using directives

using SobekCM.Core.Configuration.Authentication;
using SobekCM.Library.UI;
using System;
using System.Collections.Generic;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> Static registry of all configured authentication providers, keyed by provider code.
    /// Matches the existing static-gateway convention (<see cref="UI_ApplicationCache_Gateway"/>,
    /// Engine_ApplicationCache_Gateway) rather than dependency injection, which this codebase doesn't use </summary>
    public static class Authentication_Provider_Gateway
    {
        private static Dictionary<string, ICredential_Authentication_Provider> credentialProviders;
        private static Dictionary<string, IFederated_Authentication_Provider> federatedProviders;

        /// <summary> Rebuild the provider registry from the current <see cref="Authentication_Configuration"/> </summary>
        /// <remarks> Called from <see cref="UI_ApplicationCache_Gateway.ResetAll"/> alongside the rest of the
        /// application-level cache refresh, so a config file change is picked up the same way any other
        /// setting change is </remarks>
        public static void RefreshAll()
        {
            credentialProviders = new Dictionary<string, ICredential_Authentication_Provider>(StringComparer.OrdinalIgnoreCase)
            {
                { "sobek", new Sobek_Authentication_Provider() }
            };

            federatedProviders = new Dictionary<string, IFederated_Authentication_Provider>(StringComparer.OrdinalIgnoreCase);

            Authentication_Configuration config = UI_ApplicationCache_Gateway.Configuration?.Authentication;
            if (config == null)
                return;

            if (config.Oidc != null)
            {
                foreach (Oidc_Configuration oidcConfig in config.Oidc)
                {
                    if ((oidcConfig.Enabled) && (!String.IsNullOrEmpty(oidcConfig.Provider_Code)))
                        federatedProviders[oidcConfig.Provider_Code] = new Oidc_Authentication_Provider(oidcConfig);
                }
            }

            if (config.Saml != null)
            {
                foreach (Saml_Configuration samlConfig in config.Saml)
                {
                    if ((samlConfig.Enabled) && (!String.IsNullOrEmpty(samlConfig.Provider_Code)))
                        federatedProviders[samlConfig.Provider_Code] = new Saml_Authentication_Provider(samlConfig);
                }
            }
        }

        private static void ensure_built()
        {
            if (credentialProviders == null)
                RefreshAll();
        }

        /// <summary> Get a credential-based provider (e.g. "sobek") by provider code </summary>
        public static ICredential_Authentication_Provider Get_Credential_Provider(string ProviderCode)
        {
            ensure_built();
            if ((String.IsNullOrEmpty(ProviderCode)) || (!credentialProviders.TryGetValue(ProviderCode, out ICredential_Authentication_Provider provider)))
                return null;
            return provider;
        }

        /// <summary> Get a federated (OIDC/SAML) provider by provider code </summary>
        public static IFederated_Authentication_Provider Get_Federated_Provider(string ProviderCode)
        {
            ensure_built();
            if ((String.IsNullOrEmpty(ProviderCode)) || (!federatedProviders.TryGetValue(ProviderCode, out IFederated_Authentication_Provider provider)))
                return null;
            return provider;
        }

        /// <summary> All currently enabled federated providers, for rendering "Sign in with X" links
        /// on the logon page </summary>
        public static IEnumerable<IFederated_Authentication_Provider> All_Enabled_Federated_Providers
        {
            get
            {
                ensure_built();
                return federatedProviders.Values;
            }
        }
    }
}
