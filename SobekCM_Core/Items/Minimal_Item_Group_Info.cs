using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;

namespace SobekCM.Core.Items
{
    /// <summary> Language-specific item aggregation data </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("minimalGroupInfo")]
    public class Minimal_Item_Group_Info
    {
        /// <summary> Bibliographic identifier for this item group / title </summary>
        [DataMember(Name = "bibid")]
        [XmlAttribute("bibid")]
        [ProtoMember(1)]
        public string BibID { get; set; }

        /// <summary> Title for the entire group of items under this title </summary>
        [DataMember(Name = "title")]
        [XmlAttribute("title")]
        [ProtoMember(2)]
        public string GroupTitle { get; set; }

        /// <summary> Group thumbnail to override the individual item thumbnails in grouped results </summary>
        [DataMember(Name = "thumbnail")]
        [XmlAttribute("thumbnail")]
        [ProtoMember(3)]
        public string GroupThumbnail { get; set; }

        public Minimal_Item_Group_Info()
        {
            // Empty constructor for serialization
        }

        /// <summary> Constructor for a new instance of the Minimal_Item_Group_Info class </summary>
        /// <param name="BibID"> Bibliographic identifier for this item group / title </param>
        /// <param name="GroupTitle"> Title for the entire group of items under this title </param>
        /// <param name="GroupThumbnail"> Group thumbnail to override the individual item thumbnails in grouped results </param>
        public Minimal_Item_Group_Info(string BibID, string GroupTitle, string GroupThumbnail )
        {
            this.BibID = BibID;
            this.GroupTitle = GroupTitle;
            this.GroupThumbnail = GroupThumbnail;
        }
    }
}
