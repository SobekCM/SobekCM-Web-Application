using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using System;
using System.Linq;

namespace SobekCM.Library.AdminViewer.UserGroupAdmin.SubViewers
{
    public static class UserGroupAdminSubViewerBuilder
    {
        /// <summary> Builds the View or Edit subviewer for a specific user group </summary>
        /// <returns> The built subviewer, or NULL if the submode named no valid (or new) user group --
        /// the caller should redirect back to the user/group list in that case, matching the former
        /// monolithic <c>User_Group_AdminViewer</c>'s behavior (this screen never shows a list itself,
        /// that already lives in the Users admin screen) </returns>
        public static iUserGroupAdminSubViewer GetSubViewer(RequestCache RequestSpecificValues, HttpContext Context)
        {
            User_Group group = get_group(RequestSpecificValues, Context);

            if (group == null)
                return null;

            iUserGroupAdminSubViewer subviewer;
            if ((group.UserGroupID > 0) && (RequestSpecificValues.Current_Mode.My_Sobek_SubMode.IndexOf("v") > 0))
                subviewer = new ViewUserGroup_UserGroupAdminSubViewer();
            else
                subviewer = new EditUserGroup_UserGroupAdminSubViewer();

            subviewer.EditGroup = group;
            return subviewer;
        }

        /// <summary> Get the user group to edit/view, from the submode -- "new" for a brand new group,
        /// otherwise the group's integer ID (optionally followed by tab/view letters) </summary>
        private static User_Group get_group(RequestCache RequestSpecificValues, HttpContext Context)
        {
            string submode = RequestSpecificValues.Current_Mode.My_Sobek_SubMode;
            if (String.IsNullOrEmpty(submode)) return null;

            if (submode == "new")
            {
                object sessionEditGroup = Context.SessionObject()["Edit_UserGroup_-1"];
                return (sessionEditGroup != null) ? (User_Group)sessionEditGroup : new User_Group(String.Empty, String.Empty, -1);
            }

            // Strip out characters used by subviewers to specify tab/view, leaving only the group's numeric ID
            var only_numbers = new string(submode.Where(char.IsDigit).ToArray()).Trim();
            if ((String.IsNullOrEmpty(only_numbers)) || (!int.TryParse(only_numbers, out int edit_usergroupid)))
                return null;

            object sessionEditUser = Context.SessionObject()["Edit_UserGroup_" + edit_usergroupid];
            if (sessionEditUser != null)
                return (User_Group)sessionEditUser;

            User_Group editGroup = SobekCM_Database.Get_User_Group(edit_usergroupid, RequestSpecificValues.Tracer);
            if (editGroup != null)
            {
                editGroup.Should_Be_Able_To_Edit_All_Items = (editGroup.Editable_Regular_Expressions != null) && editGroup.Editable_Regular_Expressions.Any(ThisRegularExpression => ThisRegularExpression == "[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}");

                foreach (int typeId in SobekCM_Database.Get_Item_Types_For_Group(edit_usergroupid, RequestSpecificValues.Tracer))
                    editGroup.Add_Restricted_Item_Type(typeId);
            }
            return editGroup;
        }
    }
}
