using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;

namespace SobekCM.Startup
{
    /// <summary> Global last-resort exception log -- replaces Global.asax's Application_Error.
    /// Most request paths (sobekcm_data.aspx, sobekcm_oai.aspx, the SobekCM fallback route)
    /// already catch their own exceptions and route through Html_MainWriter's Error display,
    /// which logs to temp\exceptions.txt itself. This middleware only catches what those miss
    /// (other endpoints, or anything thrown before/outside page-load handling). The old
    /// Application_Error email-on-error branch is intentionally not ported -- it never worked. </summary>
    public static class ExceptionHandlingMiddleware
    {
        public static void Configure(WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    Exception ee = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                    if (ee != null)
                    {
                        // Most exceptions reaching this middleware are raw/unwrapped -- not bundled into a
                        // SobekCM_Traced_Exception -- so the tracer stashed by QueryInitializer (see
                        // RequestCache_Keys.Tracer) is the only way to recover the trace route here. Record
                        // stores it in the monitoring database, or (falling back) writes it to its own
                        // trace_<guid>.txt so exceptions.txt stays short and ErrorHandling:SuppressTraceFiles applies.
                        string traceText = null;
                        if ((context.Items.TryGetValue(RequestCache_Keys.Tracer, out object tracerObj)) && (tracerObj is Custom_Tracer tracer))
                        {
                            traceText = tracer.Text_Trace;
                        }

                        string requestedUrl = context.Request.GetDisplayUrl();
                        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "";

                        ExceptionLog_Gateway.Record("global-handler", ee, requestedUrl, clientIp, traceText,
                            "\nError caught in global exception handler ( " + DateTime.Now + " )\n" +
                            "User Host Address: " + clientIp + "\n" +
                            "Requested URL: " + requestedUrl + "\n" +
                            "Error Message: " + ee.Message + "\n" +
                            "Stack Trace: " + ee.StackTrace + "\n" +
                            "Inner Exception: " + (ee.InnerException != null ? ee.InnerException.Message + "\n" + ee.InnerException.StackTrace : "(none)") + "\n");
                    }

                    string errorUrl = UI_ApplicationCache_Gateway.Settings?.Servers?.System_Error_URL;
                    if (!string.IsNullOrEmpty(errorUrl))
                    {
                        context.Response.Redirect(errorUrl);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("An unexpected error occurred.");
                    }
                });
            });
        }
    }
}
