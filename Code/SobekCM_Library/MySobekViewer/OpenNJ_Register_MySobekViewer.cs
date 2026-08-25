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
using SobekCM.Library.Localization;
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
    /// <summary> OpenNJ-portal-specific registration viewer - only ever constructed to register a brand-new,
    /// anonymous user (see MySobekViewer_Factory's OpenNJ branch under My_Sobek_Type_Enum.Register); the
    /// constructor's own logged-on guard below redirects straight to Preferences otherwise, so there is no
    /// "editing an existing user" code path here </summary>
    public class OpenNJ_Register_MySobekViewer : abstract_MySobekViewer
    {
        private readonly List<string> validationErrors;
        private readonly User_Object user;

        private readonly bool desire_to_upload;
        private readonly bool? is_instructor;
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
        private readonly string passwordLabel;
        private readonly string confirmPasswordLabel;
        private readonly string col1Width;
        private readonly string col2Width;
        private readonly string col3Width;

        /// <summary> Constructor for a new instance of the OpenNJ_Register_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public OpenNJ_Register_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("OpenNJ_Register_MySobekViewer.Constructor", String.Empty);

            // If there is a user already logged on, send to preferences
            if (Context.Session.GetString(SessionCache_Keys.User) != null)
            {
                // Now, forward back to the My Sobek home page
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Preferences;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Self-registration is off for this instance (e.g. it only wants sign-in through OIDC/SAML) -
            // send anyone who lands here back to the logon page instead
            if (!UI_ApplicationCache_Gateway.Configuration.Authentication.AllowLocalAuth)
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }


            validationErrors = new List<string>();

            // Set the text to use for each value (since we use if for the validation errors as well)
            mySobekText = "my" + RequestSpecificValues.Current_Mode.Portal_Abbreviation;

            // Get the labels to use, by language
            string displayLanguage = RequestSpecificValues.Current_Mode.Language;
            accountInfoLabel = Localization_Gateway.OpenNJ_Register.Account_Info(displayLanguage);
            userNameLabel = Localization_Gateway.OpenNJ_Register.Username_Label(displayLanguage);
            personalInfoLabel = Localization_Gateway.OpenNJ_Register.Personal_Info(displayLanguage);
            familyNamesLabel = Localization_Gateway.OpenNJ_Register.Family_Names_Label(displayLanguage);
            givenNamesLabel = Localization_Gateway.OpenNJ_Register.Given_Names_Label(displayLanguage);
            nicknameLabel = Localization_Gateway.OpenNJ_Register.Nickname_Label(displayLanguage);
            emailLabel = Localization_Gateway.OpenNJ_Register.Email_Label(displayLanguage);
            emailStatsLabel = Localization_Gateway.OpenNJ_Register.Email_Stats_Label(displayLanguage);
            affilitionInfoLabel = Localization_Gateway.OpenNJ_Register.Affiliation_Info(displayLanguage);
            organizationLabel = Localization_Gateway.OpenNJ_Register.Organization_Label(displayLanguage);
            passwordLabel = Localization_Gateway.OpenNJ_Register.Password_Label(displayLanguage);
            confirmPasswordLabel = Localization_Gateway.OpenNJ_Register.Confirm_Password_Label(displayLanguage);
            col1Width = "15px";
            col2Width = "100px";
            col3Width = "605px";

            user = new User_Object();

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
                            username = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "password_enter":
                            password = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "password_confirm":
                            password2 = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefUfid":
                            ufid = Context.Request.Form[thisKey].TrimFirst().Replace("-", "");
                            break;

                        case "prefFamilyName":
                            family_name = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefGivenName":
                            given_name = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefNickName":
                            nickname = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefEmail":
                            email = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefOrganization":
                            organization = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefCollege":
                            college = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefDepartment":
                            department = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefUnit":
                            unit = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefLanguage":
                            string language_temp = Context.Request.Form[thisKey].TrimFirst();
                            if (language_temp == "es")
                                language = "Español";
                            if (language_temp == "fr")
                                language = "Français";
                            break;

                        case "prefTemplate":
                            template = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefProject":
                            project = Context.Request.Form[thisKey].TrimFirst();
                            break;

                        case "prefAllowSubmit":
                            string submit_value = Context.Request.Form[thisKey].TrimFirst();
                            if (submit_value == "allowsubmit")
                                desire_to_upload = true;
                            break;

                        case "prefIsInstructor":
                            string submit_value_instructor = Context.Request.Form[thisKey].TrimFirst();
                            if (submit_value_instructor == "isinstructor")
                            {
                                is_instructor = true;

                            }
                            else if (submit_value_instructor == "isNOTinstructor")
                            {
                                is_instructor = false;
                            }
                            break;

                        case "prefSendEmail":
                            string submit_value2 = Context.Request.Form[thisKey].TrimFirst();
                            send_email_on_submission = submit_value2 == "sendemail";
                            break;

                        case "prefEmailStats":
                            string submit_value3 = Context.Request.Form[thisKey].TrimFirst();
                            send_usages_emails = submit_value3 == "sendemail";
                            break;

                        case "prefRights":
                            default_rights = Context.Request.Form[thisKey].TrimFirst();
                            break;

                    }
                }

                // validate user name
                if (username.Trim().Length == 0)
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Username_Required(displayLanguage));
                else if (username.Trim().Length < 8)
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Username_Min_Length(displayLanguage));

                // validate password
                if ((password.Trim().Length == 0) || (password2.Trim().Length == 0))
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Select_Confirm_Password(displayLanguage));
                if (password.Trim() != password2.Trim())
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Passwords_Do_Not_Match(displayLanguage));
                else if (password.Length < 8)
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Password_Min_Length(displayLanguage));

                // validate instructor indication
                if (!is_instructor.HasValue)
                {
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Select_Instructor_Status(displayLanguage));
                }
                else if (is_instructor.Value)
                {
                    if (organization.Length == 0)
                    {
                        validationErrors.Add(Localization_Gateway.OpenNJ_Register.Instructor_Institution_Required(displayLanguage));
                    }
                }

                // validate UFID (UF only)
                if (ufid.Trim().Length > 0)
                {
                    if (ufid.Trim().Length != 8)
                    {
                        validationErrors.Add(Localization_Gateway.OpenNJ_Register.Ufid_Length(displayLanguage));
                    }
                    else
                    {
                        int ufid_convert_test;
                        if (!Int32.TryParse(ufid, out ufid_convert_test))
                            validationErrors.Add(Localization_Gateway.OpenNJ_Register.Ufid_Numeric(displayLanguage));
                    }
                }

                // Validate the basic data is okay
                if (family_name.Trim().Length == 0)
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Family_Name_Required(displayLanguage));
                if (given_name.Trim().Length == 0)
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Given_Name_Required(displayLanguage));
                if ((email.Trim().Length == 0) || (email.IndexOf("@") < 0))
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Valid_Email_Required(displayLanguage));
                if (default_rights.Trim().Length > 1000)
                {
                    validationErrors.Add(Localization_Gateway.OpenNJ_Register.Rights_Truncated(displayLanguage));
                    default_rights = default_rights.Substring(0, 1000);
                }

                if (validationErrors.Count == 0)
                {
                    bool email_exists;
                    bool username_exists;
                    SobekCM_Database.UserName_Exists(username, email, out username_exists, out email_exists, RequestSpecificValues.Tracer);
                    if (email_exists)
                    {
                        validationErrors.Add(Localization_Gateway.OpenNJ_Register.Email_Already_Exists(displayLanguage));
                    }
                    else if (username_exists)
                    {
                        validationErrors.Add(Localization_Gateway.OpenNJ_Register.Username_Taken(displayLanguage));
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
                    user.Set_Current_Default_Metadata(project.Trim());
                    user.Preferred_Language = language;
                    user.Default_Rights = default_rights;
                    user.Send_Email_On_Submission = send_email_on_submission;
                    user.Receive_Stats_Emails = send_usages_emails;

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
                        builder.Append("System Name: " + RequestSpecificValues.Current_Mode.Portal_Name + "<br />");
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
                        Email_Helper.SendEmail(email, "Welcome to " + mySobekText, "<strong>Thank you for registering for " + mySobekText + "</strong><br /><br />You can access this directly through the following link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/my\">" + RequestSpecificValues.Current_Mode.Base_URL + "/my</a><br /><br />Full Name: " + user.Full_Name + "<br />User Name: " + user.UserName + "<br /><br />You will receive an email when your request to submit items has been processed.", true, RequestSpecificValues.Current_Mode.Portal_Name);
                    }
                    else
                    {
                        Email_Helper.SendEmail(email, "Welcome to " + mySobekText, "<strong>Thank you for registering for " + mySobekText + "</strong><br /><br />You can access this directly through the following link: <a href=\"" + RequestSpecificValues.Current_Mode.Base_URL + "/my\">" + RequestSpecificValues.Current_Mode.Base_URL + "/my</a><br /><br />Full Name: " + user.Full_Name + "<br />User Name: " + user.UserName, true, RequestSpecificValues.Current_Mode.Portal_Name);
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
        public override string Web_Title
        {
            get
            {
                return String.Format(Localization_Gateway.OpenNJ_Register.Register_Page_Title_Format(RequestSpecificValues.Current_Mode.Language), RequestSpecificValues.Current_Mode.Portal_Abbreviation);
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of any form) </summary>
        /// <param name="Output">Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This does nothing </remarks>
	    public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("OpenNJ_Register_MySobekViewer.Write_HTML");

            string displayLanguage = RequestSpecificValues.Current_Mode.Language;

            // Open the item nav form
            Write_ItemNavForm_Opening(Output);

            Output.WriteLine("<h1>" + Web_Title + "</h1>");
            Output.WriteLine();
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
            Output.WriteLine("<div class=\"SobekHomeText\" >");
            Output.WriteLine("<blockquote>");

            Output.WriteLine(String.Format(Localization_Gateway.OpenNJ_Register.Registration_Intro_Format(displayLanguage), mySobekText) + "<br /><br />");
            Output.WriteLine(Localization_Gateway.OpenNJ_Register.Account_Required_Note(displayLanguage) + "<br /><br />");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
            string log_on_link = "<a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">" + Localization_Gateway.OpenNJ_Register.Log_On_Link_Text(displayLanguage) + "</a>";
            Output.WriteLine(String.Format(Localization_Gateway.OpenNJ_Register.Already_Registered_Format(displayLanguage), log_on_link) + "<br /><br />");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;

            if (validationErrors.Count > 0)
            {
                Output.WriteLine("<span style=\"color: Red;font-weight:bold;\">" + Localization_Gateway.OpenNJ_Register.Errors_Detected_Header(displayLanguage));
                Output.WriteLine("<blockquote>");
                foreach (string thisError in validationErrors)
                {
                    Output.WriteLine(thisError + "<br />");
                }
                Output.WriteLine("</blockquote>");
                Output.WriteLine("</span>");
            }

            // Add the script to hide and unhide the instructor parts
            Output.WriteLine("<script type=\"text/javascript\">");
            Output.WriteLine("\tfunction show_instruct_parts() {");
            Output.WriteLine("\t\tdocument.getElementById(\"instructSubmitRow\").style.display = \"table-row\";");
            Output.WriteLine("\t\tdocument.getElementById(\"instructSubmitRow2\").style.display = \"table-row\";");
            Output.WriteLine("\t\tdocument.getElementById(\"instructSubmitRow3\").style.display = \"table-row\";");
            Output.WriteLine("\t\tdocument.getElementById(\"instructEmailPrompt\").style.display = \"inline\";");
            Output.WriteLine("\t}");
            Output.WriteLine();
            Output.WriteLine("\tfunction hide_instruct_parts() {");
            Output.WriteLine("\t\tdocument.getElementById(\"instructSubmitRow\").style.display = \"none\";");
            Output.WriteLine("\t\tdocument.getElementById(\"instructSubmitRow2\").style.display = \"none\";");
            Output.WriteLine("\t\tdocument.getElementById(\"instructSubmitRow3\").style.display = \"none\";");
            Output.WriteLine("\t\tdocument.getElementById(\"instructEmailPrompt\").style.display = \"none\";");

            Output.WriteLine("\t}");
            Output.WriteLine();
            Output.WriteLine("\tfunction isNotInstructorClick(cb) {");
            Output.WriteLine("\tif(cb.checked) {");
            Output.WriteLine("\t\thide_instruct_parts();");
            Output.WriteLine("\t\t}");
            Output.WriteLine("\t}");
            Output.WriteLine();
            Output.WriteLine("\tfunction isInstructorClick(cb) {");
            Output.WriteLine("\tif(cb.checked) {");
            Output.WriteLine("\t\tshow_instruct_parts();");
            Output.WriteLine("\t\t}");
            Output.WriteLine("\t}");
            Output.WriteLine("</script>");

            Output.WriteLine("<table style=\"width:700px;\" cellpadding=\"5px\" class=\"sbkPmsv_InputTable\" >");

            Output.WriteLine("  <tr><th colspan=\"3\">" + Localization_Gateway.OpenNJ_Register.Account_Type_Header(displayLanguage) + "</th></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td colspan=\"2\">" + Localization_Gateway.OpenNJ_Register.Instructor_Question(displayLanguage) + "</td></tr>");

            string instructorLabel = Localization_Gateway.OpenNJ_Register.I_Am_Instructor(displayLanguage);
            string notInstructorLabel = Localization_Gateway.OpenNJ_Register.I_Am_Not_Instructor(displayLanguage);

            if (is_instructor.HasValue)
            {
                if (is_instructor.Value)
                {
                    Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"radio\" value=\"isinstructor\" name=\"prefIsInstructor\" id=\"prefIsInstructor\" onclick=\"isInstructorClick(this);\" checked=\"checked\"> /><label for=\"prefIsInstructor\">" + instructorLabel + "</label>");
                    Output.WriteLine("                                       <input type=\"radio\" value=\"isNOTinstructor\" name=\"prefIsInstructor\" id=\"prefIsNotInstructor\" onclick=\"isNotInstructorClick(this);\"  /><label for=\"prefIsNotInstructor\">" + notInstructorLabel + "</label></td></tr>");
                }
                else
                {
                    Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"radio\" value=\"isinstructor\" name=\"prefIsInstructor\" id=\"prefIsInstructor\" onclick=\"isInstructorClick(this);\" /><label for=\"prefIsInstructor\">" + instructorLabel + "</label>");
                    Output.WriteLine("                                       <input type=\"radio\" value=\"isNOTinstructor\" name=\"prefIsInstructor\" id=\"prefIsNotInstructor\" onclick=\"isNotInstructorClick(this);\" checked=\"checked\" /><label for=\"prefIsNotInstructor\">" + notInstructorLabel + "</label></td></tr>");

                }
            }
            else
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"radio\" value=\"isinstructor\" name=\"prefIsInstructor\" id=\"prefIsInstructor\" onclick=\"isInstructorClick(this);\" /><label for=\"prefIsInstructor\">" + instructorLabel + "</label>");
                Output.WriteLine("                                       <input type=\"radio\" value=\"isNOTinstructor\" name=\"prefIsInstructor\" id=\"prefIsNotInstructor\" onclick=\"isNotInstructorClick(this);\" /><label for=\"prefIsNotInstructor\">" + notInstructorLabel + "</label></td></tr>");
            }

            string submitDisplay = (is_instructor.HasValue && is_instructor.Value) ? "table-row" : "none";
            Output.WriteLine("  <tr id=\"instructSubmitRow\" style=\"display:" + submitDisplay + "\"><td>&nbsp;</td><td colspan=\"2\">" + Localization_Gateway.OpenNJ_Register.Submit_Materials_Instructions(displayLanguage) + "<br /></td></tr>");

            if (!desire_to_upload)
            {
                Output.WriteLine("  <tr id=\"instructSubmitRow2\" style=\"display:" + submitDisplay + "\"><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" /><label for=\"prefAllowSubmit\">" + Localization_Gateway.OpenNJ_Register.Allow_Submit_Label(displayLanguage) + "</label></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr><td colspan=\"2\">&nbsp;</td><td><input type=\"checkbox\" value=\"allowsubmit\" name=\"prefAllowSubmit\" id=\"prefAllowSubmit\" checked=\"checked\" /><label for=\"prefAllowSubmit\">" + Localization_Gateway.OpenNJ_Register.Allow_Submit_Label(displayLanguage) + "</label></td></tr>");
            }
            Output.WriteLine("  <tr id=\"instructSubmitRow3\" style=\"display:" + submitDisplay + "\"><td>&nbsp;</td><td colspan=\"2\">" + Localization_Gateway.OpenNJ_Register.Application_Reviewed_Notice(displayLanguage) + "<br /></td></tr>");


            Output.WriteLine("  <tr><th colspan=\"3\">" + accountInfoLabel + "</th></tr>");

            // If there was a gatorlink ufid, use that
            if (Context.SessionObject()["Gatorlink_UFID"] != null)
                ufid = Context.SessionObject()["Gatorlink_UFID"].ToString();

            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td style=\"width:" + col2Width + "\" class=\"sbkPmsv_InputLabel\"><label for=\"prefUsername\">" + userNameLabel + ":</label></td><td width=\"" + col3Width + "\"><input id=\"prefUserName\" name=\"prefUserName\" class=\"preferences_small_input sbk_Focusable\" value=\"" + username + "\" type=\"text\" />   &nbsp; &nbsp; " + Localization_Gateway.OpenNJ_Register.Username_Hint(displayLanguage) + "</td></tr>");
            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"password_enter\">" + passwordLabel + ":</label></td><td>");

            Output.WriteLine("    <input type=\"password\" id=\"password_enter\" name=\"password_enter\" class=\"preferences_small_input sbk_Focusable\" value=\"\" />");



            Output.WriteLine("     &nbsp; &nbsp; " + Localization_Gateway.OpenNJ_Register.Password_Hint(displayLanguage) + "</td></tr>");
            Output.WriteLine("  <tr><td width=\"" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"password_confirm\">" + confirmPasswordLabel + ":</label></td><td>");

            Output.WriteLine("    <input type=\"password\" id=\"password_confirm\" name=\"password_confirm\" class=\"preferences_small_input sbk_Focusable\" value=\"\" />");

            Output.WriteLine("     &nbsp; &nbsp; " + Localization_Gateway.OpenNJ_Register.Password_Hint(displayLanguage) + "</td></tr>");

            Output.WriteLine("  <tr><th colspan=\"3\">" + personalInfoLabel + "</th></tr>");

            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefGivenName\">" + givenNamesLabel + ":</label></td><td><input id=\"prefGivenName\" name=\"prefGivenName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + given_name + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefFamilyName\">" + familyNamesLabel + ":</label></td><td><input id=\"prefFamilyName\" name=\"prefFamilyName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + family_name + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefNickName\">" + nicknameLabel + ":</label></td><td><input id=\"prefNickName\" name=\"prefNickName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + nickname + "\" type=\"text\" /></td></tr>");

            // Email (may include institution prompt)
            Output.Write("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefEmail\">" + emailLabel + ":</label></td><td><input id=\"prefEmail\" name=\"prefEmail\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + email + "\" type=\"text\" />");
            string instEmailDisplay = (is_instructor.HasValue && is_instructor.Value) ? "inline" : "none";
            Output.Write("<span id=\"instructEmailPrompt\" style=\"display:" + instEmailDisplay + "\">&nbsp; &nbsp; " + Localization_Gateway.OpenNJ_Register.Institutional_Email_Hint(displayLanguage) + "</span>");
            Output.WriteLine("</td></tr>");

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
            //     Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefCollege\">" + collegeLabel + ":</label></td><td><input id=\"prefCollege\" name=\"prefCollege\" class=\"preferences_large_input sbk_Focusable\" value=\"" + college + "\"type=\"text\" /></td></tr>");
            //    Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefDepartment\">" + departmentLabel + ":</label></td><td><input id=\"prefDepartment\" name=\"prefDepartment\" class=\"preferences_large_input sbk_Focusable\" value=\"" + department + "\"type=\"text\" /></td></tr>");
            //     Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefUnit\">" + unitLabel + ":</label></td><td><input id=\"prefUnit\" name=\"prefUnit\" class=\"preferences_large_input sbk_Focusable\" value=\"" + unit + "\" type=\"text\" /></td></tr>");

            Output.WriteLine("  <tr><th colspan=\"3\">&nbsp;</th></tr>");
            Output.WriteLine("  <tr style=\"text-align:right\"><td colspan=\"3\">");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("    <button onclick=\"window.location.href = '" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.OpenNJ_Register.Cancel_Button(displayLanguage) + " </button> &nbsp; &nbsp; ");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Register;

            Output.WriteLine("    <button type=\"submit\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.OpenNJ_Register.Submit_Button(displayLanguage) + " </button> ");

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
