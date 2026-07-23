#region Using directives

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using SobekCM.Core.Configuration.Authentication;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> SAML federated authentication provider — one instance per configured IdP, using
    /// Sustainsys.Saml2's ASP.NET Core authentication handler for the AuthnRequest/assertion protocol
    /// mechanics (registered once per provider in Program.cs, scheme name = Provider_Code); this class
    /// only supplies the config, the sign-in trigger, and the claims -> User_Object translation. </summary>
    /// <remarks> Code-complete but not verified end-to-end without a real IdP metadata/certificate —
    /// see the plan's verification notes. The exact Sustainsys.Saml2 event-hook name used in Program.cs
    /// to reach this class's Complete_SignIn depends on the installed package version; confirm at
    /// deployment/integration time. </remarks>
    public class Saml_Authentication_Provider : IFederated_Authentication_Provider
    {
        private readonly Saml_Configuration config;

        /// <summary> Constructor for a new instance of the Saml_Authentication_Provider class </summary>
        /// <param name="Config"> Configuration for this specific SAML provider </param>
        public Saml_Authentication_Provider(Saml_Configuration Config)
        {
            config = Config;
        }

        /// <summary> Unique code for this provider — also the ASP.NET Core authentication scheme name </summary>
        public string Provider_Code => config.Provider_Code;

        /// <summary> Label displayed to the user on the logon page </summary>
        public string Display_Label => config.Display_Label;

        /// <summary> Flag indicates if this provider is currently enabled </summary>
        public bool Enabled => config.Enabled;

        /// <summary> Users validated by this provider are tagged as Saml-authenticated </summary>
        public User_Authentication_Type_Enum Authentication_Type => User_Authentication_Type_Enum.Saml;

        /// <summary> Redirect the browser to this provider's identity provider to begin sign-in. The
        /// scheme registered under <see cref="Provider_Code"/> in Program.cs (Sustainsys.Saml2) builds
        /// and sends the AuthnRequest; its Assertion Consumer Service (ACS) path is intercepted entirely
        /// by the UseAuthentication() middleware, never reaching this app's normal page routing </summary>
        public Task Begin_SignIn(HttpContext Context, string ReturnUrl)
        {
            AuthenticationProperties properties = new AuthenticationProperties
            {
                Items = { ["returnUrl"] = ReturnUrl }
            };
            return Context.ChallengeAsync(Provider_Code, properties);
        }

        /// <summary> Translate the validated SAML principal into a <see cref="User_Object"/>. Called from
        /// this provider's scheme notification/event registered in Program.cs — never from a page/viewer,
        /// since the ACS callback never reaches normal routing (see interface remarks) </summary>
        public Task<User_Object> Complete_SignIn(ClaimsPrincipal Principal, Custom_Tracer Tracer)
        {
            // Sustainsys.Saml2 surfaces the SAML2 NameID as the standard ClaimTypes.NameIdentifier claim
            string subjectId = Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Dictionary<string, string> claims = new Dictionary<string, string>();
            foreach (Claim claim in Principal.Claims)
                claims[claim.Type] = claim.Value;

            User_Object user = Federated_User_Provisioning_Helper.Get_Or_Provision_User(
                Provider_Code, subjectId, Authentication_Type, claims, config.Get_User_Object_Mapping, config.Constants, Tracer);

            return Task.FromResult(user);
        }
    }
}
