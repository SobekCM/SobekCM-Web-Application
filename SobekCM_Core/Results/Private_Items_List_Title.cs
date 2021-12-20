#region Using directives

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Results
{
    /// <summary> A single title which has private items, used for display of all private items within 
    /// an item aggreation, from the internal header </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("privateTitle")]
    public class Private_Items_List_Title
    {
        /// <summary> Constructor for a new instance of the Private_Items_List_Title class </summary>
        public Private_Items_List_Title()
        {
            Items = new List<Private_Items_List_Item>();
        }

        /// <summary> Number of private items within this private title </summary>
        public int Item_Count
        {
            get
            {
                return Items.Count;
            }
        }

        /// <summary> Collection of private items for this private title  </summary>
        [DataMember(Name = "items")]
        [XmlArray("items")]
        [XmlArrayItem("Private_Items_List_Item")]
        [ProtoMember(1)]
        public List<Private_Items_List_Item> Items { get; set; }

        /// <summary> Row number from within the page of results </summary>
        [DataMember(Name = "rowNum")]
        [XmlAttribute("rowNum")]
        [ProtoMember(2)]
        public int RowNumber { get; set; }

        /// <summary> Bibliographic identifier (BibID) for this title result </summary>
        [DataMember(Name = "bibid")]
        [XmlAttribute("bibid")]
        [ProtoMember(3)]
        public string BibID { get; set; }

        /// <summary> Group title for this title result </summary>
        [DataMember(Name = "groupTitle")]
        [XmlAttribute("groupTitle")]
        [ProtoMember(4)]
        public string Group_Title { get; set; }

        /// <summary> Material type for this title result </summary>
        [DataMember(Name = "type")]
        [XmlAttribute("type")]
        [ProtoMember(5)]
        public string Type { get; set; }

        /// <summary> Date of the last activity to an item in this title </summary>
        [DataMember(Name = "lastActivityDate")]
        [XmlAttribute("lastActivityDate")]
        [ProtoMember(6)]
        public DateTime LastActivityDate { get; set; }

        /// <summary> Date of the last milestone to an item in this title </summary>
        [DataMember(Name = "lastMilestoneDate")]
        [XmlAttribute("lastMilestoneDate")]
        [ProtoMember(7)]
        public DateTime LastMilestoneDate { get; set; }

        /// <summary> Complete number of items which are related to this title </summary>
        [DataMember(Name = "completeItemCount")]
        [XmlAttribute("completeItemCount")]
        [ProtoMember(8)]
        public int CompleteItemCount { get; set; }

        /// <summary> Primary alternate identifier type related to this title ( i.e., 'Accession Numner', 'LLMC#' )</summary>
        [DataMember(Name = "primaryIdentifierType")]
        [XmlAttribute("primaryIdentifierType")]
        [ProtoMember(9)]
        public string PrimaryIdentifierType { get; set; }

        /// <summary> Primary alternate identifier related to this title </summary>
        [DataMember(Name = "primaryIdentifier")]
        [XmlAttribute("primaryIdentifier")]
        [ProtoMember(10)]
        public string PrimaryIdentifier { get; set; }

        /// <summary> Adds a single item result to this private item title </summary>
        /// <param name="New_Item"> Item to add to the collection for this title </param>
        public void Add_Item_Result( Private_Items_List_Item New_Item )
        {
            Items.Add(New_Item);
        }
    }
}
