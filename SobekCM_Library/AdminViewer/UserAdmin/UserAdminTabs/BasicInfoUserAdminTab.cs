using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    class BasicInfoUserAdminTab : iUserAdminTab
    {
        public string TabName => "Basic Information";

        public bool HandlePostback(NameValueCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            string[] getKeys = form.AllKeys;

            string editTemplate = "Standard";
            List<string> projects = new List<string>();
            List<string> templates = new List<string>();

            // First, set some flags to FALSE
            editUser.Can_Submit = false;
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
                    case "admin_user_submit":
                        editUser.Can_Submit = true;
                        break;

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

                    default:
                        if (thisKey.IndexOf("admin_user_template_") == 0)
                        {
                            templates.Add(thisKey.Replace("admin_user_template_", ""));
                        }
                        if (thisKey.IndexOf("admin_user_project_") == 0)
                        {
                            projects.Add(thisKey.Replace("admin_user_project_", ""));
                        }
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

            // Determine if the projects and templates need to be updated
            bool update_templates_projects = false;
            if ((templates.Count != editUser.Templates.Count) || (projects.Count != editUser.Default_Metadata_Sets.Count))
            {
                update_templates_projects = true;
            }
            else
            {
                // Check all of the templates
                if (templates.Any(template => !editUser.Templates.Contains(template)))
                {
                    update_templates_projects = true;
                }

                // Check all the projects
                if (!update_templates_projects)
                {
                    if (projects.Any(project => !editUser.Default_Metadata_Sets.Contains(project)))
                    {
                        update_templates_projects = true;
                    }
                }
            }

            // Update the templates and projects, if requested
            if (update_templates_projects)
            {
                // Get the last defaults
                string default_project = String.Empty;
                string default_template = String.Empty;
                if (editUser.Default_Metadata_Sets.Count > 0)
                    default_project = editUser.Default_Metadata_Sets[0];
                if (editUser.Templates.Count > 0)
                    default_template = editUser.Templates[0];

                // Now, set the RequestSpecificValues.Current_User's template and projects
                editUser.Clear_Default_Metadata_Sets();
                editUser.Clear_Templates();
                foreach (string thisProject in projects)
                {
                    editUser.Add_Default_Metadata_Set(thisProject, false);
                }
                foreach (string thisTemplate in templates)
                {
                    editUser.Add_Template(thisTemplate, false);
                }

                // Try to add back the defaults, which won't do anything if 
                // the old defaults aren't in the new list
                editUser.Set_Current_Default_Metadata(default_project);
                editUser.Set_Default_Template(default_template);
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
            Output.WriteLine("      <tr height=\"27px\"><td width=\"80px\"><label for=\"admin_user_org_code\">Code:</label></td><td><input id=\"admin_user_org_code\" name=\"admin_user_org_code\" class=\"users_code_input\" value=\"" + editUser.Organization_Code + "\" type=\"text\" onfocus=\"javascript:textbox_enter('admin_user_org_code', 'users_code_input_focused')\" onblur=\"javascript:textbox_leave('admin_user_org_code', 'users_code_input')\" /></td></tr>");
            Output.WriteLine("    </table>");
            Output.WriteLine("  </blockquote>");


            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Global Permissions</span><br />");
            Output.WriteLine(editUser.Can_Submit
                                 ? "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_submit\" id=\"admin_user_submit\" checked=\"checked\" /> <label for=\"admin_user_submit\">Can submit items</label> <br />"
                                 : "    <input class=\"admin_user_checkbox\" type=\"checkbox\" name=\"admin_user_submit\" id=\"admin_user_submit\" /> <label for=\"admin_user_submit\">Can submit items</label> <br />");

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
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle\"> &nbsp; Templates and Default Metadata</span>");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("    <table>");
            Output.WriteLine("      <tr height=\"35px\" valign=\"top\" >");
            Output.WriteLine("        <td width=\"300px\">");
            Output.WriteLine("          Edit Templates: &nbsp; ");
            Output.WriteLine("          <select class=\"admin_user_select\" name=\"admin_user_edittemplate\" id=\"admin_user_edittemplate\">");

            if (editUser.Edit_Template_Code_Simple.ToUpper().IndexOf("INTERNAL") >= 0)
            {
                Output.WriteLine("            <option value=\"internal\" selected=\"selected\">Internal</option>");
                Output.WriteLine("            <option value=\"standard\">Standard</option>");
            }
            else
            {
                Output.WriteLine("            <option value=\"internal\">Internal</option>");
                Output.WriteLine("            <option value=\"standard\" selected=\"selected\">Standard</option>");
            }

            Output.WriteLine("          </select>");
            Output.WriteLine("        </td>");
            Output.WriteLine("        <td> &nbsp; </td>");
            Output.WriteLine("      </tr>");

            DataSet projectTemplateSet = Engine_Database.Get_All_Template_DefaultMetadatas(Tracer);

            Output.WriteLine("      <tr valign=\"top\" >");
            Output.WriteLine("        <td>");
            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");
            Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("    <th width=\"180px\" align=\"left\"><span style=\"color: White\">TEMPLATES</span></th>");
            Output.WriteLine("   </tr>");
            Output.WriteLine("  <tr ><td bgcolor=\"#e7e7e7\"></td></tr>");

            ReadOnlyCollection<string> user_templates = editUser.Templates;
            foreach (DataRow thisTemplate in projectTemplateSet.Tables[1].Rows)
            {
                string template_name = thisTemplate["TemplateName"].ToString();
                string template_code = thisTemplate["TemplateCode"].ToString();

                Output.Write("  <tr align=\"left\"><td><input type=\"checkbox\" name=\"admin_user_template_" + template_code + "\" id=\"admin_user_template_" + template_code + "\"");
                if (user_templates.Contains(template_code))
                {
                    Output.Write(" checked=\"checked\"");
                }
                if (template_name.Length > 0)
                {
                    Output.WriteLine(" /> &nbsp; <acronym title=\"" + template_name.Replace("\"", "'") + "\"><label for=\"admin_user_template_" + template_code + "\">" + template_code + "</label></acronym></td></tr>");
                }
                else
                {
                    Output.WriteLine(" /> &nbsp; <label for=\"admin_user_template_" + template_code + "\">" + template_code + "</label></td></tr>");
                }
                Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\"></td></tr>");
            }
            Output.WriteLine("</table>");
            Output.WriteLine("        </td>");

            Output.WriteLine("        <td>");
            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");
            Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
            Output.WriteLine("    <th width=\"180px\" align=\"left\"><span style=\"color: White\">DEFAULT METADATA</span></th>");
            Output.WriteLine("   </tr>");
            Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\"></td></tr>");

            ReadOnlyCollection<string> user_projects = editUser.Default_Metadata_Sets;
            foreach (DataRow thisProject in projectTemplateSet.Tables[0].Rows)
            {
                string project_name = thisProject["MetadataName"].ToString();
                string project_code = thisProject["MetadataCode"].ToString();

                Output.Write("  <tr align=\"left\"><td><input type=\"checkbox\" name=\"admin_user_project_" + project_code + "\" id=\"admin_user_project_" + project_code + "\"");
                if (user_projects.Contains(project_code))
                {
                    Output.Write(" checked=\"checked\"");
                }
                if (project_name.Length > 0)
                {
                    Output.WriteLine(" /> &nbsp; <acronym title=\"" + project_name.Replace("\"", "'") + "\"><label for=\"admin_user_project_" + project_code + "\">" + project_code + "</label></acronym></td></tr>");
                }
                else
                {
                    Output.WriteLine(" /> &nbsp; <label for=\"admin_user_project_" + project_code + "\">" + project_code + "</label></td></tr>");
                }

                Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\"></td></tr>");
            }
            Output.WriteLine("</table>");
            Output.WriteLine("        </td>");

            Output.WriteLine("      </tr>");
            Output.WriteLine("   </table>");
            Output.WriteLine("  </blockquote>");
        }
    }
}
