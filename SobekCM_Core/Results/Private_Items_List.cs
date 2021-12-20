#region Using directives

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Results
{
    /// <summary> Class contains the list of all private items within an item aggregation, for display from the internal header </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("privateTitle")]
    public class Private_Items_List
    {
        /// <summary> Constructor for a new instance of the Private_Items_List class </summary>
        public Private_Items_List()
        {
            TitleResults = new List<Private_Items_List_Title>();
        }

        /// <summary> Total number of items matching the search parameters </summary>
        [DataMember(Name = "totalItems")]
        [XmlAttribute("totalItems")]
        [ProtoMember(1)]
        public int TotalItems {  get; set; }

        /// <summary> Total number of titles matching the search parameters </summary>
        [DataMember(Name = "totalTitles")]
        [XmlAttribute("totalTitles")]
        [ProtoMember(2)]
        public int TotalTitles { get; set; }

        /// <summary> Single  page of results, which is of private items list titles </summary>
        [DataMember(Name = "titles")]
        [XmlArray("titles")]
        [XmlArrayItem("Private_Items_List_Title")]
        [ProtoMember(3)]
        public List<Private_Items_List_Title> TitleResults { get; set; }

        /// <summary> Page within the larger set of results that this collection represents </summary>
        [DataMember(Name = "page")]
        [XmlAttribute("page")]
        [ProtoMember(4)]
        public int Page { get; set; }

        /// <summary> Current applied sort </summary>
        [DataMember(Name = "sort")]
        [XmlAttribute("sort")]
        [ProtoMember(5)]
        public int Sort { get; set; }

        /// <summary> Number of results included per page </summary>
        [DataMember(Name = "resultPerPage")]
        [XmlAttribute("resultPerPage")]
        [ProtoMember(6)]
        public int ResultsPerPage { get; set; }

        /// <summary> Aggregation from which this list is pulled </summary>
        [DataMember(Name = "aggregation")]
        [XmlAttribute("aggregation")]
        [ProtoMember(7)]
        public string Aggregation { get; set; }

    }
}
