#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an authenticated user to change their preferences </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// Edit-only sibling of <see cref="Register_MySobekViewer"/> - the two used to be one combined class
    /// (registration + editing an existing user's preferences, switched by a "registration" flag) before being
    /// split so each pretty URL ("my/preferences" vs "my/register") resolves to its own dedicated viewer.
    /// Shared personal/affiliation/language field handling lives in <see cref="Preferences_Form_Helper"/>
    /// rather than being duplicated between the two. </remarks>
    public class Preferences_MySobekViewer : abstract_MySobekViewer
    {
        private readonly List<string> validationErrors;
        private readonly User_Object user;
        private readonly bool isFederatedAccount;
        private readonly bool send_email_on_submission;
        private readonly bool send_usages_emails;
        private readonly string default_rights;
        private string notificationMode;
        private readonly Preferences_Common_Fields commonFields;

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
        private readonly string defaultRightsLabel;
        private readonly string rightsExplanationLabel;
        private readonly string rightsInstructionLabel;
        private readonly string otherPreferencesLabel;
        private readonly string languageLabel;
        private readonly string col1Width;

        /// <summary> Constructor for a new instance of the Preferences_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Preferences_MySobekViewer(RequestCache RequestSpecificValues, HttpContext Context) : base(RequestSpecificValues, Context)
        {
            RequestSpecificValues.Tracer.Add_Trace("Preferences_MySobekViewer.Constructor", String.Empty);

            validationErrors = new List<string>();

            // Set the text to use for each value (since we use if for the validation errors as well)
            mySobekText = "my" + RequestSpecificValues.Current_Mode.Portal_Abbreviation;

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
            defaultRightsLabel = Localization_Gateway.Preferences.Default_Rights_Label(displayLanguage);
            rightsExplanationLabel = Localization_Gateway.Preferences.Rights_Explanation_Label(displayLanguage);
            rightsInstructionLabel = Localization_Gateway.Preferences.Rights_Instruction_Label(displayLanguage);
            otherPreferencesLabel = Localization_Gateway.Preferences.Other_Preferences_Label(displayLanguage);
            languageLabel = Localization_Gateway.Preferences.Language_Label(displayLanguage);

            col1Width = "15px";

            // Anonymous access to this viewer is already redirected to Logon before construction
            // (see UserObjectInitializer's gate), so there is always a session user here
            user = RequestSpecificValues.Current_User;

            // Given/family name and email come from the identity provider for a federated (OIDC/SAML)
            // account - editable here only for a native account. Nickname remains editable for everyone,
            // so a federated user whose SAML/OIDC name came over as e.g. "Thomas" can still display "Tommy"
            isFederatedAccount = !String.IsNullOrEmpty(user.External_Provider_Code);

            commonFields = new Preferences_Common_Fields();

            // Set some default first
            send_usages_emails = true;
            default_rights = String.Empty;
            notificationMode = "On";

            // Handle post back
            if ((RequestSpecificValues.Current_Mode.isPostBack) && (Context.Request.HasFormContentType))
            {
                Preferences_Form_Helper.Parse_Common_Fields(Context, commonFields);

                // Loop through and get the remaining (self-submittal) fields not covered by the shared helper
                var getKeys = Context.Request.Form.Keys;
                foreach (string thisKey in getKeys)
                {
                    switch (thisKey)
                    {
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

                        case "notification_mode":
                            string submit_value4 = Context.Request.Form[thisKey];
                            if ((submit_value4 == "On") || (submit_value4 == "Paused") || (submit_value4 == "Skip"))
                                notificationMode = submit_value4;
                            break;
                    }
                }

                // Enforced here, not just by hiding the inputs in the rendered form below - Context.Request.Form
                // is otherwise trusted at face value, so a federated user could still submit new values directly
                if (isFederatedAccount)
                {
                    commonFields.GivenName = user.Given_Name;
                    commonFields.FamilyName = user.Family_Name;
                    commonFields.Email = user.Email;
                }

                // Validate the basic data is okay
                Preferences_Form_Helper.Validate_Common_Fields(commonFields, validationErrors, displayLanguage);
                if (default_rights.Trim().Length > 1000)
                {
                    validationErrors.Add(Localization_Gateway.Preferences.Rights_Truncated(displayLanguage));
                    default_rights = default_rights.Substring(0, 1000);
                }

                if (validationErrors.Count == 0)
                {
                    commonFields.FamilyName = Preferences_Form_Helper.Capitalize_Name(commonFields.FamilyName);
                    commonFields.GivenName = Preferences_Form_Helper.Capitalize_Name(commonFields.GivenName);

                    // Now, add this information to the user
                    user.College = commonFields.College.Trim();
                    user.Department = commonFields.Department.Trim();
                    user.Email = commonFields.Email.Trim();
                    user.Family_Name = commonFields.FamilyName.Trim();
                    user.Given_Name = commonFields.GivenName.Trim();
                    user.Nickname = commonFields.Nickname.Trim();
                    user.Organization = commonFields.Organization.Trim();
                    user.Unit = commonFields.Unit.Trim();
                    user.Preferred_Language = commonFields.Language;
                    user.Default_Rights = default_rights;
                    user.Send_Email_On_Submission = send_email_on_submission;
                    user.Receive_Stats_Emails = send_usages_emails;
                    user.Add_Setting("ProcessNotificationMode", notificationMode);

                    Context.Session.SetString(SessionCache_Keys.User, CachedDataManager_UserCacheServices.UserToString(user));
                    SobekCM_Database.Save_User(user, String.Empty, user.Authentication_Type, RequestSpecificValues.Tracer);

                    // Now, forward back to the My Sobek home page
                    RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                }
            }
            else
            {
                Preferences_Form_Helper.Load_From_User(user, commonFields);
                send_email_on_submission = user.Send_Email_On_Submission;
                default_rights = user.Default_Rights;
                notificationMode = user.Get_Setting("ProcessNotificationMode", "On");
            }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Web_Title
        {
            get
            {
                return Localization_Gateway.Preferences.Edit_Preferences_Page_Title(RequestSpecificValues.Current_Mode.Language);
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
        /// <param name="Output">Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
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

            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + userNameLabel + ":</td><td>" + user.UserName + "</td></tr>");
            if ((user.ShibbID.Trim().Length > 0) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth != null) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Enabled) && (UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label.Length > 0))
            {
                Output.WriteLine("  <tr><td width=\"" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + UI_ApplicationCache_Gateway.Configuration.Authentication.Shibboleth.Label + ":</td><td>" + user.ShibbID + "</td></tr>");
            }

            Output.WriteLine("  <tr><th colspan=\"3\">" + personalInfoLabel + "</td></tr>");

            if (isFederatedAccount)
            {
                Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + givenNamesLabel + ":</td><td>" + commonFields.GivenName + "</td></tr>");
                Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + familyNamesLabel + ":</td><td>" + commonFields.FamilyName + "</td></tr>");
                string federatedNote = String.Format(Localization_Gateway.Preferences.Federated_Name_Email_Note_Format(displayLanguage), user.Authentication_Source);
                Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefNickName\">" + nicknameLabel + ":</label></td><td><input id=\"prefNickName\" name=\"prefNickName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + commonFields.Nickname + "\" type=\"text\" /> &nbsp; &nbsp; <i>" + federatedNote + "</i></td></tr>");
                Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + emailLabel + ":</td><td>" + commonFields.Email + "</td></tr>");
            }
            else
            {
                Preferences_Form_Helper.Write_Personal_Info_Rows(Output, commonFields, col1Width, givenNamesLabel, familyNamesLabel, nicknameLabel, emailLabel);
            }

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

            Preferences_Form_Helper.Write_Affiliation_Rows(Output, commonFields, affilitionInfoLabel, organizationLabel, collegeLabel, departmentLabel, unitLabel);

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

            Preferences_Form_Helper.Write_Language_Row(Output, commonFields, languageLabel);

            Output.WriteLine("  <tr><td style=\"width:" + col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"notification_mode\">Notifications:</label></td><td>");
            Output.WriteLine("    <select name=\"notification_mode\" id=\"notification_mode\" class=\"sbk_Focusable\">");
            Output.WriteLine("      <option value=\"On\"" + (notificationMode == "On" ? " selected=\"selected\"" : "") + ">On</option>");
            Output.WriteLine("      <option value=\"Paused\"" + (notificationMode == "Paused" ? " selected=\"selected\"" : "") + ">Paused</option>");
            Output.WriteLine("      <option value=\"Skip\"" + (notificationMode == "Skip" ? " selected=\"selected\"" : "") + ">Skip</option>");
            Output.WriteLine("    </select>");
            Output.WriteLine("  </td></tr>");

            Output.WriteLine("  <tr style=\"text-align:right\"><td colspan=\"3\">");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
            Output.WriteLine("    <button onclick=\"window.location.href = '" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "';return false;\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.Preferences.Cancel_Button(displayLanguage) + " </button> &nbsp; &nbsp; ");
            RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Preferences;

            Output.WriteLine("    <button type=\"submit\" class=\"sbkMySobek_BigButton\"> " + Localization_Gateway.Preferences.Submit_Button(displayLanguage) + " </button> ");

            Output.WriteLine("</td></tr></table></blockquote></div>\n\n<!-- Focus on the first preferences text box -->\n<script type=\"text/javascript\">focus_element('prefGivenName');</script>");

            // Close the item nav form
            Write_ItemNavForm_Closing(Output);
        }
    }
}
