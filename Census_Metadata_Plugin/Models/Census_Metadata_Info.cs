using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Metadata_Modules;
using System;
using System.Collections.Generic;

namespace Census_Metadata_Plugin.Models
{
    /// <summary> Top-level metadata module for the census metadata plug-in which
    /// stores information from census books and city directories </summary>
    [Serializable]
    public class Census_Metadata_Info : iMetadata_Module
    {
        private List<Census_Person_Info> people;

        /// <summary> Constructor for a new instance of the Census_Metadata_Info class </summary>
        public Census_Metadata_Info()
        {
            people = new List<Census_Person_Info>();
        }

        /// <summary> List of people linked to this census item </summary>
        public List<Census_Person_Info> People
        {
            get { return people;  }
        }

        public List<KeyValuePair<string, string>> Metadata_Search_Terms => throw new NotImplementedException();

        /// <summary> Name of this module </summary>
        public string Module_Name { get { return "Census Custom Metadata"; } }

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
