using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.SubViewers
{
    public abstract class abstractUserGroupAdminSubViewer : iUserGroupAdminSubViewer
    {
        protected User_Group editGroup;

        public User_Group EditGroup
        {
            set { editGroup = value; }
        }

        public abstract string Title { get; }

        public abstract void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context);

        public abstract void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer);
    }
}
