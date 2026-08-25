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
                        // RequestCache_Keys.Tracer) is the only way to recover the trace route here. Written
                        // to its own trace_<guid>.txt via the same ExceptionLog_Gateway helper as
                        // HeaderFooter_Helper.Add_Footer's null-skin diagnostic, so exceptions.txt stays
                        // short, each trace is easy to find, and ErrorHandling:SuppressTraceFiles applies here too.
                        string traceNote = "(no trace available)";
                        if ((context.Items.TryGetValue(RequestCache_Keys.Tracer, out object tracerObj)) && (tracerObj is Custom_Tracer tracer))
                        {
                            traceNote = ExceptionLog_Gateway.WriteTraceFileAndGetNote(tracer.Text_Trace);
                        }

                        ExceptionLog_Gateway.Append(
                            "\nError caught in global exception handler ( " + DateTime.Now + " )\n" +
                            "User Host Address: " + (context.Connection.RemoteIpAddress?.ToString() ?? "") + "\n" +
                            "Requested URL: " + context.Request.GetDisplayUrl() + "\n" +
                            "Error Message: " + ee.Message + "\n" +
                            "Stack Trace: " + ee.StackTrace + "\n" +
                            "Inner Exception: " + (ee.InnerException != null ? ee.InnerException.Message + "\n" + ee.InnerException.StackTrace : "(none)") + "\n" +
                            traceNote + "\n" +
                            "------------------------------------------------------------------\n");
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
