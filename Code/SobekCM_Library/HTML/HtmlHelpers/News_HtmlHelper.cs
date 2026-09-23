using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Users;
using SobekCM.Library.Database;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace SobekCM.Library.HTML.Helpers
{
    /// <summary> Writes the news banner at the very top of the page, above the header, for any news the
    /// current user or visitor has not closed yet </summary>
    /// <remarks> For a logged-on user, the database is only checked on the first page after logging on.  The
    /// news that applies to them is then kept in the session (<see cref="SessionCache_Keys.PendingNews"/>) until
    /// each item is closed, which posts to the /news/dismiss endpoint and drops it from the session copy too.<br /><br />
    /// A visitor who is not logged on only sees news marked for everyone (for example "The library will be closed
    /// on Labor Day").  That news is cached for the whole application for a few minutes, rather than checked per
    /// visitor, and closing it just adds its id to a cookie, since there is no user to record it against.<br /><br />
    /// Several news items stack on top of each other, newest first.  Robots never see news. </remarks>
    public static class News_HtmlHelper
    {
        /// <summary> URL (relative to the base URL) the close button posts to, for logged-on users </summary>
        public const string DISMISS_URL = "news/dismiss";

        /// <summary> Cookie holding the ids of the news a visitor who is not logged on has closed, separated by periods </summary>
        public const string CLOSED_COOKIE = "sobekcm_news_closed";

        // Key and lifetime for the news for everyone, cached for all visitors who are not logged on
        private const string PUBLIC_NEWS_CACHE_KEY = "NEWS|PUBLIC";
        private static readonly TimeSpan PUBLIC_NEWS_CACHE_LIFETIME = TimeSpan.FromMinutes(5);

        // Most news ids kept in the cookie, so it cannot grow without limit
        private const int MAX_CLOSED_IN_COOKIE = 30;

        /// <summary> Writes the news banner, if the current user or visitor has any news they have not closed yet </summary>
        /// <param name="Output"> Stream to which to write the HTML </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public static void Add_Pending_News(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            if (RequestSpecificValues.Current_Mode.Is_Robot)
                return;

            User_Object user = RequestSpecificValues.Current_User;
            bool loggedOn = (user != null) && (user.LoggedOn) && (user.UserID > 0);

            List<User_News_Item> news = loggedOn
                ? Get_Pending_News(RequestSpecificValues.Context, user, Tracer)
                : Get_Pending_Public_News(RequestSpecificValues.Context, Tracer);
            if (news.Count == 0)
                return;

            Tracer.Add_Trace("News_HtmlHelper.Add_Pending_News", "Adding " + news.Count + " pending news item(s)");

            string language = RequestSpecificValues.Current_Mode.Language;
            string closeLabel = WebUtility.HtmlEncode(Localization_Gateway.Buttons.Close(language));
            string closeTitle = WebUtility.HtmlEncode(Localization_Gateway.News.Close_Title(language));

            Output.WriteLine("<!-- Pending news (News_HtmlHelper.Add_Pending_News) -->");
            Output.WriteLine("<style>");
            Output.WriteLine("  #sbkNews_Container { margin: 0; padding: 0; font-family: inherit; }");
            Output.WriteLine("  .sbkNews_Item { display: flex; align-items: flex-start; gap: 16px; background: #fff8e1; color: #333; border-bottom: 1px solid #e0c97a; padding: 10px 20px; text-align: left; }");
            Output.WriteLine("  .sbkNews_Content { flex: 1 1 auto; min-width: 0; }");
            Output.WriteLine("  .sbkNews_Title { font-weight: bold; margin-bottom: 4px; }");
            Output.WriteLine("  .sbkNews_Body p { margin: 0 0 6px 0; }");
            Output.WriteLine("  .sbkNews_Body ul { margin: 0 0 6px 0; padding-left: 20px; }");
            Output.WriteLine("  .sbkNews_Body a { color: #1a55a8; text-decoration: underline; }");
            Output.WriteLine("  .sbkNews_Close { flex: 0 0 auto; cursor: pointer; background: #fff; color: #333; border: 1px solid #b89b3a; border-radius: 4px; padding: 4px 12px; font-size: 0.85em; }");
            Output.WriteLine("  .sbkNews_Close:hover, .sbkNews_Close:focus { background: #f3e2a9; }");
            Output.WriteLine("  @media print { #sbkNews_Container { display: none; } }");
            Output.WriteLine("</style>");

            Output.WriteLine("<div id=\"sbkNews_Container\" role=\"region\" aria-label=\"" + WebUtility.HtmlEncode(Localization_Gateway.News.Region_Label(language)) + "\">");
            foreach (User_News_Item newsItem in news)
            {
                Output.WriteLine("  <div class=\"sbkNews_Item\" id=\"sbkNews_Item_" + newsItem.NewsID + "\">");
                Output.WriteLine("    <div class=\"sbkNews_Content\">");
                if (!String.IsNullOrWhiteSpace(newsItem.Title))
                    Output.WriteLine("      <div class=\"sbkNews_Title\">" + WebUtility.HtmlEncode(newsItem.Title) + "</div>");

                // The body is HTML (so it can hold links), written by a system administrator, a news
                // administrator or an upgrade script, and is rendered exactly as authored.  Authoring news is
                // therefore a TRUSTED-HTML right, the same as editing a web content page or a skin: anyone who
                // can write news can run script on any page the news is shown on, including an administrator's.
                // Only give the news administrator role to someone already trusted that far.
                Output.WriteLine("      <div class=\"sbkNews_Body\">" + newsItem.Body + "</div>");
                Output.WriteLine("    </div>");
                Output.WriteLine("    <button type=\"button\" class=\"sbkNews_Close\" title=\"" + closeTitle + "\" onclick=\"return sbkNews_Dismiss(" + newsItem.NewsID + ");\">" + closeLabel + "</button>");
                Output.WriteLine("  </div>");
            }
            Output.WriteLine("</div>");

            // Hide the item right away, then record that it was closed.
            Output.WriteLine("<script type=\"text/javascript\">");
            Output.WriteLine("  function sbkNews_Dismiss(newsId) {");
            Output.WriteLine("    var item = document.getElementById('sbkNews_Item_' + newsId);");
            Output.WriteLine("    if (item) item.parentNode.removeChild(item);");
            Output.WriteLine("    var container = document.getElementById('sbkNews_Container');");
            Output.WriteLine("    if ((container) && (container.getElementsByClassName('sbkNews_Item').length == 0)) container.parentNode.removeChild(container);");
            if (loggedOn)
            {
                // Recorded against the user.  If the post fails, the item just comes back on the next logon.  The
                // custom header means another site cannot send this post without a CORS preflight, which this
                // endpoint never approves.
                Output.WriteLine("    if (window.fetch) {");
                Output.WriteLine("      fetch('" + RequestSpecificValues.Current_Mode.Base_URL + DISMISS_URL + "', { method: 'POST', credentials: 'same-origin', keepalive: true, headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' }, body: 'newsid=' + encodeURIComponent(newsId) }).catch(function () { });");
                Output.WriteLine("    }");
            }
            else
            {
                // Not logged on, so remember it in this browser only.  Newest ids are kept if the list gets long.
                Output.WriteLine("    var closed = [];");
                Output.WriteLine("    var match = document.cookie.match(/(?:^|;\\s*)" + CLOSED_COOKIE + "=([0-9.]*)/);");
                Output.WriteLine("    if ((match) && (match[1].length > 0)) closed = match[1].split('.');");
                Output.WriteLine("    if (closed.indexOf(String(newsId)) < 0) closed.push(String(newsId));");
                Output.WriteLine("    if (closed.length > " + MAX_CLOSED_IN_COOKIE + ") closed = closed.slice(closed.length - " + MAX_CLOSED_IN_COOKIE + ");");
                Output.WriteLine("    document.cookie = '" + CLOSED_COOKIE + "=' + closed.join('.') + '; path=/; max-age=31536000; SameSite=Lax';");
            }
            Output.WriteLine("    return false;");
            Output.WriteLine("  }");
            Output.WriteLine("</script>");
            Output.WriteLine("<!-- End of pending news -->");
        }

        /// <summary> Gets the news a logged-on user has not closed yet, from the session if it was already
        /// checked since they logged on, otherwise from the database </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="User"> Logged-on user </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Pending news that applies to this user, newest first </returns>
        public static List<User_News_Item> Get_Pending_News(HttpContext Context, User_Object User, Custom_Tracer Tracer)
        {
            if (Context.SessionObject()[SessionCache_Keys.PendingNews] is User_Pending_News cached && cached.UserID == User.UserID)
                return cached.Snapshot();

            Tracer?.Add_Trace("News_HtmlHelper.Get_Pending_News", "Checking the database for pending news for user " + User.UserID);

            // A database error (for example, before the 5.2.0 upgrade script is run) is cached as no news,
            // so it is not retried on every page
            List<User_News_Item> allPending = SobekCM_Database.Get_Pending_News(User.UserID, Tracer) ?? new List<User_News_Item>();
            List<User_News_Item> forThisUser = allPending.Where(NewsItem => NewsItem.Applies_To(User)).ToList();

            User_Pending_News pending = new User_Pending_News(User.UserID, forThisUser);
            Context.SessionObject()[SessionCache_Keys.PendingNews] = pending;
            return pending.Snapshot();
        }

        /// <summary> Gets the news for everyone that a visitor who is not logged on has not closed yet </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Pending news for everyone, newest first, less any listed in the visitor's closed-news cookie </returns>
        public static List<User_News_Item> Get_Pending_Public_News(HttpContext Context, Custom_Tracer Tracer)
        {
            if (SharedCache.Instance[PUBLIC_NEWS_CACHE_KEY] is not List<User_News_Item> publicNews)
            {
                Tracer?.Add_Trace("News_HtmlHelper.Get_Pending_Public_News", "Checking the database for news for everyone");

                // A database error is cached as no news too, so it is not retried on every page
                publicNews = SobekCM_Database.Get_Pending_News(-1, Tracer) ?? new List<User_News_Item>();
                SharedCache.Instance.Set(PUBLIC_NEWS_CACHE_KEY, publicNews, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = PUBLIC_NEWS_CACHE_LIFETIME });
            }

            if (publicNews.Count == 0)
                return publicNews;

            HashSet<int> closed = new HashSet<int>();
            string cookie = Context.Request.Cookies[CLOSED_COOKIE];
            if (!String.IsNullOrEmpty(cookie))
            {
                foreach (string closedId in cookie.Split('.', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Int32.TryParse(closedId, out int newsId))
                        closed.Add(newsId);
                }
            }

            return publicNews.Where(NewsItem => !closed.Contains(NewsItem.NewsID)).ToList();
        }

        /// <summary> Records that the logged-on user closed a news item, in both the database and the
        /// session copy of their pending news </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="User"> Logged-on user </param>
        /// <param name="NewsID"> Primary key for the news item closed </param>
        /// <returns> TRUE if it was recorded in the database, otherwise FALSE </returns>
        public static bool Dismiss_News(HttpContext Context, User_Object User, int NewsID)
        {
            if (Context.SessionObject()[SessionCache_Keys.PendingNews] is User_Pending_News cached && cached.UserID == User.UserID)
                cached.Remove(NewsID);

            return SobekCM_Database.Dismiss_News(User.UserID, NewsID, null);
        }

        /// <summary> Clears the cached news for everyone, so a change made on the news admin screen reaches
        /// visitors who are not logged on right away </summary>
        /// <remarks> Logged-on users keep the news loaded when they logged on, for the rest of their session </remarks>
        public static void Clear_Public_News_Cache()
        {
            SharedCache.Instance.Remove(PUBLIC_NEWS_CACHE_KEY);
        }
    }
}
