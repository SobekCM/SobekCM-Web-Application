using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.UserGroupAdminTabs
{
    public interface iUserGroupAdminTab
    {
        string TabName { get; }

        bool HandlePostback(IFormCollection form, User_Group editGroup, RequestCache RequestSpecificValues);

        void RenderHtml(TextWriter Output, User_Group editGroup, RequestCache RequestSpecificValues, Custom_Tracer Tracer);
    }
}
