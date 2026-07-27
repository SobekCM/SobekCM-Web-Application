#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Library.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Mutable holder for the personal/affiliation fields shared by <see cref="Register_MySobekViewer"/>
    /// (new-user registration) and <see cref="Preferences_MySobekViewer"/> (editing an existing user) </summary>
    public class Preferences_Common_Fields
    {
        public string GivenName = String.Empty;
        public string FamilyName = String.Empty;
        public string Nickname = String.Empty;
        public string Email = String.Empty;
        public string Organization = String.Empty;
        public string College = String.Empty;
        public string Department = String.Empty;
        public string Unit = String.Empty;
        public string Language = String.Empty;
    }

    /// <summary> Shared parsing/validation/rendering for the personal-info, affiliation, and language fields
    /// common to both the registration form and the edit-preferences form - split out of what used to be one
    /// combined Preferences_MySobekViewer class so neither Register_MySobekViewer nor Preferences_MySobekViewer
    /// needs its own copy </summary>
    public static class Preferences_Form_Helper
    {
        /// <summary> Read the shared personal/affiliation/language fields from a postback </summary>
        public static void Parse_Common_Fields(HttpContext Context, Preferences_Common_Fields Fields)
        {
            var getKeys = Context.Request.Form.Keys;
            foreach (string thisKey in getKeys)
            {
                switch (thisKey)
                {
                    case "prefFamilyName":
                        Fields.FamilyName = Context.Request.Form[thisKey];
                        break;

                    case "prefGivenName":
                        Fields.GivenName = Context.Request.Form[thisKey];
                        break;

                    case "prefNickName":
                        Fields.Nickname = Context.Request.Form[thisKey];
                        break;

                    case "prefEmail":
                        Fields.Email = Context.Request.Form[thisKey];
                        break;

                    case "prefOrganization":
                        Fields.Organization = Context.Request.Form[thisKey];
                        break;

                    case "prefCollege":
                        Fields.College = Context.Request.Form[thisKey];
                        break;

                    case "prefDepartment":
                        Fields.Department = Context.Request.Form[thisKey];
                        break;

                    case "prefUnit":
                        Fields.Unit = Context.Request.Form[thisKey];
                        break;

                    case "prefLanguage":
                        string language_temp = Context.Request.Form[thisKey];
                        if (language_temp == "es")
                            Fields.Language = "Español";
                        if (language_temp == "fr")
                            Fields.Language = "Français";
                        break;
                }
            }
        }

        /// <summary> Populate the shared fields from an existing user, for the initial (non-postback) render </summary>
        public static void Load_From_User(User_Object User, Preferences_Common_Fields Fields)
        {
            Fields.FamilyName = User.Family_Name;
            Fields.GivenName = User.Given_Name;
            Fields.Nickname = User.Nickname;
            Fields.Email = User.Email;
            Fields.Organization = User.Organization;
            Fields.College = User.College;
            Fields.Department = User.Department;
            Fields.Unit = User.Unit;
            Fields.Language = User.Preferred_Language;
        }

        /// <summary> Title-case a name if it was submitted as either all-caps or all-lowercase, left alone
        /// otherwise (matches the original inline logic, applied once per name instead of duplicated) </summary>
        public static string Capitalize_Name(string Name)
        {
            bool all_caps = true;
            bool all_lower = true;
            foreach (char thisChar in Name)
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
                return textInfo.ToTitleCase(Name.ToLower());
            }

            return Name;
        }

        /// <summary> Validate the shared required fields (family/given name, valid email) </summary>
        public static void Validate_Common_Fields(Preferences_Common_Fields Fields, List<string> ValidationErrors, string DisplayLanguage)
        {
            if (Fields.FamilyName.Trim().Length == 0)
                ValidationErrors.Add(Localization_Gateway.Preferences.Family_Name_Required(DisplayLanguage));
            if (Fields.GivenName.Trim().Length == 0)
                ValidationErrors.Add(Localization_Gateway.Preferences.Given_Name_Required(DisplayLanguage));
            if ((Fields.Email.Trim().Length == 0) || (Fields.Email.IndexOf("@") < 0))
                ValidationErrors.Add(Localization_Gateway.Preferences.Valid_Email_Required(DisplayLanguage));
        }

        /// <summary> Write the always-editable given/family name, nickname, and email input rows - used by
        /// Register_MySobekViewer (a new registrant is always a native account) and by the non-federated
        /// branch of Preferences_MySobekViewer </summary>
        public static void Write_Personal_Info_Rows(TextWriter Output, Preferences_Common_Fields Fields, string Col1Width,
            string GivenNamesLabel, string FamilyNamesLabel, string NicknameLabel, string EmailLabel)
        {
            Output.WriteLine("  <tr><td style=\"width:" + Col1Width + "\">&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefGivenName\">" + GivenNamesLabel + ":</label></td><td><input id=\"prefGivenName\" name=\"prefGivenName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + Fields.GivenName + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefFamilyName\">" + FamilyNamesLabel + ":</label></td><td><input id=\"prefFamilyName\" name=\"prefFamilyName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + Fields.FamilyName + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefNickName\">" + NicknameLabel + ":</label></td><td><input id=\"prefNickName\" name=\"prefNickName\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + Fields.Nickname + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefEmail\">" + EmailLabel + ":</label></td><td><input id=\"prefEmail\" name=\"prefEmail\" class=\"preferences_medium_input sbk_Focusable\" value=\"" + Fields.Email + "\" type=\"text\" /></td></tr>");
        }

        /// <summary> Write the organization/college/department/unit rows </summary>
        public static void Write_Affiliation_Rows(TextWriter Output, Preferences_Common_Fields Fields, string AffiliationInfoLabel,
            string OrganizationLabel, string CollegeLabel, string DepartmentLabel, string UnitLabel)
        {
            Output.WriteLine("  <tr><th colspan=\"3\">" + AffiliationInfoLabel + "</td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefOrganization\">" + OrganizationLabel + ":</label></td><td><input id=\"prefOrganization\" name=\"prefOrganization\" class=\"preferences_large_input sbk_Focusable\" value=\"" + Fields.Organization + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefCollege\">" + CollegeLabel + ":</label></td><td><input id=\"prefCollege\" name=\"prefCollege\" class=\"preferences_large_input sbk_Focusable\" value=\"" + Fields.College + "\"type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefDepartment\">" + DepartmentLabel + ":</label></td><td><input id=\"prefDepartment\" name=\"prefDepartment\" class=\"preferences_large_input sbk_Focusable\" value=\"" + Fields.Department + "\"type=\"text\" /></td></tr>");
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\"><label for=\"prefUnit\">" + UnitLabel + ":</label></td><td><input id=\"prefUnit\" name=\"prefUnit\" class=\"preferences_large_input sbk_Focusable\" value=\"" + Fields.Unit + "\" type=\"text\" /></td></tr>");
        }

        /// <summary> Write the language selector row </summary>
        public static void Write_Language_Row(TextWriter Output, Preferences_Common_Fields Fields, string LanguageLabel)
        {
            Output.WriteLine("  <tr><td>&nbsp;</td><td class=\"sbkPmsv_InputLabel\">" + LanguageLabel + ":</td>");
            Output.WriteLine("    <td>");
            Output.WriteLine("      <select name=\"prefLanguage\" id=\"prefLanguage\" class=\"preferences_language_select\" >");
            if ((Fields.Language != "Français") && (Fields.Language != "Español"))
            {
                Output.WriteLine("        <option selected=\"selected\" value=\"en\">English</option>");
            }
            else
            {
                Output.WriteLine("        <option value=\"en\">English</option>");
            }
            Output.WriteLine(Fields.Language == "Français"
                                   ? "        <option selected=\"selected\" value=\"fr\">Français</option>"
                                   : "        <option value=\"fr\">Français</option>");
            Output.WriteLine(Fields.Language == "Español"
                                   ? "        <option selected=\"selected\" value=\"es\">Español</option>"
                                   : "        <option value=\"es\">Español</option>");
            Output.WriteLine("      </select>");
            Output.WriteLine("    </td>");
            Output.WriteLine("  </tr>");
        }
    }
}
