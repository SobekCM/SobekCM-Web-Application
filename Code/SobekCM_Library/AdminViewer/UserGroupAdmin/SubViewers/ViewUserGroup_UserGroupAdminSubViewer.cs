using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.SubViewers
{
    /// <summary> Read-only display of a single user group's information, membership, and aggregation rights </summary>
    /// <remarks> Extracted verbatim from the former monolithic <c>User_Group_AdminViewer.Write_View_User_Group_Form</c>. </remarks>
    public class ViewUserGroup_UserGroupAdminSubViewer : abstractUserGroupAdminSubViewer
    {
        public override string Title => "View User Group Information";

        public override void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context)
        {
            // Does nothing ... this really is display only
        }

        public override void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<div class=\"SobekHomeText\">");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.Users;
            Output.WriteLine("    <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Return to user group list</a><br /><br />");
            RequestSpecificValues.Current_Mode.Admin_Type = Admin_View_Codes.User_Groups;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = editGroup.UserGroupID.ToString();
            Output.WriteLine("    <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Edit this user group</a>");
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = editGroup.UserGroupID.ToString() + "v";
            Output.WriteLine("  </blockquote>");

            Output.WriteLine("  <span class=\"SobekAdminTitle\">Basic Information</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("  <table cellpadding=\"4px\" >");
            Output.WriteLine("  <tr valign=\"top\"><td><b>Name:</b></td><td>" + editGroup.Name + "</td></tr>");
            Output.WriteLine("  <tr valign=\"top\"><td><b>Description:</b></td><td>" + editGroup.Description + "</td></tr>");

            // Build the rights statement
            var text_builder = new StringBuilder();
            if (editGroup.CanSubmit)
                text_builder.Append("Can submit items<br />");
            if (editGroup.IsInternalUser)
                text_builder.Append("Is internal user<br />");
            if ((editGroup.Editable_Regular_Expressions != null) && (editGroup.Editable_Regular_Expressions.Any(ThisRegularExpression => ThisRegularExpression == "[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}")))
            {
                text_builder.Append("Can edit all items<br />");
            }
            if (editGroup.IsSystemAdmin)
                text_builder.Append("Is system administrator<br />");
            if (text_builder.Length == 0)
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Global Permissions:</b></td><td><i>none</i></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Global Permissions:</b></td><td>" + text_builder + "</td></tr>");
                text_builder.Remove(0, text_builder.Length);
            }

            Output.WriteLine("  </table>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <span class=\"SobekAdminTitle\">User Membership</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            if (editGroup.Users_Count == 0)
            {
                Output.WriteLine("<i> &nbsp;This user group does not currently contain any users</i>");
            }
            else
            {
                foreach (User_Group_Member thisUser in editGroup.Users)
                {
                    text_builder.Append(thisUser.UserName + "<br />");
                }
                Output.WriteLine("  <table cellpadding=\"4px\" >");
                Output.WriteLine("  <tr valign=\"top\"><td><b>Users:</b></td><td>" + text_builder + "</td></tr>");
                Output.WriteLine("  </table>");
            }

            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <span class=\"SobekAdminTitle\">Aggregations</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            if (editGroup.Aggregations_Count == 0)
            {
                Output.WriteLine("<i> &nbsp;No special aggregation rights are assigned to this user group</i>");
            }
            else
            {
                Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");

                // Is this using detailed permissions?
                bool detailedPermissions = UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions;

                // Dertermine the number of columns
                int columns = 7;
                if (detailedPermissions)
                    columns = 12;

                // Get the list of collections lists in the user object
                List<User_Permissioned_Aggregation> aggregations_in_editable_user = editGroup.Aggregations;
                Dictionary<string, User_Permissioned_Aggregation> lookup_aggs = aggregations_in_editable_user.ToDictionary(ThisAggr => ThisAggr.Code.ToLower());

                // Step through each aggregation type
                foreach (string aggregationType in UI_ApplicationCache_Gateway.Aggregations.All_Types)
                {
                    bool type_label_drawn = false;

                    // Show all matching rows
                    ReadOnlyCollection<Item_Aggregation_Related_Aggregations> aggrsByType = UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type(aggregationType);
                    foreach (Item_Aggregation_Related_Aggregations thisAggr in aggrsByType)
                    {
                        if (!lookup_aggs.ContainsKey(thisAggr.Code.ToLower()))
                            continue;

                        if (!type_label_drawn)
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

                            if (detailedPermissions)
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
                                Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can edit any item in this aggregation\">CAN<br />EDIT</acronym></span></td>");
                            }

                            Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can perform curatorial or collection manager tasks on this aggregation\">IS<br />CURATOR</acronym></span></td>");
                            Output.WriteLine("    <td width=\"50px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Can perform curatorial or collection manager tasks on this aggregation\">IS<br />ADMIN</acronym></span></td>");

                            Output.WriteLine("    <td align=\"left\" colspan=\"2\"><span style=\"color: White\">ITEM AGGREGATION</span></td>");
                            Output.WriteLine("   </tr>");

                            type_label_drawn = true;
                        }

                        Output.WriteLine("  <tr align=\"left\" >");

                        User_Permissioned_Aggregation matchingAggr = lookup_aggs[thisAggr.Code.ToLower()];

                        Output.WriteLine(matchingAggr.CanSelect
                                             ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                             : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                        if (detailedPermissions)
                        {
                            Output.WriteLine(matchingAggr.CanEditMetadata
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(matchingAggr.CanEditBehaviors
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(matchingAggr.CanPerformQc
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(matchingAggr.CanUploadFiles
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(matchingAggr.CanChangeVisibility
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(matchingAggr.CanDelete
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");
                        }
                        else
                        {
                            Output.WriteLine(matchingAggr.CanEditItems
                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");
                        }

                        Output.WriteLine(matchingAggr.IsCurator
                                             ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                             : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                        Output.WriteLine(matchingAggr.IsAdmin
                                             ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                             : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                        Output.WriteLine("    <td>" + thisAggr.Code + "</td>");
                        Output.WriteLine("    <td>" + thisAggr.Name + "</td>");
                        Output.WriteLine("   </tr>");
                        Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"" + columns + "\"></td></tr>");
                    }
                }

                Output.WriteLine("</table>");
                Output.WriteLine("<br />");
            }
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("</div>");
        }
    }
}
