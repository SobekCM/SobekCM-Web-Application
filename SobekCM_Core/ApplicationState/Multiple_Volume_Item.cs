#region Using directives

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

#endregion

namespace SobekCM.Core.ApplicationState
{
    /// <summary> Enumeration tells the type of group thumbail included for this title </summary>
    public enum Group_Thumbnail_Enum : byte
    {
        /// <summary> No group thumbnail is set for this bibid </summary>
        No_Group_Thumbnail,

        /// <summary> Use the standard named JPEG group thumbnail </summary>
        JPEG_Thumbnail,

        /// <summary> Use the standard named PNG group thumbnail </summary>
        PNG_Thumbnail,

        /// <summary> Use the standard named GIF group thumbnail </summary>
        GIF_Thumbnail,

        /// <summary> Use the custom group thumbnail indicated </summary>
        Custom_Thumbnail
    }

	/// <summary> Stores information about a title which has multiple volumes or is always represented as multiple volumes ( i.e. Newspapers and Serials) </summary>
    [DataContract]
	public class Multiple_Volume_Item
	{
        /// <summary> Actual byte which contains the title-level flags, mostly used internally </summary>
        public byte FlagByte { get; set; }

        /// <summary> Group title to be displayed for this multi-volume title in results returning more than one item </summary>
        public string GroupTitle { get; set; }

        /// <summary> Custom group thumbnail file </summary>
        public string CustomThumbnail { get; set; }

        /// <summary> Does this bibid have group metadata? </summary>
        /// <remarks> Use bitwise operator to check the first, ones bit of the flag </remarks>
        public bool HasGroupMetadata
        {
            get
            {
                // Check the bitwise operator (ones bit)
                return ((FlagByte & 0x1) == 1);
            }
        }

        /// <summary> Does this bibid have a group title? </summary>
        /// <remarks> Use bitwise operator to check the two, fours, and eight bits of the flag </remarks>
        public Group_Thumbnail_Enum GroupThumbnailType
        {
            get
            {
                int result = FlagByte & 0xE;
                switch( result )
                {
                    case 0:
                        return Group_Thumbnail_Enum.No_Group_Thumbnail;

                    case 2:
                        return Group_Thumbnail_Enum.JPEG_Thumbnail;

                    case 4:
                        return Group_Thumbnail_Enum.GIF_Thumbnail;

                    case 8:
                        return Group_Thumbnail_Enum.PNG_Thumbnail;

                    case 14:
                        if (!String.IsNullOrEmpty(CustomThumbnail))
                            return Group_Thumbnail_Enum.Custom_Thumbnail;
                        else
                            return Group_Thumbnail_Enum.No_Group_Thumbnail;
                }

                // If it got here, there was an error
                return Group_Thumbnail_Enum.No_Group_Thumbnail;
            }
        }

	    /// <summary> Constructor for a new instance of the Multiple_Volume_Item class </summary>
        public Multiple_Volume_Item()
		{
            
        }
    }
}
