using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Resource_Object.Behaviors
{
    /// <summary> Class holds information about a single user and what access
    /// rights the user has </summary>
    public class User_Permissions
    {
        /// <summary> Primary key to this user in the database </summary>
        public int UserId { get; set; }

        /// <summary> UserName of the user that has this access (saves as UserID in database) </summary>
        public string UserName { get; set; }

        /// <summary> Flag indicates if this user is the owner </summary>
        public bool isOwner { get; set; }

        /// <summary> Flag indicates if this user can view the item </summary>
        public bool CanView { get; set; }

        /// <summary> Flag indicates if this user can edit the item metadata </summary>
        public bool CanEditMetadata { get; set; }

        /// <summary> Flag indicates if this user can edit the item behaviors </summary>
        public bool CanEditBehaviors { get; set; }

        /// <summary> Flag indicates if this user can edit can run the QC app on the item </summary>
        public bool CanPerformQc { get; set; }

        /// <summary> Flag indicates if this user can edit can manage the files for the item </summary>
        public bool CanUploadFiles { get; set; }

        /// <summary> Flag indicates if this user can change the visibility for this item </summary>
        public bool CanChangeVisibility { get; set; }

        /// <summary> Flag indicates if this user can delete the item </summary>
        public bool CanDelete { get; set; }

        /// <summary> Any additional custom permissions this user may have on the item </summary>
        public string CustomPermissions { get; set; }
    }
}