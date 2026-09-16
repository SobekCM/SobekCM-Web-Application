using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Core.RateLimiting;
using SobekCM.Library;
using SobekCM.Tools;

namespace SobekCM.QueryInitializerHelpers
{
    /// <summary> Phase 6 of the GCS rate-limiting plan, robot level: while site-wide item traffic is heavy, answers an
    /// identified robot's item request with HTTP 503 instead of the item </summary>
    /// <remarks> The quiet level below the login-only fuse. A crawler understands 503 with Retry-After as "temporarily
    /// overloaded, come back later" and slows down without dropping the pages from its index, which is what a 404 or 403
    /// would risk. People see nothing.
    /// <para>Runs before ItemViewRateLimitInitializer, so a paused robot's request is never counted -- which is the
    /// point: the crawl that would have driven the site-wide total up to ItemHitsPerHourThreshold stops contributing,
    /// so anonymous visitors are much less likely to ever meet the logon wall. It also runs before
    /// UserObjectInitializer, since a robot is never logged on, so a paused request costs almost nothing.</para>
    /// <para>Only item pages are refused. Robots can still crawl the home page, aggregations and search pages, which
    /// don't mint signed GCS URLs. See LoginOnlyMode_Gateway.Robot_Items_Blocked_Seconds_Left for the two reasons a
    /// robot is held off: the robot pause itself, and items requiring a logon at all.</para> </remarks>
    public class RobotItemPauseInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("RobotItemPauseInitializer.Initialize");

            Navigation_Object currentMode = request.Current_Mode;
            if ((currentMode == null) || (!currentMode.Is_Robot))
                return QueryInitializerHelperResponse.Successful;

            if ((currentMode.Mode != Display_Mode_Enum.Item_Display) && (currentMode.Mode != Display_Mode_Enum.Item_Print))
                return QueryInitializerHelperResponse.Successful;

            int? secondsLeft = LoginOnlyMode_Gateway.Robot_Items_Blocked_Seconds_Left();
            if (!secondsLeft.HasValue)
                return QueryInitializerHelperResponse.Successful;

            tracer.Add_Trace("RobotItemPauseInitializer.Initialize", "Item traffic is heavy -- answering this robot with a 503 and Retry-After " + secondsLeft.Value);

            // No body: a crawler reads the status and the header, and there's nothing here a person should see
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers["Retry-After"] = secondsLeft.Value.ToString();
            currentMode.Request_Completed = true;

            return QueryInitializerHelperResponse.Successful;
        }
    }
}
