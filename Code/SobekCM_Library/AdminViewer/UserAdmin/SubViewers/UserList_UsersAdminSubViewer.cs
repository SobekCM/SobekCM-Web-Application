using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Email;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text;


namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public class UserList_UsersAdminSubViewer : abstractUsersAdminSubViewer
    {
        private string actionMessage;

        /// <summary> User setting which remembers whether this admin wants to see all users, or only active users </summary>
        private const string USER_FILTER_SETTING = "Users_AdminViewer:User Filter";

        /// <summary> User setting which remembers whether the top-level admin wants system users included in
        /// the list - meaningless (and never honored) for anyone who isn't the top-level admin, since system
        /// users are hidden from everyone else regardless of this setting </summary>
        private const string SYSTEM_USER_FILTER_SETTING = "Users_AdminViewer:System User Filter";

        public override string Title => "Registered Users and Groups";

        public override void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context)
        {
            // Save a change to the all / active users filter as this admin's preference
            string new_filter = Context.Request.Form["admin_user_filter"];
            if (((new_filter == "all") || (new_filter == "active")) && (new_filter != RequestSpecificValues.Current_User.Get_Setting(USER_FILTER_SETTING, "active")))
            {
                RequestSpecificValues.Current_User.Add_Setting(USER_FILTER_SETTING, new_filter);
                Engine_Database.Set_User_Setting(RequestSpecificValues.Current_User.UserID, USER_FILTER_SETTING, new_filter);
                SobekCM.Core.MemoryMgmt.CachedDataManager_UserCacheServices.Save_To_Session(Context.Session, RequestSpecificValues.Current_User);
            }

            // Save a change to the system users filter as this admin's preference - only ever shown to (and
            // honored for) the top-level admin, but harmless to save even if posted by anyone else
            string new_system_filter = Context.Request.Form["admin_user_system_filter"];
            if (((new_system_filter == "show") || (new_system_filter == "hide")) && (new_system_filter != RequestSpecificValues.Current_User.Get_Setting(SYSTEM_USER_FILTER_SETTING, "hide")))
            {
                RequestSpecificValues.Current_User.Add_Setting(SYSTEM_USER_FILTER_SETTING, new_system_filter);
                Engine_Database.Set_User_Setting(RequestSpecificValues.Current_User.UserID, SYSTEM_USER_FILTER_SETTING, new_system_filter);
                SobekCM.Core.MemoryMgmt.CachedDataManager_UserCacheServices.Save_To_Session(Context.Session, RequestSpecificValues.Current_User);
            }

            try
            {
                string reset_value = Context.Request.Form["admin_user_reset"];
                if (reset_value.Length > 0)
                {
                    int userid = Convert.ToInt32(reset_value);
                    User_Object reset_user = Engine_Database.Get_User(userid, RequestSpecificValues.Tracer);

                    // Deactivated users aren't returned above.  The link is disabled for them in the list, so this
                    // only happens with a direct post (or a user deactivated since the list was drawn)
                    if (reset_user == null)
                    {
                        actionMessage = "ERROR - Cannot reset the password of a user who is not active";
                        return;
                    }

                    // A system user's reset link is never rendered for anyone but the top-level admin, but the
                    // userid here comes straight off the posted form and is easy to guess - block it explicitly
                    // rather than relying on the link simply not being shown. Same "not active" message, so this
                    // doesn't reveal that the id belongs to a system user.
                    if (reset_user.Is_System_User)
                    {
                        bool isTopLevelAdmin = ((!UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (RequestSpecificValues.Current_User.Is_System_Admin)) || (RequestSpecificValues.Current_User.Is_Host_Admin);
                        if (!isTopLevelAdmin)
                        {
                            actionMessage = "ERROR - Cannot reset the password of a user who is not active";
                            return;
                        }
                    }

                    // Create the random password
                    var passwordBuilder = new StringBuilder();
                    while (passwordBuilder.Length < 12)
                    {
                        switch (RandomNumberGenerator.GetInt32(0, 3))
                        {
                            case 0:
                                int randomNumber = RandomNumberGenerator.GetInt32(65, 91);
                                if ((randomNumber != 79) && (randomNumber != 75)) // Omit the 'O' and the 'K', confusing
                                    passwordBuilder.Append((char)randomNumber);
                                break;

                            case 1:
                                int randomNumber2 = RandomNumberGenerator.GetInt32(97, 123);
                                if ((randomNumber2 != 111) && (randomNumber2 != 108) && (randomNumber2 != 107))  // Omit the 'o' and the 'l' and the 'k', confusing
                                    passwordBuilder.Append((char)randomNumber2);
                                break;

                            case 2:
                                // Zero and one is omitted in this range, confusing
                                int randomNumber3 = RandomNumberGenerator.GetInt32(50, 58);
                                passwordBuilder.Append((char)randomNumber3);
                                break;
                        }
                    }
                    string password = passwordBuilder.ToString();

                    // Reset this password
                    if (!SobekCM_Database.Reset_User_Password(userid, password, true, RequestSpecificValues.Tracer))
                    {
                        actionMessage = "ERROR reseting password";
                    }
                    else
                    {
                        if (Email_Helper.SendEmail(reset_user.Email, "my" + RequestSpecificValues.Current_Mode.Portal_Abbreviation.ToUpper() + " Password Reset", reset_user.Full_Name + ",\n\nYour my" + RequestSpecificValues.Current_Mode.Portal_Abbreviation.ToUpper() + " password has been reset to a temporary password.  The first time you logon, you will be required to change it.\n\n\tUsername: " + reset_user.UserName + "\n\tPassword: " + password + "\n\nYour password is case-sensitive and must be entered exactly as it appears above when logging on.\n\nIf you have any questions or problems logging on, feel free to contact us at " + UI_ApplicationCache_Gateway.Settings.Email.System_Email + ", or reply to this email.\n\n" + RequestSpecificValues.Current_Mode.Base_URL + "my/home\n", false, RequestSpecificValues.Current_Mode.Portal_Name))
                        {
                            actionMessage = "Reset of password (" + password + ") for '" + reset_user.Full_Name + "' successful";
                        }
                        else
                        {
                            actionMessage = "ERROR while sending new password (" + password + ") to '" + reset_user.Full_Name + "'!";
                        }
                    }
                }

                string delete_value = Context.Request.Form["admin_user_group_delete"];
                if (delete_value.Length > 0)
                {
                    int deleteId = Convert.ToInt32(delete_value);
                    int result = SobekCM_Database.Delete_User_Group(deleteId, null);
                    switch (result)
                    {
                        case 1:
                            actionMessage = "Succesfully deleted user group";
                            break;

                        case -1:
                            actionMessage = "ERROR while deleting user group - Cannot delete a user group which is still linked to users";
                            break;

                        case -2:
                            actionMessage = "ERROR - You cannot delete a special user group";
                            break;

                        case -3:
                            actionMessage = "ERROR while deleting user group - unknown exception caught";
                            break;

                    }
                    return;
                }
            }
            catch
            {
                actionMessage = "ERROR while checking postback";
            }
        }

        public override void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {

            Output.WriteLine("<div class=\"SobekHomeText\">");

            // Display the action message if there is one
            if (!String.IsNullOrWhiteSpace(actionMessage))
            {
                Output.WriteLine("  <br />");
                Output.WriteLine("  <center><b>" + actionMessage + "</b></center>");
            }

            Output.WriteLine("  <br />");
            Output.WriteLine("  <span class=\"SobekAdminTitle\">Existing User Groups</span>");
            Output.WriteLine("  <br /><br />");

            // get the list of all RequestSpecificValues.Current_User groups
            List<User_Group> userGroup = Engine_Database.Get_All_User_Groups(Tracer);

            // Get the redirect
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "XXXXXXX";
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.User_Groups;
            string redirect = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Users;

            // Show the RequestSpecificValues.Current_User groups
            if ((userGroup == null) || (userGroup.Count == 0))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.User_Groups;
                Output.WriteLine("<blockquote>No user groups exist within this library instance. <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Click here to add a new RequestSpecificValues.Current_User group.</a></blockquote>");
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Users;
            }
            else
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.User_Groups;
                Output.WriteLine("  <blockquote>Select a user group to edit or view.  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Click here to add a new user group.</a></blockquote>");
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Users;

                Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\" id=\"sbkAdmListUsers_UsersGroupTable\">");
                Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
                Output.WriteLine("    <th width=\"200px\" align=\"left\"><span style=\"color: White\"> &nbsp; ACTIONS</span></th>");
                Output.WriteLine("    <th width=\"140px\" align=\"left\"><span style=\"color: White\">NAME</span></th>");
                Output.WriteLine("    <th align=\"left\"><span style=\"color: White\">DESCRIPTION</span></th>");
                Output.WriteLine("   </tr>");

                foreach (User_Group thisRow in userGroup)
                {
                    Output.WriteLine("  <tr align=\"left\" class=\"sbkAdmListUsers_ContentRow\" >");
                    Output.Write("    <td class=\"SobekAdminActionLink\" >( ");

                    Output.Write("<a title=\"Click to edit\" href=\"" + redirect.Replace("XXXXXXX", thisRow.UserGroupID.ToString()) + "\">edit</a> | ");
                    Output.Write("<a title=\"Click to view\" href=\"" + redirect.Replace("XXXXXXX", thisRow.UserGroupID.ToString()) + "v\">view</a>");
                    if (!thisRow.IsSpecialGroup)
                        Output.Write(" | <a title=\"Click to delete this user group entirely\" href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "l/technical/javascriptrequired\" onclick=\"return delete_user_group('" + thisRow.Name + "'," + thisRow.UserGroupID + ");\">delete</a> ) </td>");
                    else
                        Output.Write(" ) </td>");


                    Output.WriteLine("    <td>" + thisRow.Name + "</td>");
                    Output.WriteLine("    <td>" + thisRow.Description + "</td>");
                    Output.WriteLine("  </tr>");
                    Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");
                }

                Output.WriteLine("</table>");
                Output.WriteLine("  <br />");
            }
            Output.WriteLine("  <br />");

            // List of all users
            Output.WriteLine("  <span class=\"SobekAdminTitle\">Existing Registered Users</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>Select a user to edit. Click <i>reset password</i> to email a new temporary password to the user.</blockquote>");

            // All / active users filter, remembered in this admin's user settings
            bool active_only = RequestSpecificValues.Current_User.Get_Setting(USER_FILTER_SETTING, "active") != "all";
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <label for=\"admin_user_filter\">Show:</label> ");
            Output.WriteLine("    <select id=\"admin_user_filter\" name=\"admin_user_filter\" onchange=\"this.form.submit();\">");
            Output.WriteLine("      <option value=\"active\"" + (active_only ? " selected=\"selected\"" : String.Empty) + ">Active Users</option>");
            Output.WriteLine("      <option value=\"all\"" + (active_only ? String.Empty : " selected=\"selected\"") + ">All Users</option>");
            Output.WriteLine("    </select>");

            // Whether this is the top-level admin - the Host Administrator on a hosted instance, otherwise the
            // System Administrator - who is the only one allowed to see system users at all
            bool isTopLevelAdmin = ((!UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (RequestSpecificValues.Current_User.Is_System_Admin)) || (RequestSpecificValues.Current_User.Is_Host_Admin);
            bool showSystemUsers = false;
            if (isTopLevelAdmin)
            {
                showSystemUsers = RequestSpecificValues.Current_User.Get_Setting(SYSTEM_USER_FILTER_SETTING, "hide") == "show";
                Output.WriteLine("    &nbsp; &nbsp; <label for=\"admin_user_system_filter\">System Users:</label> ");
                Output.WriteLine("    <select id=\"admin_user_system_filter\" name=\"admin_user_system_filter\" onchange=\"this.form.submit();\">");
                Output.WriteLine("      <option value=\"hide\"" + (showSystemUsers ? String.Empty : " selected=\"selected\"") + ">Hide</option>");
                Output.WriteLine("      <option value=\"show\"" + (showSystemUsers ? " selected=\"selected\"" : String.Empty) + ">Show</option>");
                Output.WriteLine("    </select>");
            }

            Output.WriteLine("  </blockquote>");

            // Get the list of all users
            DataTable usersTable = SobekCM_Database.Get_All_Users(Tracer);
            bool has_active_column = usersTable.Columns.Contains("isActive");
            bool has_system_user_column = usersTable.Columns.Contains("IsSystemUser");

            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\" id=\"sbkAdmListUsers_UsersTable\">");
            Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("    <th width=\"220px\" align=\"left\"><span style=\"color: White\"> &nbsp; ACTIONS</span></th>");
            Output.WriteLine("    <th width=\"32px\"></th>");
            Output.WriteLine("    <th width=\"320px\" align=\"left\"><span style=\"color: White\">NAME</span></th>");
            Output.WriteLine("    <th align=\"left\"><span style=\"color: White\">EMAIL</span></th>");
            Output.WriteLine("   </tr>");
            Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");

            // Get the redirect
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "XXXXXXX";
            redirect = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;

            // Write the data for each interface
            foreach (DataRow thisRow in usersTable.Rows)
            {
                // Pull all these values
                string userid = thisRow["UserID"].ToString();
                string fullname = thisRow["Full_Name"].ToString();
                string username = thisRow["UserName"].ToString();
                string email = thisRow["EmailAddress"].ToString();
                int requests = Int32.Parse(thisRow["PendingRequests"].ToString());
                bool is_active = (!has_active_column) || (Convert.ToBoolean(thisRow["isActive"]));
                bool is_system_user = (has_system_user_column) && (Convert.ToBoolean(thisRow["IsSystemUser"]));

                // Skip deactivated users, unless showing all users
                if ((active_only) && (!is_active))
                    continue;

                // System users are hidden entirely from anyone who isn't the top-level admin; for the
                // top-level admin, only shown when their System Users filter is set to "show"
                if ((is_system_user) && (!showSystemUsers))
                    continue;

                // Build the action links
                Output.WriteLine("  <tr align=\"left\" class=\"sbkAdmListUsers_ContentRow\" >");
                Output.Write("    <td class=\"SobekAdminActionLink\" >( ");

                Output.Write("<a title=\"Click to edit\" href=\"" + redirect.Replace("XXXXXXX", userid) + "\">edit</a> | ");
                if (is_active)
                    Output.Write("<a title=\"Click to reset the password\" id=\"RESET_" + userid + "\" href=\"javascript:reset_password('" + userid + "','" + fullname.Replace("'", "") + "');\">reset password</a> | ");
                else
                    Output.Write("<span title=\"Disabled since this user is not active\" id=\"RESET_" + userid + "\" style=\"color:#999999;cursor:not-allowed;\">reset password</span> | ");
                Output.Write("<a title=\"Click to view\" href=\"" + redirect.Replace("XXXXXXX", userid) + "v\">view</a> ) </td>");

                // Any pending requests?
                if (requests > 0)
                {
                    string request_title = "1 pending user request!";
                    if (requests > 1)
                        request_title = requests + " pending user requests!";
                    Output.WriteLine("    <td><img src=\"" + Static_Resources_Gateway.Warning_Img_Small + "\" title=\"" + request_title + "\" /></td>");
                }
                else
                {
                    Output.WriteLine("    <td></td>");
                }

                // Add the rest of the row with data
                Output.WriteLine("    <td><span>" + fullname + " ( " + username + " )</span></td>");
                Output.WriteLine("    <td><span>" + email + "</span></td>");
                Output.WriteLine("   </tr>");
                Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"4\"></td></tr>");

            }

            Output.WriteLine("</table>");
            Output.WriteLine("<br />");
            Output.WriteLine("</div>");
            Output.WriteLine();
        }
    }
}
