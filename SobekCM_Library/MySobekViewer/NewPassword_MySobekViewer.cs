#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an authenticated user to change their password </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// MySobek Viewers are used for registration and authentication with mySobek, as well as performing any task which requires
    /// authentication, such as online submittal, metadata editing, and system administrative tasks.<br /><br />
    /// During a valid html request, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
    /// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  Since this action requires authentication, an instance of the  <see cref="MySobek_HtmlSubwriter"/> class is created. </li>
    /// <li>The mySobek subwriter creates an instance of this viewer for changing their password </li>
    /// </ul></remarks>
    public class NewPassword_MySobekViewer : abstract_MySobekViewer
    {
        private readonly List<string> validationErrors;
        private readonly bool registration;
        private readonly User_Object user;

        /// <summary> Constructor for a new instance of the NewPassword_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public NewPassword_MySobekViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            RequestSpecificValues.Tracer.Add_Trace("NewPassword_MySobekViewer.Constructor", String.Empty);

            validationErrors = new List<string>();

            user = RequestSpecificValues.Current_User;
            registration = (Context.SessionObject()["user"] == null);
            if (registration)
                user = new User_Object();

            if (!RequestSpecificValues.Current_Mode.isPostBack)
                return;

            string current_password = String.Empty;
            string new_password = String.Empty;
            string new_password2 = String.Empty;

            string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
            foreach (string thisKey in getKeys)
            {
                switch (thisKey)
                {
                    case "current_password_enter":
                        current_password = HttpContext.Current.Request.Form[thisKey];
                        break;

                    case "new_password_enter":
                        new_password = HttpContext.Current.Request.Form[thisKey];
                        break;

                    case "new_password_confirm":
                        new_password2 = HttpContext.Current.Request.Form[thisKey];
                        break;
                }
            }

            if ((new_password.Trim().Length == 0) || (new_password2.Trim().Length == 0))
                validationErrors.Add("Select and confirm a new password");
            if (new_password != new_password2)
                validationErrors.Add("New passwords do not match");
            else if ((new_password.Length < 8) && (new_password.Length > 0))
                validationErrors.Add("Password must be at least eight digits");
            if (validationErrors.Count == 0)
            {
                if (new_password == current_password)
                    validationErrors.Add("The new password cannot match the old password");
            }

            if (validationErrors.Count == 0)
            {
                bool success = SobekCM_Database.Change_Password(user.UserName, current_password, new_password, false, RequestSpecificValues.Tracer);
                if (success)
                {
                    user.Is_Temporary_Password = false;
                    string raw_url = HttpContext.Current.Request.RawUrl;
                    if (raw_url.ToUpper().IndexOf("M=HML") > 0)
                    {
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    }
                    else
                    {
                        HttpContext.Current.Response.Redirect(raw_url, false);
                        HttpContext.Current.ApplicationInstance.CompleteRequest();
                        RequestSpecificValues.Current_Mode.Request_Completed = true;
                    }
                }
                else
                {
                    validationErrors.Add("Unable to change password.  Verify current password.");
                }
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This always returns the value 'Change your password' </value>
        public override string Web_Title
        {
            get { return "Change your password"; }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("NewPassword_MySobekViewer.Write_HTML", "Do nothing");
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area with the form </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_ItemNavForm_Opening(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("NewPassword_MySobekViewer.Write_ItemNavForm_Opening", String.Empty);

            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<div class=\"SobekHomeText\">");
            Output.WriteLine("<br />");
            Output.WriteLine("<blockquote>");
            Output.WriteLine(user.Is_Temporary_Password
                ? "You are required to change your password to continue."
                : "Please enter your existing password and your new password.");

            if (validationErrors.Count > 0)
            {
                Output.WriteLine("<br /><br /><strong><span style=\"color: Red\">The following errors were detected:");
                Output.WriteLine("<blockquote>");
                foreach (string thisError in validationErrors)
                    Output.WriteLine(thisError + "<br />");
                Output.WriteLine("</blockquote>");
                Output.WriteLine("</span></strong>");
            }

            Output.WriteLine("<table width=\"700px\">");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td width=\"180px\">&nbsp;</td>");
            Output.WriteLine("    <td width=\"200px\"><label for=\"current_password_enter\">Existing Password:</label></td>");
            Output.WriteLine("    <td width=\"180px\"><input type=\"password\" id=\"current_password_enter\" name=\"current_password_enter\" class=\"preferences_small_input sbk_Focusable\" /></td>");
            Output.WriteLine("    <td width=\"140px\">&nbsp;</td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td>&nbsp;</td>");
            Output.WriteLine("    <td><label for=\"new_password_enter\">New Password:</label></td>");
            Output.WriteLine("    <td><input type=\"password\" id=\"new_password_enter\" name=\"new_password_enter\" class=\"preferences_small_input sbk_Focusable\" /></td>");
            Output.WriteLine("    <td>&nbsp;</td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine("  <tr>");
            Output.WriteLine("    <td>&nbsp;</td>");
            Output.WriteLine("    <td><label for=\"new_password_confirm\">Confirm New Password:</label></td>");
            Output.WriteLine("    <td><input type=\"password\" id=\"new_password_confirm\" name=\"new_password_confirm\" class=\"preferences_small_input sbk_Focusable\" /></td>");
            Output.WriteLine("    <td>&nbsp;</td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine("  <tr align=\"right\" valign=\"bottom\" height=\"50px\">");
            Output.WriteLine("    <td colspan=\"3\">");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Log_Out;
            Output.WriteLine("      <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\"><img src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "design/skins/" + RequestSpecificValues.Current_Mode.Base_Skin_Or_Skin + "/buttons/cancel_button.gif\" border=\"0\" alt=\"CANCEL\" /></a> &nbsp; ");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.New_Password;
            Output.WriteLine("      <button type=\"submit\" class=\"sbkMySobek_BigButton\"> SAVE </button>");
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
            Output.WriteLine("</table>");
            Output.WriteLine("</blockquote>");
            Output.WriteLine("</div>");
            Output.WriteLine();
            Output.WriteLine("<!-- Focus on current password text box -->");
            Output.WriteLine("<script type=\"text/javascript\">focus_element('current_password_enter');</script>");
        }
    }
}
