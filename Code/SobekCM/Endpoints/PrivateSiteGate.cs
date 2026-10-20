using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.RateLimiting;
using SobekCM.Library.UI;

namespace SobekCM.Endpoints
{
    /// <summary> Enforces a private repository (RequireLogon in sobekcm_authentication.config) on the endpoints
    /// that never go through QueryInitializer, and so never reach LoginOnlyModeInitializer </summary>
    public static class PrivateSiteGate
    {
        /// <summary> Whether this instance is a private repository </summary>
        public static bool Is_Private_Site => UI_ApplicationCache_Gateway.Configuration?.Authentication?.RequireLogon ?? false;

        /// <summary> Whether this request's session holds a logged-on user </summary>
        /// <remarks> Only the session is checked, not the "remember me" cookie: any page view restores the session
        /// from that cookie first, and these endpoints are only reached from links and calls on those pages </remarks>
        public static bool Session_Is_Logged_On(HttpContext context)
        {
            return AnonymousRequest.Is_Logged_On(CachedDataManager_UserCacheServices.StringToUser(context.Session.GetString(SessionCache_Keys.User)));
        }
    }
}
