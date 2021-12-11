using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
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
    class GroupMembershipUserAdminTab : iUserAdminTab
    {
        public string TabName => "Group Membership";

        public bool HandlePostback(NameValueCollection form, User_Object editUser, RequestCache RequestSpecificValues)
        {
            // Check the RequestSpecificValues.Current_User groups for update
            bool update_user_groups = false;
            List<User_Group> userGroup = Engine_Database.Get_All_User_Groups(RequestSpecificValues.Tracer);
            List<Simple_User_Group_Info> newGroups = new List<Simple_User_Group_Info>();
            foreach (User_Group thisRow in userGroup)
            {
                if (form["group_" + thisRow.UserGroupID] != null)
                {
                    newGroups.Add(new Simple_User_Group_Info(thisRow.UserGroupID, thisRow.Name));
                }
            }

            // Should we add the new RequestSpecificValues.Current_User groups?  Did it change?
            if (newGroups.Count != editUser.User_Groups.Count)
            {
                update_user_groups = true;
            }
            else
            {
                foreach (Simple_User_Group_Info thisGroup in newGroups)
                {
                    bool found_group = false;
                    foreach (Simple_User_Group_Info existingGroup in editUser.User_Groups)
                    {
                        if (existingGroup.UserGroupID == thisGroup.UserGroupID)
                        {
                            found_group = true;
                            break;
                        }
                    }
                    if (!found_group)
                    {
                        update_user_groups = true;
                        break;
                    }
                }
            }
            if (update_user_groups)
            {
                editUser.Clear_UserGroup_Membership();
                foreach (Simple_User_Group_Info thisUserGroup in newGroups)
                    editUser.Add_User_Group(thisUserGroup.UserGroupID, thisUserGroup.Name);
            }

            // No immediate save necesary
            return false;
        }

        public void RenderHtml(TextWriter Output, User_Object editUser, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <span class=\"SobekEditItemSectionTitle_first\"> &nbsp; User Group Membership</span>");
            Output.WriteLine("  <blockquote>");

            List<User_Group> userGroup = Engine_Database.Get_All_User_Groups(Tracer);
            if ((userGroup == null) || (userGroup.Count == 0))
            {
                Output.WriteLine("<br />");
                Output.WriteLine("<b>No user groups exist within this library instance</b>");
                Output.WriteLine("<br />");
            }
            else
            {
                Output.WriteLine("<table border=\"0px\" cellspacing=\"0px\" class=\"statsWhiteTable\">");
                Output.WriteLine("  <tr align=\"left\" bgcolor=\"#0022a7\" >");
                Output.WriteLine("    <td colspan=\"3\"><span style=\"color: White\"><b>USER GROUPS</b></span></td>");
                Output.WriteLine("   </tr>");
                //Output.WriteLine("  <tr align=\"left\" bgcolor=\"#7d90d5\" >");
                //Output.WriteLine("    <td width=\"100px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Is this RequestSpecificValues.Current_User a member of this group?\">IS MEMBER</acronym></span></td>");
                //Output.WriteLine("    <td width=\"120px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Name of this RequestSpecificValues.Current_User group\">GROUP NAME</acronym></span></td>");
                //Output.WriteLine("    <td width=\"300px\" align=\"left\"><span style=\"color: White\"><acronym title=\"Description of this RequestSpecificValues.Current_User group\">GROUP DESCRIPTION</acronym></span></td>");
                //Output.WriteLine("   </tr>");

                // Get the dictionary of user groups in this user
                Dictionary<int, Simple_User_Group_Info> editUserGroups = new Dictionary<int, Simple_User_Group_Info>();
                foreach (Simple_User_Group_Info editUserGroup in editUser.User_Groups)
                    editUserGroups[editUserGroup.UserGroupID] = editUserGroup;

                foreach (User_Group thisRow in userGroup)
                {
                    Output.WriteLine("  <tr align=\"left\" >");

                    Output.Write("    <td width=\"50px\" ><input type=\"checkbox\" name=\"group_" + thisRow.UserGroupID + "\" id=\"group_" + thisRow.UserGroupID + "\" ");
                    if (editUserGroups.ContainsKey(thisRow.UserGroupID))
                        Output.Write(" checked=\"checked\"");
                    Output.WriteLine("/></td>");
                    Output.WriteLine("    <td width=\"150px\" >" + thisRow.Name + "</td>");
                    Output.WriteLine("    <td width=\"400px\">" + thisRow.Description + "</td>");
                    Output.WriteLine("  </tr>");
                    Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"3\"></td></tr>");
                }

                Output.WriteLine("</table>");
            }

            Output.WriteLine("  </blockquote>");
        }
    }
}
