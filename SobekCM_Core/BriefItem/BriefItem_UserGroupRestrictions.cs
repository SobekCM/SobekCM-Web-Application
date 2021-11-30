using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SobekCM.Core.BriefItem
{
    /// <summary> Class holds the information about a single user group and its special permissions on an item </summary>
    /// <remarks>This data is not stored in the item metadata, but is retrieved from the database when the item is displayed in the digital library </remarks>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("userTag")]
    public class BriefItem_UserGroupRestrictions
    {
        /// <summary> Primary key for this user group from the database </summary>
        [DataMember(Name = "groupid")]
        [XmlElement("groupid")]
        [ProtoMember(1)]
        public int GroupID { get; set; }

        /// <summary> Name of the user group </summary>
        [DataMember(Name = "groupname")]
        [XmlElement("groupname")]
        [ProtoMember(2)]
        public string GroupName { get; set; }

        /// <summary> Flag indicates whether this group can view the material </summary>
        /// <remarks> The existence of any group with special view access restricts the item from 
        /// all other users </remarks>
        [DataMember(Name = "can_view")]
        [XmlElement("can_view")]
        [ProtoMember(3)]
        public bool CanView { get; set; }
    }
}
