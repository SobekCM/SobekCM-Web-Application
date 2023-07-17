using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Engine_Library.Items.BriefItems.Mappers
{
    /// <summary> Maps the user group permissions from the METS-based SobekCM_Item object
    /// to the BriefItem, used for most the public functions of the front-end </summary>
    public class Permissions_BriefItemMapper : IBriefItemMapper
    {
        /// <summary> Map one or more data elements from the original METS-based object to the
        /// BriefItem object </summary>
        /// <param name="Original"> Original METS-based object </param>
        /// <param name="New"> New object to populate some data from the original </param>
        /// <returns> TRUE if successful, FALSE if an exception is encountered </returns>
        public bool MapToBriefItem(SobekCM_Item Original, BriefItemInfo New)
        {
            // Add the user group permissions
            if (Original.Behaviors.User_Group_Access_Count > 0)
            {
                foreach (var permission in Original.Behaviors.User_Group_Access)
                {
                    New.Behaviors.Add_Restriction(permission.UserGroupId, permission.GroupName, permission.CanView);
                }
            }

            New.Behaviors.RestrictionMessage = Original.Behaviors.RestrictionMessage;

            // If there IS a restriction message, add that to the desription
            if ( !String.IsNullOrEmpty(Original.Behaviors.RestrictionMessage))
            {
                New.Add_Description("Access Restrictions", Original.Behaviors.RestrictionMessage);
            }

            return true;
        }
    }
}
