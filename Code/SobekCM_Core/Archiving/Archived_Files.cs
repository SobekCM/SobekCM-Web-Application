#region Using directives

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Archiving
{
    /// <summary> Flattened list of archived file/snapshot/copy rows for a single item </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("archivedFiles")]
    public class Archived_Files
    {
        /// <summary> Constructor for a new instance of the Archived_Files class </summary>
        public Archived_Files()
        {
            Files = new List<Archived_File>();
        }

        /// <summary> Primary key for the item these archived files belong to </summary>
        [DataMember(Name = "itemID")]
        [XmlAttribute("itemID")]
        [ProtoMember(1)]
        public int ItemID { get; set; }

        /// <summary> List of archived file/snapshot/copy rows for this item </summary>
        [DataMember(Name = "files")]
        [XmlArray("files")]
        [XmlArrayItem("file", typeof(Archived_File))]
        [ProtoMember(2)]
        public List<Archived_File> Files { get; set; }
    }
}
