using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace SobekCM.Library.AdminViewer.UserAdmin.UserAdminTabs
{
    class InstructorUserAdminTab : iUserAdminTab
    {
        public string TabName => "Instructor";

        public bool HandlePostback(NameValueCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            // Get the curret action
            string action = form["admin_user_save"];

            if (action == "set_instructor")
            {
                List<User_Group> userGroup2 = Engine_Database.Get_All_User_Groups(RequestSpecificValues.Tracer);
                if ((userGroup2 != null) || (userGroup2.Count > 0))
                {
                    int instructor_id = -1;
                    foreach (var group in userGroup2)
                    {
                        if (group.Name.Equals("Instructors", StringComparison.OrdinalIgnoreCase))
                        {
                            instructor_id = group.UserGroupID;
                            break;
                        }
                    }

                    if (instructor_id > 0)
                    {
                        editUser.Add_User_Group(instructor_id, "Instructors");

                        // Ensure the upload template is selected
                        bool instructor_upload_template_selected = false;
                        foreach (string template in editUser.Templates)
                        {
                            if (template.ToUpper().IndexOf("INSTRUCT") >= 0)
                            {
                                instructor_upload_template_selected = true;
                                break;
                            }
                        }
                        if (!instructor_upload_template_selected)
                        {
                            editUser.Add_Template("INSTRUCT", false);
                        }

                        // Set the edit templates
                        editUser.Edit_Template_Code_Simple = "oer-edit";
                        editUser.Edit_Template_Code_Complex = "oer-edit";

                        // Set the user settings to simplify the UI
                        editUser.Add_Setting(User_Setting_Constants.ItemViewer_ShowBehaviors, "false");
                        editUser.Add_Setting(User_Setting_Constants.ItemViewer_ShowQc, "false");
                        editUser.Add_Setting(User_Setting_Constants.ItemViewer_AllowPermissionChanges, "false");

                        // We want this to save immediately
                        return true;
                    }
                }
            }
            
            // No immediate save necesary, since the button wasn't pushed
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; Instructor Status</span>");
            Output.WriteLine("  <blockquote>");

            List<User_Group> userGroup2 = Engine_Database.Get_All_User_Groups(Tracer);
            if ((userGroup2 == null) || (userGroup2.Count == 0))
            {
                Output.WriteLine("<br />");
                Output.WriteLine("<b>No user groups exist within this library instance</b>");
                Output.WriteLine("<br />");
            }
            else
            {
                int instructor_id = -1;
                foreach (var group in userGroup2)
                {
                    if (group.Name.Equals("Instructors", StringComparison.OrdinalIgnoreCase))
                    {
                        instructor_id = group.UserGroupID;
                        break;
                    }
                }

                if (instructor_id == -1)
                {
                    Output.WriteLine("<br />");
                    Output.WriteLine("<b>Create an 'Instructors' user group to use this feature</b>");
                    Output.WriteLine("<br />");
                }
                else
                {
                    // Is this user an instructor?
                    bool instructor = false;
                    foreach (Simple_User_Group_Info editUserGroup in editUser.User_Groups)
                    {
                        if (editUserGroup.UserGroupID == instructor_id)
                            instructor = true;
                    }

                    if (instructor)
                    {
                        Output.WriteLine("<br />");
                        Output.WriteLine("<b>This user is an instructor already</b>");
                        Output.WriteLine("<br />");
                    }
                    else
                    {
                        Output.WriteLine("<br />");
                        Output.WriteLine("<b>This user is NOT an instructor</b>");
                        Output.WriteLine("<br /><br />");

                        // Add the button
                        Output.WriteLine("    <button title=\"Make this user an instructor\" class=\"sbkAdm_RoundButton\" onclick=\"return user_edits_special_action('set_instructor');return false;\">MAKE THIS USER AN INSTRUCTOR</button>");
                    }

                }
            }


            Output.WriteLine("  </blockquote>");
        }
    }
}
