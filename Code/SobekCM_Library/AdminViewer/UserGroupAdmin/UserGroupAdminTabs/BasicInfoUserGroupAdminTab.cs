using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.UserGroupAdminTabs
{
    /// <summary> Basic Information tab for editing a single user group -- name, description, and
    /// global permission flags </summary>
    /// <remarks> Extracted verbatim (page 1) from the former monolithic <c>User_Group_AdminViewer</c>,
    /// as part of the same tab/subviewer split <c>Users_AdminViewer</c> already went through. The
    /// legacy Templates / Default Metadata checklists that used to live here were removed in the
    /// 5.2.0 redesign, superseded by Item Type assignment on the Submissions tab. </remarks>
    public class BasicInfoUserGroupAdminTab : iUserGroupAdminTab
    {
        public string TabName => "Basic Information";

        public bool HandlePostback(IFormCollection form, User_Group editGroup, RequestCache RequestSpecificValues)
        {
            // First, set some flags to FALSE (CanSubmit is deliberately NOT reset here -- it now lives
            // exclusively on the Submissions tab, so a Basic Info save must not touch it)
            editGroup.IsInternalUser = false;
            editGroup.Should_Be_Able_To_Edit_All_Items = false;
            editGroup.IsPortalAdmin = false;
            editGroup.IsSystemAdmin = false;

            // Step through each key
            foreach (string thisKey in form.Keys)
            {
                switch (thisKey)
                {
                    case "groupName":
                        editGroup.Name = form[thisKey].TrimFirst();
                        break;

                    case "groupDescription":
                        editGroup.Description = form[thisKey].TrimFirst();
                        break;

                    case "admin_user_internal":
                        editGroup.IsInternalUser = true;
                        break;

                    case "admin_user_editall":
                        editGroup.Should_Be_Able_To_Edit_All_Items = true;
                        break;

                    case "admin_user_admin":
                        editGroup.IsSystemAdmin = true;
                        break;

                    case "admin_user_portaladmin":
                        editGroup.IsPortalAdmin = true;
                        break;
                }
            }

            // No immediate save necessary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Group editGroup, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; User Group Information</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <table>");
            Output.WriteLine("      <tr><td><b><label for=\"groupName\">Name:</label></b></td><td><input id=\"groupName\" name=\"groupName\" class=\"admin_small_input sbk_Focusable\" value=\"" + editGroup.Name + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("      <tr><td><b><label for=\"groupDescription\">Description:</label></b></td><td><input id=\"groupDescription\" name=\"groupDescription\" class=\"admin_large_input sbk_Focusable\" value=\"" + editGroup.Description + "\" type=\"text\" /></td></tr>");
            Output.WriteLine("    </table>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Global Permissions</span><br />");
            Output.WriteLine("    <i>Submission ability, default visibility, and permissions agreement now live on the Submissions tab.</i> <br />");

            Output.WriteLine(editGroup.IsInternalUser
                ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_internal\" id=\"admin_user_internal\" checked=\"checked\" /> <label for=\"admin_user_internal\">Is internal user</label> <br />"
                : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_internal\" id=\"admin_user_internal\" /> <label for=\"admin_user_internal\">Is internal user</label> <br />");

            Output.WriteLine(editGroup.Should_Be_Able_To_Edit_All_Items
                ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_editall\" id=\"admin_user_editall\" checked=\"checked\" /> <label for=\"admin_user_editall\">Can edit <u>all</u> items</label> <br />"
                : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_editall\" id=\"admin_user_editall\" /> <label for=\"admin_user_editall\">Can edit <u>all</u> items</label> <br />");

            Output.WriteLine(editGroup.IsSystemAdmin
                ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_admin\" id=\"admin_user_admin\" checked=\"checked\" /> <label for=\"admin_user_admin\">Is system administrator</label> <br />"
                : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_admin\" id=\"admin_user_admin\" /> <label for=\"admin_user_admin\">Is system administrator</label> <br />");
        }
    }
}
