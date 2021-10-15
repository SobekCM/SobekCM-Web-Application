#region Using directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Email;
using SobekCM.Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;

#endregion


namespace SobekCM.Library.MySobekViewer
{
    public class OpenNJ_Register_MySobekViewer : abstract_MySobekViewer
    {
        private readonly List<string> validationErrors;
        private readonly User_Object user;

        private readonly bool registration;
        private readonly bool desire_to_upload;
        private readonly bool is_instrcutor;
        private readonly bool send_email_on_submission;
        private readonly bool send_usages_emails;
        private readonly string family_name;
        private readonly string given_name;
        private readonly string nickname;
        private readonly string email;
        private readonly string organization;
        private readonly string college;
        private readonly string department;
        private readonly string unit;
        private readonly string username;
        private string ufid;
        private readonly string language;
        private readonly string default_rights;

        private readonly string mySobekText;
        private readonly string accountInfoLabel;
        private readonly string userNameLabel;
        private readonly string personalInfoLabel;
        private readonly string familyNamesLabel;
        private readonly string givenNamesLabel;
        private readonly string nicknameLabel;
        private readonly string emailLabel;
        private readonly string emailStatsLabel;
        private readonly string affilitionInfoLabel;
        private readonly string organizationLabel;
        private readonly string collegeLabel;
        private readonly string departmentLabel;
        private readonly string unitLabel;
        private readonly string selfSubmittalPrefLabel;
        private readonly string sendEmailLabel;
        private readonly string templateLabel;
        private readonly string projectLabel;
        private readonly string defaultRightsLabel;
        private readonly string rightsExplanationLabel;
        private readonly string rightsInstructionLabel;
        private readonly string otherPreferencesLabel;
        private readonly string languageLabel;
        private readonly string passwordLabel;
        private readonly string confirmPasswordLabel;
        private readonly string col1Width;
        private readonly string col2Width;
        private readonly string col3Width;

        /// <summary> Constructor for a new instance of the Preferences_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public OpenNJ_Register_MySobekViewer(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            RequestSpecificValues.Tracer.Add_Trace("OpenNJ_Register_MySobekViewer.Constructor", String.Empty);

            // If there is a user already logged on, send to preferences
            if (HttpContext.Current.Session["user"] != null)
            {
                // Now, forward back to the My Sobek home page
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Preferences;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }


            validationErrors = new List<string>();

            // Set the text to use for each value (since we use if for the validation errors as well)
            mySobekText = "my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation;

            // Get the labels to use, by language
            accountInfoLabel = "Account Information";
            userNameLabel = "UserName";
            personalInfoLabel = "Personal Information";
            familyNamesLabel = "Last/Family Name(s)";
            givenNamesLabel = "First/Given Name(s)";
            nicknameLabel = "Nickname";
            emailLabel = "Email";
            emailStatsLabel = "Send me monthly usage statistics for my items";
            affilitionInfoLabel = "Current Affiliation Information";
            organizationLabel = "Organization/University";
            collegeLabel = "College";
            departmentLabel = "Department";
            unitLabel = "Unit";
            selfSubmittalPrefLabel = "Self-Submittal Preferences";
            sendEmailLabel = "Send me an email when I submit new items";
            templateLabel = "Template";
            projectLabel = "Default Metadata";
            defaultRightsLabel = "Default Rights";
            rightsExplanationLabel = "(These are the default rights you give for sharing, repurposing, or remixing your item to other users. You can set this with each new item you submit, but this will be the default that appears.)";
            rightsInstructionLabel = "You may also select a <a title=\"Explanation of different creative commons licenses.\" href=\"http://creativecommons.org/about/licenses/\">Creative Commons License</a> option below.";
            otherPreferencesLabel = "Other Preferences";
            languageLabel = "Language";
            passwordLabel = "Password";
            confirmPasswordLabel = "Confirm Password";
            col1Width = "15px";
            col2Width = "100px";
            col3Width = "605px";

            // Is this for registration
            user = RequestSpecificValues.Current_User;
            registration = (HttpContext.Current.Session["user"] == null);
            if (registration)
            {
                user = new User_Object();
            }


            // Set some default first
            send_usages_emails = true;
            family_name = String.Empty;
            given_name = String.Empty;
            nickname = String.Empty;
            email = String.Empty;
            organization = String.Empty;
            college = String.Empty;
            department = String.Empty;
            unit = String.Empty;
            string template = String.Empty;
            string project = String.Empty;
            username = String.Empty;
            string password = String.Empty;
            string password2 = String.Empty;
            ufid = String.Empty;
            language = String.Empty;
            default_rights = String.Empty;

            // Handle post back
            if (RequestSpecificValues.Current_Mode.isPostBack)
            {
                // Loop through and get the dataa
                string[] getKeys = HttpContext.Current.Request.Form.AllKeys;
                foreach (string thisKey in getKeys)
                {
                    switch (thisKey)
                    {
                        case "prefUserName":
                            username = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "password_enter":
                            password = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "password_confirm":
                            password2 = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefUfid":
                            ufid = HttpContext.Current.Request.Form[thisKey].Trim().Replace("-", "");
                            break;

                        case "prefFamilyName":
                            family_name = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefGivenName":
                            given_name = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefNickName":
                            nickname = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefEmail":
                            email = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefOrganization":
                            organization = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefCollege":
                            college = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefDepartment":
                            department = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefUnit":
                            unit = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefLanguage":
                            string language_temp = HttpContext.Current.Request.Form[thisKey];
                            if (language_temp == "es")
                                language = "Español";
                            if (language_temp == "fr")
                                language = "Français";
                            break;

                        case "prefTemplate":
                            template = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefProject":
                            project = HttpContext.Current.Request.Form[thisKey];
                            break;

                        case "prefAllowSubmit":
                            string submit_value = HttpContext.Current.Request.Form[thisKey];
                            if (submit_value == "allowsubmit")
                                desire_to_upload = true;
                            break;

                        case "prefIsInstructor":
                            string submit_value_instructor = HttpContext.Current.Request.Form[thisKey];
                            if (submit_value_instructor == "isinstructor")
                                is_instrcutor = true;
                            break;

                        case "prefSendEmail":
                            string submit_value2 = HttpContext.Current.Request.Form[thisKey];
                            send_email_on_submission = submit_value2 == "sendemail";
                            break;

                        case "prefEmailStats":
                            string submit_value3 = HttpContext.Current.Request.Form[thisKey];
                            send_usages_emails = submit_value3 == "sendemail";
                            break;

                        case "prefRights":
                            default_rights = HttpContext.Current.Request.Form[thisKey];
                            break;

                    }
                }

                if (registration)
                {
                    if (username.Trim().Length == 0)
                        validationErrors.Add("Username is a required field");
                    else if (username.Trim().Length < 8)
                        validationErrors.Add("Username must be at least eight digits");
                    if ((password.Trim().Length == 0) || (password2.Trim().Length == 0))
                        validationErrors.Add("Select and confirm a password");
                    if (password.Trim() != password2.Trim())
                        validationErrors.Add("Passwords do not match");
                    else if (password.Length < 8)
                        validationErrors.Add("Password must be at least eight digits");
                    if (ufid.Trim().Length > 0)
                    {
                        if (ufid.Trim().Length != 8)
                        {
                            validationErrors.Add("UFIDs are always eight digits");
                        }
                        else
                        {
                            int ufid_convert_test;
                            if (!Int32.TryParse(ufid, out ufid_convert_test))
                                validationErrors.Add("UFIDs are always numeric");
                        }
                    }
                }

                // Validate the basic data is okay
                if (family_name.Trim().Length == 0)
                    validationErrors.Add("Family name is a required field");
                if (given_name.Trim().Length == 0)
                    validationErrors.Add("Given name is a required field");
                if ((email.Trim().Length == 0) || (email.IndexOf("@") < 0))
                    validationErrors.Add("A valid email is required");
                if (default_rights.Trim().Length > 1000)
                {
                    validationErrors.Add("Rights statement truncated to 1000 characters.");
                    default_rights = default_rights.Substring(0, 1000);
                }

                if ((registration) && (validationErrors.Count == 0))
                {
                    bool email_exists;
                    bool username_exists;
                    SobekCM_Database.UserName_Exists(username, email, out username_exists, out email_exists, RequestSpecificValues.Tracer);
                    if (email_exists)
                    {
                        validationErrors.Add("An account for that email address already exists.");
                    }
                    else if (username_exists)
                    {
                        validationErrors.Add("That username is taken.  Please choose another.");
                    }
                }

                if (validationErrors.Count == 0)
                {
                    // Ensure the last name and first name are capitalized somewhat
                    bool all_caps = true;
                    bool all_lower = true;
                    foreach (char thisChar in family_name)
                    {
                        if (Char.IsUpper(thisChar))
                            all_lower = false;
                        if (Char.IsLower(thisChar))
                            all_caps = false;

                        if ((!all_caps) && (!all_lower))
                            break;
                    }
                    if ((all_caps) || (all_lower))
                    {
                        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                        family_name = textInfo.ToTitleCase(family_name.ToLower()); //War And Peace
                    }
                    all_lower = true;
                    all_caps = true;
                    foreach (char thisChar in given_name)
                    {
                        if (Char.IsUpper(thisChar))
                            all_lower = false;
                        if (Char.IsLower(thisChar))
                            all_caps = false;

                        if ((!all_caps) && (!all_lower))
                            break;
                    }
                    if ((all_caps) || (all_lower))
                    {
                        TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                        given_name = textInfo.ToTitleCase(given_name.ToLower()); //War And Peace
                    }

                    // Now, add this information to the user, so the new user can be saved
                    user.College = college.Trim();
                    user.Department = department.Trim();
                    user.Email = email.Trim();
                    user.Family_Name = family_name.Trim();
                    user.Given_Name = given_name.Trim();
                    user.Nickname = nickname.Trim();
                    user.Organization = organization.Trim();
                    user.Unit = unit.Trim();
                    user.Set_Default_Template(template.Trim());
                    // See if the project is different, if this is not registration
                    if ((!registration) && (user.Default_Metadata_Sets_Count > 0) && (user.Default_Metadata_Sets[0] != project.Trim()))
                    {
                        // Determine the in process directory for this
                        string user_in_process_directory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" + user.UserName;
                        if (user.ShibbID.Trim().Length > 0)
                            user_in_process_directory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" + user.ShibbID;
                        if (Directory.Exists(user_in_process_directory))
                        {
                            if (File.Exists(user_in_process_directory + "\\TEMP000001_00001.mets"))
                                File.Delete(user_in_process_directory + "\\TEMP000001_00001.mets");
                        }
                    }
                    user.Set_Current_Default_Metadata(project.Trim());
                    user.Preferred_Language = language;
                    user.Default_Rights = default_rights;
                    user.Send_Email_On_Submission = send_email_on_submission;
                    user.Receive_Stats_Emails = send_usages_emails;

                    if (registration)
                    {
                        user.Can_Submit = false;
                        user.Send_Email_On_Submission = true;
                        user.ShibbID = ufid;
                        user.UserName = username;
                        user.UserID = -1;

                        // Save this new user
                        SobekCM_Database.Save_User(user, password, user.Authentication_Type, RequestSpecificValues.Tracer);

                        // Retrieve the user from the database
                        user = Engine_Database.Get_User(username, password, RequestSpecificValues.Tracer);

                        // Special code in case this is the very first user
                        if (user.UserID == 1)
                        {
                            // Add each template and project
                            DataSet projectTemplateSet = Engine_Database.Get_All_Template_DefaultMetadatas(RequestSpecificValues.Tracer);
                            List<string> templates = (from DataRow thisTemplate in projectTemplateSet.Tables[1].Rows select thisTemplate["TemplateCode"].ToString()).ToList();
                            List<string> projects = (from DataRow thisProject in projectTemplateSet.Tables[0].Rows select thisProject["MetadataCode"].ToString()).ToList();

                            // Save the updates to this admin user
                            SobekCM_Database.Save_User(user, password, User_Authentication_Type_Enum.Sobek, RequestSpecificValues.Tracer);
                            SobekCM_Database.Update_SobekCM_User(user.UserID, true, true, true, true, true, true, true, true, "edit_internal", "editmarc_internal", true, true, true, RequestSpecificValues.Tracer);
                            SobekCM_Database.Update_SobekCM_User_DefaultMetadata(user.UserID, new ReadOnlyCollection<string>(projects), RequestSpecificValues.Tracer);
                            SobekCM_Database.Update_SobekCM_User_Templates(user.UserID, new ReadOnlyCollection<string>(templates), RequestSpecificValues.Tracer);

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
                        HttpContext.Current.Session["user"] = user;

                        // Will we be sending an email?
                        if ((!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email)) || (desire_to_upload))
                        {
                            // Build the information about this registrant
                            StringBuilder builder = new StringBuilder();
                            builder.Append("Name: " + user.Full_Name + "<br />");
                            builder.Append("Email: " + user.Email + "<br />");
                            builder.Append("UserName: " + user.UserName + "<br />");
                            if (!String.IsNullOrEmpty(user.Organization))
                                builder.Append("Organization: " + user.Organization + "<br />");
                            builder.Append("System Name: " + RequestSpecificValues.Current_Mode.Instance_Name + "<br />");
                            builder.Append("System URL: " + RequestSpecificValues.Current_Mode.Base_URL + "</br />");

                            // If they want to be able to contribue, send an email
                            if (!String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email))
                            {
                                if (desire_to_upload)
                                {
                                    Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email, "New user registered " + user.Full_Name, "New user requested ability to submit new items to " + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + ".<br /><br /><blockquote>" + builder + "</blockquote>", true, RequestSpecificValues.Current_Mode.Instance_Name);
                                }
                                else
                                {
                                    Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.User_Registration_Email, "New user registered " + user.Full_Name, "A new user registered to use " + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + ".<br /><br /><blockquote>" + builder + "</blockquote>", true, RequestSpecificValues.Current_Mode.Instance_Name);
                                }
                            }
                            else if (desire_to_upload)
                            {
                                Email_Helper.SendEmail(UI_ApplicationCache_Gateway.Settings.Email.System_Email, "Submittal rights requested by " + user.Full_Name, "New user requested ability to submit new items.<br /><br /><blockquote>" + builder + "</blockquote>", true, RequestSpecificValues.Current_Mode.Instance_Name);
                            }
                        }

                        // Email the user their registation information
                        if (desire_to_upload)
                        {
                            Email_Helper.SendEmail(email, "Welcome to " + mySobekText, "<strong>Thank you for registering for " + mySobekText + "</strong><br /><br />You can access this directly through the following link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/my\">" + RequestSpecificValues.Current_Mode.Base_URL + "/my</a><br /><br />Full Name: " + user.Full_Name + "<br />User Name: " + user.UserName + "<br /><br />You will receive an email when your request to submit items has been processed.", true, RequestSpecificValues.Current_Mode.Instance_Name);
                        }
                        else
                        {
                            Email_Helper.SendEmail(email, "Welcome to " + mySobekText, "<strong>Thank you for registering for " + mySobekText + "</strong><br /><br />You can access this directly through the following link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/my\">" + RequestSpecificValues.Current_Mode.Base_URL + "/my</a><br /><br />Full Name: " + user.Full_Name + "<br />User Name: " + user.UserName, true, RequestSpecificValues.Current_Mode.Instance_Name);
                        }

                        // Now, forward back to the My Sobek home page
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;

                        // If this is the first user to register (who would have been set to admin), send to the 
                        // system-wide settings screen
                        if (user.UserID == 1)
                        {
                            RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                            RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Settings;
                        }
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    }
                    else
                    {
                        HttpContext.Current.Session["user"] = user;
                        SobekCM_Database.Save_User(user, String.Empty, user.Authentication_Type, RequestSpecificValues.Tracer);

                        // Now, forward back to the My Sobek home page
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                    }
                }
            }
            else
            {
                family_name = user.Family_Name;
                given_name = user.Given_Name;
                nickname = user.Nickname;
                email = user.Email;
                organization = user.Organization;
                college = user.College;
                department = user.Department;
                unit = user.Unit;
                username = user.UserName;
                ufid = user.ShibbID;
                language = user.Preferred_Language;
                send_email_on_submission = user.Send_Email_On_Submission;
                default_rights = user.Default_Rights;

            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This value changes; if the user is logged on it returns 'Edit Your Preferences', otherwise it has a 'Register for...' message </value>
        public override string Web_Title
        {
            get
            {
                if (HttpContext.Current.Session["user"] == null)
                {
                    return "Register for My" + RequestSpecificValues.Current_Mode.Instance_Abbreviation;
                }
                return "Edit Your Account Preferences";
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of any form) </summary>
        /// <param name="Output">Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This does nothing </remarks>
	    public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area with the form </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Write_ItemNavForm_Opening(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Preferences_MySobekViewer.Write_HTML", "Do nothing");

            Output.WriteLine("<h1>" + Web_Title + "</h1>");
            Output.WriteLine();
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<div class=\"SobekHomeText\" >");
            Output.WriteLine("<blockquote>");
            if (registration)
            {
                Output.WriteLine("Registration for " + mySobekText + " is free and open to the public.  Enter your information below to be instantly registered.<br /><br />");
                Output.WriteLine("Account information, name, and email are required for each new account.<br /><br />");
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                Output.WriteLine("Already registered?  <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Log on</a>.<br /><br />");
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;
            }
            if (validationErrors.Count > 0)
            {
                Output.WriteLine("<span style=\"color: Red;font-weight:bold;\">The following errors were detected:");
                Output.WriteLine("<blockquote>");
                foreach (string thisError in validationErrors)
                {
                    Output.WriteLine(thisError + "<br />");
                }
                Output.WriteLine("</blockquote>");
                Output.WriteLine("</span>");
            }

            Output.WriteLine("<table style=\"width:700px;\" cellpadding=\"5px\" class=\"sbkPmsv_InputTable\" >");

            Output.WriteLine("  <tr><th colspan=\"3\">Account Type</th></tr>");
            Output.WriteLine("  <tr><td colspan=\"3\">Are you an instructor?  Let us know below to get access to restricted course materials.</td></tr>");
            Output.WriteLine("  <tr><td colspan=\"3\">Once your application has been reviewed and approved, you will receive email notification.<br /></td></tr>");

            if (!is_instrcutor)
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"isinstructor\" name=\"prefIsInstructor\" id=\"prefIsInstructor\" /><label for=\"prefIsInstructor\">I am an instructor</label></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"isinstructor\" name=\"prefIsInstructor\" id=\"prefIsInstructor\" checked=\"checked\" /><label for=\"prefIsInstructor\">I am an instructor</label></td></tr>");
            }

            Output.WriteLine("  <tr><td colspan=\"3\">Click the option below to submit materials.  To use the online open publishing tools in Open-NJ, you will need to be approved as an instructor and approved to submit materials.<br /></td></tr>");

            if (!desire_to_upload)
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" /><label for=\"prefAllowSubmit\">I would like to be able to submit materials online.</label></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" checked=\"checked\" /><label for=\"prefAllowSubmit\">I would like to be able to submit materials online.</label></td></tr>");
            }


            Output.WriteLine("  <tr><th colspan=\"3\">" + accountInfoLabel + "</th></tr>");
            if (registration)
            {
                // If there was a gatorlink ufid, use that
                if (HttpContext.Current.Session["Gatorlink_UFID"] != null)
                    ufid = HttpContext.Current.Session["Gatorlink_UFID"].ToString();

                Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td style=\"width:" + col2Width + "\" class=\"sbkPmsv_InputLabel\"><label for=\"prefUsername\">" + userNameLabel + ":</label></td><td width=\"" + col3Width + "\"><input id=\"prefUserName\" name=\"prefUserName\" class=\"preferences_small_input sbk_Focusable\" value=\"" + username + "\" type=\"text\" />   &nbsp; &nbsp; (minimum of eight digits)</td></tr>");
                Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"password_enter\">" + passwordLabel + ":</label></td><td>");

                Output.WriteLine("    <input type=\"password\" id=\"password_enter\" name=\"password_enter\" class=\"preferences_small_input sbk_Focusable\" value=\"\" />");



                Output.WriteLine("     &nbsp; &nbsp; (minimum of eight digits, different than username)</td></tr>");
                Output.WriteLine("  <tr><td width=\"" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"password_confirm\">" + confirmPasswordLabel + ":</label></td><td>");

                Output.WriteLine("    <input type=\"password\" id=\"password_confirm\" name=\"password_confirm\" class=\"preferences_small_input sbk_Focusable\" value=\"\" />");

                Output.WriteLine("     &nbsp; &nbsp; (minimum of eight digits, different than username)</td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + userNameLabel + ":</td><td>" + user.UserName + "</td></tr>");
                if ((user.ShibbID.Trim().Length > 0) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0))
                {
                    Output.WriteLine("  <tr><td width=\"" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label + ":</td><td>" + user.ShibbID + "</td></tr>");
                }
            }

            Output.WriteLine("  <tr><th colspan=\"3\">" + personalInfoLabel + "</th></tr>");

            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefGivenName\">" + givenNamesLabel + ":</label></td><td><input id=\"prefGivenName\" name=\"prefGivenName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + given_name + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefFamilyName\">" + familyNamesLabel + ":</label></td><td><input id=\"prefFamilyName\" name=\"prefFamilyName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + family_name + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefNickName\">" + nicknameLabel + ":</label></td><td><input id=\"prefNickName\" name=\"prefNickName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + nickname + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefEmail\">" + emailLabel + ":</label></td><td><input id=\"prefEmail\" name=\"prefEmail\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + email + "\" type=\"text\" /></td></tr>");

            if (user.Has_Item_Stats)
            {
                if (!send_usages_emails)
                {
                    Output.WriteLine("  <tr><td colspan=\"2\"></td><td><input type=\"checkbox\" value=\"sendemail\" name=\"prefStatsEmail\" id=\"prefStatsEmail\" /><label for=\"prefStatsEmail\">" + emailStatsLabel + "</label></td></tr>");
                }
                else
                {
                    Output.WriteLine("  <tr><td colspan=\"2\"></td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefStatsEmail\" id=\"prefStatsEmail\" checked=\"checked\" /><label for=\"prefStatsEmail\">" + emailStatsLabel + "</label></td></tr>");
                }
            }

            Output.WriteLine("  <tr><th colspan=\"3\">" + affilitionInfoLabel + "</th></tr>");

            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefOrganization\">" + organizationLabel + ":</label></td><td><input id=\"prefOrganization\" name=\"prefOrganization\" class=\"preferences_large_input sbk_Focusable\" value=\"" + organization + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefCollege\">" + collegeLabel + ":</label></td><td><input id=\"prefCollege\" name=\"prefCollege\" class=\"preferences_large_input sbk_Focusable\" value=\"" + college + "\"type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefDepartment\">" + departmentLabel + ":</label></td><td><input id=\"prefDepartment\" name=\"prefDepartment\" class=\"preferences_large_input sbk_Focusable\" value=\"" + department + "\"type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefUnit\">" + unitLabel + ":</label></td><td><input id=\"prefUnit\" name=\"prefUnit\" class=\"preferences_large_input sbk_Focusable\" value=\"" + unit + "\" type=\"text\" /></td></tr>");


            Output.WriteLine("  <tr style=\"text-align:right\"><td colspan=\"3\">");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("    <button onclick=\"window.location.href = '" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" class=\"sbkMySobek_BigButton\"> CANCEL </button> &nbsp; &nbsp; ");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;

            Output.WriteLine("    <button type=\"submit\" class=\"sbkMySobek_BigButton\"> SUBMIT </button> ");

            Output.WriteLine(registration
                 ? "</td></tr></table></blockquote></div>\n\n<!-- Focus on the first registration text box -->\n<script type=\"text/javascript\">focus_element('prefUsername');</script>"
                 : "</td></tr></table></blockquote></div>\n\n<!-- Focus on the first preferences text box -->\n<script type=\"text/javascript\">focus_element('prefGivenName');</script>");


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
