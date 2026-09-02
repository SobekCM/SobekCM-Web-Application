using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    class BasicInfoUserAdminTab : iUserAdminTab
    {
        public string TabName => "Basic Information";

        public bool HandlePostback(IFormCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            var getKeys = form.Keys;

            string editTemplate = "Standard";

            // First, set some flags to FALSE (Can_Submit is deliberately NOT reset here -- it now lives
            // exclusively on the Submissions tab, so a Basic Info save must not touch it)
            editUser.Is_Internal_User = false;
            editUser.Should_Be_Able_To_Edit_All_Items = false;
            editUser.Is_System_Admin = false;
            editUser.Is_Portal_Admin = false;
            editUser.Is_User_Admin = false;
            editUser.Include_Tracking_In_Standard_Forms = false;
            editUser.Can_Delete_All = false;

            if ((UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (RequestSpecificValues.Current_User.Is_Host_Admin))
            {
                editUser.Is_Host_Admin = false;
            }

            // Step through each key
            foreach (string thisKey in getKeys)
            {
                switch (thisKey)
                {
                    case "admin_user_internal":
                        editUser.Is_Internal_User = true;
                        break;

                    case "admin_user_editall":
                        editUser.Should_Be_Able_To_Edit_All_Items = true;
                        break;

                    case "admin_user_deleteall":
                        editUser.Can_Delete_All = true;
                        break;

                    case "admin_user_host":
                        editUser.Is_Host_Admin = true;
                        break;

                    case "admin_user_sysadmin":
                        editUser.Is_System_Admin = true;
                        break;

                    case "admin_user_portaladmin":
                        editUser.Is_Portal_Admin = true;
                        break;

                    case "admin_user_useradmin":
                        editUser.Is_User_Admin = true;
                        break;

                    case "admin_user_includetracking":
                        editUser.Include_Tracking_In_Standard_Forms = true;
                        break;

                    case "admin_user_edittemplate":
                        editTemplate = form["admin_user_edittemplate"];
                        break;

                    case "admin_user_organization":
                        editUser.Organization = form["admin_user_organization"];
                        break;

                    case "admin_user_college":
                        editUser.College = form["admin_user_college"];
                        break;

                    case "admin_user_department":
                        editUser.Department = form["admin_user_department"];
                        break;

                    case "admin_user_unit":
                        editUser.Unit = form["admin_user_unit"];
                        break;

                    case "admin_user_org_code":
                        editUser.Organization_Code = form["admin_user_org_code"];
                        break;
                }
            }

            // Determine the name for the actual edit templates from the combo box selection
            editUser.Edit_Template_Code_Simple = "edit";
            editUser.Edit_Template_Code_Complex = "editmarc";
            if (editTemplate == "internal")
            {
                editUser.Edit_Template_Code_Simple = "edit_internal";
                editUser.Edit_Template_Code_Complex = "editmarc_internal";
            }

            // No immediate save necesary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; User Information</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <table>");

            if (editUser.ShibbID.Trim().Length > 0)
            {
                if (editUser.ShibbID.Length > 4)
                {
                    Output.Write("      <tr height=\"27px\"><td width=\"80px\">UFID:</td><td width=\"200px\"><span class=\"form_linkline\">" + editUser.ShibbID.Substring(0, 4) + "-" + editUser.ShibbID.Substring(4) + " &nbsp; &nbsp; </span></td>");
                }
                else
                {
                    Output.Write("      <tr height=\"27px\"><td width=\"80px\">UFID:</td><td width=\"200px\"><span class=\"form_linkline\">" + editUser.ShibbID + " &nbsp; &nbsp; </span></td>");
                }
            }
            else
            {
                Output.Write("      <tr height=\"27px\"><td width=\"80px\">&nbsp</td><td width=\"200px\">&nbsp;</span></td>");
            }

            Output.WriteLine("<td width=\"80\">Email:</td><td><span class=\"form_linkline\">" + editUser.Email + " &nbsp; &nbsp; </span></td></tr>");
            Output.WriteLine("      <tr height=\"27px\"><td>UserName:</td><td><span class=\"form_linkline\">" + editUser.UserName + " &nbsp; &nbsp; </span></td><td>Full Name:</td><td><span class=\"form_linkline\">" + editUser.Full_Name + " &nbsp; &nbsp; </span></td></tr>");
            Output.WriteLine("    </table>");
            Output.WriteLine("  </blockquote>");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Current Affiliation Information</span><br />");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <table>");
            Output.WriteLine("      <tr height=\"27px\"><td width=\"80px\"><label for=\"admin_user_organization\">Organization/University:</label></td><td><input id=\"admin_user_organization\" name=\"admin_user_organization\" class=\"users_large_input\" value=\"" + editUser.Organization + "\" type=\"text\" onfocus=\"javascript:textbox_enter('admin_user_organization', 'users_large_input_focused')\" onblur=\"javascript:textbox_leave('admin_user_organization', 'users_large_input')\" /></td></tr>");
            Output.WriteLine("      <tr height=\"27px\"><td width=\"80px\"><label for=\"admin_user_college\">College:</label></td><td><input id=\"admin_user_college\" name=\"admin_user_college\" class=\"users_large_input\" value=\"" + editUser.College + "\"type=\"text\" onfocus=\"javascript:textbox_enter('admin_user_college', 'users_large_input_focused')\" onblur=\"javascript:textbox_leave('admin_user_college', 'users_large_input')\" /></td></tr>");
            Output.WriteLine("      <tr height=\"27px\"><td width=\"80px\"><label for=\"admin_user_department\">Department:</label></td><td><input id=\"admin_user_department\" name=\"admin_user_department\" class=\"users_large_input\" value=\"" + editUser.Department + "\"type=\"text\" onfocus=\"javascript:textbox_enter('admin_user_department', 'users_large_input_focused')\" onblur=\"javascript:textbox_leave('admin_user_department', 'users_large_input')\" /></td></tr>");
            Output.WriteLine("      <tr height=\"27px\"><td width=\"80px\"><label for=\"admin_user_unit\">Unit:</label></td><td><input id=\"admin_user_unit\" name=\"admin_user_unit\" class=\"users_large_input\" value=\"" + editUser.Unit + "\" type=\"text\" onfocus=\"javascript:textbox_enter('admin_user_unit', 'users_large_input_focused')\" onblur=\"javascript:textbox_leave('admin_user_unit', 'users_large_input')\" /></td></tr>");


            // Get the list of institution-type aggregations
            var allInstAggs = new List<Item_Aggregation_Related_Aggregations>();
            foreach (string thisType in UI_ApplicationCache_Gateway.Aggregations.All_Types)
            {
                if (thisType.IndexOf("Institution") >= 0)
                {
                    allInstAggs.AddRange(UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type(thisType).Where(a => a.Code.Length > 1).ToList());
                }
            }

            // Sort by institution name
            allInstAggs = allInstAggs.OrderBy(a => a.Name).ToList();
            Output.WriteLine("      <tr height=\"27px\">");
            Output.WriteLine("        <td width=\"80px\"><label for=\"admin_user_org_code\">Source:</label></td>");
            Output.WriteLine("        <td>");
            Output.WriteLine("          <select id=\"admin_user_org_code\" name=\"admin_user_org_code\" class=\"users_code_input\">");

            // Add all the options
            bool found_match = false;
            foreach (Item_Aggregation_Related_Aggregations agg in allInstAggs)
            {

                Output.Write("            <option value=\"" + agg.Code + "\"");
                if (editUser.Organization_Code.Equals(agg.Code, StringComparison.OrdinalIgnoreCase))
                {
                    Output.Write(" selected=\"selected\"");
                    found_match = true;
                }
                Output.WriteLine(">" + agg.Name + " (" + agg.Code.ToLower() + ")</option>");
            }

            // Add empty option
            Output.Write("            <option value=\"\"");
            if (!found_match)
                Output.Write(" selected=\"selected\"");
            Output.WriteLine("></option>");

            Output.WriteLine("          </select>");
            Output.WriteLine("        </td>");
            Output.WriteLine("      </tr>");

            Output.WriteLine("    </table>");
            Output.WriteLine("  </blockquote>");

            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Global Permissions</span><br />");
            Output.WriteLine("    <i>Submission ability, default visibility, and permissions agreement now live on the Submissions tab.</i> <br />");

            Output.WriteLine(editUser.Is_Internal_User
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_internal\" id=\"admin_user_internal\" checked=\"checked\" /> <label for=\"admin_user_internal\">Is power user</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_internal\" id=\"admin_user_internal\" /> <label for=\"admin_user_internal\">Is power user</label> <br />");

            // bool canEditAll = editUser.Editable_Regular_Expressions.Any(thisRegularExpression => thisRegularExpression == "[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}");
            Output.WriteLine(editUser.Should_Be_Able_To_Edit_All_Items
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_editall\" id=\"admin_user_editall\" checked=\"checked\" /> <label for=\"admin_user_editall\">Can edit <u>all</u> items</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_editall\" id=\"admin_user_editall\" /> <label for=\"admin_user_editall\">Can edit <u>all</u> items</label> <br />");

            Output.WriteLine(editUser.Can_Delete_All
             ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_deleteall\" id=\"admin_user_deleteall\" checked=\"checked\" /> <label for=\"admin_user_deleteall\">Can delete <u>all</u> items</label> <br />"
             : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_deleteall\" id=\"admin_user_deleteall\" /> <label for=\"admin_user_deleteall\">Can delete <u>all</u> items</label> <br />");


            Output.WriteLine(editUser.Is_User_Admin
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_useradmin\" id=\"admin_user_useradmin\" checked=\"checked\" /> <label for=\"admin_user_useradmin\">Is user administrator</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_useradmin\" id=\"admin_user_useradmin\" /> <label for=\"admin_user_useradmin\">Is user administrator</label> <br />");

            Output.WriteLine(editUser.Is_Portal_Admin
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_portaladmin\" id=\"admin_user_portaladmin\" checked=\"checked\" /> <label for=\"admin_user_portaladmin\">Is portal administrator</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_portaladmin\" id=\"admin_user_portaladmin\" /> <label for=\"admin_user_portaladmin\">Is portal administrator</label> <br />");

            Output.WriteLine(editUser.Is_System_Admin
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_sysadmin\" id=\"admin_user_sysadmin\" checked=\"checked\" /> <label for=\"admin_user_sysadmin\">Is system administrator</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_sysadmin\" id=\"admin_user_sysadmin\" /> <label for=\"admin_user_sysadmin\">Is system administrator</label> <br />");

            if ((UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (RequestSpecificValues.Current_User.Is_Host_Admin))
            {
                Output.WriteLine(editUser.Is_Host_Admin
                        ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_host\" id=\"admin_user_host\" checked=\"checked\" /> <label for=\"admin_user_host\">Is HOST administrator</label> <br />"
                        : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_host\" id=\"admin_user_host\" /> <label for=\"admin_user_host\">Is HOST administrator</label> <br />");

            }

            Output.WriteLine(editUser.Include_Tracking_In_Standard_Forms
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_includetracking\" id=\"admin_user_includetracking\" checked=\"checked\" /> <label for=\"admin_user_includetracking\">Tracking data should be included in standard input forms</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_includetracking\" id=\"admin_user_includetracking\" /> <label for=\"admin_user_includetracking\">Tracking data should be included in standard input forms</label> <br />");

            Output.WriteLine("  <br />");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Metadata Edit Form</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    Edit Templates: &nbsp; ");
            Output.WriteLine("    <select class=\"admin_user_select\" name=\"admin_user_edittemplate\" id=\"admin_user_edittemplate\">");

            if (editUser.Edit_Template_Code_Simple.ToUpper().IndexOf("INTERNAL") >= 0)
            {
                Output.WriteLine("      <option value=\"internal\" selected=\"selected\">Internal</option>");
                Output.WriteLine("      <option value=\"standard\">Standard</option>");
            }
            else
            {
                Output.WriteLine("      <option value=\"internal\">Internal</option>");
                Output.WriteLine("      <option value=\"standard\" selected=\"selected\">Standard</option>");
            }

            Output.WriteLine("    </select>");
            Output.WriteLine("  </blockquote>");
        }
    }
}
