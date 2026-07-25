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
    /// <summary> OpenID Connect federated authentication provider — one instance per configured IdP.
    /// The redirect/callback protocol mechanics are handled entirely by the ASP.NET Core OpenIdConnect
    /// authentication handler (registered once per provider in Program.cs, scheme name = Provider_Code);
    /// this class only supplies the config, the challenge trigger, and the claims -> User_Object translation. </summary>
    public class Oidc_Authentication_Provider : IFederated_Authentication_Provider
    {
        private readonly Oidc_Configuration config;

        /// <summary> Constructor for a new instance of the Oidc_Authentication_Provider class </summary>
        /// <param name="Config"> Configuration for this specific OIDC provider </param>
        public Oidc_Authentication_Provider(Oidc_Configuration Config)
        {
            config = Config;
        }

        /// <summary> Unique code for this provider — also the ASP.NET Core authentication scheme name </summary>
        public string Provider_Code => config.Provider_Code;

        /// <summary> Label displayed to the user on the logon page </summary>
        public string Display_Label => config.Display_Label;

        /// <summary> Flag indicates if this provider is currently enabled </summary>
        public bool Enabled => config.Enabled;

        /// <summary> Users validated by this provider are tagged as OpenIdConnect-authenticated </summary>
        public User_Authentication_Type_Enum Authentication_Type => User_Authentication_Type_Enum.OpenIdConnect;

        /// <summary> Redirect the browser to this provider's identity provider to begin sign-in.
        /// The scheme registered under <see cref="Provider_Code"/> in Program.cs handles everything
        /// from here — building the authorization URL, PKCE, state/nonce — until its CallbackPath
        /// is hit, which is intercepted entirely by the UseAuthentication() middleware, never
        /// reaching this app's normal page routing </summary>
        public Task Begin_SignIn(HttpContext Context, string ReturnUrl)
        {
            AuthenticationProperties properties = new AuthenticationProperties
            {
                Items = { ["returnUrl"] = ReturnUrl }
            };
            return Context.ChallengeAsync(Provider_Code, properties);
        }

        /// <summary> Translate the validated OIDC principal into a <see cref="User_Object"/>. Called from
        /// the "OnTokenValidated" event registered for this provider's scheme in Program.cs — never from
        /// a page/viewer, since the callback never reaches normal routing (see interface remarks) </summary>
        public Task<User_Object> Complete_SignIn(ClaimsPrincipal Principal, Custom_Tracer Tracer)
        {
            // "sub" is the standard OIDC subject claim; fall back to the ASP.NET Core-mapped
            // NameIdentifier in case MapInboundClaims wasn't disabled for this scheme
            string subjectId = Principal.FindFirst("sub")?.Value ?? Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Dictionary<string, string> claims = new Dictionary<string, string>();
            foreach (Claim claim in Principal.Claims)
                claims[claim.Type] = claim.Value;

            User_Object user = Federated_User_Provisioning_Helper.Get_Or_Provision_User(
                Provider_Code, subjectId, Authentication_Type, claims, config.Get_User_Object_Mapping, config.Constants, Tracer);

            return Task.FromResult(user);
        }
    }
}
