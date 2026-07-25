using Microsoft.AspNetCore.Http;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Tools;
using System.IO;

namespace SobekCM.Library.AdminViewer
{
    public class User_Requests_AdminViewer : abstract_AdminViewer
    {
        public User_Requests_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {

        }

        public override string Web_Title => "User Permission Requests";

        public override string Viewer_Icon => Static_Resources_Gateway.Users_Img;

        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add the banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);



            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
