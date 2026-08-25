using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SobekCM.Core.Configuration.Authentication;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Library.Authentication;
using SobekCM.Tools;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;
using System;

namespace SobekCM.Startup
{
    /// <summary> Registers one OIDC/SAML authentication scheme per configured provider in
    /// Authentication_Configuration, and completes the SobekCM User_Object sign-in for each on a
    /// successful callback. See IFederated_Authentication_Provider's remarks for the full flow --
    /// everything downstream of a successful sign-in uses the existing User_Object-in-session model,
    /// not a ClaimsPrincipal. </summary>
    public static class FederatedAuthenticationStartup
    {
        /// <summary> Used only to carry short-lived state (nonce/returnUrl) across the redirect
        /// round-trip for the OIDC/SAML providers registered below -- never read anywhere else in
        /// the app as identity. </summary>
        private const string AUTH_CORRELATION_SCHEME = "SobekCM.AuthCorrelation";

        /// <summary> Set by Program.cs once the real value exists, after app.Build(). Read by the
        /// SAML notification below, which (unlike the OIDC handler's Events) isn't handed an
        /// HttpContext directly. </summary>
        public static IHttpContextAccessor HttpContextAccessor { get; set; }

        public static void Configure(WebApplicationBuilder builder)
        {
            Authentication_Configuration authConfig = Engine_ApplicationCache_Gateway.Configuration?.Authentication;

            AuthenticationBuilder authBuilder = builder.Services.AddAuthentication();
            authBuilder.AddCookie(AUTH_CORRELATION_SCHEME, options =>
            {
                options.Cookie.Name = AUTH_CORRELATION_SCHEME;
            });

            if (authConfig == null)
                return;

            foreach (Oidc_Configuration oidcConfig in authConfig.Oidc)
            {
                if ((!oidcConfig.Enabled) || (String.IsNullOrEmpty(oidcConfig.Provider_Code)))
                    continue;

                string providerCode = oidcConfig.Provider_Code;
                authBuilder.AddOpenIdConnect(providerCode, options =>
                {
                    options.SignInScheme = AUTH_CORRELATION_SCHEME;
                    options.Authority = oidcConfig.Authority;
                    options.ClientId = oidcConfig.ClientId;
                    options.ClientSecret = oidcConfig.ClientSecret;
                    options.ResponseType = "code";
                    options.CallbackPath = "/my/oidc/" + providerCode + "/callback";
                    options.SaveTokens = false;

                    // Keep raw OIDC claim names (e.g. "sub", "given_name") instead of ASP.NET Core's
                    // default long-URI claim-type remapping, matching what admins configure in
                    // sobekcm_authentication.config's <mapping Name="..."> entries
                    options.MapInboundClaims = false;

                    options.Scope.Add("profile");
                    options.Scope.Add("email");

                    options.Events = new OpenIdConnectEvents
                    {
                        // Fires after the OIDC handler has already validated the token (signature,
                        // issuer, audience, nonce). This is the ONLY place Complete_SignIn is called
                        // for OIDC — the callback request never reaches QueryInitializer/a viewer,
                        // since UseAuthentication() intercepts CallbackPath directly. See
                        // IFederated_Authentication_Provider's remarks for the full explanation.
                        OnTokenValidated = async ctx =>
                        {
                            var provider = Authentication_Provider_Gateway.Get_Federated_Provider(providerCode) as Oidc_Authentication_Provider;
                            var tracer = new Custom_Tracer();
                            User_Object user = provider != null ? await provider.Complete_SignIn(ctx.Principal, tracer) : null;

                            if (user == null)
                            {
                                ctx.Fail("Unable to establish a user account for this identity");
                                ExceptionLog_Gateway.Append("\n\n" + tracer.Text_Trace + "\n\n");
                                return;
                            }

                            ctx.HttpContext.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));

                            string returnUrl = (ctx.Properties?.Items != null) && ctx.Properties.Items.TryGetValue("returnUrl", out string r) && (!String.IsNullOrEmpty(r))
                                ? r : "/";
                            ctx.Properties.RedirectUri = returnUrl;
                        }
                    };
                });
            }

            // SAML providers, via Sustainsys.Saml2. Verified against the actual installed package
            // (Sustainsys.Saml2.AspNetCore2 2.11.0) rather than guessed, but the returnUrl round-trip
            // through CommandResult.RelayData specifically has not been confirmed against a real IdP —
            // Saml2Handler doesn't derive from ASP.NET Core's RemoteAuthenticationHandler<T>, so it
            // doesn't necessarily propagate AuthenticationProperties.Items the same way the OIDC
            // handler does. Verify this with a real IdP before relying on it.
            foreach (Saml_Configuration samlConfig in authConfig.Saml)
            {
                if ((!samlConfig.Enabled) || (String.IsNullOrEmpty(samlConfig.Provider_Code)))
                    continue;

                string providerCode = samlConfig.Provider_Code;
                authBuilder.AddSaml2(providerCode, options =>
                {
                    options.SignInScheme = AUTH_CORRELATION_SCHEME;

                    // ModulePath defaults to a fixed "/Saml2" shared across every scheme, which would
                    // collide as soon as a second SAML provider is configured (all of them would answer
                    // to the same Acs/Metadata/etc. URLs). Scope it per provider, mirroring the OIDC
                    // CallbackPath pattern above - but NOT to "/my/saml/{ProviderCode}" alone: that's the
                    // exact same path Saml_Landing_MySobekViewer's "Sign in" link already points at to
                    // *begin* the login (see UrlWriterHelper's SAML_Landing case). Since Sustainsys.Saml2's
                    // Saml2Handler (registered via UseAuthentication(), which runs before this app's own
                    // routing - see PrettyUrl_Rewrite's comment below) claims every request whose path
                    // starts with ModulePath, an exact-match collision means Sustainsys itself swallows
                    // the landing click before Saml_Landing_MySobekViewer/ChallengeAsync ever runs - it
                    // falls back to its own default command (observed as an unwanted metadata-file
                    // download) instead of redirecting to the IdP. The "/sso" suffix keeps ModulePath a
                    // strict sub-path so the bare landing URL falls through to this app's own routing.
                    // This changes the Reply URL (ACS) each IdP must have registered to
                    // "https://<host>/my/saml/{ProviderCode}/sso/Acs".
                    options.SPOptions.ModulePath = "/my/saml/" + providerCode + "/sso";

                    options.SPOptions.EntityId = new EntityId(samlConfig.EntityId);
                    options.IdentityProviders.Add(new IdentityProvider(new EntityId(samlConfig.IdpEntityId), options.SPOptions)
                    {
                        MetadataLocation = samlConfig.IdpMetadataUrl,
                        LoadMetadata = true
                    });

                    // Sustainsys.Saml2's older "Notifications" delegate API doesn't hand this event an
                    // HttpContext (unlike the OIDC handler's Events), so the ambient HttpContext is
                    // resolved via IHttpContextAccessor instead.
                    options.Notifications.AcsCommandResultCreated = (commandResult, samlResponse) =>
                    {
                        HttpContext currentContext = HttpContextAccessor?.HttpContext;
                        if ((commandResult.Principal == null) || (currentContext == null))
                            return;

                        var provider = Authentication_Provider_Gateway.Get_Federated_Provider(providerCode) as Saml_Authentication_Provider;
                        if (provider == null)
                            return;

                        var tracer = new Custom_Tracer();
                        User_Object user = provider.Complete_SignIn(commandResult.Principal, tracer).GetAwaiter().GetResult();
                        if (user == null)
                            return;

                        currentContext.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));

                        if ((commandResult.RelayData != null) && commandResult.RelayData.TryGetValue("returnUrl", out string returnUrl) && (!String.IsNullOrEmpty(returnUrl)))
                            commandResult.Location = new Uri(returnUrl);
                    };
                });
            }
        }
    }
}
