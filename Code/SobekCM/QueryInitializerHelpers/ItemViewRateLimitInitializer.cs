using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Tools;

namespace SobekCM.QueryInitializerHelpers
{
    /// <summary> Records rate-limiting hits (see <see cref="RateLimiting_Gateway.RecordHit"/> and
    /// SustainedRateLimiting_Gateway.RecordHit) for an item view, logged on or not, each limiter giving
    /// logged-on views more room rather than an exemption -- the specific traffic this feature actually cares about limiting,
    /// since it's meant as a proxy for load against GCS cloud storage, and neither JP2 deep-zoom tiles nor
    /// thumbnail images ever reach this app's own request pipeline (see RateLimiting_Gateway's remarks). </summary>
    /// <remarks> Must run after both NavigationObjectInitializer (needs Current_Mode) and
    /// UserObjectInitializer (needs Current_User) have populated the request -- see QueryInitializer.cs,
    /// where this is called right before TopLevelAggregationInitializer. Never blocks the request or
    /// returns anything but success -- a hit that pushes an IP over the limit only bans that IP starting
    /// with its next request (checked by RateLimitingMiddleware, early in the pipeline, well before
    /// QueryInitializer runs again for that next request). </remarks>
    public class ItemViewRateLimitInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("ItemViewRateLimitInitializer.Initialize");

            var currentMode = request.Current_Mode;
            if (currentMode == null)
                return QueryInitializerHelperResponse.Successful;

            if ((currentMode.Mode != Display_Mode_Enum.Item_Display) && (currentMode.Mode != Display_Mode_Enum.Item_Print))
                return QueryInitializerHelperResponse.Successful;

            // Both limiters count logged-on views too, with more room rather than an exemption: a logged-on
            // session is just a cookie, and a cookie can be exported into a scraper
            bool loggedOn = AnonymousRequest.Is_Logged_On(request.Current_User);
            SustainedRateLimiting_Gateway.RecordHit(ClientSubnetKey.From(context));
            RateLimiting_Gateway.RecordHit(context.Items[RequestCache_Keys.UserIP]?.ToString(), loggedOn);

            // And the site-wide item-hit counter behind the automatic login-only fuse (Phase 6)
            LoginOnlyMode_Gateway.RecordItemHit();

            return QueryInitializerHelperResponse.Successful;
        }
    }
}
