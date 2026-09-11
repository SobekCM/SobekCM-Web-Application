using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading.Tasks;

namespace SobekCM.Startup
{
    /// <summary> Per-IP request throttling -- an IP making more than RateLimiting:RequestLimit requests
    /// within RateLimiting:WindowSeconds gets a 429 for RateLimiting:BanMinutes. The counting/banning
    /// itself lives in <see cref="RateLimiting_Gateway"/> (SharedCache-backed); this class only wires
    /// config from appsettings.json and writes the 429 response. </summary>
    /// <remarks> Registered after StaticFilesStartup, so a single page load's CSS/JS/image requests never
    /// reach it and don't count against the limit -- only "real" application requests do (same placement
    /// rationale as RequestContextMiddleware, registered right after this). The health check endpoint is
    /// exempted by path so an external monitor polling it isn't at risk of tripping its own ban. </remarks>
    public static class RateLimitingMiddleware
    {
        public static void Configure(WebApplication app)
        {
            RateLimiting_Gateway.Enabled = app.Configuration.GetValue<bool>("RateLimiting:Enabled");
            RateLimiting_Gateway.RequestLimit = app.Configuration.GetValue("RateLimiting:RequestLimit", RateLimiting_Gateway.RequestLimit);
            RateLimiting_Gateway.WindowSeconds = app.Configuration.GetValue("RateLimiting:WindowSeconds", RateLimiting_Gateway.WindowSeconds);
            RateLimiting_Gateway.BanMinutes = app.Configuration.GetValue("RateLimiting:BanMinutes", RateLimiting_Gateway.BanMinutes);
            RateLimiting_Gateway.LoggingEnabled = app.Configuration.GetValue("RateLimiting:LoggingEnabled", RateLimiting_Gateway.LoggingEnabled);

            app.Use(Invoke);
        }

        private static async Task Invoke(HttpContext context, Func<Task> next)
        {
            if (string.Equals(context.Request.Path.Value, "/health", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            string ip = context.Connection?.RemoteIpAddress?.ToString();
            int? banMinutesRemaining = RateLimiting_Gateway.CheckAndRecord(ip);
            if (banMinutesRemaining.HasValue)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = (banMinutesRemaining.Value * 60).ToString();
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(
                    "<html><head><title>Too Many Requests</title></head><body>" +
                    "<h1>Too Many Requests</h1>" +
                    "<p>You have made too many requests in a short period of time. Please wait " +
                    banMinutesRemaining.Value + " minute(s) and try again.</p>" +
                    "</body></html>");
                return;
            }

            await next();
        }
    }
}
