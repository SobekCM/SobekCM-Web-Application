#region Using directives

using System;
using System.IO;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AdminViewer.UserAdmin.SubViewers;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Class allows an authenticated system admin to view all existing users, and choose a RequestSpecificValues.Current_User to edit </summary>
    /// <remarks> This class extends the <see cref="abstract_AdminViewer"/> class.<br /><br />
    /// MySobek Viewers are used for registration and authentication with mySobek, as well as performing any task which requires
    /// authentication, such as online submittal, metadata editing, and system administrative tasks.<br /><br />
    /// During a valid html request, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
    /// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  Since this action requires authentication, an instance of the  <see cref="MySobek_HtmlSubwriter"/> class is created. </li>
    /// <li>The mySobek subwriter creates an instance of this viewer to view all registered users digital library</li>
    /// </ul></remarks>
    public class Users_AdminViewer : abstract_AdminViewer
    {
        private iUsersAdminSubViewer subviewer;

		/// <summary> Constructor for a new instance of the Users_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
		/// <remarks> Postback from a RequestSpecificValues.Current_User edit or from reseting a RequestSpecificValues.Current_User's password is handled here in the constructor </remarks>
        public Users_AdminViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
		{
            RequestSpecificValues.Tracer.Add_Trace("Users_AdminViewer.Constructor", String.Empty);

			// Ensure there is a user
			if (RequestSpecificValues.Current_User == null)
			{
				RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
				RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
				UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
				return;
			}

            // Ensure the user is the system admin, or user admin
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_User_Admin))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Create the subviewer
            subviewer = UsersAdminSubViewerBuilder.GetSubViewer(RequestSpecificValues);

		    // Perform post back work in the subviewer
			if (RequestSpecificValues.Current_Mode.isPostBack)
			{
                subviewer.HandlePostback(RequestSpecificValues);
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
        /// <remarks> This class does nothing </remarks>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Users_AdminViewer.Write_HTML", "Do nothing");
        }

        /// <summary> This is an opportunity to write HTML directly into the main form, without
        /// using the pop-up html form architecture </summary>
        /// <param name="Output"> Textwriter to write the pop-up form HTML for this viewer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This text will appear within the ItemNavForm form tags </remarks>
		public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Users_AdminViewer.Write_ItemNavForm_Closing", "Add hidden field");

            // Add the hidden field
            Output.WriteLine("<!-- Hidden field is used for postbacks to indicate what to save and reset -->");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_user_reset\" name=\"admin_user_reset\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_user_save\" name=\"admin_user_save\" value=\"\" />");
            Output.WriteLine("<input type=\"hidden\" id=\"admin_user_group_delete\" name=\"admin_user_group_delete\" value=\"\" />");
            Output.WriteLine();

            Tracer.Add_Trace("Users_AdminViewer.Write_ItemNavForm_Closing", "Add the rest of the form");

            Output.WriteLine("<!-- Users_AdminViewer.Write_ItemNavForm_Closing -->");
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Admin_Js + "\" type=\"text/javascript\"></script>");

            // Fill in the rest of the form from the subviewer
            subviewer.Write_SubView(Output, RequestSpecificValues, Tracer);
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass => (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.My_Sobek_SubMode)) ? "sbkUgav_ContainerInnerWide" : null;
    }
}