#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.Security.Claims;
using System.Threading.Tasks;

#endregion

namespace SobekCM.Library.Authentication
{
    /// <summary> Common surface shared by every authentication provider (internal Sobek, LDAP, OIDC, SAML, ...) </summary>
    public interface IAuthentication_Provider
    {
        /// <summary> Unique code for this provider — matches the corresponding configuration entry,
        /// and for federated providers is also the ASP.NET Core authentication scheme name and the
        /// "/my/oidc/{Provider_Code}" (or "/my/saml/{Provider_Code}") URL segment </summary>
        string Provider_Code { get; }

        /// <summary> Label displayed to the user on the logon page </summary>
        string Display_Label { get; }

        /// <summary> Flag indicates if this provider is currently enabled </summary>
        bool Enabled { get; }

        /// <summary> Which <see cref="User_Authentication_Type_Enum"/> a user authenticated through
        /// this provider should be tagged with </summary>
        User_Authentication_Type_Enum Authentication_Type { get; }
    }

    /// <summary> An authentication provider that validates a directly-submitted username/password
    /// (e.g. the internal Sobek database, or a future LDAP provider) </summary>
    public interface ICredential_Authentication_Provider : IAuthentication_Provider
    {
        /// <summary> Validate a username/password and return the matching user, or NULL if invalid </summary>
        /// <param name="Username"> Username (or email) as submitted on the logon form </param>
        /// <param name="Password"> Plain-text password as submitted on the logon form </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <returns> Fully built <see cref="User_Object"/> if valid, otherwise NULL </returns>
        User_Object Authenticate(string Username, string Password, Custom_Tracer Tracer);
    }

    /// <summary> An authentication provider backed by a redirect-based external identity provider
    /// (OpenID Connect, SAML). </summary>
    /// <remarks> The two legs of the round-trip happen in very different places, because ASP.NET Core's
    /// remote authentication handlers (the OpenIdConnect handler, Sustainsys.Saml2) intercept their
    /// configured callback path directly in the <c>UseAuthentication()</c> middleware, before the
    /// request ever reaches this app's normal QueryInitializer/MySobekViewer routing:
    /// <list type="bullet">
    /// <item><see cref="Begin_SignIn"/> is called from a normal page request (a MySobekViewer,
    /// e.g. "/my/oidc/{Provider_Code}") to kick off the redirect to the identity provider.</item>
    /// <item><see cref="Complete_SignIn"/> is called from that scheme's token/assertion-validated
    /// event, registered once in Program.cs per configured provider — never from a viewer — with the
    /// already-validated <c>ClaimsPrincipal</c> the protocol library produced. The protocol library's
    /// own cookie-auth identity is discarded after this call; it never reaches the rest of the app.</item>
    /// </list></remarks>
    public interface IFederated_Authentication_Provider : IAuthentication_Provider
    {
        /// <summary> Redirect the browser to this provider's identity provider to begin sign-in </summary>
        /// <param name="Context"> Current HTTP context </param>
        /// <param name="ReturnUrl"> URL to return to once sign-in completes </param>
        Task Begin_SignIn(HttpContext Context, string ReturnUrl);

        /// <summary> Translate an already-validated identity-provider principal into a <see cref="User_Object"/>,
        /// looking up an existing linked account or provisioning a new one </summary>
        /// <param name="Principal"> Validated claims principal produced by the protocol library </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones </param>
        /// <returns> Fully built <see cref="User_Object"/> if successful, otherwise NULL </returns>
        Task<User_Object> Complete_SignIn(ClaimsPrincipal Principal, Custom_Tracer Tracer);
    }
}
