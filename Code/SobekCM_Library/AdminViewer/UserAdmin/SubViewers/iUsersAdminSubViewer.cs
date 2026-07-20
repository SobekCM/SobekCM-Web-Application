using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;


namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public interface iUsersAdminSubViewer
    {
        User_Object EditUser { set; }

        string Title { get; }

        void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer);

        void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context);

    }
}
