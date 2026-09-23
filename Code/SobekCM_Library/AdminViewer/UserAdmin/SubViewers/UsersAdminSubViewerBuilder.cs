using Microsoft.AspNetCore.Http;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.UI;
using System;
using System.Linq;



namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public static class UsersAdminSubViewerBuilder
    {
        public static iUsersAdminSubViewer GetSubViewer(RequestCache RequestSpecificValues, HttpContext Context)
        {
            // Get the user to edit, if there was a user id in the submode
            User_Object User = get_user(RequestSpecificValues, Context);

            iUsersAdminSubViewer subviewer;

            if (User != null)
            {
                if (RequestSpecificValues.Current_Mode.My_Sobek_SubMode.IndexOf("v") > 0)
                    subviewer = new ViewUser_UsersAdminSubViewer();
                else
                    subviewer = new EditUser_UsersAdminSubViewer();

                subviewer.EditUser = User;
            }
            else
            {
                subviewer = new UserList_UsersAdminSubViewer();
                RequestSpecificValues.Current_Mode.My_Sobek_SubMode = String.Empty;
            }

            return subviewer;
        }

        /// <summary> Get the user to edit, if there was a user id in the submode </summary>
        /// <param name="RequestSpecificValues"></param>
        /// <returns> User, or NULL if there was no match </returns>
        private static User_Object get_user(RequestCache RequestSpecificValues, HttpContext Context)
        {
            if (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.My_Sobek_SubMode)) return null;

            // Strip out characters (used by subviewers to specify tab, other things potentially)
            var only_numbers = new string(RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Where(c => char.IsDigit(c)).ToArray()).Trim();
            if ((String.IsNullOrEmpty(only_numbers)) || (!int.TryParse(only_numbers, out int edit_userid)))
                return null;

            User_Object editUser;

            // Check this admin's session for this RequestSpecificValues.Current_User object
            Object sessionEditUser = Context.SessionObject()["Edit_User_" + edit_userid];
            if (sessionEditUser != null)
                editUser = (User_Object)sessionEditUser;
            else
            {
                // Pull from the database (including a deactivated user, so it can be viewed and reactivated)
                editUser = Engine_Database.Get_User(edit_userid, true, RequestSpecificValues.Tracer);
                if (editUser != null)
                    editUser.Should_Be_Able_To_Edit_All_Items = editUser.Editable_Regular_Expressions.Any(ThisRegularExpression => ThisRegularExpression == "[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}");
            }

            if (editUser == null)
                return null;

            // A system user can only be viewed or edited by the top-level admin (Host Administrator if hosted,
            // otherwise System Administrator) - same rule the users admin list uses to hide them. UserIDs are
            // sequential and easy to guess, so a lower admin who guesses one is treated exactly as if that
            // user did not exist, whether the URL asks to view or edit.
            if (editUser.Is_System_User)
            {
                bool isTopLevelAdmin = ((!UI_ApplicationCache_Gateway.Settings.Servers.isHosted) && (RequestSpecificValues.Current_User.Is_System_Admin)) || (RequestSpecificValues.Current_User.Is_Host_Admin);
                if (!isTopLevelAdmin)
                    return null;
            }

            return editUser;
        }
    }
}
