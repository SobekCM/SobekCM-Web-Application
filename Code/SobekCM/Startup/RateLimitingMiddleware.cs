using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SobekCM.Core.Configuration.Engine;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.RateLimiting;
using SobekCM.Library.UI;
using SobekCM.Tools.IpRangeUtilities;
using System;
using System.Threading.Tasks;

namespace SobekCM.Startup
{
    /// <summary> Two checks, cheapest first: (1) an outright, hardcoded user-agent blocklist (see
    /// UserAgentBlocklist_Gateway) gets an unconditional 403, no matter the IP; (2) an IP that has racked up more
    /// than RateLimiting:RequestLimit anonymous hits, or more than RateLimiting:LoggedOnRequestLimit logged-on
    /// hits, within RateLimiting:WindowSeconds gets a 429 for RateLimiting:BanMinutes, and a range with
    /// RateLimiting:RangeBanThreshold IPs banned at once gets a 429 for RateLimiting:RangeBanHours. This class only
    /// checks ban status and writes the 403/429 response; it never counts the traffic that leads to an IP ban
    /// itself -- that hit recording happens later in the pipeline, in
    /// SobekCM.QueryInitializerHelpers.ItemViewRateLimitInitializer, once a Navigation_Object/User_Object are
    /// available to tell whether this request is even the kind worth counting (see RateLimiting_Gateway's remarks
    /// for the full rationale). Both share the same SharedCache-backed state via RateLimiting_Gateway. </summary>
    /// <remarks> Registered after StaticFilesStartup, so a single page load's CSS/JS/image requests never
    /// reach it -- only "real" application requests do (same placement rationale as RequestContextMiddleware,
    /// registered right after this). Being just a ban check now (no counting), this also runs as cheaply
    /// as possible ahead of QueryInitializer -- an already-banned IP, or a blocklisted user agent, is rejected
    /// before any of the request setup in QueryInitializer.cs even starts. The health check endpoint is exempted
    /// by path so an external monitor polling it isn't at risk of tripping its own ban. </remarks>
    public static class RateLimitingMiddleware
    {
        public static void Configure(WebApplication app)
        {
            RateLimiting_Gateway.Enabled = app.Configuration.GetValue<bool>("RateLimiting:Enabled");
            RateLimiting_Gateway.RequestLimit = app.Configuration.GetValue("RateLimiting:RequestLimit", RateLimiting_Gateway.RequestLimit);
            RateLimiting_Gateway.LoggedOnRequestLimit = app.Configuration.GetValue("RateLimiting:LoggedOnRequestLimit", RateLimiting_Gateway.LoggedOnRequestLimit);
            RateLimiting_Gateway.WindowSeconds = app.Configuration.GetValue("RateLimiting:WindowSeconds", RateLimiting_Gateway.WindowSeconds);
            RateLimiting_Gateway.BanMinutes = app.Configuration.GetValue("RateLimiting:BanMinutes", RateLimiting_Gateway.BanMinutes);
            RateLimiting_Gateway.RangeBanThreshold = app.Configuration.GetValue("RateLimiting:RangeBanThreshold", RateLimiting_Gateway.RangeBanThreshold);
            RateLimiting_Gateway.RangeBanHours = app.Configuration.GetValue("RateLimiting:RangeBanHours", RateLimiting_Gateway.RangeBanHours);
            // Covers every limiters' entries in temp/ratelimiting.txt, not only bans (see RateLimitLog_Gateway)
            RateLimitLog_Gateway.Enabled = app.Configuration.GetValue("RateLimiting:LoggingEnabled", RateLimitLog_Gateway.Enabled);
            RateLimiting_Gateway.IsExemptIp = Is_Ip_In_Engine_Restriction_Ranges;

            // SobekCM_Core can't see the HttpContext, so the log gateway asks for the tripping request's user agent
            // through this. Every event trips during request handling, after UserIpInitializer has cached it; the raw
            // header is only the fallback for anything that trips before that.
            IHttpContextAccessor httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
            RateLimitLog_Gateway.CurrentUserAgent = () =>
            {
                HttpContext context = httpContextAccessor.HttpContext;
                if (context == null)
                    return null;

                return (context.Items[RequestCache_Keys.UserAgent] as string) ?? context.Request.Headers["User-Agent"].ToString();
            };

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

            // Checked first: the cheapest possible test (no cache lookup, doesn't even need the IP), and an
            // unconditional no regardless of how well-behaved this particular request looks -- see
            // UserAgentBlocklist_Gateway for why this is a separate, hardcoded list rather than an extension of
            // the IP-ban machinery below.
            string userAgent = context.Request.Headers["User-Agent"].ToString();
            if (UserAgentBlocklist_Gateway.IsBanned(userAgent))
            {
                UserAgentBlocklist_Gateway.RecordBan(ip, userAgent);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Forbidden");
                return;
            }

            int? banMinutesRemaining = RateLimiting_Gateway.IsBanned(ip, out bool rangeBan);
            if (banMinutesRemaining.HasValue)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = (banMinutesRemaining.Value * 60).ToString();
                context.Response.ContentType = "text/html";

                string wait = describe_wait(banMinutesRemaining.Value);
                string explanation = rangeBan
                    // A range ban can catch someone who did nothing themselves -- they just share a provider's range
                    // with a crawler -- so say it's their network, and don't suggest logging on: a logged-on visitor
                    // from a banned range is blocked too
                    ? "<p>An unusual amount of automated traffic has come from your network, so access from it is " +
                      "temporarily blocked. Please try again in " + wait + ".</p>" +
                      "<p>If you believe this is a mistake, please contact the library.</p>"
                    : "<p>You have made too many requests in a short period of time. Please wait " + wait + " and try again.</p>" +
                      // Worded for everyone, since this runs before the request's user is known: a logged-on user
                      // banned from a shared address sees it too. No logon link either -- during the ban every request
                      // from this IP, the logon page included, gets this same response.
                      "<p>Visitors who are logged on to an account are allowed more requests. If you have an account, " +
                      "or register for a free one, please log on once the wait is over.</p>";

                await context.Response.WriteAsync(
                    "<html><head><title>Too Many Requests</title></head><body>" +
                    "<h1>Too Many Requests</h1>" +
                    explanation +
                    "</body></html>");
                return;
            }

            await next();
        }

        /// <summary> Short text for how long a ban still has to run, e.g. "10 minute(s)" or "about 8 hour(s)" </summary>
        private static string describe_wait(int Minutes)
        {
            if (Minutes < 120)
                return Minutes + " minute(s)";

            return "about " + (int)Math.Round(Minutes / 60.0) + " hour(s)";
        }
    }
}
