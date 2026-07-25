#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Configuration.Localization;
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
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an unauthenticated user to register and an authenticated user to change their preferences </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// MySobek Viewers are used for registration and authentication with mySobek, as well as performing any task which requires
    /// authentication, such as online submittal, metadata editing, and system administrative tasks.<br /><br />
    /// During a valid html request, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
    /// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  Since this action requires authentication, an instance of the  <see cref="MySobek_HtmlSubwriter"/> class is created. </li>
    /// <li>The mySobek subwriter creates an instance of this viewer for either registering or changing their preferences </li>
    /// </ul></remarks>
    public class Preferences_MySobekViewer : abstract_MySobekViewer
    {
        private readonly List<string> validationErrors;
        private readonly User_Object user;

        private readonly bool registration;
        private readonly bool desire_to_upload;
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
        public Preferences_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Preferences_MySobekViewer.Constructor", String.Empty);

            validationErrors = new List<string>();

            // Set the text to use for each value (since we use if for the validation errors as well)
            mySobekText = "my" + RequestSpecificValues.Current_Mode.Instance_Abbreviation;

            // Get the labels to use, by language
            string displayLanguage = RequestSpecificValues.Current_Mode.Language;
            accountInfoLabel = Localization_Gateway.Preferences.Account_Info(displayLanguage);
            userNameLabel = Localization_Gateway.Preferences.Username_Label(displayLanguage);
            personalInfoLabel = Localization_Gateway.Preferences.Personal_Info(displayLanguage);
            familyNamesLabel = Localization_Gateway.Preferences.Family_Names_Label(displayLanguage);
            givenNamesLabel = Localization_Gateway.Preferences.Given_Names_Label(displayLanguage);
            nicknameLabel = Localization_Gateway.Preferences.Nickname_Label(displayLanguage);
            emailLabel = Localization_Gateway.Preferences.Email_Label(displayLanguage);
            emailStatsLabel = Localization_Gateway.Preferences.Email_Stats_Label(displayLanguage);
            affilitionInfoLabel = Localization_Gateway.Preferences.Affiliation_Info(displayLanguage);
            organizationLabel = Localization_Gateway.Preferences.Organization_Label(displayLanguage);
            collegeLabel = Localization_Gateway.Preferences.College_Label(displayLanguage);
            departmentLabel = Localization_Gateway.Preferences.Department_Label(displayLanguage);
            unitLabel = Localization_Gateway.Preferences.Unit_Label(displayLanguage);
            selfSubmittalPrefLabel = Localization_Gateway.Preferences.Self_Submittal_Pref_Label(displayLanguage);
            sendEmailLabel = Localization_Gateway.Preferences.Send_Email_Label(displayLanguage);
            templateLabel = Localization_Gateway.Preferences.Template_Label(displayLanguage);
            projectLabel = Localization_Gateway.Preferences.Project_Label(displayLanguage);
            defaultRightsLabel = Localization_Gateway.Preferences.Default_Rights_Label(displayLanguage);
            rightsExplanationLabel = Localization_Gateway.Preferences.Rights_Explanation_Label(displayLanguage);
            rightsInstructionLabel = Localization_Gateway.Preferences.Rights_Instruction_Label(displayLanguage);
            otherPreferencesLabel = Localization_Gateway.Preferences.Other_Preferences_Label(displayLanguage);
            languageLabel = Localization_Gateway.Preferences.Language_Label(displayLanguage);
            passwordLabel = Localization_Gateway.Preferences.Password_Label(displayLanguage);
            confirmPasswordLabel = Localization_Gateway.Preferences.Confirm_Password_Label(displayLanguage);

            // Layout widths differ for French/Spanish since translated labels run longer
            if ((displayLanguage == "fr") || (displayLanguage == "es"))
            {
                col1Width = "10px";
                col2Width = "220px";
                col3Width = "490px";
            }
            else
            {
                col1Width = "15px";
                col2Width = "100px";
                col3Width = "605px";
            }

            // Is this for registration
            user = RequestSpecificValues.Current_User;
            registration = (Context.Session.GetString(SessionCache_Keys.User) == null);
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
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                // Loop through and get the dataa
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

                        case "prefFamilyName":
                            family_name = Context.Request.Form[thisKey];
                            break;

                        case "prefGivenName":
                            given_name = Context.Request.Form[thisKey];
                            break;

                        case "prefNickName":
                            nickname = Context.Request.Form[thisKey];
                            break;

                        case "prefEmail":
                            email = Context.Request.Form[thisKey];
                            break;

                        case "prefOrganization":
                            organization = Context.Request.Form[thisKey];
                            break;

                        case "prefCollege":
                            college = Context.Request.Form[thisKey];
                            break;

                        case "prefDepartment":
                            department = Context.Request.Form[thisKey];
                            break;

                        case "prefUnit":
                            unit = Context.Request.Form[thisKey];
                            break;

                        case "prefLanguage":
                            string language_temp = Context.Request.Form[thisKey];
                            if (language_temp == "es")
                                language = "Español";
                            if (language_temp == "fr")
                                language = "Français";
                            break;

                        case "prefTemplate":
                            template = Context.Request.Form[thisKey];
                            break;

                        case "prefProject":
                            project = Context.Request.Form[thisKey];
                            break;

                        case "prefAllowSubmit":
                            string submit_value = Context.Request.Form[thisKey];
                            if (submit_value == "allowsubmit")
                                desire_to_upload = true;
                            break;

                        case "prefSendEmail":
                            string submit_value2 = Context.Request.Form[thisKey];
                            send_email_on_submission = submit_value2 == "sendemail";
                            break;

                        case "prefEmailStats":
                            string submit_value3 = Context.Request.Form[thisKey];
                            send_usages_emails = submit_value3 == "sendemail";
                            break;

                        case "prefRights":
                            default_rights = Context.Request.Form[thisKey];
                            break;
                    }
                }

                if (registration)
                {
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
                }

                // Validate the basic data is okay
                if (family_name.Trim().Length == 0)
                    validationErrors.Add(Localization_Gateway.Preferences.Family_Name_Required(displayLanguage));
                if (given_name.Trim().Length == 0)
                    validationErrors.Add(Localization_Gateway.Preferences.Given_Name_Required(displayLanguage));
                if ((email.Trim().Length == 0) || (email.IndexOf("@") < 0))
                    validationErrors.Add(Localization_Gateway.Preferences.Valid_Email_Required(displayLanguage));
                if (default_rights.Trim().Length > 1000)
                {
                    validationErrors.Add(Localization_Gateway.Preferences.Rights_Truncated(displayLanguage));
                    default_rights = default_rights.Substring(0, 1000);
                }

                if ((registration) && (validationErrors.Count == 0))
                {
                    bool email_exists;
                    bool username_exists;
                    SobekCM_Database.UserName_Exists(username, email, out username_exists, out email_exists, RequestSpecificValues.Tracer);
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
                            // Add each template and project
                            DataSet projectTemplateSet = Engine_Database.Get_All_Template_DefaultMetadatas(RequestSpecificValues.Tracer);
                            List<string> templates = (from DataRow thisTemplate in projectTemplateSet.Tables[1].Rows select thisTemplate["TemplateCode"].ToString()).ToList();
                            List<string> projects = (from DataRow thisProject in projectTemplateSet.Tables[0].Rows select thisProject["MetadataCode"].ToString()).ToList();

                            // Save the updates to this admin user
                            SobekCM_Database.Save_User(user, password, User_Authentication_Type_Enum.Sobek, RequestSpecificValues.Tracer);
                            SobekCM_Database.Update_SobekCM_User(user.UserID, true, true, true, true, true, true, true, true, true, "edit_internal", "editmarc_internal", true, true, true, RequestSpecificValues.Tracer);
                            SobekCM_Database.Update_SobekCM_User_DefaultMetadata(user.UserID, new List<string>(projects), RequestSpecificValues.Tracer);
                            SobekCM_Database.Update_SobekCM_User_Templates(user.UserID, new List<string>(templates), RequestSpecificValues.Tracer);

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
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                    }
                    else
                    {
                        Context.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));
                        SobekCM_Database.Save_User(user, String.Empty, user.Authentication_Type, RequestSpecificValues.Tracer);

                        // Now, forward back to the My Sobek home page
                        RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
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
                if (Context.Session.GetString(SessionCache_Keys.User) == null)
                {
                    return String.Format(Localization_Gateway.Preferences.Register_Page_Title_Format(RequestSpecificValues.Current_Mode.Language), RequestSpecificValues.Current_Mode.Instance_Abbreviation);
                }
                return Localization_Gateway.Preferences.Edit_Preferences_Page_Title(RequestSpecificValues.Current_Mode.Language);
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of any form) </summary>
        /// <param name="Output">Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This does nothing </remarks>
	    public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Preferences_MySobekViewer.Write_HTML");

            string displayLanguage = RequestSpecificValues.Current_Mode.Language;

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<h1>" + Web_Title + "</h1>");
            Output.WriteLine();
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<div class=\"SobekHomeText\" >");
            Output.WriteLine("<blockquote>");
            if (registration)
            {
                Output.WriteLine(String.Format(Localization_Gateway.Preferences.Registration_Intro_Format(displayLanguage), mySobekText) + "<br /><br />");
                Output.WriteLine(Localization_Gateway.Preferences.Account_Required_Note(displayLanguage) + "<br /><br />");
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                string log_on_link = "<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.Preferences.Log_On_Link_Text(displayLanguage) + "</a>";
                Output.WriteLine(String.Format(Localization_Gateway.Preferences.Already_Registered_Format(displayLanguage), log_on_link) + "<br /><br />");
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;
            }
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
            if (registration)
            {
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
            }
            else
            {
                Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + userNameLabel + ":</td><td>" + user.UserName + "</td></tr>");
                if ((user.ShibbID.Trim().Length > 0) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0))
                {
                    Output.WriteLine("  <tr><td width=\"" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label + ":</td><td>" + user.ShibbID + "</td></tr>");
                }
            }

            Output.WriteLine("  <tr><th colspan=\"3\">" + personalInfoLabel + "</td></tr>");

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

            Output.WriteLine("  <tr><th colspan=\"3\">" + affilitionInfoLabel + "</td></tr>");

            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefOrganization\">" + organizationLabel + ":</label></td><td><input id=\"prefOrganization\" name=\"prefOrganization\" class=\"preferences_large_input sbk_Focusable\" value=\"" + organization + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefCollege\">" + collegeLabel + ":</label></td><td><input id=\"prefCollege\" name=\"prefCollege\" class=\"preferences_large_input sbk_Focusable\" value=\"" + college + "\"type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefDepartment\">" + departmentLabel + ":</label></td><td><input id=\"prefDepartment\" name=\"prefDepartment\" class=\"preferences_large_input sbk_Focusable\" value=\"" + department + "\"type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefUnit\">" + unitLabel + ":</label></td><td><input id=\"prefUnit\" name=\"prefUnit\" class=\"preferences_large_input sbk_Focusable\" value=\"" + unit + "\" type=\"text\" /></td></tr>");

            if ((registration) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0))
            {
                Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefUfid\">" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label + ":</label></td><td><input id=\"prefUfid\" name=\"prefUfid\" class=\"preferences_small_input sbk_Focusable\" value=\"" + ufid + "\" type=\"text\" />    &nbsp; &nbsp; " + Localization_Gateway.Preferences.Gatorlink_Hint(displayLanguage) + "</td></tr>");
            }


            if (user.Can_Submit)
            {
                Output.WriteLine("  <tr><th colspan=\"3\">" + selfSubmittalPrefLabel + "</td></tr>");

                if (!send_email_on_submission)
                {
                    Output.WriteLine("  <tr><td colspan=\"2\"></td><td><input type=\"checkbox\" value=\"sendemail\" name=\"prefSendEmail\" id=\"prefSendEmail\" /><label for=\"prefSendEmail\">" + sendEmailLabel + "</label></td></tr>");
                }
                else
                {
                    Output.WriteLine("  <tr><td colspan=\"2\"></td><td><input type=\"checkbox\" value=\"sendemail\" name=\"prefSendEmail\" id=\"prefSendEmail\" checked=\"checked\" /><label for=\"prefSendEmail\">" + sendEmailLabel + "</label></td></tr>");
                }

                if (user.Templates.Count > 0)
                {
                    Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + templateLabel + ":</td>");
                    Output.WriteLine("    <td>");
                    Output.WriteLine("      <select name=\"prefTemplate\" id=\"prefTemplate\" class=\"preferences_language_select\" >");
                    Output.WriteLine("        <option selected=\"selected\" value=\"" + user.Templates[0] + "\">" + user.Templates[0] + "</option>");
                    for (int i = 1; i < user.Templates.Count; i++)
                    {
                        Output.WriteLine("        <option value=\"" + user.Templates[i] + "\">" + user.Templates[i] + "</option>");
                    }
                    Output.WriteLine("      </select>");
                    Output.WriteLine("    </td>");
                    Output.WriteLine("  </tr>");
                }
                if (user.Default_Metadata_Sets.Count > 0)
                {
                    Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + projectLabel + ":</td>");
                    Output.WriteLine("    <td>");
                    Output.WriteLine("      <select name=\"prefProject\" id=\"prefProject\" class=\"preferences_language_select\" >");
                    Output.WriteLine("        <option selected=\"selected\" value=\"" + user.Default_Metadata_Sets[0] + "\">" + user.Default_Metadata_Sets[0] + "</option>");
                    for (int i = 1; i < user.Default_Metadata_Sets.Count; i++)
                    {
                        Output.WriteLine("        <option value=\"" + user.Default_Metadata_Sets[i] + "\">" + user.Default_Metadata_Sets[i] + "</option>");
                    }
                    Output.WriteLine("      </select>");
                    Output.WriteLine("    </td>");
                    Output.WriteLine("  </tr>");
                }
                Output.WriteLine("  <tr style=\"vertical-align:top\"><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + defaultRightsLabel + ":</td><td>" + rightsExplanationLabel + "</td></tr>");
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;<td><textarea rows=\"5\" cols=\"88\" name=\"prefRights\" id=\"prefRights\" class=\"preference_rights_input sbk_Focusable\">" + default_rights + "</textarea></div></td></tr>");
                Output.WriteLine("  <tr valign=\"top\">");
                Output.WriteLine("    <td colspan=\"2\">&nbsp;</td>");
                Output.WriteLine("    <td>");
                Output.WriteLine("      " + rightsInstructionLabel + "<br />");
                Output.WriteLine("      <table cellpadding=\"3px\" cellspacing=\"3px\" >");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc0] The author dedicated the work to the Commons by waiving all of his or her rights to the work worldwide under copyright law and all related or neighboring legal rights he or she had in the work, to the extent allowable by law.');\"><img title=\"You dedicate the work to the Commons by waiving all of your rights to the work worldwide under copyright law and all related or neighboring legal rights you had in the work, to the extent allowable by law.\" src=\"" + Static_Resources_Gateway.Cc_Zero_Img + "\" /></a></td><td><b>No Copyright</b><br /><i>cc0</i></td></tr>");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc by] This item is licensed with the Creative Commons Attribution License.  This license lets others distribute, remix, tweak, and build upon this work, even commercially, as long as they credit the author for the original creation.');\"><img title=\"This license lets others distribute, remix, tweak, and build upon your work, even commercially, as long as they credit you for the original creation.\" src=\"" + Static_Resources_Gateway.Cc_By_Img + "\" /></a></td><td><b>Attribution</b><br /><i>cc by</i></td></tr>");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc by-sa] This item is licensed with the Creative Commons Attribution Share Alike License.  This license lets others remix, tweak, and build upon this work even for commercial reasons, as long as they credit the author and license their new creations under the identical terms.');\"><img title=\"This license lets others remix, tweak, and build upon your work even for commercial reasons, as long as they credit you and license their new creations under the identical terms.\" src=\"" + Static_Resources_Gateway.Cc_By_Sa_Img + "\" /></a></td><td><b>Attribution Share Alike</b><br /><i>cc by-sa</i></td></tr>");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc by-nd] This item is licensed with the Creative Commons Attribution No Derivatives License.  This license allows for redistribution, commercial and non-commercial, as long as it is passed along unchanged and in whole, with credit to the author.');\"><img title=\"This license allows for redistribution, commercial and non-commercial, as long as it is passed along unchanged and in whole, with credit to you.\" src=\"" + Static_Resources_Gateway.Cc_By_Nd_Img + "\" /></a></td><td><b>Attribution No Derivatives</b><br /><i>cc by-nd</i></td></tr>");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc by-nc] This item is licensed with the Creative Commons Attribution Non-Commerical License.  This license lets others remix, tweak, and build upon this work non-commercially, and although their new works must also acknowledge the author and be non-commercial, they don’t have to license their derivative works on the same terms.');\"><img title=\"This license lets others remix, tweak, and build upon your work non-commercially, and although their new works must also acknowledge you and be non-commercial, they don’t have to license their derivative works on the same terms.\" src=\"" + Static_Resources_Gateway.Cc_By_Nc_Img + "\" /></a></td><td><b>Attribution Non-Commercial</b><br /><i>cc by-nc</i></td></tr>");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc by-nc-sa] This item is licensed with the Creative Commons Attribution Non-Commercial Share Alike License.  This license lets others remix, tweak, and build upon this work non-commercially, as long as they credit the author and license their new creations under the identical terms.');\"><img title=\"This license lets others remix, tweak, and build upon your work non-commercially, as long as they credit you and license their new creations under the identical terms.\" src=\"" + Static_Resources_Gateway.Cc_By_Nc_Sa_Img + "\" /></a></td><td><b>Attribution Non-Commercial Share Alike</b><br /><i>cc by-nc-sa</i></td></tr>");
                Output.WriteLine("        <tr><td> &nbsp; <a href=\"\" onclick=\"return set_cc_rights('prefRights','[cc by-nc-nd] This item is licensed with the Creative Commons Attribution Non-Commercial No Derivative License.  This license allows others to download this work and share them with others as long as they mention the author and link back to the author, but they can’t change them in any way or use them commercially.');\"><img title=\"This license allows others to download your works and share them with others as long as they mention you and link back to you, but they can’t change them in any way or use them commercially.\" src=\"" + Static_Resources_Gateway.Cc_By_Nc_Nd_Img + "\" /></a></td><td><b>Attribution Non-Commercial No Derivatives</b><br /><i>cc by-nc-nd</i></td></tr>");
                Output.WriteLine("      </table>");
                Output.WriteLine("    </td>");
                Output.WriteLine("  </tr>");

            }

            Output.WriteLine("  <tr><th colspan=\"3\">" + otherPreferencesLabel + "</td></tr>");

            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + languageLabel + ":</td>");
            Output.WriteLine("    <td>");
            Output.WriteLine("      <select name=\"prefLanguage\" id=\"prefLanguage\" class=\"preferences_language_select\" >");
            if ((language != "Français") && (language != "Español"))
            {
                Output.WriteLine("        <option selected=\"selected\" value=\"en\">English</option>");
            }
            else
            {
                Output.WriteLine("        <option value=\"en\">English</option>");
            }
            Output.WriteLine(language == "Français"
                                   ? "        <option selected=\"selected\" value=\"fr\">Français</option>"
                                   : "        <option value=\"fr\">Français</option>");
            Output.WriteLine(language == "Español"
                                   ? "        <option selected=\"selected\" value=\"es\">Español</option>"
                                   : "        <option value=\"es\">Español</option>");
            Output.WriteLine("      </select>");
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");

            if (registration)
            {
                if (!desire_to_upload)
                {
                    Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" /><label for=\"prefAllowSubmit\">" + Localization_Gateway.Preferences.Allow_Submit_With_Notice_Label(displayLanguage) + "</label></td></tr>");
                }
                else
                {
                    Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" checked=\"checked\" /><label for=\"prefAllowSubmit\">" + Localization_Gateway.Preferences.Allow_Submit_With_Notice_Label(displayLanguage) + "</label></td></tr>");
                }
            }

            Output.WriteLine("  <tr style=\"text-align:right\"><td colspan=\"3\">");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("    <button onclick=\"window.location.href = '" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.Preferences.Cancel_Button(displayLanguage) + " </button> &nbsp; &nbsp; ");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;

            Output.WriteLine("    <button type=\"submit\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.Preferences.Submit_Button(displayLanguage) + " </button> ");

            Output.WriteLine(registration
                 ? "</td></tr></table></blockquote></div>\n\n<!-- Focus on the first registration text box -->\n<script type=\"text/javascript\">focus_element('prefUsername');</script>"
                 : "</td></tr></table></blockquote></div>\n\n<!-- Focus on the first preferences text box -->\n<script type=\"text/javascript\">focus_element('prefGivenName');</script>");

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
