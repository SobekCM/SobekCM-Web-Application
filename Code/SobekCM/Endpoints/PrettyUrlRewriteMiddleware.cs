using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library.UI;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Rewrites bare item/aggregation paths (e.g. /AA00008275/00001/3j) into the
    /// urlrelative query parameter the rest of the pipeline expects, and handles a handful of
    /// passthroughs and special cases that used to live in the SobekCM_URL_Rewriter IHttpModule. </summary>
    /// <remarks> Portal resolution no longer needs to be threaded through here -- <see cref="SobekCM.QueryInitializerHelpers.UrlInitializer"/>
    /// already derives Base_URL directly from the request host. The old rewriter's static-file
    /// extension checks are also gone: UseStaticFiles, registered earlier in the pipeline, already
    /// serves anything that exists on disk before this middleware ever runs. </remarks>
    public static class PrettyUrlRewriteMiddleware
    {
        public static async Task Invoke(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.Value.Contains(".css") || context.Request.Path.Value.Contains(".js") ||
                context.Request.Path.Value.Contains(".jpg") || context.Request.Path.Value.Contains(".gif") ||
                context.Request.Path.Value.Contains(".png"))
                return;

            string relative = (context.Request.Path.Value ?? "").Trim('/').ToLower();
            string host = context.Request.Host.Host;

            // Leave requests for already-mapped routes alone — otherwise a direct hit to one of these
            // would fall into the generic rewrite below and get a bogus urlrelative injected into its
            // query string.
            if (relative == "robots.txt" || relative == "htmleditfilehandler.ashx" || relative == "uploadifivefilehandler.ashx" ||
                relative == "dashboard.aspx" ||
                relative.StartsWith("files/") || relative == "engine" || relative.StartsWith("engine/"))
            {
                await next();
                return;
            }

            // Block AmazonBot entirely
            string userAgent = context.Request.Headers.UserAgent.ToString();
            if (userAgent.IndexOf("amazonbot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                context.Response.Redirect("https://sobekdigital.com/about/", true);
                return;
            }

            // Per-portal favicon, e.g. design/favicons/dcdp.uoc.cw/favicon.ico
            if (relative == "favicon.ico")
            {
                string faviconPath = Path.Combine(UI_ApplicationCache_Gateway.Settings.Servers.Base_Design_Location, "favicons", host, "favicon.ico");
                if (File.Exists(faviconPath))
                {
                    context.Response.ContentType = "image/x-icon";
                    await context.Response.SendFileAsync(faviconPath);
                    return;
                }
                await next();
                return;
            }

            // Nothing to rewrite for the site root
            if (relative.Length == 0)
            {
                await next();
                return;
            }

            // Save the pre-rewrite URL for later reference (e.g. "back" links, email logs)
            context.Items[RequestCache_Keys.OriginalUrl] = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";

            // Fold the path into urlrelative and let the main fallback handler resolve it - every writer
            // type (HTML, dataset/xml/json/dataprovider/iiif, etc.) is resolved generically there now
            Add_UrlRelative_To_QueryString(context, relative);
            await next();
        }

        private static void Add_UrlRelative_To_QueryString(HttpContext context, string relative)
        {
            string existing = context.Request.QueryString.HasValue ? context.Request.QueryString.Value.TrimStart('?') : "";
            string merged = "urlrelative=" + Uri.EscapeDataString(relative);
            if (!string.IsNullOrEmpty(existing))
                merged += "&" + existing;
            context.Request.QueryString = new QueryString("?" + merged);
        }
    }
}
