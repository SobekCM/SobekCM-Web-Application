using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Tools;

namespace SobekCM.Library.AdminViewer
{
    public class User_Requests_AdminViewer : abstract_AdminViewer
    {
        public User_Requests_AdminViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {

        }

        public override string Web_Title => "User Permission Requests";

        public override string Viewer_Icon => Static_Resources_Gateway.Users_Img;

        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            
        }
    }
}
