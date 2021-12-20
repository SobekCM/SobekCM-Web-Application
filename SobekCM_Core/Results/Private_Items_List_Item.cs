#region Using directives

using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Results
{
    /// <summary> A single private item, used for display of all private items within 
    /// an item aggregation, from the internal header </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("privateItem")]
    public class Private_Items_List_Item
    {
        /// <summary> Volume identifier (VID) for this private item </summary>
        [DataMember(Name = "vid")]
        [XmlAttribute("vid")]
        [ProtoMember(1)]
        public string VID { get; set; }

        /// <summary>Title for this private item </summary>
        [DataMember(Name = "title")]
        [XmlAttribute("title")]
        [ProtoMember(2)]
        public string Title { get; set; }

        /// <summary>Creator for this private item </summary>
        [DataMember(Name = "creator")]
        [XmlAttribute("creator")]
        [ProtoMember(3)]
        public string Creator { get; set; }

        /// <summary>Internal comments for this private item </summary>
        [DataMember(Name="comments",EmitDefaultValue = false)]
        [XmlAttribute("comments")]
        [ProtoMember(4)]
        public string Internal_Comments { get; set; }

        /// <summary>Publication date string for this private item </summary>
        [DataMember(Name = "pubDate", EmitDefaultValue = false)]
        [XmlAttribute("pubDate")]
        [ProtoMember(5)]
        public string PubDate { get; set; }

        /// <summary>Flag indicates if this private item has been locally archived </summary>
        [DataMember(Name = "locallyArchived")]
        [XmlAttribute("locallyArchived")]
        [ProtoMember(6)]
        public bool LocallyArchived { get; set; }

        /// <summary>Flag indicates if this private item has been remotely archived </summary>
        [DataMember(Name = "remotelyArchived")]
        [XmlAttribute("remotelyArchived")]
        [ProtoMember(7)]
        public bool RemotelyArchived { get; set; }

        /// <summary>Aggregation codes for this private item ( i.e., 'dloc,cndl,fdnl') </summary>
        [DataMember(Name = "aggregations")]
        [XmlAttribute("aggregations")]
        [ProtoMember(8)]
        public string AggregationCodes { get; set; }

        /// <summary>Last date any activity occurred for this private item </summary>
        [DataMember(Name = "lastActivityDate")]
        [XmlAttribute("lastActivityDate")]
        [ProtoMember(9)]
        public DateTime LastActivityDate { get; set; }

        /// <summary> Last activity type which occurred for this item </summary>
        [DataMember(Name = "lastActivityType")]
        [XmlAttribute("lastActivityType")]
        [ProtoMember(10)]
        public string LastActivityType { get; set; }

        /// <summary>Last milestone date for this private item </summary>
        [DataMember(Name = "lastMilestoneDate")]
        [XmlAttribute("lastMilestoneDate")]
        [ProtoMember(11)]
        public DateTime LastMilestoneDate { get; set; }

        /// <summary> Last milestone number which occurred for this item </summary>
        [DataMember(Name = "lastMilestone")]
        [XmlAttribute("lastMilestone")]
        [ProtoMember(12)]
        public int LastMilestone { get; set; }

        /// <summary> Embargo date (if one exists) for this item </summary>
        [DataMember(Name = "embargoDate", EmitDefaultValue = false)]
        [XmlAttribute("embargoDate")]
        [ProtoMember(13)]
        public DateTime? EmbargoDate { get; set; }

        /// <summary> Last milestone which occurred for this item, as a string </summary>
        public string Last_Milestone_String
        {
            get
            {
                switch (LastMilestone)
                {
                    case 0:
                        return "record created";

                    case 1:
                        return "scanned";

                    case 2:
                        return "processed";

                    case 3:
                        return "quality control";

                    case 4:
                        return "online completed";

                    default:
                        return "unknown milestone";                
                }
            }
        }
    }
}
