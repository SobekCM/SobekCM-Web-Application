using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library.UI;
using SobekCM.Tools.IpRangeUtilities;
using System;
using System.Threading.Tasks;

namespace SobekCM.Startup
{
    /// <summary> Per-IP request throttling -- an IP that has racked up more than RateLimiting:RequestLimit
    /// hits within RateLimiting:WindowSeconds gets a 429 for RateLimiting:BanMinutes. This class only
    /// checks ban status and writes the 429 response; it never counts anything itself -- the actual hit
    /// recording happens later in the pipeline, in SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer,
    /// once a Navigation_Object/User_Object are available to tell whether this request is even the kind
    /// worth counting (see RateLimiting_Gateway's remarks for the full rationale). Both share the same
    /// SharedCache-backed state via RateLimiting_Gateway. </summary>
    /// <remarks> Registered after StaticFilesStartup, so a single page load's CSS/JS/image requests never
    /// reach it -- only "real" application requests do (same placement rationale as RequestContextMiddleware,
    /// registered right after this). Being just a ban check now (no counting), this also runs as cheaply
    /// as possible ahead of QueryInitializer -- an already-banned IP is rejected before any of the request
    /// setup in QueryInitializer.cs even starts. The health check endpoint is exempted by path so an
    /// external monitor polling it isn't at risk of tripping its own ban. </remarks>
    public static class RateLimitingMiddleware
    {
        public static void Configure(WebApplication app)
        {
            RateLimiting_Gateway.Enabled = app.Configuration.GetValue<bool>("RateLimiting:Enabled");
            RateLimiting_Gateway.RequestLimit = app.Configuration.GetValue("RateLimiting:RequestLimit", RateLimiting_Gateway.RequestLimit);
            RateLimiting_Gateway.WindowSeconds = app.Configuration.GetValue("RateLimiting:WindowSeconds", RateLimiting_Gateway.WindowSeconds);
            RateLimiting_Gateway.BanMinutes = app.Configuration.GetValue("RateLimiting:BanMinutes", RateLimiting_Gateway.BanMinutes);
            RateLimiting_Gateway.LoggingEnabled = app.Configuration.GetValue("RateLimiting:LoggingEnabled", RateLimiting_Gateway.LoggingEnabled);
            RateLimiting_Gateway.IsExemptIp = Is_Ip_In_Engine_Restriction_Ranges;

            app.Use(Invoke);
        }

        /// <summary> Exempts loopback plus every IP address/range listed across ALL of the Engine's
        /// configured RestrictionRanges (Settings &gt; Engine &gt; IP restrictions -- dev boxes, this web
        /// server itself, the Builder machine, etc.), regardless of which specific range/endpoint they're
        /// otherwise scoped to -- this is deliberately broader than any single endpoint's own access check
        /// (see Engine_VerbMapping.AccessPermitted, which this mirrors for the loopback shortcut and the
        /// underlying IpRangeSetV4 machinery). Re-reads the live configuration on every call rather than
        /// caching, so it stays correct across a config reload without any invalidation logic -- the range
        /// list is tiny (a handful of entries), so rebuilding it per call costs nothing meaningful. </summary>
        private static bool Is_Ip_In_Engine_Restriction_Ranges(string ipAddress)
        {
            // Same loopback shortcut Engine_VerbMapping.AccessPermitted already grants -- mainly relevant
            // for this app's own server-to-server /engine/ calls (see SobekEngineClient), not just local
            // debugging
            if ((ipAddress == "::1") || (ipAddress == "127.0.0.1"))
                return true;

            var restrictionRanges = UI_ApplicationCache_Gateway.Configuration?.Engine?.RestrictionRanges;
            if ((restrictionRanges == null) || (restrictionRanges.Count == 0))
                return false;

            var rangeTester = new IpRangeSetV4();
            foreach (Engine_RestrictionRange thisRangeSet in restrictionRanges)
            {
                if (thisRangeSet.IpRanges == null)
                    continue;

                foreach (Engine_IpRange thisRange in thisRangeSet.IpRanges)
                {
                    if (!string.IsNullOrEmpty(thisRange.EndIp))
                        rangeTester.AddIpRange(thisRange.StartIp, thisRange.EndIp);
                    else
                        rangeTester.AddIpRange(thisRange.StartIp);
                }
            }

            return rangeTester.Contains(ipAddress);
        }

        private static async Task Invoke(HttpContext context, Func<Task> next)
        {
            if (string.Equals(context.Request.Path.Value, "/health", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            string ip = context.Connection?.RemoteIpAddress?.ToString();
            int? banMinutesRemaining = RateLimiting_Gateway.IsBanned(ip);
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
