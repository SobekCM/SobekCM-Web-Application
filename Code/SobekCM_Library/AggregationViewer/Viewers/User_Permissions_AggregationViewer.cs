#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.Database;
using SobekCM.Library.HTML;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

#endregion

namespace SobekCM.Library.AggregationViewer.Viewers
{
    /// <summary> Aggregation viewer to dispay the list of users that have special permissions on a single aggregation </summary>
    public class User_Permissions_AggregationViewer : abstractAggregationViewer
    {
        /// <summary> Constructor for a new instance of the User_Permissions_AggregationViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ViewBag"> Aggregation-specific request information, such as aggregation object and any browse object requested </param>
        public User_Permissions_AggregationViewer(RequestCache RequestSpecificValues, AggregationViewBag ViewBag, HttpContext Context)
            : base(RequestSpecificValues, ViewBag, Context)
        {
            // User must AT LEAST be logged on, return
            if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
            {
                RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // If the user is not an admin of some type, also return
            if ((!RequestSpecificValues.Current_User.Is_System_Admin) && (!RequestSpecificValues.Current_User.Is_Portal_Admin) && (!RequestSpecificValues.Current_User.Is_Aggregation_Curator(ViewBag.Hierarchy_Object.Code)))
            {
                RequestSpecificValues.Current_Mode.Aggregation_Type = Aggregation_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
            }
        }

        /// <summary> Gets the collection of special behaviors which this aggregation viewer
        /// requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> AggregationViewer_Behaviors
        {
            get { return new List<HtmlSubwriter_Behaviors_Enum> { HtmlSubwriter_Behaviors_Enum.Use_Jquery_DataTables }; }
        }

        /// <summary> Gets the type of collection view or search supported by this collection viewer </summary>
        public override Item_Aggregation_Views_Searches_Enum Type
        {
            get { return Item_Aggregation_Views_Searches_Enum.User_Permissions; }
        }

        /// <summary> Flag which indicates whether the selection panel should be displayed </summary>
        /// <value> This defaults to <see cref="Selection_Panel_Display_Enum.Selectable"/> but is overwritten by most collection viewers </value>
        public override Selection_Panel_Display_Enum Selection_Panel_Display
        {
            get { return Selection_Panel_Display_Enum.Never; }
        }

        /// <summary> Gets flag which indicates whether this is an internal view, which may have a 
        /// slightly different design feel </summary>
        /// <remarks> This returns FALSE by default, but can be overriden by individual viewer implementations</remarks>
        public override bool Is_Internal_View
        {
            get { return true; }
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Viewer_Title
        {
            get { return Localization_Gateway.User_Permissions_Aggregation.Viewer_Title(RequestSpecificValues.Current_Mode.Language); }
        }

        /// <summary> Gets the URL for the icon related to this aggregational viewer task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources_Gateway.User_Permission_Img; }
        }

        /// <summary> Add the HTML to be displayed in the search box </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This does nothing - as an internal type view, this will not be called </remarks>
        public override void Write_Search_Box_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing
        }

        /// <summary> Add the main HTML to be added to the page </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This writes the HTML from the static browse or info page here  </remarks>
        public override void Write_Main_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            string language = RequestSpecificValues.Current_Mode.Language;
            DataTable permissionsTbl = SobekCM_Database.Get_Aggregation_User_Permissions(ViewBag.Hierarchy_Object.Code, RequestSpecificValues.Tracer);


            // Is this a system administrator?
            bool isSysAdmin = ((RequestSpecificValues.Current_User.Is_System_Admin) || (RequestSpecificValues.Current_User.Is_Host_Admin));
            string userAdminUrl = String.Empty;
            string userGroupAdminUrl = String.Empty;

            // If no permissions received, just show a message
            if ((permissionsTbl == null) || (permissionsTbl.Rows.Count == 0))
            {
                Output.WriteLine("<p>" + Localization_Gateway.User_Permissions_Aggregation.No_Permissions_Message(language) + "</p>");

                if (isSysAdmin)
                {
                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                    RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Users;

                    Output.WriteLine("<p>" + String.Format(Localization_Gateway.User_Permissions_Aggregation.Assign_Permissions_Sentence(language), UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode)));

                    RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                }

                Output.WriteLine("  <br /><br />");

                return;
            }

            Output.WriteLine("<p style=\"text-align: left; padding:0 20px 0 20px;\">" + Localization_Gateway.User_Permissions_Aggregation.Users_List_Intro(language) + "</p>");





            // Show a message about selecting the user below to edit them
            if (isSysAdmin)
            {
                Output.WriteLine("<p style=\"text-align: left; padding:0 20px 0 20px;\">" + Localization_Gateway.User_Permissions_Aggregation.Select_User_Prompt(language) + "</p>");

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Users;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "Xyzzy";
                userAdminUrl = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.User_Groups;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = "Xyzzy";
                userGroupAdminUrl = UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode);

                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
            }

            // Is this using detailed permissions?
            bool detailedPermissions = UI_ApplicationCache_Gateway.Settings.System.Detailed_User_Aggregation_Permissions;

            Output.WriteLine("  <table id=\"sbkPrav_DetailedUsers\">");
            Output.WriteLine("  <thead>");
            Output.WriteLine("    <tr>");
            Output.WriteLine("      <th style=\"width:180px;\">" + Localization_Gateway.User_Permissions_Aggregation.User_Column(language) + "</th>");
            Output.WriteLine("      <th style=\"width:90px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Can_Select_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Can_Select_Header(language) + "</acronym></th>");

            if (detailedPermissions)
            {
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Edit_Metadata_Header(language) + "</acronym></th>");
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Edit_Behaviors_Header(language) + "</acronym></th>");
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Perform_Qc_Header(language) + "</acronym></th>");
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Upload_Files_Header(language) + "</acronym></th>");
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Change_Visibility_Header(language) + "</acronym></th>");
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Can_Delete_Header(language) + "</acronym></th>");

            }
            else
            {
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Can_Edit_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Can_Edit_Header(language) + "</acronym></th>");
            }

            Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Curator_Admin_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Is_Curator_Header(language) + "</acronym></th>");
            Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Curator_Admin_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Is_Admin_Header(language) + "</acronym></th>");
            Output.WriteLine("    </tr>");
            Output.WriteLine("  </thead>");
            Output.WriteLine("  <tbody>");

            // Collect the relevant user group rows, if some permissions were assined by user group
            var userGroupRows = new SortedDictionary<string, DataRow>();

            // Users that are attached to user groups may have multiple rows with their name, so collect
            // all the user information from all rows before displaying
            int last_userid = -1;
            string username = String.Empty;
            bool canSelect = false;
            bool canEditMetadata = false;
            bool canEditBehaviors = false;
            bool canPerformQc = false;
            bool canUploadFiles = false;
            bool canChangeVisibility = false;
            bool canDelete = false;
            bool isCurator = false;
            bool isAdmin = false;
            foreach (DataRow thisUser in permissionsTbl.Rows)
            {
                // Get the user id for this user row
                int thisUserId = Convert.ToInt32(thisUser["UserID"].ToString());

                // See if this is a new user, to be displayed
                if ((last_userid > 0) && (last_userid != thisUserId))
                {
                    // Show the information for this user
                    if (isSysAdmin)
                    {
                        Output.WriteLine("    <tr class=\"sbkUpav_SelectableRow\" onclick=\"window.open('" + userAdminUrl.Replace("Xyzzy", thisUserId.ToString()) + "', '_UserEdit+ " + thisUserId + "');\">");
                    }
                    else
                    {
                        Output.WriteLine("    <tr>");
                    }

                    Output.WriteLine("      <td>" + username + "</td>");
                    Output.WriteLine("      <td>" + flag_to_display(canSelect) + "</td>");
                    if (detailedPermissions)
                    {
                        Output.WriteLine("      <td>" + flag_to_display(canEditMetadata) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(canEditBehaviors) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(canPerformQc) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(canUploadFiles) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(canChangeVisibility) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(canDelete) + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>" + flag_to_display(canEditMetadata) + "</td>");
                    }


                    Output.WriteLine("      <td>" + flag_to_display(isCurator) + "</td>");
                    Output.WriteLine("      <td>" + flag_to_display(isAdmin) + "</td>");

                    Output.WriteLine("    </tr>");


                    // Prepare to collect the information about this user
                    canSelect = false;
                    canEditMetadata = false;
                    canEditBehaviors = false;
                    canPerformQc = false;
                    canUploadFiles = false;
                    canChangeVisibility = false;
                    canDelete = false;
                    isCurator = false;
                    isAdmin = false;
                }
                last_userid = thisUserId;

                // Collect the flags from this row
                username = thisUser["LastName"] + "," + thisUser["FirstName"];
                if ((thisUser["CanSelect"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanSelect"]))) canSelect = true;
                if ((thisUser["CanEditMetadata"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanEditMetadata"]))) canEditMetadata = true;
                if ((thisUser["CanEditBehaviors"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanEditBehaviors"]))) canEditBehaviors = true;
                if ((thisUser["CanPerformQc"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanPerformQc"]))) canPerformQc = true;
                if ((thisUser["CanUploadFiles"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanUploadFiles"]))) canUploadFiles = true;
                if ((thisUser["CanChangeVisibility"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanChangeVisibility"]))) canChangeVisibility = true;
                if ((thisUser["CanDelete"] != DBNull.Value) && (Convert.ToBoolean(thisUser["CanDelete"]))) canDelete = true;
                if ((thisUser["IsCollectionManager"] != DBNull.Value) && (Convert.ToBoolean(thisUser["IsCollectionManager"]))) isCurator = true;
                if ((thisUser["IsAggregationAdmin"] != DBNull.Value) && (Convert.ToBoolean(thisUser["IsAggregationAdmin"]))) isAdmin = true;

                // If this is from a user group, save that row as well
                if ((thisUser["GroupName"] != DBNull.Value) && (thisUser["GroupName"].ToString().Length > 0))
                {
                    string groupName = thisUser["GroupName"].ToString();
                    if (!userGroupRows.ContainsKey(groupName))
                    {
                        userGroupRows[groupName] = thisUser;
                    }
                }
            }

            // Show the information for the last user
            if (isSysAdmin)
            {
                Output.WriteLine("    <tr class=\"sbkUpav_SelectableRow\" onclick=\"window.open('" + userAdminUrl.Replace("Xyzzy", last_userid.ToString()) + "', '_UserEdit+ " + last_userid + "');\">");
            }
            else
            {
                Output.WriteLine("    <tr>");
            }
            Output.WriteLine("      <td>" + username + "</td>");
            Output.WriteLine("      <td>" + flag_to_display(canSelect) + "</td>");
            if (detailedPermissions)
            {
                Output.WriteLine("      <td>" + flag_to_display(canEditMetadata) + "</td>");
                Output.WriteLine("      <td>" + flag_to_display(canEditBehaviors) + "</td>");
                Output.WriteLine("      <td>" + flag_to_display(canPerformQc) + "</td>");
                Output.WriteLine("      <td>" + flag_to_display(canUploadFiles) + "</td>");
                Output.WriteLine("      <td>" + flag_to_display(canChangeVisibility) + "</td>");
                Output.WriteLine("      <td>" + flag_to_display(canDelete) + "</td>");
            }
            else
            {
                Output.WriteLine("      <td>" + flag_to_display(canEditMetadata) + "</td>");
            }


            Output.WriteLine("      <td>" + flag_to_display(isCurator) + "</td>");
            Output.WriteLine("      <td>" + flag_to_display(isAdmin) + "</td>");

            Output.WriteLine("    </tr>");
            Output.WriteLine("  <tbody>");
            Output.WriteLine("  </table>");
            Output.WriteLine("  <br /><br />");

            Output.WriteLine("<script type=\"text/javascript\">");
            Output.WriteLine("    $(document).ready(function() { ");
            Output.WriteLine("        var table = $('#sbkPrav_DetailedUsers').DataTable({ ");
            Output.WriteLine("            \"paging\":   false, ");
            Output.WriteLine("            \"filter\":   false, ");
            Output.WriteLine("            \"info\":   false });");
            Output.WriteLine("    } );");
            Output.WriteLine("</script>");


            // If there were user groups, add them now also.
            if (userGroupRows.Count > 0)
            {
                Output.WriteLine("<p style=\"text-align: left; padding:0 20px 0 20px;\">" + Localization_Gateway.User_Permissions_Aggregation.Group_Permissions_Intro(language) + "</p>");


                Output.WriteLine("  <table id=\"sbkPrav_DetailedUserGroups\">");
                Output.WriteLine("  <thead>");
                Output.WriteLine("    <tr>");
                Output.WriteLine("      <th style=\"width:180px;\">" + Localization_Gateway.User_Permissions_Aggregation.User_Group_Column(language) + "</th>");
                Output.WriteLine("      <th style=\"width:90px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Can_Select_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Can_Select_Header(language) + "</acronym></th>");

                if (detailedPermissions)
                {
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Edit_Metadata_Header(language) + "</acronym></th>");
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Edit_Behaviors_Header(language) + "</acronym></th>");
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Perform_Qc_Header(language) + "</acronym></th>");
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Upload_Files_Header(language) + "</acronym></th>");
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Change_Visibility_Header(language) + "</acronym></th>");
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Item_Permissions_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Item_Can_Delete_Header(language) + "</acronym></th>");

                }
                else
                {
                    Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Can_Edit_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Can_Edit_Header(language) + "</acronym></th>");
                }

                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Curator_Admin_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Is_Curator_Header(language) + "</acronym></th>");
                Output.WriteLine("      <th style=\"width:85px;\"><acronym title=\"" + Localization_Gateway.User_Permissions_Aggregation.Curator_Admin_Tooltip(language) + "\">" + Localization_Gateway.User_Permissions_Aggregation.Is_Admin_Header(language) + "</acronym></th>");
                Output.WriteLine("    </tr>");
                Output.WriteLine("  </thead>");
                Output.WriteLine("  <tbody>");


                foreach (KeyValuePair<string, DataRow> userGroupRow in userGroupRows)
                {
                    DataRow thisUser = userGroupRow.Value;
                    int userGroupId = Convert.ToInt32(thisUser["UserGroupID"]);

                    if (isSysAdmin)
                    {
                        Output.WriteLine("    <tr class=\"sbkUpav_SelectableRow\" onclick=\"window.open('" + userGroupAdminUrl.Replace("Xyzzy", userGroupId.ToString()) + "', '_UserGroupEdit+ " + userGroupId + "');\">");
                    }
                    else
                    {
                        Output.WriteLine("    <tr>");
                    }
                    Output.WriteLine("      <td>" + userGroupRow.Key + "</td>");
                    Output.WriteLine("      <td>" + flag_to_display(thisUser["CanSelect"]) + "</td>");
                    if (detailedPermissions)
                    {
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanEditMetadata"]) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanEditBehaviors"]) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanPerformQc"]) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanUploadFiles"]) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanChangeVisibility"]) + "</td>");
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanDelete"]) + "</td>");
                    }
                    else
                    {
                        Output.WriteLine("      <td>" + flag_to_display(thisUser["CanEditMetadata"]) + "</td>");
                    }


                    Output.WriteLine("      <td>" + flag_to_display(thisUser["IsCollectionManager"]) + "</td>");
                    Output.WriteLine("      <td>" + flag_to_display(thisUser["IsAggregationAdmin"]) + "</td>");

                    Output.WriteLine("    </tr>");
                }

                Output.WriteLine("  </tbody>");
                Output.WriteLine("  </table>");
                Output.WriteLine("  <br /><br />");

                Output.WriteLine("<script type=\"text/javascript\">");
                Output.WriteLine("    $(document).ready(function() { ");
                Output.WriteLine("        var table2 = $('#sbkPrav_DetailedUserGroups').DataTable({ ");
                Output.WriteLine("            \"paging\":   false, ");
                Output.WriteLine("            \"filter\":   false, ");
                Output.WriteLine("            \"info\":   false });");
                Output.WriteLine("    } );");
                Output.WriteLine("</script>");
            }

            if (isSysAdmin)
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Administrative;
                RequestSpecificValues.Current_Mode.Admin_Type = Admin_Type_Enum.Users;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;

                Output.WriteLine("  <p style=\"text-align: left; padding:0 20px 0 20px;\">" + String.Format(Localization_Gateway.User_Permissions_Aggregation.Assign_New_Permissions_Sentence(language), UrlWriterHelper.Redirect_URL(RequestSpecificValues.Current_Mode)));
                Output.WriteLine("  <br /><br />");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
            }
        }


        private string flag_to_display(bool ToDisplay)
        {
            if (ToDisplay)
                return "Y";
            return "";
        }

        private string flag_to_display(object ToDisplay)
        {
            if ((ToDisplay != DBNull.Value) && (Convert.ToBoolean(ToDisplay)))
                return "Y";
            return "";
        }
    }
}