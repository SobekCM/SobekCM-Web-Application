using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Core.Navigation;
using SobekCM.Tools;

namespace SobekCM.Library.MySobekViewer
{
    class Rights_Management_MySobekViewer : abstract_MySobekViewer
    {
        public Rights_Management_MySobekViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            // With the user logged in at this point check for the permission, if not held, go back
            if (RequestSpecificValues.Current_User.Get_Setting("Rights_Management_MySobekViewer", string.Empty) != "true")
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }
        }

        public override string Web_Title
        {
            get
            {
                { return "Rights Management"; }
            }
        }

        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("<p>Rights Management functionality under construction...</p>");
        }
    }
}