using SobekCM.Core.Users;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public abstract class abstractUsersAdminSubViewer : iUsersAdminSubViewer
    {
        protected User_Object editUser;

        public User_Object EditUser
        {
            set { editUser = value;  }
        }

        public abstract string Title { get; }

        public abstract void HandlePostback(RequestCache RequestSpecificValues);

        public abstract void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer);
        
    }
}
