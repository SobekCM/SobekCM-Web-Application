using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Metadata_Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NSLA_Metadata_Plugin
{
    /// <summary> Top-level metadata module for the NSLA metadata plug-in which 
    /// contains a number of custom metadat fields for Nevada State Library, Archives,
    /// and Public Records </summary>
    [Serializable]
    public class NSLA_Metadata_Info : iMetadata_Module
    {
        /// <summary> Constructor for a new instance of the NSLA_Metadata_Info class </summary>
        public NSLA_Metadata_Info()
        {
            Patentee = new List<string>();
        }

        /// <summary> Original page numbers for this item within a larger item </summary>
        public string PageNumber { get; set; }

        /// <summary> Sender of this correspondence </summary>
        public string Correspondence_Sender { get; set; }

        /// <summary> Recepient of this correspondence </summary>
        public string Correspondence_Recepient { get; set; }

        /// <summary> Case number </summary>
        public string Case_Number { get; set; }

        /// <summary> Case name </summary>
        public string Case_Name { get; set; }

        /// <summary> Patent date </summary>
        public string Patent_Date { get; set; }

        /// <summary> Patent number </summary>
        public string Patent_Number { get; set; }

        /// <summary> Patentee(s) related to this patent information </summary>
        public List<string> Patentee { get; private set; }

        /// <summary> Name of this module </summary>
        public string Module_Name { get { return "NSLA Custom Metadata"; } }

        public List<KeyValuePair<string, string>> Metadata_Search_Terms => throw new NotImplementedException();


        /// <summary> Chance for this metadata module to perform any additional database work
        /// such as saving digital resource data into custom tables </summary>
        /// <param name="ItemID"> Primary key for this item within the SobekCM database </param>
        /// <param name="DB_ConnectionString"> Connection string for the current database </param>
        /// <param name="Error_Message"> In the case of an error, this contains text of the error </param>
        /// <param name="BibObject"> Entire resource, in case there are dependencies between this module and somethingt in the full resource </param>
        /// <returns> TRUE if no error occurred, otherwise FALSE </returns>
        public bool Save_Additional_Info_To_Database(int ItemID, string DB_ConnectionString, SobekCM_Item BibObject, out string Error_Message)
        {
            // Nothing is saved to the database for this module
            Error_Message = String.Empty;
            return true;
        }

        /// <summary> Chance for this metadata module to load any additional data from the 
        /// database when building this digital resource  in memory </summary>
        /// <param name="ItemID"> Primary key for this item within the SobekCM database </param>
        /// <param name="DB_ConnectionString">Connection string for the current database</param>
        /// <param name="BibObject"> Entire resource, in case there are dependencies between this module and somethingt in the full resource </param>
        /// <param name="Error_Message"> In the case of an error, this contains text of the error </param>
        /// <returns> TRUE if no error occurred, otherwise FALSE </returns>
        public bool Retrieve_Additional_Info_From_Database(int ItemID, string DB_ConnectionString, SobekCM_Item BibObject, out string Error_Message)
        {
            // Nothing is saved to the database for this module
            Error_Message = String.Empty;
            return true;
        }
    }
}
