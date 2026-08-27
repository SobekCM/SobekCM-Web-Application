using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.AdminViewer.UserGroupAdmin.UserGroupAdminTabs;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.SubViewers
{
    /// <summary> Tabbed editor for a single user group, mirroring <c>EditUser_UsersAdminSubViewer</c>'s
    /// shape exactly (a list of tabs, a letter-offset submode encoding which one is current, all
    /// persistence handled here in one batch regardless of which tab was active when SAVE was pressed) </summary>
    public class EditUserGroup_UserGroupAdminSubViewer : abstractUserGroupAdminSubViewer
    {
        private List<iUserGroupAdminTab> tabs;
        private iUserGroupAdminTab currentTab;
        private string actionMessage = String.Empty;

        public EditUserGroup_UserGroupAdminSubViewer()
        {
            build_tabs();
        }

        private void build_tabs()
        {
            // Same "hard-coded for now, could be config/plug-in driven later" approach as
            // EditUser_UsersAdminSubViewer.build_tabs()
            tabs = new List<iUserGroupAdminTab>
            {
                new BasicInfoUserGroupAdminTab(),
                new AggregationsUserGroupAdminTab(),
                new SubmissionsUserGroupAdminTab()
            };
        }

        private void set_current_page(RequestCache RequestSpecificValues)
        {
            int page = 0;
            string remaining_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Replace(editGroup.UserGroupID.ToString(), "").ToLower();
            if (remaining_submode.Length > 0)
            {
                page = ((int)remaining_submode[0]) - ((int)'a');
                if ((page < 0) || (page >= tabs.Count))
                    page = 0;
            }
            currentTab = tabs[page];
        }

        public override string Title => "Edit User Group";

        public override void HandlePostback(RequestCache RequestSpecificValues, HttpContext Context)
        {
            set_current_page(RequestSpecificValues);

            var form = Context.Request.Form;
            string action = form["admin_user_group_save"];

            // If this is CANCEL, get rid of the current edit object in the session
            if (action == "cancel")
            {
                Context.SessionObject()["Edit_UserGroup_" + editGroup.UserGroupID] = null;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                return;
            }

            // Let the current tab handle the postback
            bool saveImmediately = currentTab.HandlePostback(form, editGroup, RequestSpecificValues);

            // Should this be saved to the database?
            if ((action == "save") || (saveImmediately))
            {
                // Must have a name to continue
                if (editGroup.Name.Length > 0)
                {
                    bool successful_save = true;

                    int newid = SobekCM_Database.Save_User_Group(editGroup.UserGroupID, editGroup.Name, editGroup.Description, editGroup.CanSubmit, editGroup.IsInternalUser, editGroup.Should_Be_Able_To_Edit_All_Items, editGroup.IsSystemAdmin, editGroup.IsPortalAdmin, false, true, false, editGroup.IsSobekDefault, editGroup.IsShibbolethDefault, editGroup.IsLdapDefault, RequestSpecificValues.Tracer, editGroup.Default_Visibility, editGroup.Permissions_Agreement_Id);
                    if (editGroup.UserGroupID < 0)
                    {
                        editGroup.UserGroupID = newid;
                    }

                    if (editGroup.UserGroupID > 0)
                    {
                        // Update the Item Type restriction (an empty list clears it, making this group unrestricted)
                        SobekCM_Database.Update_Item_Types_For_Group(editGroup.UserGroupID, editGroup.Restricted_Item_Types ?? new List<int>(), RequestSpecificValues.Tracer);
                    }

                    // Update the aggregationPermissions, if requested
                    if (editGroup.Aggregations_Count > 0)
                    {
                        if (!SobekCM_Database.Update_SobekCM_User_Group_Aggregations(editGroup.UserGroupID, editGroup.Aggregations, RequestSpecificValues.Tracer))
                        {
                            successful_save = false;
                        }
                    }

                    if (successful_save)
                    {
                        Context.SessionObject()["Edit_UserGroup_" + editGroup.UserGroupID] = null;

                        if (saveImmediately)
                        {
                            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = editGroup.UserGroupID.ToString();
                        }
                        else
                        {
                            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                        }

                        UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
                    }
                }
                else
                {
                    actionMessage = "User group's name must have a length greater than zero";

                    // Stash back to session so the entered (but unsaved) values survive the redisplay
                    Context.SessionObject()["Edit_UserGroup_" + editGroup.UserGroupID] = editGroup;
                }
            }
            else
            {
                // Tab switch, not a save -- stash to session and redirect to the new tab
                Context.SessionObject()["Edit_UserGroup_" + editGroup.UserGroupID] = editGroup;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = action;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode, Context);
            }
        }

        public override void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            set_current_page(RequestSpecificValues);

            Output.WriteLine("  <div class=\"SobekHomeText\">");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <b>Edit this user group's permissions and abilities</b>");
            Output.WriteLine("    <ul>");
            Output.WriteLine("      <li>Enter the permissions for this user group below and press the SAVE button when all your edits are complete.</li>");
            Output.WriteLine("      <li>For clarification of any terms on this form, <a href=\"" + UI_ApplicationCache_Gateway.Settings.System.Help_URL(RequestSpecificValues.Current_Mode.Base_URL) + "adminhelp/users\" target=\"ADMIN_USER_HELP\" >click here to view the help page</a>.</li>");
            Output.WriteLine("     </ul>");
            Output.WriteLine("  </div>");

            if (!String.IsNullOrEmpty(actionMessage))
            {
                Output.WriteLine("  <strong>" + actionMessage + "</strong>");
            }

            // Start the outer tab container
            Output.WriteLine("  <div id=\"tabContainer\" class=\"fulltabs\">");
            Output.WriteLine("  <div class=\"tabs\">");
            Output.WriteLine("    <ul>");

            string last_mode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Replace("b", "").Replace("c", "");

            char tab_letter = 'a';
            foreach (iUserGroupAdminTab tab in tabs)
            {
                if (tab == currentTab)
                {
                    Output.WriteLine($"      <li class=\"tabActiveHeader\"> {tab.TabName.ToUpper()} </li>");
                }
                else
                {
                    Output.WriteLine($"      <li onclick=\"return new_user_group_edit_page('{editGroup.UserGroupID}{tab_letter}');\"> {tab.TabName.ToUpper()} </li>");
                }
                tab_letter++;
            }

            Output.WriteLine("    </ul>");
            Output.WriteLine("  </div>");

            Output.WriteLine("    <div class=\"tabscontent\">");
            Output.WriteLine("    	<div class=\"sbkUgav_TabPage\" id=\"tabpage_1\">");

            // Add the buttons
            Output.WriteLine("  <div class=\"sbkSeav_ButtonsDiv\">");
            Output.WriteLine("    <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"return cancel_user_group_edits();return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            Output.WriteLine("    <button title=\"Save changes to this user group\" class=\"sbkAdm_RoundButton\" onclick=\"return save_user_group_edits();return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("  </div>");
            Output.WriteLine();

            Output.WriteLine("  <br /><br />");
            Output.WriteLine();

            // Add the tab html
            currentTab.RenderHtml(Output, editGroup, RequestSpecificValues, Tracer);

            // Add the buttons
            Output.WriteLine("  <div class=\"sbkSeav_ButtonsDiv\">");
            Output.WriteLine("    <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"return cancel_user_group_edits();return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            Output.WriteLine("    <button title=\"Save changes to this user group\" class=\"sbkAdm_RoundButton\" onclick=\"return save_user_group_edits();return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("  </div>");

            Output.WriteLine();
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_mode;

            Output.WriteLine("</div>");
            Output.WriteLine("</div>");
            Output.WriteLine("</div>");

            Output.WriteLine("<br />");
            Output.WriteLine("<br />");
        }
    }
}
