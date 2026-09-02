using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.UserGroupAdminTabs
{
    /// <summary> Aggregations tab for editing a single user group's per-aggregation permission grid </summary>
    /// <remarks> Extracted verbatim (page 2) from the former monolithic <c>User_Group_AdminViewer</c>,
    /// as part of the same tab/subviewer split <c>Users_AdminViewer</c> already went through. Structurally
    /// identical to <c>AggregationsUserAdminTab</c> (the per-user equivalent), just without the
    /// GroupDefined/disabled-checkbox logic -- a user group has no group of its own to inherit from. </remarks>
    public class AggregationsUserGroupAdminTab : iUserGroupAdminTab
    {
        public string TabName => "Aggregations";

        public bool HandlePostback(IFormCollection form, User_Group editGroup, RequestCache RequestSpecificValues)
        {
            var aggregations = new Dictionary<string, User_Permissioned_Aggregation>();

            // Step through each key
            foreach (string thisKey in form.Keys)
            {
                if (thisKey.IndexOf("admin_project_onhome_") == 0)
                {
                    string select_project = thisKey.Replace("admin_project_onhome_", "");
                    if (aggregations.ContainsKey(select_project))
                    {
                        aggregations[select_project].OnHomePage = true;
                    }
                    else
                    {
                        aggregations.Add(select_project, new User_Permissioned_Aggregation(select_project, string.Empty, false, false, false, true, false));
                    }
                }
                if (thisKey.IndexOf("admin_project_select_") == 0)
                {
                    string select_project = thisKey.Replace("admin_project_select_", "");
                    if (aggregations.ContainsKey(select_project))
                    {
                        aggregations[select_project].CanSelect = true;
                    }
                    else
                    {
                        aggregations.Add(select_project, new User_Permissioned_Aggregation(select_project, string.Empty, true, false, false, false, false));
                    }
                }
                if (thisKey.IndexOf("admin_project_editall_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_editall_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanEditItems = true;
                    }
                    else
                    {
                        aggregations.Add(edit_project, new User_Permissioned_Aggregation(edit_project, string.Empty, false, true, false, false, false));
                    }
                }
                if (thisKey.IndexOf("admin_project_edit_metadata_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_edit_metadata_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanEditMetadata = true;
                    }
                    else
                    {
                        var thisAggrLink = new User_Permissioned_Aggregation(edit_project, string.Empty, false, false, false, false, false) { CanEditMetadata = true };
                        aggregations.Add(edit_project, thisAggrLink);
                    }
                }
                if (thisKey.IndexOf("admin_project_edit_behavior_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_edit_behavior_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanEditBehaviors = true;
                    }
                    else
                    {
                        var thisAggrLink = new User_Permissioned_Aggregation(edit_project, string.Empty, false, false, false, false, false) { CanEditBehaviors = true };
                        aggregations.Add(edit_project, thisAggrLink);
                    }
                }
                if (thisKey.IndexOf("admin_project_perform_qc_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_perform_qc_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanPerformQc = true;
                    }
                    else
                    {
                        var thisAggrLink = new User_Permissioned_Aggregation(edit_project, string.Empty, false, false, false, false, false) { CanPerformQc = true };
                        aggregations.Add(edit_project, thisAggrLink);
                    }
                }
                if (thisKey.IndexOf("admin_project_upload_files_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_upload_files_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanUploadFiles = true;
                    }
                    else
                    {
                        var thisAggrLink = new User_Permissioned_Aggregation(edit_project, string.Empty, false, false, false, false, false) { CanUploadFiles = true };
                        aggregations.Add(edit_project, thisAggrLink);
                    }
                }
                if (thisKey.IndexOf("admin_project_change_visibility_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_change_visibility_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanChangeVisibility = true;
                    }
                    else
                    {
                        var thisAggrLink = new User_Permissioned_Aggregation(edit_project, string.Empty, false, false, false, false, false) { CanChangeVisibility = true };
                        aggregations.Add(edit_project, thisAggrLink);
                    }
                }
                if (thisKey.IndexOf("admin_project_can_delete_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_can_delete_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanDelete = true;
                    }
                    else
                    {
                        var thisAggrLink = new User_Permissioned_Aggregation(edit_project, string.Empty, false, false, false, false, false) { CanDelete = true };
                        aggregations.Add(edit_project, thisAggrLink);
                    }
                }
                if (thisKey.IndexOf("admin_project_curator_") == 0)
                {
                    string admin_project = thisKey.Replace("admin_project_curator_", "");
                    if (aggregations.ContainsKey(admin_project))
                    {
                        aggregations[admin_project].IsCurator = true;
                    }
                    else
                    {
                        aggregations.Add(admin_project, new User_Permissioned_Aggregation(admin_project, string.Empty, false, false, true, false, false));
                    }
                }
                if (thisKey.IndexOf("admin_project_admin_") == 0)
                {
                    string admin_project = thisKey.Replace("admin_project_admin_", "");
                    if (aggregations.ContainsKey(admin_project))
                    {
                        aggregations[admin_project].IsAdmin = true;
                    }
                    else
                    {
                        aggregations.Add(admin_project, new User_Permissioned_Aggregation(admin_project, string.Empty, false, false, false, false, true));
                    }
                }
            }

            // Copy to the object now
            if (editGroup.Aggregations != null) editGroup.Aggregations.Clear();
            foreach (User_Permissioned_Aggregation thisPermissionsAggregation in aggregations.Values)
                editGroup.Add_Aggregation(thisPermissionsAggregation);

            // No immediate save necessary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Group editGroup, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");

            // Get the list of collections lists in the user group object
            List<User_Permissioned_Aggregation> aggregations_in_editable_group = editGroup.Aggregations;
            Dictionary<string, User_Permissioned_Aggregation> lookup_aggs = aggregations_in_editable_group != null ? aggregations_in_editable_group.ToDictionary(ThisAggr => ThisAggr.Code.ToLower()) : new Dictionary<string, User_Permissioned_Aggregation>();

            // Determine if this is a detailed view of rights
            int columns = 7;
            if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
            {
                columns = 12;
            }

            // Step through each aggregation type
            foreach (string aggregationType in UI_ApplicationCache_Gateway.Aggregations.All_Types)
            {
                Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
                if ((aggregationType.Length > 0) && (aggregationType[aggregationType.Length - 1] != 'S'))
                {
                    Output.WriteLine("    <td colspan=\"" + columns + "\"><span style=\"color: White\"><b>" + aggregationType.ToUpper() + "S</b></span></td>");
                }
                else
                {
                    Output.WriteLine("    <td colspan=\"" + columns + "\"><span style=\"color: White\"><b>" + aggregationType.ToUpper() + "</b></span></td>");
                }
                Output.WriteLine("  </tr>");

                Output.WriteLine("  <tr align=\"left\" bgcolor=\"#7d90d5\" >");
                Output.WriteLine("    <td width=\"57px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can select this aggregation when editing or submitting an item\">CAN<br />SELECT</acronym></span></td>");

                if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
                {
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">ITEM<br />EDIT<br />METADATA</acronym></span></td>");
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">ITEM<br />EDIT<br />BEHAVIORS</acronym></span></td>");
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">ITEM<br />PERFORM<br />QC</acronym></span></td>");
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">ITEM<br />UPLOAD<br />FILES</acronym></span></td>");
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">ITEM<br />CHANGE<br />VISIBILITY</acronym></span></td>");
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">ITEM<br />CAN<br />DELETE</acronym></span></td>");
                }
                else
                {
                    Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit anything about an item in this aggregation ( i.e., behaviors, metadata, visibility, etc.. )\">CAN<br />EDIT</acronym></span></td>");
                }
                Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can perform curatorial or collection manager tasks on this aggregation\">IS<br />CURATOR</acronym></span></td>");
                Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can perform administrative tasks on this aggregation\">IS<br />ADMIN</acronym></span></td>");
                Output.WriteLine("    <td align=\"left\" colspan=\"2\"><span style=\"color: White\">ITEM AGGREGATION</span></td>");
                Output.WriteLine("   </tr>");

                // Show all matching rows
                foreach (Item_Aggregation_Related_Aggregations thisAggr in UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type(aggregationType))
                {
                    Output.WriteLine("  <tr align=\"left\" >");
                    if (!lookup_aggs.ContainsKey(thisAggr.Code.ToLower()))
                    {
                        Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_select_" + thisAggr.Code + "\" id=\"admin_project_select_" + thisAggr.Code + "\" /></td>");
                        if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
                        {
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_metadata_" + thisAggr.Code + "\" id=\"admin_project_edit_metadata_" + thisAggr.Code + "\" /></td>");
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_behavior_" + thisAggr.Code + "\" id=\"admin_project_edit_behavior_" + thisAggr.Code + "\" /></td>");
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_perform_qc_" + thisAggr.Code + "\" id=\"admin_project_perform_qc_" + thisAggr.Code + "\" /></td>");
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_upload_files_" + thisAggr.Code + "\" id=\"admin_project_upload_files_" + thisAggr.Code + "\" /></td>");
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_change_visibility_" + thisAggr.Code + "\" id=\"admin_project_change_visibility_" + thisAggr.Code + "\" /></td>");
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_can_delete_" + thisAggr.Code + "\" id=\"admin_project_can_delete_" + thisAggr.Code + "\" /></td>");
                        }
                        else
                        {
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_editall_" + thisAggr.Code + "\" id=\"admin_project_editall_" + thisAggr.Code + "\" /></td>");
                        }
                        Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" /></td>");
                        Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_admin_" + thisAggr.Code + "\" id=\"admin_project_admin_" + thisAggr.Code + "\" /></td>");
                    }
                    else
                    {
                        User_Permissioned_Aggregation foundAggre = lookup_aggs[thisAggr.Code.ToLower()];

                        if (foundAggre.CanSelect)
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_select_" + thisAggr.Code + "\" id=\"admin_project_select_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        else
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_select_" + thisAggr.Code + "\" id=\"admin_project_select_" + thisAggr.Code + "\" /></td>");

                        if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
                        {
                            if (foundAggre.CanEditMetadata)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_metadata_" + thisAggr.Code + "\" id=\"admin_project_edit_metadata_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_metadata_" + thisAggr.Code + "\" id=\"admin_project_edit_metadata_" + thisAggr.Code + "\" /></td>");

                            if (foundAggre.CanEditBehaviors)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_behavior_" + thisAggr.Code + "\" id=\"admin_project_edit_behavior_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_behavior_" + thisAggr.Code + "\" id=\"admin_project_edit_behavior_" + thisAggr.Code + "\" /></td>");

                            if (foundAggre.CanPerformQc)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_perform_qc_" + thisAggr.Code + "\" id=\"admin_project_perform_qc_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_perform_qc_" + thisAggr.Code + "\" id=\"admin_project_perform_qc_" + thisAggr.Code + "\" /></td>");

                            if (foundAggre.CanUploadFiles)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_upload_files_" + thisAggr.Code + "\" id=\"admin_project_upload_files_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_upload_files_" + thisAggr.Code + "\" id=\"admin_project_upload_files_" + thisAggr.Code + "\" /></td>");

                            if (foundAggre.CanChangeVisibility)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_change_visibility_" + thisAggr.Code + "\" id=\"admin_project_change_visibility_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_change_visibility_" + thisAggr.Code + "\" id=\"admin_project_change_visibility_" + thisAggr.Code + "\" /></td>");

                            if (foundAggre.CanDelete)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_can_delete_" + thisAggr.Code + "\" id=\"admin_project_can_delete_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_can_delete_" + thisAggr.Code + "\" id=\"admin_project_can_delete_" + thisAggr.Code + "\" /></td>");
                        }
                        else
                        {
                            if (foundAggre.CanEditItems)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_editall_" + thisAggr.Code + "\" id=\"admin_project_editall_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_editall_" + thisAggr.Code + "\" id=\"admin_project_editall_" + thisAggr.Code + "\" /></td>");
                        }

                        if (foundAggre.IsCurator)
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        else
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" /></td>");

                        if (foundAggre.IsAdmin)
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_admin_" + thisAggr.Code + "\" id=\"admin_project_admin_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        else
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_admin_" + thisAggr.Code + "\" id=\"admin_project_admin_" + thisAggr.Code + "\" /></td>");
                    }

                    Output.WriteLine("    <td>" + thisAggr.Code + "</td>");
                    Output.WriteLine("    <td>" + thisAggr.Name + "</td>");
                    Output.WriteLine("   </tr>");
                    Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"" + columns + "\"></td></tr>");
                }
            }

            Output.WriteLine("</table>");
            Output.WriteLine("<br />");
        }
    }
}
