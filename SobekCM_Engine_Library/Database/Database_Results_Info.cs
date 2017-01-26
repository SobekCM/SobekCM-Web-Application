using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ProtoBuf;
using SobekCM.Core.Results;

namespace SobekCM.Engine_Library.Database
{
    /// <summary> Serializable wrapper class for a list of results and the search result statistics </summary>
    /// <remarks> This is used for the tile aggregation viewer </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("databaseResult")]
    public class Database_Results_Info
    {
        /// <summary> Constructor for a new instance of the Database_Results_Info class </summary>
        public Database_Results_Info()
        {
            Results = new List<Database_Title_Result>();
            Metadata_Labels = new List<string>();
        }

        /// <summary> List of the metadata labels associated with each of the values
        /// found in the title results in the page of results </summary>
        /// <remarks> This allows each aggregation to customize which values are returned
        /// in searches and browses.  This is used to add the labels for each metadata value
        /// in the table and brief views. </remarks>
        [DataMember(Name = "metadataLabels")]
        [XmlArray("metadataLabels")]
        [XmlArrayItem("label", typeof(string))]
        [ProtoMember(1)]
        public List<string> Metadata_Labels { get; set; }

        /// <summary> List of results </summary>
        [DataMember(Name = "results")]
        [XmlArray("results")]
        [XmlArrayItem("result", typeof(Database_Title_Result))]
        [ProtoMember(2)]
        public List<Database_Title_Result> Results { get; set; }
    }


}
