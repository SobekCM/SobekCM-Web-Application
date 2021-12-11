using SobekCM.Core.Users;
using SobekCM.Tools;
using System.Collections.Specialized;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    public interface iUserAdminTab
    {
        string TabName { get; }

        bool HandlePostback(NameValueCollection form, User_Object editUser, RequestCache RequestSpecificValues);

        void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer);
    }
}
