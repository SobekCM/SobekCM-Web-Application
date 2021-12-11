using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Email;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Web;

namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public class UserList_UsersAdminSubViewer : abstractUsersAdminSubViewer
    {
        private string actionMessage;

        public override string Title => "Registered Users and Groups";

        public override void HandlePostback(RequestCache RequestSpecificValues)
        {
            try
            {
                string reset_value = HttpContext.Current.Request.Form["admin_user_reset"];
                if (reset_value.Length > 0)
                {
                    int userid = Convert.ToInt32(reset_value);
                    User_Object reset_user = Engine_Database.Get_User(userid, RequestSpecificValues.Tracer);

                    // Create the random password
                    StringBuilder passwordBuilder = new StringBuilder();
                    Random randomGenerator = new Random(DateTime.Now.Millisecond);
                    while (passwordBuilder.Length < 12)
                    {
                        switch (randomGenerator.Next(0, 3))
                        {
                            case 0:
                                int randomNumber = randomGenerator.Next(65, 91);
                                if ((randomNumber != 79) && (randomNumber != 75)) // Omit the 'O' and the 'K', confusing
                                    passwordBuilder.Append((char)randomNumber);
                                break;

                            case 1:
                                int randomNumber2 = randomGenerator.Next(97, 123);
                                if ((randomNumber2 != 111) && (randomNumber2 != 108) && (randomNumber2 != 107))  // Omit the 'o' and the 'l' and the 'k', confusing
                                    passwordBuilder.Append((char)randomNumber2);
                                break;

                            case 2:
                                // Zero and one is omitted in this range, confusing
                                int randomNumber3 = randomGenerator.Next(50, 58);
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
                        if (Email_Helper.SendEmail(reset_user.Email, "my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation.ToUpper() + " Password Reset", reset_user.Full_Name + ",\n\nYour my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation.ToUpper() + " password has been reset to a temporary password.  The first time you logon, you will be required to change it.\n\n\tUsername: " + reset_user.UserName + "\n\tPassword: " + password + "\n\nYour password is case-sensitive and must be entered exactly as it appears above when logging on.\n\nIf you have any questions or problems logging on, feel free to contact us at " + UI_ApplicationCache_Gateway.Settings.Email.System_Email + ", or reply to this email.\n\n" + RequestSpecificValues.Current_Mode.Base_URL + "my/home\n", false, RequestSpecificValues.Current_Mode.Instance_Name))
                        {
                            if ((RequestSpecificValues.Current_User.UserID == 1) || (RequestSpecificValues.Current_User.UserID == 2))
                                actionMessage = "Reset of password (" + password + ") for '" + reset_user.Full_Name + "' complete";
                            else
                                actionMessage = "Reset of password for '" + reset_user.Full_Name + "' complete";
                        }
                        else
                        {
                            if ((RequestSpecificValues.Current_User.UserID == 1) || (RequestSpecificValues.Current_User.UserID == 2))
                                actionMessage = "ERROR while sending new password (" + password + ") to '" + reset_user.Full_Name + "'!";
                            else
                                actionMessage = "ERROR while sending new password to '" + reset_user.Full_Name + "'!";
                        }
                    }
                }

                string delete_value = HttpContext.Current.Request.Form["admin_user_group_delete"];
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
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.User_Groups;
            string redirect = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Users;

            // Show the RequestSpecificValues.Current_User groups
            if ((userGroup == null) || (userGroup.Count == 0))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.User_Groups;
                Output.WriteLine("<blockquote>No user groups exist within this library instance. <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Click here to add a new RequestSpecificValues.Current_User group.</a></blockquote>");
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Users;
            }
            else
            {
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "new";
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.User_Groups;
                Output.WriteLine("  <blockquote>Select a user group to edit or view.  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Click here to add a new user group.</a></blockquote>");
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Users;

                Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");
                Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
                Output.WriteLine("    <th width=\"200px\" align=\"left\"><span style=\"color: White\"> &nbsp; ACTIONS</span></th>");
                Output.WriteLine("    <th width=\"140px\" align=\"left\"><span style=\"color: White\">NAME</span></th>");
                Output.WriteLine("    <th align=\"left\"><span style=\"color: White\">DESCRIPTION</span></th>");
                Output.WriteLine("   </tr>");

                foreach (User_Group thisRow in userGroup)
                {
                    Output.WriteLine("  <tr align=\"left\" >");
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

            // Get the list of all users
            DataTable usersTable = SobekCM_Database.Get_All_Users(Tracer);

            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");
            Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("    <th width=\"190px\" align=\"left\"><span style=\"color: White\"> &nbsp; ACTIONS</span></th>");
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

                // Build the action links
                Output.WriteLine("  <tr align=\"left\" >");
                Output.Write("    <td class=\"SobekAdminActionLink\" >( ");

                Output.Write("<a title=\"Click to edit\" href=\"" + redirect.Replace("XXXXXXX", userid) + "\">edit</a> | ");
                Output.Write("<a title=\"Click to reset the password\" id=\"RESET_" + userid + "\" href=\"javascript:reset_password('" + userid + "','" + fullname.Replace("'", "") + "');\">reset password</a> | ");
                Output.Write("<a title=\"Click to view\" href=\"" + redirect.Replace("XXXXXXX", userid) + "v\">view</a> ) </td>");

                // Add the rest of the row with data
                Output.WriteLine("    <td>" + fullname + " ( " + username + " )</span></td>");
                Output.WriteLine("    <td>" + email + "</span></td>");
                Output.WriteLine("   </tr>");
                Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");

            }

            Output.WriteLine("</table>");
            Output.WriteLine("<br />");
            Output.WriteLine("</div>");
            Output.WriteLine();
        }
    }
}
