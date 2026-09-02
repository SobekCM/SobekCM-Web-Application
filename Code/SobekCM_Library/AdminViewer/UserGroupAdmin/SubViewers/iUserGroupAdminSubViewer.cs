using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.SubViewers
{
    public interface iUserGroupAdminSubViewer
    {
        User_Group EditGroup { set; }

        string Title { get; }

        void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer);

        void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context);
    }
}
