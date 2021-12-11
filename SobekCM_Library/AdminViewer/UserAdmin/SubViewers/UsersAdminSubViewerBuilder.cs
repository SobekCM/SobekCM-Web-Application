using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using System;
using System.Linq;
using System.Web;

namespace SobekCM.Library.AdminViewer.UserAdmin.SubViewers
{
    public static class UsersAdminSubViewerBuilder
    {
        public static iUsersAdminSubViewer GetSubViewer(RequestCache RequestSpecificValues)
        {
            // Get the user to edit, if there was a user id in the submode
            User_Object User = get_user(RequestSpecificValues);

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
        private static User_Object get_user(RequestCache RequestSpecificValues)
        {
            if (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.My_Sobek_SubMode)) return null;

            // Strip out characters (used by subviewers to specify tab, other things potentially)
            string only_numbers = new string(RequestSpecificValues.Current_Mode.My_Sobek_SubMode.Where(c => char.IsDigit(c)).ToArray()).Trim();
            if ((String.IsNullOrEmpty(only_numbers)) || (!int.TryParse(only_numbers, out int edit_userid)))
                return null;

            // Check this admin's session for this RequestSpecificValues.Current_User object
            Object sessionEditUser = HttpContext.Current.Session["Edit_User_" + edit_userid];
            if (sessionEditUser != null)
                return (User_Object)sessionEditUser;

            // Pull from the database and return
            User_Object editUser = Engine_Database.Get_User(edit_userid, RequestSpecificValues.Tracer);
            if (editUser != null)
                editUser.Should_Be_Able_To_Edit_All_Items = editUser.Editable_Regular_Expressions.Any(ThisRegularExpression => ThisRegularExpression == "[A-Z]{2}[A-Z|0-9]{4}[0-9]{4}");
            return editUser;
        }
    }
}
