using System;
using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object;

namespace SobekCM.Engine_Library.Items.BriefItems.Mappers
{
    /// <summary> Maps the other URL from the METS-based SobekCM_Item object
    /// to the BriefItem, used for most the public functions of the front-end </summary>
    public class Other_URL_BriefItemMapper : IBriefItemMapper
    {
        /// <summary> Map one or more data elements from the original METS-based object to the
        /// BriefItem object </summary>
        /// <param name="Original"> Original METS-based object </param>
        /// <param name="New"> New object to populate some data from the original </param>
        /// <returns> TRUE if successful, FALSE if an exception is encountered </returns>
        public bool MapToBriefItem(SobekCM_Item Original, BriefItemInfo New)
        {
            // If not URL, jsut return
            if (String.IsNullOrEmpty(Original.Bib_Info.Location.Other_URL))
                return true;

            string mainEntry = Original.Bib_Info.Location.Other_URL;
            if (!String.IsNullOrEmpty(Original.Bib_Info.Location.Other_URL_Note))
                mainEntry = Original.Bib_Info.Location.Other_URL_Note;

            // Add this other URL
            BriefItem_DescTermValue otherUrlObj = New.Add_Description("Other Item", mainEntry);
            otherUrlObj.Add_URI(Original.Bib_Info.Location.Other_URL);

            // Add display label, if one exists
            if (!String.IsNullOrEmpty(Original.Bib_Info.Location.Other_URL_Display_Label))
                otherUrlObj.SubTerm = Original.Bib_Info.Location.Other_URL_Display_Label;

            return true;
        }
    }
}
