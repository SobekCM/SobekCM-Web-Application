using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Users;
using SobekCM.Library.HTML.Helpers;
using System;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Records that the logged-on user closed a news item from the banner at the top of the page,
    /// so it is not shown to them again (see <see cref="News_HtmlHelper"/>) </summary>
    /// <remarks> Only acts for the user logged on in this session, never a user id from the request.  Requires
    /// the X-Requested-With header the banner's script sends, so another site cannot send this post without a
    /// CORS preflight (which is never approved).  Visitors who are not logged on close news with a cookie
    /// instead, and never call this. </remarks>
    public static class NewsDismissEndpoint
    {
        public static async Task Invoke(HttpContext context)
        {
            if ((!context.Request.HasFormContentType) || (context.Request.Headers["X-Requested-With"] != "XMLHttpRequest"))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            IFormCollection form = await context.Request.ReadFormAsync();
            if (!Int32.TryParse(form["newsid"], out int newsId) || (newsId <= 0))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            await context.Session.LoadAsync();
            string userString = context.Session.GetString(SessionCache_Keys.User);
            User_Object user = String.IsNullOrEmpty(userString) ? null : CachedDataManager_UserCacheServices.StringToUser(userString);
            if ((user == null) || (user.UserID <= 0))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.StatusCode = News_HtmlHelper.Dismiss_News(context, user, newsId)
                ? StatusCodes.Status204NoContent
                : StatusCodes.Status500InternalServerError;
        }
    }
}
