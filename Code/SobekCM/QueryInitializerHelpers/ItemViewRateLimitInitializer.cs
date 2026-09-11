using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Library;
using SobekCM.Tools;

namespace SobekCM.QueryInitializerHelpers
{
    /// <summary> Records one rate-limiting hit (see <see cref="RateLimiting_Gateway.RecordHit"/>) for a
    /// non-logged-on user's item view -- the specific traffic this feature actually cares about limiting,
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

            // Logged-on users are never rate limited
            if ((currentMode == null) || ((request.Current_User != null) && (request.Current_User.LoggedOn)))
                return QueryInitializerHelperResponse.Successful;

            if ((currentMode.Mode == Display_Mode_Enum.Item_Display) || (currentMode.Mode == Display_Mode_Enum.Item_Print))
            {
                string ip = context.Items[RequestCache_Keys.UserIP]?.ToString();
                RateLimiting_Gateway.RecordHit(ip);
            }

            return QueryInitializerHelperResponse.Successful;
        }
    }
}
