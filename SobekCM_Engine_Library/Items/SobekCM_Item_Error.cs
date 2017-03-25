using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SobekCM.Engine_Library.Items
{
    public enum SobekCM_Item_Error_Type_Enum : byte
    {
        NONE = 1,

        Invalid_BibID,

        Invalid_VID,

        System_Error
    }

    /// <summary> Error encountered when pulling an existing item </summary>
    public class SobekCM_Item_Error
    {
        /// <summary> Type, or classification, of this error </summary>
        public SobekCM_Item_Error_Type_Enum Type { get; set; }

        /// <summary> If the BibID was valid, but the VID invalid, this is the 
        /// first valid VID within that BibID </summary>
        public string FirstValidVid { get; set; }

        /// <summary> Error message with additional details </summary>
        public string Message { get; set; }

        /// <summary> Constructor for a new instance of the SobekCM_Item_Error class </summary>
        /// <param name="Type"> Type, or classification, of this error </param>
        public SobekCM_Item_Error(SobekCM_Item_Error_Type_Enum Type)
        {
            this.Type = Type;
        }

        /// <summary> Constructor for a new instance of the SobekCM_Item_Error class </summary>
        /// <param name="Type"> Type, or classification, of this error </param>
        /// <param name="Message"> Error message with additional details </param>
        public SobekCM_Item_Error(SobekCM_Item_Error_Type_Enum Type, string Message )
        {
            this.Type = Type;
        }
    }
}
