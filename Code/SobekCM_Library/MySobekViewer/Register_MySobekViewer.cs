#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Email;
using SobekCM.Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an unauthenticated user to register a brand-new account </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// Split out of what used to be a combined Preferences_MySobekViewer class (registration + editing an
    /// existing user's preferences, switched by a "registration" flag) so registration and preferences-editing
    /// are two independent viewers, matching the pretty URL split ("my/register" vs "my/preferences"). Shared
    /// personal/affiliation/language field handling lives in <see cref="Preferences_Form_Helper"/> rather than
    /// being duplicated here. </remarks>
    public class Register_MySobekViewer : abstract_MySobekViewer
    {
        private readonly List<string> validationErrors;
        private readonly bool desire_to_upload;
        private readonly string username;
        private string ufid;
        private readonly Preferences_Common_Fields commonFields;
        private User_Object user;

        private readonly string mySobekText;
        private readonly string accountInfoLabel;
        private readonly string userNameLabel;
        private readonly string personalInfoLabel;
        private readonly string familyNamesLabel;
        private readonly string givenNamesLabel;
        private readonly string nicknameLabel;
        private readonly string emailLabel;
        private readonly string affilitionInfoLabel;
        private readonly string organizationLabel;
        private readonly string collegeLabel;
        private readonly string departmentLabel;
        private readonly string unitLabel;
        private readonly string otherPreferencesLabel;
        private readonly string languageLabel;
        private readonly string passwordLabel;
        private readonly string confirmPasswordLabel;
        private readonly string col1Width;
        private readonly string col2Width;
        private readonly string col3Width;

        /// <summary> Constructor for a new instance of the Register_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Register_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Register_MySobekViewer.Constructor", String.Empty);

            // Self-registration is off for this instance (e.g. it only wants sign-in through OIDC/SAML) -
            // send anyone who lands here back to the logon page instead
            if (!UI_ApplicationCache_Gateway.Configuration.Authentication.AllowLocalAuth)
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            validationErrors = new List<string>();
            user = new User_Object();
            commonFields = new Preferences_Common_Fields();

            mySobekText = "my" + RequestSpecificValues.Current_Mode.Portal_Abbreviation;

            string displayLanguage = RequestSpecificValues.Current_Mode.Language;
            accountInfoLabel = Localization_Gateway.Preferences.Account_Info(displayLanguage);
            userNameLabel = Localization_Gateway.Preferences.Username_Label(displayLanguage);
            personalInfoLabel = Localization_Gateway.Preferences.Personal_Info(displayLanguage);
            familyNamesLabel = Localization_Gateway.Preferences.Family_Names_Label(displayLanguage);
            givenNamesLabel = Localization_Gateway.Preferences.Given_Names_Label(displayLanguage);
            nicknameLabel = Localization_Gateway.Preferences.Nickname_Label(displayLanguage);
            emailLabel = Localization_Gateway.Preferences.Email_Label(displayLanguage);
            affilitionInfoLabel = Localization_Gateway.Preferences.Affiliation_Info(displayLanguage);
            organizationLabel = Localization_Gateway.Preferences.Organization_Label(displayLanguage);
            collegeLabel = Localization_Gateway.Preferences.College_Label(displayLanguage);
            departmentLabel = Localization_Gateway.Preferences.Department_Label(displayLanguage);
            unitLabel = Localization_Gateway.Preferences.Unit_Label(displayLanguage);
            otherPreferencesLabel = Localization_Gateway.Preferences.Other_Preferences_Label(displayLanguage);
            languageLabel = Localization_Gateway.Preferences.Language_Label(displayLanguage);
            passwordLabel = Localization_Gateway.Preferences.Password_Label(displayLanguage);
            confirmPasswordLabel = Localization_Gateway.Preferences.Confirm_Password_Label(displayLanguage);

            col1Width = "15px";
            col2Width = "220px";
            col3Width = "605px";

            username = String.Empty;
            ufid = String.Empty;

            // Handle post back
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                string password = String.Empty;
                string password2 = String.Empty;

                var getKeys = Context.Request.Form.Keys;
                foreach (string thisKey in getKeys)
                {
                    switch (thisKey)
                    {
                        case "prefUserName":
                            username = Context.Request.Form[thisKey];
                            break;

                        case "password_enter":
                            password = Context.Request.Form[thisKey];
                            break;

                        case "password_confirm":
                            password2 = Context.Request.Form[thisKey];
                            break;

                        case "prefUfid":
                            ufid = Context.Request.Form[thisKey].TrimFirst().Replace("-", "");
                            break;

                        case "prefAllowSubmit":
                            string submit_value = Context.Request.Form[thisKey];
                            if (submit_value == "allowsubmit")
                                desire_to_upload = true;
                            break;
                    }
                }

                Preferences_Form_Helper.Parse_Common_Fields(Context, commonFields);

                // validate user name
                if (username.Trim().Length == 0)
                    validationErrors.Add(Localization_Gateway.Preferences.Username_Required(displayLanguage));
                else if (username.Trim().Length < 8)
                    validationErrors.Add(Localization_Gateway.Preferences.Username_Min_Length(displayLanguage));

                // validate password
                if ((password.Trim().Length == 0) || (password2.Trim().Length == 0))
                    validationErrors.Add(Localization_Gateway.Preferences.Select_Confirm_Password(displayLanguage));
                if (password.Trim() != password2.Trim())
                    validationErrors.Add(Localization_Gateway.Preferences.Passwords_Do_Not_Match(displayLanguage));
                else if (password.Length < 8)
                    validationErrors.Add(Localization_Gateway.Preferences.Password_Min_Length(displayLanguage));

                // validate UFID (UF only)
                if (ufid.Trim().Length > 0)
                {
                    if (ufid.Trim().Length != 8)
                    {
                        validationErrors.Add(Localization_Gateway.Preferences.Ufid_Length(displayLanguage));
                    }
                    else
                    {
                        int ufid_convert_test;
                        if (!Int32.TryParse(ufid, out ufid_convert_test))
                            validationErrors.Add(Localization_Gateway.Preferences.Ufid_Numeric(displayLanguage));
                    }
                }

                Preferences_Form_Helper.Validate_Common_Fields(commonFields, validationErrors, displayLanguage);

                if (validationErrors.Count == 0)
                {
                    bool email_exists;
                    bool username_exists;
                    SobekCM_Database.UserName_Exists(username, commonFields.Email, out username_exists, out email_exists, RequestSpecificValues.Tracer);
                    if (email_exists)
                    {
                        validationErrors.Add(Localization_Gateway.Preferences.Email_Already_Exists(displayLanguage));
                    }
                    else if (username_exists)
                    {
                        validationErrors.Add(Localization_Gateway.Preferences.Username_Taken(displayLanguage));
                    }
                }

                if (validationErrors.Count == 0)
                {
                    commonFields.FamilyName = Preferences_Form_Helper.Capitalize_Name(commonFields.FamilyName);
                    commonFields.GivenName = Preferences_Form_Helper.Capitalize_Name(commonFields.GivenName);

                    // Now, add this information to the user, so the new user can be saved
                    user.College = commonFields.College.Trim();
                    user.Department = commonFields.Department.Trim();
                    user.Email = commonFields.Email.Trim();
                    user.Family_Name = commonFields.FamilyName.Trim();
                    user.Given_Name = commonFields.GivenName.Trim();
                    user.Nickname = commonFields.Nickname.Trim();
                    user.Organization = commonFields.Organization.Trim();
                    user.Unit = commonFields.Unit.Trim();
                    user.Preferred_Language = commonFields.Language;
                    user.Default_Rights = String.Empty;
                    user.Receive_Stats_Emails = true;

                    user.Can_Submit = false;
                    user.Send_Email_On_Submission = true;
                    user.ShibbID = ufid;
                    user.UserName = username;
                    user.UserID = -1;

                    // See if we can match the institution.. if so, assign the org code
                    if ((String.IsNullOrEmpty(user.Organization_Code)) || (!String.IsNullOrEmpty(user.Organization)))
                    {
                        foreach (var inst in UI_ApplicationCache_Gateway.Aggregations.All_Aggregations)
                        {
                            if (inst.Type.IndexOf("institution", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if ((inst.Name.Equals(user.Organization, StringComparison.OrdinalIgnoreCase)) ||
                                    (inst.ShortName.Equals(user.Organization, StringComparison.OrdinalIgnoreCase)))
                                {
                                    user.Organization_Code = inst.Code;
                                    break;
                                }
                            }
                        }
                    }

                    // Save this new user
                    SobekCM_Database.Save_User(user, password, user.Authentication_Type, RequestSpecificValues.Tracer);

                    // Retrieve the user from the database
                    user = Engine_Database.Get_User(username, password, RequestSpecificValues.Tracer);

                    // Special code in case this is the very first user
                    if (user.UserID == 1)
                    {
                        // Save the updates to this admin user
                        SobekCM_Database.Save_User(user, password, User_Authentication_Type_Enum.Sobek, RequestSpecificValues.Tracer);
                        SobekCM_Database.Update_SobekCM_User(user.UserID, true, true, true, true, true, true, true, true, true, "edit_internal", "editmarc_internal", true, true, RequestSpecificValues.Tracer);

                        // Retrieve the user information again
                        user = Engine_Database.Get_User(username, password, RequestSpecificValues.Tracer);

                        // Also, use the current email address for some system emails
                        if (user.Email.Length > 0)
                        {
                            Engine_Database.Set_Setting("System Email", user.Email);
                            Engine_Database.Set_Setting("System Error Email", user.Email);
                            Engine_Database.Set_Setting("Privacy Email Address", user.Email);
                            Engine_Database.Set_Setting("Email Default From Address", user.Email);
                        }
                    }

                    user.Is_Just_Registered = true;
                    Context.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));

                    // Will we be sending an email?
                    if ((!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email)) || (desire_to_upload))
                    {
                        // Build the information about this registrant
                        var builder = new StringBuilder();
                        builder.Append("Name: " + user.Full_Name + "<br />");
                        builder.Append("Email: " + user.Email + "<br />");
                        builder.Append("UserName: " + user.UserName + "<br />");
                        if (!String.IsNullOrEmpty(user.Organization))
                            builder.Append("Organization: " + user.Organization + "<br />");
                        builder.Append("System Name: " + RequestSpecificValues.Current_Mode.Portal_Abbreviation + "<br />");
                        builder.Append("System URL: " + RequestSpecificValues.Current_Mode.Base_URL + "</br />");

                        // If they want to be able to contribue, send an email
                        if (!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email))
                        {
                            if (desire_to_upload)
                            {
                                Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email, "New user registered " + user.Full_Name, "New user requested ability to submit new items to " + UI_ApplicationCache_Gateway.Settings.System.System_Code + ".<br /><br /><blockquote>" + builder + "</blockquote>", true, RequestSpecificValues.Current_Mode.Portal_Name);
                            }
                            else
                            {
                                Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email, "New user registered " + user.Full_Name, "A new user registered to use " + UI_ApplicationCache_Gateway.Settings.System.System_Code + ".<br /><br /><blockquote>" + builder + "</blockquote>", true, RequestSpecificValues.Current_Mode.Portal_Name);
                            }
                        }
                        else if (desire_to_upload)
                        {
                            Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.System_Email, "Submittal rights requested by " + user.Full_Name, "New user requested ability to submit new items.<br /><br /><blockquote>" + builder + "</blockquote>", true, RequestSpecificValues.Current_Mode.Portal_Name);
                        }
                    }

                    // Email the user their registation information
                    if (desire_to_upload)
                    {
                        Email_Helper.SendEmail(user.Email, "Welcome to " + mySobekText, "<strong>Thank you for registering for " + mySobekText + "</strong><br /><br />You can access this directly through the following link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/my\">" + RequestSpecificValues.Current_Mode.Base_URL + "/my</a><br /><br />Full Name: " + user.Full_Name + "<br />User Name: " + user.UserName + "<br /><br />You will receive an email when your request to submit items has been processed.", true, RequestSpecificValues.Current_Mode.Portal_Name);
                    }
                    else
                    {
                        Email_Helper.SendEmail(user.Email, "Welcome to " + mySobekText, "<strong>Thank you for registering for " + mySobekText + "</strong><br /><br />You can access this directly through the following link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/my\">" + RequestSpecificValues.Current_Mode.Base_URL + "/my</a><br /><br />Full Name: " + user.Full_Name + "<br />User Name: " + user.UserName, true, RequestSpecificValues.Current_Mode.Portal_Name);
                    }

                    // Now, forward back to the My Sobek home page
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;

                    // If this is the first user to register (who would have been set to admin), send to the
                    // system-wide settings screen
                    if (user.UserID == 1)
                    {
                        RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                        RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Settings;
                    }
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                }
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get
            {
                return String.Format(Localization_Gateway.Preferences.Register_Page_Title_Format(RequestSpecificValues.Current_Mode.Language), RequestSpecificValues.Current_Mode.Portal_Abbreviation);
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output">Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Register_MySobekViewer.Write_HTML");

            string displayLanguage = RequestSpecificValues.Current_Mode.Language;

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<h1>" + Web_Title + "</h1>");
            Output.WriteLine();
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<div class=\"SobekHomeText\" >");
            Output.WriteLine("<blockquote>");

            Output.WriteLine(String.Format(Localization_Gateway.Preferences.Registration_Intro_Format(displayLanguage), mySobekText) + "<br /><br />");
            Output.WriteLine(Localization_Gateway.Preferences.Account_Required_Note(displayLanguage) + "<br /><br />");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
            string log_on_link = "<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.Preferences.Log_On_Link_Text(displayLanguage) + "</a>";
            Output.WriteLine(String.Format(Localization_Gateway.Preferences.Already_Registered_Format(displayLanguage), log_on_link) + "<br /><br />");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;

            if (validationErrors.Count > 0)
            {
                Output.WriteLine("<span style=\"color: Red;font-weight:bold;\">" + Localization_Gateway.Preferences.Errors_Detected_Header(displayLanguage));
                Output.WriteLine("<blockquote>");
                foreach (string thisError in validationErrors)
                {
                    Output.WriteLine(thisError + "<br />");
                }
                Output.WriteLine("</blockquote>");
                Output.WriteLine("</span>");
            }

            Output.WriteLine("<table style=\"width:700px;\" cellpadding=\"5px\" class=\"sbkPmsv_InputTable\" >");
            Output.WriteLine("  <tr><th colspan=\"3\">" + accountInfoLabel + "</td></tr>");

            // If there was a gatorlink ufid, use that
            if (Context.SessionObject()["Gatorlink_UFID"] != null)
                ufid = Context.SessionObject()["Gatorlink_UFID"].ToString();

            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td style=\"width:" + col2Width + "\" class=\"sbkPmsv_InputLabel\"><label for=\"prefUsername\">" + userNameLabel + ":</label></td><td width=\"" + col3Width + "\"><input id=\"prefUserName\" name=\"prefUserName\" class=\"preferences_small_input sbk_Focusable\" value=\"" + username + "\" type=\"text\" />   &nbsp; &nbsp; " + Localization_Gateway.Preferences.Username_Hint(displayLanguage) + "</td></tr>");
            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"password_enter\">" + passwordLabel + ":</label></td><td>");
            Output.WriteLine("    <input type=\"password\" id=\"password_enter\" name=\"password_enter\" class=\"preferences_small_input sbk_Focusable\" value=\"\" />");
            Output.WriteLine("     &nbsp; &nbsp; " + Localization_Gateway.Preferences.Password_Hint(displayLanguage) + "</td></tr>");
            Output.WriteLine("  <tr><td width=\"" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"password_confirm\">" + confirmPasswordLabel + ":</label></td><td>");
            Output.WriteLine("    <input type=\"password\" id=\"password_confirm\" name=\"password_confirm\" class=\"preferences_small_input sbk_Focusable\" value=\"\" />");
            Output.WriteLine("     &nbsp; &nbsp; " + Localization_Gateway.Preferences.Password_Hint(displayLanguage) + "</td></tr>");

            Output.WriteLine("  <tr><th colspan=\"3\">" + personalInfoLabel + "</td></tr>");

            Preferences_Form_Helper.Write_Personal_Info_Rows(Output, commonFields, col1Width, givenNamesLabel, familyNamesLabel, nicknameLabel, emailLabel);

            if ((UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0))
            {
                Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefUfid\">" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label + ":</label></td><td><input id=\"prefUfid\" name=\"prefUfid\" class=\"preferences_small_input sbk_Focusable\" value=\"" + ufid + "\" type=\"text\" />    &nbsp; &nbsp; " + Localization_Gateway.Preferences.Gatorlink_Hint(displayLanguage) + "</td></tr>");
            }

            Preferences_Form_Helper.Write_Affiliation_Rows(Output, commonFields, affilitionInfoLabel, organizationLabel, collegeLabel, departmentLabel, unitLabel);

            Output.WriteLine("  <tr><th colspan=\"3\">" + otherPreferencesLabel + "</td></tr>");

            Preferences_Form_Helper.Write_Language_Row(Output, commonFields, languageLabel);

            if (!desire_to_upload)
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" /><label for=\"prefAllowSubmit\">" + Localization_Gateway.Preferences.Allow_Submit_With_Notice_Label(displayLanguage) + "</label></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" checked=\"checked\" /><label for=\"prefAllowSubmit\">" + Localization_Gateway.Preferences.Allow_Submit_With_Notice_Label(displayLanguage) + "</label></td></tr>");
            }

            Output.WriteLine("  <tr style=\"text-align:right\"><td colspan=\"3\">");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("    <button onclick=\"window.location.href = '" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.Preferences.Cancel_Button(displayLanguage) + " </button> &nbsp; &nbsp; ");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;

            Output.WriteLine("    <button type=\"submit\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.Preferences.Submit_Button(displayLanguage) + " </button> ");

            Output.WriteLine("</td></tr></table></blockquote></div>\n\n<!-- Focus on the first registration text box -->\n<script type=\"text/javascript\">focus_element('prefUsername');</script>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }

        /// <summary> Flag indicates if a user must be logged in to access this
        /// admin or mySobek view.  </summary>
        /// <value> Returns FALSE since this page allows users to register </value>
        public override bool Requires_Logged_In_User
        {
            get { return false; }
        }
    }
}
