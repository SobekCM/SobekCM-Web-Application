using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;


namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public abstract class abstractUsersAdminSubViewer : iUsersAdminSubViewer
    {
        protected User_Object editUser;

        public User_Object EditUser
        {
            set { editUser = value; }
        }

        public abstract string Title { get; }

        public abstract void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context);

        public abstract void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer);

    }
}
