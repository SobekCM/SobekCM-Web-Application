using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Endpoints
{
    /// <summary> Replaces Files.aspx -- serves (or forwards, or redirects, or dark-restricts) a
    /// file under an item's image-server directory, given a /files/{bibid}/{vid}/... URL. </summary>
    public static class FilesEndpoint
    {
        public static async Task Invoke(HttpContext context, string urlrelative)
        {
            urlrelative ??= "";

            // Robot check
            string userAgent = context.Request.Headers.UserAgent.ToString();
            string userHostAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";
            if (Navigation_Object.Is_UserAgent_IP_Robot(userAgent, userHostAddress))
            {
                context.Response.Clear();
                await context.Response.WriteAsync("RESTRICTED ITEM");
                return;
            }

            if (urlrelative.Length <= 4)
                return;

            string[] urlParts = urlrelative.ToLower().Split('/');
            var urlList = urlParts.Where(p => p.Length > 0).ToList();

            if (urlList.Count <= 2 || urlList[2].Length != 10)
                return;

            string bibID = urlList[2].ToUpper();
            string vid = null;
            if (urlList.Count > 3)
            {
                string possibleVid = urlList[3].Trim().PadLeft(5, '0');
                if (int.TryParse(possibleVid, out _))
                    vid = possibleVid;
            }

            if (string.IsNullOrEmpty(bibID) || string.IsNullOrEmpty(vid) || urlList.Count <= 4)
                return;

            var filePathBuilder = new StringBuilder(
                UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network +
                bibID[..2] + "\\" + bibID[2..4] + "\\" + bibID[4..6] + "\\" + bibID[6..8] + "\\" + bibID[8..] +
                "\\" + vid + "\\" + urlList[4]);
            for (int i = 5; i < urlList.Count; i++)
                filePathBuilder.Append("\\" + urlList[i]);

            string filePath = filePathBuilder.ToString();
            string extension = Path.GetExtension(filePath)?.ToLower();
            if (string.IsNullOrEmpty(extension))
                return;

            if (!UI_ApplicationCache_Gateway.Mime_Types.TryGetValue(extension, out var mimeType) || mimeType.isBlocked)
                return;

            SobekCM_Database.Get_Item_Restrictions(bibID, vid, null, out bool isDark, out short restrictions);

            if (!isDark && restrictions > 0)
            {
                if (context.Session.GetString(SessionCache_Keys.IpRangeMembership) == null)
                {
                    int ipMask = UI_ApplicationCache_Gateway.IP_Restrictions.Restrictive_Range_Membership(userHostAddress);
                    context.Session.SetString(SessionCache_Keys.IpRangeMembership, ipMask.ToString());
                }
                if (int.TryParse(context.Session.GetString(SessionCache_Keys.IpRangeMembership), out int userMask) && (restrictions & userMask) == 0)
                {
                    var possibleUser = CachedDataManager_UserCacheServices.StringToUser(context.Session.GetString(SessionCache_Keys.User));
                    if (possibleUser == null || possibleUser.Authentication_Type != User_Authentication_Type_Enum.Shibboleth)
                        isDark = true;
                }
            }

            if (isDark)
            {
                context.Response.Clear();
                await context.Response.WriteAsync("RESTRICTED ITEM");
                return;
            }

            if (mimeType.shouldForward)
            {
                var forwardBuilder = new StringBuilder(
                    UI_ApplicationCache_Gateway.Settings.Servers.Image_URL +
                    bibID[..2] + "/" + bibID[2..4] + "/" + bibID[4..6] + "/" + bibID[6..8] + "/" + bibID[8..] +
                    "/" + vid + "/" + urlList[4]);
                for (int i = 5; i < urlList.Count; i++)
                    forwardBuilder.Append("/" + urlList[i]);
                context.Response.Redirect(forwardBuilder.ToString());
                return;
            }

            if (!File.Exists(filePath))
                return;

            context.Response.ContentType = mimeType.MIME_Type;
            await using var fileStream = File.OpenRead(filePath);
            await fileStream.CopyToAsync(context.Response.Body);
        }
    }
}
