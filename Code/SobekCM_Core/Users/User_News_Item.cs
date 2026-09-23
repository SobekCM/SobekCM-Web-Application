using System;
using System.Collections.Generic;
using System.Linq;

namespace SobekCM.Core.Users
{
    /// <summary> A single news message, shown in a banner at the top of every page to the people it
    /// targets, until each person closes it </summary>
    /// <remarks> A message targets every user matching ANY of its audience flags or user groups.  Audience
    /// flags are used (rather than lists of users) so an upgrade script can add release news for the
    /// administrators of any instance. </remarks>
    public class User_News_Item
    {
        /// <summary> Primary key for this news item in the database </summary>
        public int NewsID { get; set; }

        /// <summary> Title shown in bold at the top of the banner </summary>
        public string Title { get; set; }

        /// <summary> Body of the message, as HTML, so it can include links </summary>
        public string Body { get; set; }

        /// <summary> Shown to everyone, including visitors who are not logged on (who close it with a cookie) </summary>
        public bool For_Everyone { get; set; }

        /// <summary> Shown to every logged-on user </summary>
        public bool For_All_Users { get; set; }

        /// <summary> Shown to the administrators: system, portal, user and news administrators, but never
        /// host administrators (see <see cref="Is_Admin_Audience"/>) </summary>
        public bool For_Admins { get; set; }

        /// <summary> Shown to the collection managers: anyone who is a manager or administrator of at least one collection </summary>
        public bool For_Collection_Managers { get; set; }

        /// <summary> User groups whose members are shown this message </summary>
        public List<int> User_Group_IDs { get; set; }

        /// <summary> First day this message is shown </summary>
        public DateTime Start_Date { get; set; }

        /// <summary> Last day this message is shown, or NULL to show it until each user closes it </summary>
        public DateTime? End_Date { get; set; }

        /// <summary> Inactive messages are never shown, but are kept for the admin screen </summary>
        public bool Is_Active { get; set; }

        /// <summary> Date this message was added (only loaded for the admin screen) </summary>
        public DateTime? Date_Created { get; set; }

        /// <summary> Who added this message (only loaded for the admin screen) </summary>
        public string Created_By { get; set; }

        /// <summary> Date this message was last edited (only loaded for the admin screen) </summary>
        public DateTime? Date_Modified { get; set; }

        /// <summary> Number of users who have closed this message (only loaded for the admin screen) </summary>
        public int Dismissed_Count { get; set; }

        /// <summary> Constructor for a new instance of the User_News_Item class </summary>
        public User_News_Item()
        {
            Title = String.Empty;
            Body = String.Empty;
            Created_By = String.Empty;
            User_Group_IDs = new List<int>();
            Start_Date = DateTime.Today;
            Is_Active = true;
        }

        /// <summary> Flag indicates this message does not target anyone yet </summary>
        public bool Has_No_Audience
        {
            get
            {
                return (!For_Everyone) && (!For_All_Users) && (!For_Admins) && (!For_Collection_Managers) && (User_Group_IDs.Count == 0);
            }
        }

        /// <summary> Checks to see if this message targets the given user </summary>
        /// <param name="User"> Logged-on user </param>
        /// <returns> TRUE if any of this message's role flags or user groups match the user </returns>
        /// <remarks> Does not check the display dates, active flag or whether the user already closed
        /// it, since the database only returns pending news in the first place </remarks>
        public bool Applies_To(User_Object User)
        {
            if (User == null)
                return false;

            if ((For_Everyone) || (For_All_Users))
                return true;
            if ((For_Admins) && (Is_Admin_Audience(User)))
                return true;
            if ((For_Collection_Managers) && (User.PermissionedAggregations != null) && (User.PermissionedAggregations.Any(Aggr => Aggr.IsAdmin || Aggr.IsCurator)))
                return true;
            if ((User_Group_IDs.Count > 0) && (User.User_Groups != null) && (User.User_Groups.Any(Group => User_Group_IDs.Contains(Group.UserGroupID))))
                return true;

            return false;
        }

        /// <summary> Checks to see if a user receives the news sent to the administrators </summary>
        /// <param name="User"> Logged-on user </param>
        /// <returns> TRUE for system, portal, user and news administrators, unless they are also a host administrator </returns>
        /// <remarks> Host administrators are left out on purpose.  They host many instances, and would otherwise get
        /// every instance's administrator news, including the release news they wrote themselves. </remarks>
        public static bool Is_Admin_Audience(User_Object User)
        {
            if ((User == null) || (User.Is_Host_Admin))
                return false;

            return (User.Is_System_Admin) || (User.Is_Portal_Admin) || (User.Is_User_Admin) || (User.Is_News_Admin);
        }
    }
}
