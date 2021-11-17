using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Resource_Object.Behaviors
{
    /// <summary> Class holds information about a user group and what access
    /// rights the user group has </summary>
    public class User_Group_Permissions
    {
        /// <summary> User group id for this user group </summary>
        public int UserGroupId { get; set; }

        /// <summary> GroupName of the user group that has this access (saves as User GroupID in database) </summary>
        public string GroupName { get; set; }

        /// <summary> Flag indicates if this user group is the owner </summary>
        public bool isOwner { get; set; }

        /// <summary> Flag indicates if this user group can view the item </summary>
        public bool CanView { get; set; }

        /// <summary> Flag indicates if this user group can edit the item metadata </summary>
        public bool CanEditMetadata { get; set; }

        /// <summary> Flag indicates if this user group can edit the item behaviors </summary>
        public bool CanEditBehaviors { get; set; }

        /// <summary> Flag indicates if this user group can edit can run the QC app on the item </summary>
        public bool CanPerformQc { get; set; }

        /// <summary> Flag indicates if this user group can edit can manage the files for the item </summary>
        public bool CanUploadFiles { get; set; }

        /// <summary> Flag indicates if this user group can change the visibility for this item </summary>
        public bool CanChangeVisibility { get; set; }

        /// <summary> Flag indicates if this user group can delete the item </summary>
        public bool CanDelete { get; set; }

        /// <summary> Any additional custom permissions this user group may have on the item </summary>
        public string CustomPermissions { get; set; }
    }
}
