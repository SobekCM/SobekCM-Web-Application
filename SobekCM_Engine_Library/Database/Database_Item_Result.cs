#region Using directives

using System;
using SobekCM.Core.Results;

#endregion

namespace SobekCM.Engine_Library.Database
{
	/// <summary> Class contains all the display and identification information about a single item 
	/// within a title in a search result set from a database search  </summary>
	[Serializable]
	public class Database_Item_Result : iSearch_Item_Result
	{
		/// <summary> Gets the primary key for this item, from the database </summary>
		public int ItemID { get; set; }

		#region Basic properties the implement the iSearch_Item_Result interface

		/// <summary> Volume identifier (VID) for this item within a title within a collection of results </summary>
		public string VID 
		{ 
			get; set; 
		}

		/// <summary> Title for this item within a title within a collection of results </summary>
		public string Title { get; set; }

		/// <summary> IP restriction mask for this item within a title within a collection of results </summary>
		public short IP_Restriction_Mask { get; set; }

		/// <summary> Thumbnail image for this item within a title within a collection of results </summary>
		public string MainThumbnail { get; set; }

		/// <summary> Index of the first serial hierarchy level for this item within a title within a collection of results </summary>
		public short Level1_Index { get; set; }

		/// <summary> Text of the first serial hierarchy level for this item within a title within a collection of results </summary>
		public string Level1_Text { get; set; }

		/// <summary> Index of the second serial hierarchy level for this item within a title within a collection of results </summary>
		public short Level2_Index { get; set; }

		/// <summary> Text of the second serial hierarchy level for this item within a title within a collection of results </summary>
		public string Level2_Text { get; set; }

		/// <summary> Index of the third serial hierarchy level for this item within a title within a collection of results </summary>
		public short Level3_Index { get; set; }

		/// <summary> Text of the third serial hierarchy level for this item within a title within a collection of results </summary>
		public string Level3_Text { get; set; }

		/// <summary> Publication date for this item within a title within a collection of results </summary>
		public string PubDate { get; set; }

		/// <summary> Number of pages within this item within a title within a collection of results </summary>
		public int PageCount { get; set; }

		/// <summary> External URL for this item within a title within a collection of results </summary>
		public string Link { get; set; }

		/// <summary> Spatial coverage as KML for this item within a title result for map display </summary>
		public string Spatial_KML { get; set; }

		/// <summary> COinS OpenURL format of citation for citation sharing </summary>
		public string COinS_OpenURL { get; set; }
        
        /// <summary> Flag indicates if this is dark, private, etc.. </summary>
        public string AccessType
        {
            get { return String.Empty; }
        }

        /// <summary> List of groups (by id) that have access to this item </summary>
        public string Group_Restrictions { get; set; }

        /// <summary> Restriction message (to be displayed if restricted) </summary>
        public string RestrictedMsg { get; set; }

        #endregion
    }
}
