#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AdminViewer.UserGroupAdmin.SubViewers;
using SobekCM.Library.HTML.Helpers;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Class allows an authenticated system admin to view all existing user groups, and choose a user group to edit </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class. Refactored into the same
    /// orchestrator/SubViewer/Tab shape as <see cref="Users_AdminViewer"/> -- see
    /// <see cref="UserGroupAdminSubViewerBuilder"/>, <c>SubViewers/</c>, and <c>UserGroupAdminTabs/</c> --
    /// so a new tab (or a future group-specific subviewer) can be added the same way the Users editor
    /// already accommodates its own extra tabs (e.g. the OpenNJ Instructor tab). This screen never shows
    /// a list of groups itself; that already lives in the Users admin screen's list subviewer, so an
    /// invalid or missing group ID redirects there instead. </remarks>
    public class User_Group_AdminViewer : abstract_AdminViewer
    {
        private readonly iUserGroupAdminSubViewer subviewer;

        /// <summary> Constructor for a new instance of the User_Group_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <remarks> Postback from a user group edit is handled here in the constructor </remarks>
        public User_Group_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("User_Group_AdminViewer.Constructor", String.Empty);

            // Ensure there is a user
            if (RequestSpecificValues.Current_User == null)
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Ensure the user is the system admin, or user admin
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_User_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Create the subviewer -- NULL means the submode named no valid (or new) group
            subviewer = UserGroupAdminSubViewerBuilder.GetSubViewer(RequestSpecificValues, Context);
            if (subviewer == null)
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Users;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Perform post back work in the subviewer
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                subviewer.HandlePostback(RequestSpecificValues, Context);
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns various values depending on the current submode </value>
        public override string Web_Title => subviewer.Title;

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        public override string Viewer_Icon => Static_Resources_Gateway.Users_Img;

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("User_Group_AdminViewer.Write_HTML");

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            // Add the banner
            Banner_HtmlHelper.Add_Banner(Output, "sbkAhs_BannerDiv", "System Administration", RequestSpecificValues.Current_Mode, RequestSpecificValues.HTML_Skin, RequestSpecificValues.Top_Collection);

            // Add the hidden field
            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_user_group_save\" name=\"admin_user_group_save\" value=\"\" />");
            Output.WriteLine();

            Output.WriteLine("<!-- User_Group_AdminViewer.Write_HTML -->");
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            // Fill in the rest of the form from the subviewer
            subviewer.Write_SubView(Output, RequestSpecificValues, Tracer);

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass => (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.My_Sobek_SubMode)) ? "sbkUgav_ContainerInnerWide" : null;
    }
}
