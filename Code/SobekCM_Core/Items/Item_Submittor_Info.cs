using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Items
{
    /// <summary> Information about the submittor of a digital resource </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("submittor")]
    public class Item_Submittor_Info
    {
        /// <summary> Constructor for a new instance of the Item_Submittor_Info </summary>
        public Item_Submittor_Info()
        {
            UserId = -1;
        }

        /// <summary> Full name of the submittor/user </summary>
        [DataMember(Name = "name")]
        [XmlAttribute("name")]
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary> Email address of the submittor/user </summary>
        [DataMember(Name = "email")]
        [XmlAttribute("email")]
        [ProtoMember(2)]
        public string Email { get; set; }

        /// <summary> UserID of the submittor/user </summary>
        [DataMember(Name = "id")]
        [XmlAttribute("id")]
        [ProtoMember(3)]
        public int UserId { get; set; }
    }
}
