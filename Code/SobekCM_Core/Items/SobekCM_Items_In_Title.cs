#region Using directives

using System;
using System.Data;

#endregion

namespace SobekCM.Core.Items
{
    /// <summary> Wrapper class that holds the list of all items for a particular volume </summary>
    public class SobekCM_Items_In_Title
    {
        private readonly DataTable innerData;

        /// <summary> Constructor for a new instance of the SobekCM_Items_In_Title class </summary>
        public SobekCM_Items_In_Title()
        {
            innerData = new DataTable();
            innerData.Columns.Add("ItemID", typeof(Int32));
            innerData.Columns.Add("Title");
            innerData.Columns.Add("Level1_Text");
            innerData.Columns.Add("Level1_Index", typeof(Int32));
            innerData.Columns.Add("Level2_Text");
            innerData.Columns.Add("Level2_Index", typeof(Int32));
            innerData.Columns.Add("Level3_Text");
            innerData.Columns.Add("Level3_Index", typeof(Int32));
            innerData.Columns.Add("Level4_Text");
            innerData.Columns.Add("Level4_Index", typeof(Int32));
            innerData.Columns.Add("Level5_Text");
            innerData.Columns.Add("Level5_Index", typeof(Int32));
            innerData.Columns.Add("MainThumbnail");
            innerData.Columns.Add("VID");
            innerData.Columns.Add("IP_Restriction_Mask", typeof(Int16));
            innerData.Columns.Add("Dark", typeof(Boolean));
        }

        /// <summary> Constructor for a new instance of the SobekCM_Items_In_Title class </summary>
        /// <param name="Item_Information"> Raw data from the search or browse with item and title information </param>
        public SobekCM_Items_In_Title(DataTable Item_Information)
        {
            innerData = Item_Information;
        }

        /// <summary> Gets the inner table with all of the information for all items within this title </summary>
        public DataTable Item_Table => innerData;
    }
}
