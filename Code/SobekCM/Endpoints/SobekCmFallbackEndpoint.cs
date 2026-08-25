using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Replaces SobekCM.aspx, SobekCM_data.aspx and SobekCM_oai.aspx. Every main writer
    /// (HTML, JSON, XML, IIIF, OAI-PMH, etc.) sets its own Content-Type and produces its own
    /// complete response body, so this single handler no longer needs to know which writer type
    /// it's dealing with -- see abstractMainWriter subclasses' constructors/Write_Body. </summary>
    public static class SobekCmFallbackEndpoint
    {
        public static async Task Invoke(HttpContext context)
        {
            var request_url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

            //context.Response.Redirect("/sobekcm.aspx" + context.Request.QueryString);
            bool isPostBack = string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase);
            var pageGlobals = new QueryInitializer(context, "SOBEKCM");

            // The constructor may have already redirected (e.g. logoff) and have nothing left to do
            if (pageGlobals.currentMode == null || pageGlobals.currentMode.Request_Completed)
                return;

            try
            {
                pageGlobals.On_Page_Load();
            }
            catch (OutOfMemoryException ee)
            {
                if (pageGlobals.currentMode != null)
                {
                    pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                    pageGlobals.currentMode.Error_Message = "SobekCM Out of Memory Exception";
                    pageGlobals.currentMode.Caught_Exception = ee;
                }
            }
            catch (Exception ee)
            {
                if (pageGlobals.currentMode != null)
                {
                    pageGlobals.currentMode.Mode = Display_Mode_Enum.Error;
                    pageGlobals.currentMode.Error_Message = "Unknown error caught while executing your request";
                    pageGlobals.currentMode.Caught_Exception = ee;
                }
            }

            // No HTML rendering is needed, a redirect was likely called
            if (pageGlobals.currentMode.Request_Completed)
                return;

            // Save the current URL to session for "back" navigation
            string originalUrl = context.Items[RequestCache_Keys.OriginalUrl]?.ToString() ?? context.Request.GetDisplayUrl();

            if (pageGlobals.currentMode.Mode != Display_Mode_Enum.Preferences &&
                pageGlobals.currentMode.Mode != Display_Mode_Enum.Contact)
            {
                context.Session.SetString(SessionCache_Keys.LastMode, originalUrl);
            }

            if (pageGlobals.mainWriter != null)
            {
                using var writer = new StringWriter();
                pageGlobals.mainWriter.Write_Body(writer, pageGlobals.tracer);
                await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8);
            }
        }
    }
}
