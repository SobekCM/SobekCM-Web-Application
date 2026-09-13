using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;
using System;

namespace SobekCM.QueryInitializerHelpers
{
    /// <summary> Phase 6 of the GCS rate-limiting plan: when the whole site has been switched to login-only
    /// (see LoginOnlyMode_Gateway.Site_Requires_Logon), sends an anonymous request to the logon screen
    /// instead of the page it asked for. </summary>
    /// <remarks> Reuses the existing mechanism UserObjectInitializer already uses for mySobek pages --
    /// switching the request to the logon screen and setting Logon_Required. The logon screen shown in place
    /// on the original URL redirects back to that URL after a successful logon, so nothing else is needed.
    /// <para>Must run after UserObjectInitializer (needs Current_User), and before ItemViewRateLimitInitializer
    /// so a request turned away here is never counted as an item hit -- see QueryInitializer.cs.
    /// Non-pipeline traffic (/engine, /files, robots.txt, the health check, static files) never reaches this
    /// at all.</para>
    /// <para>Fails closed: every request is gated unless it is a known data feed whose plugin writer is actually
    /// registered (see is_open_data_feed). Classifying the other way round -- gating only known page writers --
    /// would leave any unrecognized writer code open, and an unrecognized or disabled writer code doesn't
    /// produce a data feed at all: MainWriter_Factory falls back to a normal HTML page for it.</para> </remarks>
    public class LoginOnlyModeInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("LoginOnlyModeInitializer.Initialize");

            Navigation_Object currentMode = request.Current_Mode;
            if ((currentMode == null) || (!LoginOnlyMode_Gateway.Site_Requires_Logon()) || (AnonymousRequest.Is_Logged_On(request.Current_User)))
                return QueryInitializerHelperResponse.Successful;

            if ((is_open_data_feed(currentMode.Writer_Type)) || (is_always_allowed(currentMode.Mode)))
                return QueryInitializerHelperResponse.Successful;

            tracer.Add_Trace("LoginOnlyModeInitializer.Initialize", "Site is in login-only mode -- sending anonymous request to log on");
            currentMode.Return_URL = UrlWriterHelper.Redirect_URL(currentMode);
            currentMode.Is_Robot = false;
            currentMode.Writer_Type = Writer_Codes.HTML;
            currentMode.Mode = Display_Mode_Enum.My_Sobek;
            currentMode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
            currentMode.Logon_Required = true;

            return QueryInitializerHelperResponse.Successful;
        }

        /// <summary> Writer codes for the data feeds that stay open while the site is login-only </summary>
        /// <remarks> None of these mint signed GCS URLs, and harvesters (OAI-PMH, IIIF clients) can't log on.
        /// A plugin data feed added later is gated until its code is added here -- in an emergency switch,
        /// blocking too much is the right way to be wrong. </remarks>
        private static readonly string[] Open_Data_Feed_Writer_Codes =
        {
            Writer_Codes.OAI, Writer_Codes.IIIF, Writer_Codes.JSON, Writer_Codes.XML, Writer_Codes.DataSet, Writer_Codes.Data_Provider
        };

        /// <summary> Whether this request is for a data feed that stays open in login-only mode </summary>
        /// <remarks> The code has to be one of the known data feeds AND have its plugin writer registered.
        /// QueryString_Analyzer sets these writer codes from the URL whether or not the matching plugin is
        /// enabled, and MainWriter_Factory renders a normal HTML page for any code it can't resolve -- so a
        /// disabled feed plugin would otherwise turn "/xml/..." into an ungated full page. </remarks>
        private static bool is_open_data_feed(string WriterType)
        {
            if (String.IsNullOrEmpty(WriterType))
                return false;

            foreach (string code in Open_Data_Feed_Writer_Codes)
            {
                if (String.Equals(code, WriterType, StringComparison.OrdinalIgnoreCase))
                    return MainWriter_Factory.Has_Plugin_Writer(WriterType);
            }

            return false;
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
