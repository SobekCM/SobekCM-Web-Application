using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SobekCM.Core.Aggregations
{
    /// <summary> Contains information about a set of items within an aggregation, including
    /// the visibility, the number of titles, items, pages, and files </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("itemSet")]
    public class Item_Aggregation_Visibility_Statistic
    {
        /// <summary> Visibility of this set of items within this aggregation  </summary>
        [DataMember(Name = "visibility")]
        [XmlAttribute("visibility")]
        [ProtoMember(1)]
        public string Visibility { get; set; }

        /// <summary> Number of titles in this set of items within this aggregation </summary>
        [DataMember(Name = "titles")]
        [XmlAttribute("titles")]
        [ProtoMember(2)]
        public int Titles { get; set; }

        /// <summary> Number of items in this set of items within this aggregation </summary>
        [DataMember(Name = "items")]
        [XmlAttribute("items")]
        [ProtoMember(3)]
        public int Items { get; set; }

        /// <summary> Number of pages in this set of items within this aggregation </summary>
        [DataMember(Name = "pages")]
        [XmlAttribute("pages")]
        [ProtoMember(4)]
        public int Pages { get; set; }

        /// <summary> Number of files in this set of items within this aggregation </summary>
        [DataMember(Name = "files")]
        [XmlAttribute("files")]
        [ProtoMember(5)]
        public int Files { get; set; }
    }
}
