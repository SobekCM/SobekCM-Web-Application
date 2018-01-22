using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using SobekCM.Core.Aggregations;

namespace SobekCM.Core.Search
{
    /// <summary> Contains all the options for the search, such as what fields
    /// to include in the facet, fields to include in the results, whether to group the
    /// results by title, etc.. </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("searchOptions")]
    public class Search_Options_Info
    {
        /// <summary> Constructor for a new instance of the Search_Options_Info class </summary>
        public Search_Options_Info()
        {
            AggregationCode = String.Empty;
            ResultsPerPage = 20;
            Page = 1;
            Sort = 0;
            GroupItemsByTitle = false;
            IncludeFullTextSnippets = false;
            Facets = new List<Complete_Item_Aggregation_Metadata_Type>();
            Fields = new List<Complete_Item_Aggregation_Metadata_Type>();
        }

        /// <summary> Aggregation code to limit this search to, which is used to build the search string </summary>
        [DataMember(Name = "aggregation")]
        [XmlAttribute("aggregation")]
        [ProtoMember(1)]
        public string AggregationCode { get; set; }

        /// <summary> Fields to include in the facets for this item </summary>
        [DataMember(Name = "facets")]
        [XmlArray("facets")]
        [XmlArrayItem("facet", typeof(Complete_Item_Aggregation_Metadata_Type))]
        [ProtoMember(2)]
        public List<Complete_Item_Aggregation_Metadata_Type> Facets { get; set; }

        /// <summary> Fields to include with each results </summary>
        [DataMember(Name = "fields")]
        [XmlArray("fields")]
        [XmlArrayItem("field", typeof(Complete_Item_Aggregation_Metadata_Type))]
        [ProtoMember(3)]
        public List<Complete_Item_Aggregation_Metadata_Type> Fields { get; set; }

        /// <summary> Number of results per page </summary>
        [DataMember(Name = "resultsPerPage")]
        [XmlAttribute("resultsPerPage")]
        [ProtoMember(4)]
        public int ResultsPerPage { get; set; }

        /// <summary> Page number within the complete set of results </summary>
        [DataMember(Name = "page")]
        [XmlAttribute("page")]
        [ProtoMember(5)]
        public int Page { get; set; }

        /// <summary> Sort indicator </summary>
        [DataMember(Name = "sort")]
        [XmlAttribute("sort")]
        [ProtoMember(6)]
        public ushort Sort { get; set; }

        /// <summary> Flag indicates if the results should be grouped by title </summary>
        [DataMember(Name = "groupItemsByTitle")]
        [XmlAttribute("groupItemsByTitle")]
        [ProtoMember(7)]
        public bool GroupItemsByTitle { get; set; }

        /// <summary> Flag indicates if the full text snippets should be included in the results </summary>
        /// <remarks> This does greatly increase the size of the results, since snippets are usually 250 characters long </remarks>
        [DataMember(Name = "includeFullTextSnippets")]
        [XmlAttribute("includeFullTextSnippets")]
        [ProtoMember(8)]
        public bool IncludeFullTextSnippets { get; set; }
    }
}
