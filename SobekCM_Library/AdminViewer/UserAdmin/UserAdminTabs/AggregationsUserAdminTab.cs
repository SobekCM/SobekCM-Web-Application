using SobekCM.Core.Aggregations;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    class AggregationsUserAdminTab : iUserAdminTab
    {
        public string TabName => "Aggregations";

        public bool HandlePostback(NameValueCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            string[] getKeys = form.AllKeys;

            Dictionary<string, User_Permissioned_Aggregation> aggregations = new Dictionary<string, User_Permissioned_Aggregation>();

            // Step through each key
            foreach (string thisKey in getKeys)
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
                        aggregations.Add(select_project, new User_Permissioned_Aggregation(select_project, String.Empty, false, false, false, true, false));
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
                        aggregations.Add(select_project, new User_Permissioned_Aggregation(select_project, String.Empty, true, false, false, false, false));
                    }
                }
                if (thisKey.IndexOf("admin_project_editall_") == 0)
                {
                    string edit_project = thisKey.Replace("admin_project_edit_", "");
                    if (aggregations.ContainsKey(edit_project))
                    {
                        aggregations[edit_project].CanEditItems = true;
                    }
                    else
                    {
                        aggregations.Add(edit_project, new User_Permissioned_Aggregation(edit_project, String.Empty, false, true, false, false, false));
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
                        User_Permissioned_Aggregation thisAggrLink = new User_Permissioned_Aggregation(edit_project, String.Empty, false, false, false, false, false) { CanEditMetadata = true };
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
                        User_Permissioned_Aggregation thisAggrLink = new User_Permissioned_Aggregation(edit_project, String.Empty, false, false, false, false, false) { CanEditBehaviors = true };
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
                        User_Permissioned_Aggregation thisAggrLink = new User_Permissioned_Aggregation(edit_project, String.Empty, false, false, false, false, false) { CanPerformQc = true };
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
                        User_Permissioned_Aggregation thisAggrLink = new User_Permissioned_Aggregation(edit_project, String.Empty, false, false, false, false, false) { CanUploadFiles = true };
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
                        User_Permissioned_Aggregation thisAggrLink = new User_Permissioned_Aggregation(edit_project, String.Empty, false, false, false, false, false) { CanChangeVisibility = true };
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
                        User_Permissioned_Aggregation thisAggrLink = new User_Permissioned_Aggregation(edit_project, String.Empty, false, false, false, false, false) { CanDelete = true };
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
                        aggregations.Add(admin_project, new User_Permissioned_Aggregation(admin_project, String.Empty, false, false, true, false, false));
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
                        aggregations.Add(admin_project, new User_Permissioned_Aggregation(admin_project, String.Empty, false, false, false, false, true));
                    }
                }
            }

            // Determine if the aggregationPermissions need to be edited
            bool update_aggregations = false;
            if (editUser.PermissionedAggregations == null || (aggregations.Count != editUser.PermissionedAggregations.Count))
            {
                update_aggregations = true;
            }
            else
            {
                // Build a dictionary of the RequestSpecificValues.Current_User aggregationPermissions as well
                Dictionary<string, User_Permissioned_Aggregation> existingAggr = editUser.PermissionedAggregations.ToDictionary(ThisAggr => ThisAggr.Code);

                // Check all the aggregationPermissions
                foreach (User_Permissioned_Aggregation adminAggr in aggregations.Values)
                {
                    if (existingAggr.ContainsKey(adminAggr.Code))
                    {
                        if ((adminAggr.CanSelect != existingAggr[adminAggr.Code].CanSelect) || (adminAggr.CanEditMetadata != existingAggr[adminAggr.Code].CanEditMetadata) ||
                            (adminAggr.CanEditBehaviors != existingAggr[adminAggr.Code].CanEditBehaviors) || (adminAggr.CanPerformQc != existingAggr[adminAggr.Code].CanPerformQc) ||
                            (adminAggr.CanUploadFiles != existingAggr[adminAggr.Code].CanUploadFiles) || (adminAggr.CanChangeVisibility != existingAggr[adminAggr.Code].CanChangeVisibility) ||
                            (adminAggr.CanDelete != existingAggr[adminAggr.Code].CanDelete) || (adminAggr.IsCurator != existingAggr[adminAggr.Code].IsCurator) ||
                            (adminAggr.OnHomePage != existingAggr[adminAggr.Code].OnHomePage) || (adminAggr.IsAdmin != existingAggr[adminAggr.Code].IsAdmin))
                        {
                            update_aggregations = true;
                            break;
                        }
                    }
                    else
                    {
                        update_aggregations = true;
                        break;
                    }
                }
            }

            // Update the aggregationPermissions, if requested
            if (update_aggregations)
            {
                editUser.Clear_Aggregations();
                if (aggregations.Count > 0)
                {
                    foreach (User_Permissioned_Aggregation dictionaryAggregation in aggregations.Values)
                    {
                        editUser.Add_Aggregation(dictionaryAggregation.Code, dictionaryAggregation.Name, dictionaryAggregation.CanSelect, dictionaryAggregation.CanEditMetadata, dictionaryAggregation.CanEditBehaviors, dictionaryAggregation.CanPerformQc, dictionaryAggregation.CanUploadFiles, dictionaryAggregation.CanChangeVisibility, dictionaryAggregation.CanDelete, dictionaryAggregation.IsCurator, dictionaryAggregation.OnHomePage, dictionaryAggregation.IsAdmin, false);
                    }
                }
            }

            // No immediate save necesary
            return false;
        }


        public void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");

            // Get the list of collections lists in the RequestSpecificValues.Current_User object
            List<User_Permissioned_Aggregation> aggregations_in_editable_user = editUser.PermissionedAggregations;

            Dictionary<string, List<User_Permissioned_Aggregation>> lookup_aggs = new Dictionary<string, List<User_Permissioned_Aggregation>>();
            if (aggregations_in_editable_user != null)
                foreach (User_Permissioned_Aggregation thisAggr in aggregations_in_editable_user)
                {
                    if (lookup_aggs.ContainsKey(thisAggr.Code.ToLower()))
                    {
                        lookup_aggs[thisAggr.Code.ToLower()].Add(thisAggr);
                    }
                    else
                    {
                        List<User_Permissioned_Aggregation> thisAggrList = new List<User_Permissioned_Aggregation>();
                        thisAggrList.Add(thisAggr);
                        lookup_aggs[thisAggr.Code.ToLower()] = thisAggrList;
                    }
                }

            // Determine if this is a detailed view of rights
            int columns = 8;
            if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
            {
                columns = 13;
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
                Output.WriteLine("    <td width=\"55px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Is on user's custom home page\">ON<br />HOME</acronym></span></td>");
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
                        Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_onhome_" + thisAggr.Code + "\" id=\"admin_project_onhome_" + thisAggr.Code + "\" /></td>");
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
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_" + thisAggr.Code + "\" id=\"admin_project_edit_" + thisAggr.Code + "\" /></td>");
                        }

                        Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" /></td>");
                        Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_admin_" + thisAggr.Code + "\" id=\"admin_project_admin_" + thisAggr.Code + "\" /></td>");
                    }
                    else
                    {
                        if (lookup_aggs[thisAggr.Code.ToLower()][0].OnHomePage)
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_onhome_" + thisAggr.Code + "\" id=\"admin_project_onhome_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        else
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_onhome_" + thisAggr.Code + "\" id=\"admin_project_onhome_" + thisAggr.Code + "\" /></td>");

                        bool can_select = false;
                        bool can_select_from_group = false;
                        bool can_edit_metadata = false;
                        bool can_edit_metadata_from_group = false;
                        bool can_edit_behaviors = false;
                        bool can_edit_behaviors_from_group = false;
                        bool can_perform_qc = false;
                        bool can_perform_qc_from_group = false;
                        bool can_change_visibility = false;
                        bool can_change_visibility_from_group = false;
                        bool can_delete_item = false;
                        bool can_delete_item_from_group = false;
                        bool can_upload_files = false;
                        bool can_upload_files_from_group = false;
                        bool is_curator = false;
                        bool is_curator_from_group = false;
                        bool is_admin = false;
                        bool is_admin_from_group = false;
                        foreach (User_Permissioned_Aggregation thisAggrFromList in lookup_aggs[thisAggr.Code.ToLower()])
                        {
                            if (thisAggrFromList.CanSelect)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_select = true;
                                    can_select_from_group = true;
                                }
                                else
                                {
                                    can_select = true;
                                }
                            }
                            if (thisAggrFromList.CanEditMetadata)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_edit_metadata = true;
                                    can_edit_metadata_from_group = true;
                                }
                                else
                                {
                                    can_edit_metadata = true;
                                }
                            }
                            if (thisAggrFromList.CanEditBehaviors)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_edit_behaviors = true;
                                    can_edit_behaviors_from_group = true;
                                }
                                else
                                {
                                    can_edit_behaviors = true;
                                }
                            }
                            if (thisAggrFromList.CanChangeVisibility)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_change_visibility = true;
                                    can_change_visibility_from_group = true;
                                }
                                else
                                {
                                    can_change_visibility = true;
                                }
                            }
                            if (thisAggrFromList.CanDelete)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_delete_item = true;
                                    can_delete_item_from_group = true;
                                }
                                else
                                {
                                    can_delete_item = true;
                                }
                            }
                            if (thisAggrFromList.CanUploadFiles)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_upload_files = true;
                                    can_upload_files_from_group = true;
                                }
                                else
                                {
                                    can_upload_files = true;
                                }
                            }
                            if (thisAggrFromList.CanPerformQc)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    can_perform_qc = true;
                                    can_perform_qc_from_group = true;
                                }
                                else
                                {
                                    can_perform_qc = true;
                                }
                            }
                            if (thisAggrFromList.IsCurator)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    is_curator = true;
                                    is_curator_from_group = true;
                                }
                                else
                                {
                                    is_curator = true;
                                }
                            }
                            if (thisAggrFromList.IsAdmin)
                            {
                                if (thisAggrFromList.GroupDefined)
                                {
                                    is_admin = true;
                                    is_admin_from_group = true;
                                }
                                else
                                {
                                    is_admin = true;
                                }
                            }
                        }

                        if (can_select)
                        {
                            if (can_select_from_group)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_select_" + thisAggr.Code + "\" id=\"admin_project_select_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_select_" + thisAggr.Code + "\" id=\"admin_project_select_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        }
                        else
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_select_" + thisAggr.Code + "\" id=\"admin_project_select_" + thisAggr.Code + "\" /></td>");


                        if (UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions)
                        {
                            if (can_edit_metadata)
                            {
                                if (can_edit_metadata_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_metadata_" + thisAggr.Code + "\" id=\"admin_project_edit_metadata_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_metadata_" + thisAggr.Code + "\" id=\"admin_project_edit_metadata_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_metadata_" + thisAggr.Code + "\" id=\"admin_project_edit_metadata_" + thisAggr.Code + "\" /></td>");

                            if (can_edit_behaviors)
                            {
                                if (can_edit_behaviors_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_behavior_" + thisAggr.Code + "\" id=\"admin_project_edit_behavior_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_behavior_" + thisAggr.Code + "\" id=\"admin_project_edit_behavior_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_edit_behavior_" + thisAggr.Code + "\" id=\"admin_project_edit_behavior_" + thisAggr.Code + "\" /></td>");

                            if (can_perform_qc)
                            {
                                if (can_perform_qc_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_perform_qc_" + thisAggr.Code + "\" id=\"admin_project_perform_qc_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_perform_qc_" + thisAggr.Code + "\" id=\"admin_project_perform_qc_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_perform_qc_" + thisAggr.Code + "\" id=\"admin_project_perform_qc_" + thisAggr.Code + "\" /></td>");

                            if (can_upload_files)
                            {
                                if (can_upload_files_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_upload_files_" + thisAggr.Code + "\" id=\"admin_project_upload_files_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_upload_files_" + thisAggr.Code + "\" id=\"admin_project_upload_files_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_upload_files_" + thisAggr.Code + "\" id=\"admin_project_upload_files_" + thisAggr.Code + "\" /></td>");

                            if (can_change_visibility)
                            {
                                if (can_change_visibility_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_change_visibility_" + thisAggr.Code + "\" id=\"admin_project_change_visibility_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_change_visibility_" + thisAggr.Code + "\" id=\"admin_project_change_visibility_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_change_visibility_" + thisAggr.Code + "\" id=\"admin_project_change_visibility_" + thisAggr.Code + "\" /></td>");

                            if (can_delete_item)
                            {
                                if (can_delete_item_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_can_delete_" + thisAggr.Code + "\" id=\"admin_project_can_delete_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_can_delete_" + thisAggr.Code + "\" id=\"admin_project_can_delete_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_can_delete_" + thisAggr.Code + "\" id=\"admin_project_can_delete_" + thisAggr.Code + "\" /></td>");
                        }
                        else
                        {
                            if (can_edit_metadata)
                            {
                                if (can_edit_metadata_from_group)
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_editall_" + thisAggr.Code + "\" id=\"admin_project_editall_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                                else
                                    Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_editall_" + thisAggr.Code + "\" id=\"admin_project_editall_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                            }
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_editall_" + thisAggr.Code + "\" id=\"admin_project_editall_" + thisAggr.Code + "\" /></td>");
                        }

                        if (is_curator)
                        {
                            if (is_curator_from_group)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        }
                        else
                            Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_curator_" + thisAggr.Code + "\" id=\"admin_project_curator_" + thisAggr.Code + "\" /></td>");

                        if (is_admin)
                        {
                            if (is_admin_from_group)
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_admin_" + thisAggr.Code + "\" id=\"admin_project_admin_" + thisAggr.Code + "\" checked=\"checked\" disabled=\"disabled\" /></td>");
                            else
                                Output.WriteLine("    <td><input type=\"checkbox\" name=\"admin_project_admin_" + thisAggr.Code + "\" id=\"admin_project_admin_" + thisAggr.Code + "\" checked=\"checked\" /></td>");
                        }
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
