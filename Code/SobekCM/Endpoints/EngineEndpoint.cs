using Microsoft.AspNetCore.Http;
using SobekCM.Engine_Library;
using SobekCM.Engine_Library.ApplicationState;
using System;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Dispatches a request to the engine's MicroserviceHandler, translating the
    /// route-captured path (everything after /engine/) into the "urlrelative" query parameter it
    /// expects -- the same value the old rewriter produced via appRelative.Substring(6). In the old
    /// app, an IHttpModule rewrote /engine/... requests to ~/sobekcm.svc?urlrelative=... within this
    /// SAME application -- sobekcm.svc was never a separately deployed site. MicroserviceHandler is
    /// that same handler, just invoked directly and in-process here instead of via rewrite. </summary>
    public static class EngineEndpoint
    {
        public static async Task Invoke(HttpContext context, string urlrelative)
        {
            urlrelative ??= "";

            // On a private site, most engine endpoints (item citations, MARC, search results, ...) have no IP restriction
            // of their own, so anonymous callers are refused here unless they're a trusted client in one of the ranges.
            // This web server calls its own engine over HTTP, so it must be in one -- it already is if R1-only calls,
            // like the aggregation list, are working.  A logged-on browser's own calls carry its session.
            if ((PrivateSiteGate.Is_Private_Site) && (!PrivateSiteGate.Session_Is_Logged_On(context)) &&
                (!Engine_ApplicationCache_Gateway.Configuration.Engine.Ip_In_Any_Restriction_Range(context.Connection.RemoteIpAddress?.ToString())))
            {
                context.Response.ContentType = "text/plain";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("This repository requires a logon");
                return;
            }

            string existing = context.Request.QueryString.HasValue ? context.Request.QueryString.Value.TrimStart('?') : "";
            string merged = "urlrelative=" + Uri.EscapeDataString(urlrelative);
            if (!string.IsNullOrEmpty(existing))
                merged += "&" + existing;
            context.Request.QueryString = new QueryString("?" + merged);

            await new MicroserviceHandler().ProcessRequest(context);
        }
    }
}
