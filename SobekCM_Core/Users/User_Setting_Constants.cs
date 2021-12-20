using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Core.Users
{
    /// <summary> Class holds constant string values used for the keys and some
    /// controlled values in the user settings </summary>
    public static class User_Setting_Constants
    {
        /// <summary> Constant string for user settings controlly showing the QC options </summary>
        /// <value>Item_HtemlSubwriter.Show_QualityControl</value>
        public const string ItemViewer_ShowQc = "Item_HtemlSubwriter.Show_QualityControl";

        /// <summary> Constant string for user settings controlly showing the behavior screen </summary>
        /// <value>Item_HtemlSubwriter.Show_Behaviors</value>
        public const string ItemViewer_ShowBehaviors = "Item_HtemlSubwriter.Show_Behaviors";

        /// <summary> Constant string for user settings controlly access to screen to change permissions (i.e., private, public, etc..) </summary>
        /// <value>Item_HtemlSubwriter.Allow_Permision_Changes</value>
        public const string ItemViewer_AllowPermissionChanges = "Item_HtemlSubwriter.Allow_Permision_Changes";
    }
}
