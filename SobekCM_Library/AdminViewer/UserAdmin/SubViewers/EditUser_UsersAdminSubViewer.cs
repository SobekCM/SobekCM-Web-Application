using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public class EditUser_UsersAdminSubViewer : abstractUsersAdminSubViewer
    {
        private bool tei_plugin_enabled;
        private List<iUserAdminTab> tabs;
        private iUserAdminTab currentTab;

        public EditUser_UsersAdminSubViewer()
        {
            // Build the tabs
            build_tabs();       
        }

        private void build_tabs()
        {
            // IN THE FUTURE, WE CAN DRIVE THIS OFF CONFIGURATION INFORMATION,
            // WHICH CAN BE UPDATED WITH PLUG-INS.. For now...

            tabs = new List<iUserAdminTab>();
            
            // Add basic first
            tabs.Add(new BasicInfoUserAdminTab());

            // If this is Open-NJ, add the instructor form
            if (UI_ApplicationCache_Gateway.URL_Portals.Default_Portal.Abbreviation.Equals("OpenNJ", StringComparison.OrdinalIgnoreCase))
                tabs.Add(new InstructorUserAdminTab());

            // Add the next standard ones
            tabs.Add(new GroupMembershipUserAdminTab());
            tabs.Add(new AggregationsUserAdminTab());

            // Determine if TEI is enabled
            if ((UI_ApplicationCache_Gateway.Configuration.Extensions != null) &&
                (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI") != null) &&
                (UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Extension("TEI").Enabled))
            {
                // Yes.. so add that tab page
                tabs.Add(new TeiUserAdminTab());
            }
        }

        private void set_current_page(RequestCache RequestSpecificValues)
        {
            // Determine which page you are on
            int page = 0;
            string remaining_submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Replace(editUser.UserID.ToString(), "").ToLower();
            if (remaining_submode.Length > 0)
            {
                page = ((int)remaining_submode[0]) - ((int)'a');
                if (page >= tabs.Count)
                    page = 0;
            }
            currentTab = tabs[page];
        }

        public override string Title => "Edit User";

        public override void HandlePostback(RequestCache RequestSpecificValues)
        {
            // Determine which page you are on
            set_current_page(RequestSpecificValues);

            // Get a reference to this form and get the action from hidden field
            NameValueCollection form = HttpContext.Current.Request.Form;
            string action = form["admin_user_save"];

            // If this is CANCEL, get rid of the currrent edit object in the session
            if (action == "cancel")
            {
                // Clear the RequestSpecificValues.Current_User from the sessions
                HttpContext.Current.Session["Edit_User_" + editUser.UserID] = null;

                // Redirect the RequestSpecificValues.Current_User
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Let the subviewer handle the postback
            bool saveImmediately = currentTab.HandlePostback(form, editUser, RequestSpecificValues);

            // Should this be saved to the database?
            if ((action == "save") || (saveImmediately))
            {
                bool successful_save = true;

                // Save this user
                SobekCM_Database.Save_User(editUser, String.Empty, RequestSpecificValues.Current_User.Authentication_Type, RequestSpecificValues.Tracer);

                // Update the basic user information
                SobekCM_Database.Update_SobekCM_User(editUser.UserID, editUser.Can_Submit, editUser.Is_Internal_User, editUser.Should_Be_Able_To_Edit_All_Items, editUser.Can_Delete_All, editUser.Is_User_Admin, editUser.Is_System_Admin, editUser.Is_Host_Admin, editUser.Is_Portal_Admin, editUser.Include_Tracking_In_Standard_Forms, editUser.Edit_Template_Code_Simple, editUser.Edit_Template_Code_Complex, true, true, true, RequestSpecificValues.Tracer);

                // Update projects, if necessary
                if (editUser.Default_Metadata_Sets.Count > 0)
                {
                    if (!SobekCM_Database.Update_SobekCM_User_DefaultMetadata(editUser.UserID, editUser.Default_Metadata_Sets, RequestSpecificValues.Tracer))
                    {
                        successful_save = false;
                    }
                }

                // Update templates, if necessary
                if (editUser.Templates_Count > 0)
                {
                    if (!SobekCM_Database.Update_SobekCM_User_Templates(editUser.UserID, editUser.Templates, RequestSpecificValues.Tracer))
                    {
                        successful_save = false;
                    }
                }

                // Save the aggregationPermissions linked to this user
                if (editUser.PermissionedAggregations_Count > 0)
                {
                    if (!SobekCM_Database.Update_SobekCM_User_Aggregations(editUser.UserID, editUser.PermissionedAggregations, RequestSpecificValues.Tracer))
                    {
                        successful_save = false;
                    }
                }

                // Save the user group links
                List<User_Group> userGroup = Engine_Database.Get_All_User_Groups(RequestSpecificValues.Tracer);
                foreach (Simple_User_Group_Info userGroup2 in editUser.User_Groups)
                {
                    SobekCM_Database.Link_User_To_User_Group(editUser.UserID, userGroup2.UserGroupID);
                }

                // Forward back to the list of users, if this was successful
                if (successful_save)
                {
                    // Clear the RequestSpecificValues.Current_User from the sessions
                    HttpContext.Current.Session["Edit_User_" + editUser.UserID] = null;

                    // If this was due to an immediate save, redirect back to editing that user
                    if ( saveImmediately )
                    {
                        RequestSpecificValues.Current_Mode.My_Sobek_SubMode = editUser.UserID.ToString();
                    }
                    else
                    {
                        RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
                    }

                    UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                }
            }
            else
            {
                // Save to the current session
                HttpContext.Current.Session["Edit_User_" + editUser.UserID] = editUser;
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = action;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
            }
        }

        
        public override void Write_SubView(TextWriter Output, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            // Determine which page you are on
            set_current_page(RequestSpecificValues);

            // Start writing the HTML to the output stream
            Output.WriteLine("  <div class=\"SobekHomeText\">");
            Output.WriteLine("  <br />");
            Output.WriteLine("  <b>Edit this users's permissions, abilities, and basic information</b>");
            Output.WriteLine("    <ul>");
            Output.WriteLine("      <li>Enter the permissions for this user below and press the SAVE button when all your edits are complete.</li>");
            Output.WriteLine("      <li>For clarification of any terms on this form, <a href=\"" + UI_ApplicationCache_Gateway.Settings.System.Help_URL(RequestSpecificValues.Current_Mode.Base_URL) + "adminhelp/users\" target=\"ADMIN_USER_HELP\" >click here to view the help page</a>.</li>");
            Output.WriteLine("     </ul>");
            Output.WriteLine("  </div>");

            // Start the outer tab containe
            Output.WriteLine("  <div id=\"tabContainer\" class=\"fulltabs\">");
            Output.WriteLine("  <div class=\"tabs\">");
            Output.WriteLine("    <ul>");

            string last_mode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Replace("b", "").Replace("c", "");

            // Write the tabs 
            char tab_letter = 'a';
            foreach( iUserAdminTab tab in tabs )
            {
                if ( tab == currentTab )
                {
                    Output.WriteLine($"      <li class=\"tabActiveHeader\"> {tab.TabName.ToUpper()} </li>");
                }
                else
                {
                    Output.WriteLine($"      <li onclick=\"return new_user_edit_page('{editUser.UserID}{tab_letter}');\"> {tab.TabName.ToUpper()} </li>");
                }
                tab_letter++;
            }

            Output.WriteLine("    </ul>");
            Output.WriteLine("  </div>");

            Output.WriteLine("    <div class=\"tabscontent\">");
            Output.WriteLine("    	<div class=\"sbkUgav_TabPage\" id=\"tabpage_1\">");

            // Add the buttons
            Output.WriteLine("  <div class=\"sbkSeav_ButtonsDiv\">");
            Output.WriteLine("    <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"return cancel_user_edits();return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            Output.WriteLine("    <button title=\"Save changes to this user group\" class=\"sbkAdm_RoundButton\" onclick=\"return save_user_edits();return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("  </div>");
            Output.WriteLine();

            Output.WriteLine("  <br /><br />");
            Output.WriteLine();

            // Add the tab html
            currentTab.RenderHtml(Output, editUser, RequestSpecificValues, Tracer);

            // Add the buttons
            Output.WriteLine("  <div class=\"sbkSeav_ButtonsDiv\">");
            Output.WriteLine("    <button title=\"Do not apply changes\" class=\"sbkAdm_RoundButton\" onclick=\"return cancel_user_edits();return false;\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkAdm_RoundButton_LeftImg\" alt=\"\" /> CANCEL</button> &nbsp; &nbsp; ");
            Output.WriteLine("    <button title=\"Save changes to this user\" class=\"sbkAdm_RoundButton\" onclick=\"return save_user_edits();return false;\">SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkAdm_RoundButton_RightImg\" alt=\"\" /></button>");
            Output.WriteLine("  </div>");

            Output.WriteLine();
            RequestSpecificValues.Current_Mode.My_Sobek_SubMode = last_mode;

            Output.WriteLine("</div>");
            Output.WriteLine("</div>");

            Output.WriteLine("<br />");
            Output.WriteLine("<br />");
        }
    }
}
