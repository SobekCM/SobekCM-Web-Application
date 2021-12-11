using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public class ViewUser_UsersAdminSubViewer :  abstractUsersAdminSubViewer
    {
        public override string Title => "View User Information";

        public override void HandlePostback(RequestCache RequestSpecificValues)
        {
            // Does nothing ... this really is display only
        }

        public override void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("<div class=\"SobekHomeText\">");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            Output.WriteLine("    <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Return to user list</a><br /><br />");
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = editUser.UserID.ToString();
            Output.WriteLine("    <a href=\"" + UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode) + "\">Edit this user</a>");
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = editUser.UserID.ToString() + "v";
            Output.WriteLine("  </blockquote>");

            Output.WriteLine("  <span class=\"SobekAdminTitle\">Basic Information</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            Output.WriteLine("  <table cellpadding=\"4px\" >");
            if (editUser.ShibbID.Trim().Length > 0)
                Output.WriteLine("  <tr valign=\"top\"><td><b>UFID:</b></td><td>" + editUser.ShibbID + "</td></tr>");
            Output.WriteLine("  <tr valign=\"top\"><td><b>UserName:</b></td><td>" + editUser.UserName + "</td></tr>");
            Output.WriteLine("  <tr valign=\"top\"><td><b>Email:</b></td><td>" + editUser.Email + "</td></tr>");
            Output.WriteLine("  <tr valign=\"top\"><td><b>Full Name:</b></td><td>" + editUser.Full_Name + "</td></tr>");

            // Build the rights statement
            StringBuilder text_builder = new StringBuilder();
            if (editUser.Can_Submit)
                text_builder.Append("Can submit items<br />");
            if (editUser.Is_Internal_User)
                text_builder.Append("Is power user<br />");
            if (editUser.Editable_Regular_Expressions.Any(ThisRegularExpression => ThisRegularExpression == "[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}"))
            {
                text_builder.Append("Can edit all items<br />");
            }
            if (editUser.Can_Delete_All)
                text_builder.Append("Can delete all items<br />");
            if (editUser.Is_User_Admin)
                text_builder.Append("Is user administrator<br />");
            if (editUser.Is_Portal_Admin)
                text_builder.Append("Is portal administrator<br />");

            if (editUser.Is_System_Admin)
                text_builder.Append("Is system administrator<br />");

            if ((UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (editUser.Is_Host_Admin))
                text_builder.Append("Is host administrator<br />");

            if (editUser.Include_Tracking_In_Standard_Forms)
                text_builder.Append("Tracking data should be included in standard input forms<br />");

            if (text_builder.Length == 0)
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Global Permissions:</b></td><td><i>none</i></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Global Permissions:</b></td><td>" + text_builder + "</td></tr>");
                text_builder.Remove(0, text_builder.Length);
            }

            Output.WriteLine("  <tr valign=\"top\"><td><b>Edit Templates:</b></td><td>" + editUser.Edit_Template_Code_Complex + "<br />" + editUser.Edit_Template_Code_Simple + "</td></tr>");

            // Build the templates list
            List<string> addedtemplates = new List<string>();
            foreach (string thisTemplate in editUser.Templates)
            {
                if (!addedtemplates.Contains(thisTemplate))
                {
                    text_builder.Append(thisTemplate + "<br />");
                    addedtemplates.Add(thisTemplate);
                }
            }
            if (text_builder.Length == 0)
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Templates:</b></td><td><i>none</i></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Templates:</b></td><td>" + text_builder + "</td></tr>");
                text_builder.Remove(0, text_builder.Length);
            }

            // Build the projects list
            List<string> addedprojects = new List<string>();
            foreach (string thisProject in editUser.Default_Metadata_Sets)
            {
                if (!addedprojects.Contains(thisProject))
                {
                    text_builder.Append(thisProject + "<br />");
                    addedprojects.Add(thisProject);
                }
            }
            if (text_builder.Length == 0)
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Default Metadata:</b></td><td><i>none</i></td></tr>");
            }
            else
            {
                Output.WriteLine("  <tr valign=\"top\"><td><b>Default Metadata:</b></td><td>" + text_builder + "</td></tr>");
                text_builder.Remove(0, text_builder.Length);
            }


            Output.WriteLine("  </table>");
            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <span class=\"SobekAdminTitle\">Group Membership</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            if (editUser.User_Groups.Count == 0)
            {
                Output.WriteLine("<i> &nbsp;This user is not a member of any user groups</i>");
            }
            else
            {
                foreach (Simple_User_Group_Info userGroup in editUser.User_Groups)
                {
                    text_builder.Append(userGroup.Name + "<br />");
                }
                Output.WriteLine("  <table cellpadding=\"4px\" >");
                Output.WriteLine("  <tr valign=\"top\"><td><b>User Groups:</b></td><td>" + text_builder + "</td></tr>");
                Output.WriteLine("  </table>");
            }

            Output.WriteLine("  </blockquote>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <span class=\"SobekAdminTitle\">Aggregations</span>");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <blockquote>");
            if (editUser.PermissionedAggregations == null || editUser.PermissionedAggregations.Count == 0)
            {

                Output.WriteLine("<i> &nbsp;No special aggregation rights are assigned to this user</i>");

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

                // Get the list of collections lists in the RequestSpecificValues.Current_User object
                List<User_Permissioned_Aggregation> aggregations_in_editable_user = editUser.PermissionedAggregations;
                Dictionary<string, User_Permissioned_Aggregation> lookup_aggs = new Dictionary<string, User_Permissioned_Aggregation>();
                foreach (User_Permissioned_Aggregation thisAggr in aggregations_in_editable_user)
                {
                    if (!lookup_aggs.ContainsKey(thisAggr.Code.ToLower()))
                        lookup_aggs[thisAggr.Code.ToLower()] = thisAggr;
                    else
                    {
                        User_Permissioned_Aggregation current = lookup_aggs[thisAggr.Code.ToLower()];
                        if (thisAggr.CanChangeVisibility) current.CanChangeVisibility = true;
                        if (thisAggr.CanDelete) current.CanDelete = true;
                        if (thisAggr.CanEditBehaviors) current.CanEditBehaviors = true;
                        if (thisAggr.CanEditItems) current.CanEditItems = true;
                        if (thisAggr.CanEditMetadata) current.CanEditMetadata = true;
                        if (thisAggr.CanPerformQc) current.CanPerformQc = true;
                        if (thisAggr.CanSelect) current.CanSelect = true;
                        if (thisAggr.CanUploadFiles) current.CanUploadFiles = true;
                        if (thisAggr.IsAdmin) current.IsAdmin = true;
                        if (thisAggr.IsCurator) current.IsCurator = true;
                        if (thisAggr.OnHomePage) current.OnHomePage = true;
                    }
                }

                // Step through each aggregation type
                foreach (string aggregationType in UI_ApplicationCache_Gateway.Aggregations.All_Types)
                {
                    bool type_label_drawn = false;

                    // Show all matching rows
                    foreach (Item_Aggregation_Related_Aggregations thisAggr in UI_ApplicationCache_Gateway.Aggregations.Aggregations_By_Type(aggregationType))
                    {

                        if (lookup_aggs.ContainsKey(thisAggr.Code.ToLower()))
                        {
                            User_Permissioned_Aggregation aggrPermissions = lookup_aggs[thisAggr.Code.ToLower()];

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
                                Output.WriteLine("    <td width=\"55px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Is on user's custom home page\">ON<br />HOME</acronym></span></td>");
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
                            Output.WriteLine(aggrPermissions.OnHomePage
                                                 ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                                 : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(aggrPermissions.CanSelect
                                                 ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                                 : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");


                            if (detailedPermissions)
                            {
                                Output.WriteLine(aggrPermissions.CanEditMetadata
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                                Output.WriteLine(aggrPermissions.CanEditBehaviors
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                                Output.WriteLine(aggrPermissions.CanPerformQc
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                                Output.WriteLine(aggrPermissions.CanUploadFiles
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                                Output.WriteLine(aggrPermissions.CanChangeVisibility
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                                Output.WriteLine(aggrPermissions.CanDelete
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            }
                            else
                            {
                                Output.WriteLine(aggrPermissions.CanEditItems
                                    ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                    : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");
                            }

                            Output.WriteLine(aggrPermissions.IsCurator
                                                 ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                                 : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine(aggrPermissions.IsAdmin
                                                ? "    <td><input type=\"checkbox\" disabled=\"disabled\" checked=\"checked\" /></td>"
                                                : "    <td><input type=\"checkbox\" disabled=\"disabled\" /></td>");

                            Output.WriteLine("    <td>" + thisAggr.Code + "</td>");
                            Output.WriteLine("    <td>" + thisAggr.Name + "</td>");
                            Output.WriteLine("   </tr>");
                            Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"" + columns + "\"></td></tr>");
                        }
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
