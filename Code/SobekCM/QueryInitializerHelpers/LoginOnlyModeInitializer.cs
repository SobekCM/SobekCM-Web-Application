using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Tools;
using System;

namespace SobekCM.QueryInitializerHelpers
{
    /// <summary> Phase 6 of the GCS rate-limiting plan: when the whole site has been switched to login-only
    /// (see LoginOnlyMode_Gateway.Site_Requires_Logon), sends an anonymous page request to the logon screen
    /// instead of the page it asked for. </summary>
    /// <remarks> Reuses the existing mechanism UserObjectInitializer already uses for mySobek pages --
    /// switching the request to the logon screen and setting Logon_Required. The logon screen shown in place
    /// on the original URL redirects back to that URL after a successful logon, so nothing else is needed.
    /// <para>Must run after UserObjectInitializer (needs Current_User), and before ItemViewRateLimitInitializer
    /// so a request turned away here is never counted as an item hit -- see QueryInitializer.cs. Only page
    /// requests are affected: data feeds (OAI-PMH, IIIF manifests, JSON, XML, dataset) stay open, since none
    /// of them mint signed GCS URLs and harvesters can't log on anyway. Non-pipeline traffic (/engine, /files,
    /// robots.txt, the health check, static files) never reaches this at all.</para> </remarks>
    public class LoginOnlyModeInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("LoginOnlyModeInitializer.Initialize");

            Navigation_Object currentMode = request.Current_Mode;
            if ((currentMode == null) || (!LoginOnlyMode_Gateway.Site_Requires_Logon()) || (AnonymousRequest.Is_Logged_On(request.Current_User)))
                return QueryInitializerHelperResponse.Successful;

            if ((!is_page_writer(currentMode.Writer_Type)) || (is_always_allowed(currentMode.Mode)))
                return QueryInitializerHelperResponse.Successful;

            tracer.Add_Trace("LoginOnlyModeInitializer.Initialize", "Site is in login-only mode -- sending anonymous page request to log on");
            currentMode.Mode = Display_Mode_Enum.My_Sobek;
            currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
            currentMode.Logon_Required = true;

            return QueryInitializerHelperResponse.Successful;
        }

        private static bool is_page_writer(string WriterType)
        {
            return (String.IsNullOrEmpty(WriterType))
                || (WriterType == Writer_Codes.HTML)
                || (WriterType == Writer_Codes.HTML_LoggedIn)
                || (WriterType == Writer_Codes.HTML_Echo)
                || (WriterType == Writer_Codes.Text);
        }

        /// <summary> Modes that stay reachable while the site is login-only </summary>
        /// <remarks> mySobek covers the logon, registration and OIDC/SAML sign-in pages -- UserObjectInitializer
        /// already forces every other anonymous mySobek request to the logon screen. Contact stays open so
        /// someone locked out can still reach the library. Cache reload and reset keep infrastructure working,
        /// a legacy URL only redirects (and the new URL is gated normally), and an error page is left alone. </remarks>
        private static bool is_always_allowed(Display_Mode_Enum Mode)
        {
            switch (Mode)
            {
                case Display_Mode_Enum.My_Sobek:
                case Display_Mode_Enum.Contact:
                case Display_Mode_Enum.Contact_Sent:
                case Display_Mode_Enum.Error:
                case Display_Mode_Enum.Item_Cache_Reload:
                case Display_Mode_Enum.Reset:
                case Display_Mode_Enum.Legacy_URL:
                    return true;

                default:
                    return false;
            }
        }
    }
}
