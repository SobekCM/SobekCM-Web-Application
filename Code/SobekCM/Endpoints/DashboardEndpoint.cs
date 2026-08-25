using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Replaces Dashboard.aspx -- shows the last exception caught for this session, if
    /// any, for local diagnostics. </summary>
    public static class DashboardEndpoint
    {
        public static async Task Invoke(HttpContext context)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            using var writer = new StringWriter();

            writer.WriteLine("<!DOCTYPE html><html><head><title>SobekCM Dashboard</title></head><body>");

            if (context.SessionObject()[SessionCache_Keys.LastException] is Exception lastException)
            {
                if (lastException is SobekCM_Traced_Exception traced)
                {
                    writer.WriteLine("<h1>EXCEPTION CAUGHT</h1>");
                    writer.WriteLine("<h2>SobekCM Message</h2><blockquote>" + traced.Message + "</blockquote>");
                    writer.WriteLine("<h2>Inner Message</h2><blockquote>" + traced.InnerException?.Message + "</blockquote>");
                    if (!string.IsNullOrEmpty(traced.InnerException?.StackTrace))
                        writer.WriteLine("<h2>Stack Trace</h2><blockquote>" + traced.InnerException.StackTrace.Replace("\n", "<br />") + "</blockquote>");
                    writer.WriteLine("<h2>SobekCM Tracer</h2><blockquote>" + traced.Trace_Route_HTML + "</blockquote>");
                }
                else
                {
                    writer.WriteLine("<h1>EXCEPTION CAUGHT</h1>");
                    writer.WriteLine("<h2>Message</h2><blockquote>" + lastException.Message + "</blockquote>");
                    if (!string.IsNullOrEmpty(lastException.StackTrace))
                        writer.WriteLine("<h2>Stack Trace</h2><blockquote>" + lastException.StackTrace.Replace("\n", "<br />") + "</blockquote>");
                }
                context.SessionObject()[SessionCache_Keys.LastException] = null;
            }
            else
            {
                writer.WriteLine("<h1>SobekCM Dashboard</h1>");
                writer.WriteLine("This dashboard displays exceptions when the application is run locally.");
            }

            writer.WriteLine("</body></html>");

            await context.Response.WriteAsync(writer.ToString(), Encoding.UTF8);
        }
    }
}
