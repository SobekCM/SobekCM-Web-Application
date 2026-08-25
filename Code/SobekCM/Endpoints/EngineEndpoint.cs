using Microsoft.AspNetCore.Http;
using SobekCM.Engine_Library;
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

            string existing = context.Request.QueryString.HasValue ? context.Request.QueryString.Value.TrimStart('?') : "";
            string merged = "urlrelative=" + Uri.EscapeDataString(urlrelative);
            if (!string.IsNullOrEmpty(existing))
                merged += "&" + existing;
            context.Request.QueryString = new QueryString("?" + merged);

            await new MicroserviceHandler().ProcessRequest(context);
        }
    }
}
