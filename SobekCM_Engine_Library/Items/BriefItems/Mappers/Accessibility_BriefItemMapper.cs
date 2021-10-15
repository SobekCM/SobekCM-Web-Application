#region Using directives

using System;
using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object;

#endregion

namespace SobekCM.Engine_Library.Items.BriefItems.Mappers
{
    /// <summary> Maps the accessibility info from the METS-based SobekCM_Item object
    /// to the BriefItem, used for most the public functions of the front-end </summary>
    public class Accessibility_BriefItemMapper : IBriefItemMapper
    {
        /// <summary> Map one or more data elements from the original METS-based object to the
        /// BriefItem object </summary>
        /// <param name="Original"> Original METS-based object </param>
        /// <param name="New"> New object to populate some data from the original </param>
        /// <returns> TRUE if successful, FALSE if an exception is encountered </returns>
        public bool MapToBriefItem(SobekCM_Item Original, BriefItemInfo New)
        {
            // Add the donor
            if (!String.IsNullOrWhiteSpace(Original.Bib_Info.Accessibility))
            {
                New.Add_Description("Accessibility", Original.Bib_Info.Accessibility);
            }

            return true;
        }
    }
}
