using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Navigation;
using SobekCM.Library;
using SobekCM.Tools;
using System;

namespace SobekCM.QueryInitializerHelpers
{
    /// <summary> Makes an explicit "l=xx" language choice stick for the rest of the session, and applies
    /// the session (or logged-on user's preferred) language to requests that don't name one </summary>
    /// <remarks> Precedence, highest first:
    /// <list type="number">
    /// <item>"lo=xx" (language once) -- this request only, the session is left alone</item>
    /// <item>"l=xx" -- this request, and saved to the session for every request after it</item>
    /// <item>the language already saved to the session</item>
    /// <item>the logged-on user's Preferred_Language, which is then saved to the session</item>
    /// <item>the browser's Accept-Language / configured default (Navigation_Object.Default_Language)</item>
    /// </list>
    /// QueryString_Analyzer.Parse_Query already applied the first two (and the last) to Current_Mode.Language;
    /// this adds the session half.  Neither URL param is re-appended to generated URLs anymore (see
    /// UrlWriterHelper.URL_Options), so a hand-written link that drops the URL options no longer drops the language.
    /// <para>Must run after UserObjectInitializer (needs Current_User), and before anything that reads
    /// Current_Mode.Language, such as TopLevelAggregationInitializer -- see QueryInitializer.cs.</para> </remarks>
    public class LanguageSessionInitializer : IQueryInitializerHelper
    {
        public QueryInitializerHelperResponse Initialize(HttpContext context, RequestCache request, Custom_Tracer tracer)
        {
            tracer.Add_Trace("LanguageSessionInitializer.Initialize");

            Navigation_Object currentMode = request.Current_Mode;
            if ((currentMode == null) || (request.QueryString == null))
                return QueryInitializerHelperResponse.Successful;

            // Language once -- already applied by the query string analyzer, and deliberately not saved
            if ((request.QueryString.TryGetValue("lo", out string onceValue)) && (QueryString_Analyzer.Resolve_Language_Code(onceValue) != null))
                return QueryInitializerHelperResponse.Successful;

            // Robots don't keep cookies, so saving anything to their session would only fill the cache with
            // sessions that are never seen again
            bool canUseSession = !currentMode.Is_Robot;

            // Explicit language switch -- already applied by the query string analyzer, so just remember it
            if ((request.QueryString.TryGetValue("l", out string urlValue)) && (QueryString_Analyzer.Resolve_Language_Code(urlValue) != null))
            {
                if (canUseSession)
                    context.Session.SetString(SessionCache_Keys.Language, currentMode.Language);
                return QueryInitializerHelperResponse.Successful;
            }

            if (!canUseSession)
                return QueryInitializerHelperResponse.Successful;

            // Language chosen earlier in this session (ignored if that language is no longer configured)
            string sessionLanguage = QueryString_Analyzer.Resolve_Language_Code(context.Session.GetString(SessionCache_Keys.Language));
            if (sessionLanguage != null)
            {
                currentMode.Language = sessionLanguage;
                return QueryInitializerHelperResponse.Successful;
            }

            // Nothing chosen yet this session, so seed it from the logged-on user's preference
            if ((request.Current_User != null) && (request.Current_User.LoggedOn))
            {
                string preferredLanguage = QueryString_Analyzer.Resolve_Language_Code(request.Current_User.Preferred_Language);
                if (preferredLanguage != null)
                {
                    tracer.Add_Trace("LanguageSessionInitializer.Initialize", "Seeding session language from user preference: " + preferredLanguage);
                    currentMode.Language = preferredLanguage;
                    context.Session.SetString(SessionCache_Keys.Language, preferredLanguage);
                }
            }

            return QueryInitializerHelperResponse.Successful;
        }
    }
}
