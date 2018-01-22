using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Search
{
    /// <summary> User-specific membership information, related to a search, which 
    /// can be used to determine which items this user can discover </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("searchUser")]
    public class Search_User_Membership_Info
    {
        /// <summary> Constructor for a new instance of the Search_User_Membership_Info class </summary>
        public Search_User_Membership_Info()
        {
            LoggedIn = false;
        }

        /// <summary> Flag indicates there is a logged on user at all </summary>
        /// <remarks> If this is not set to TRUE, no need to even look at user group membership or 
        /// user-specific discovery rights </remarks>
        [DataMember(Name = "loggedIn")]
        [XmlAttribute("loggedIn")]
        [ProtoMember(1)]
        public bool LoggedIn { get; set; }

        /// <summary> Primary key to this user, which may give him unique access to 
        /// discover items </summary>
        [DataMember(Name = "userId")]
        [XmlAttribute("userId")]
        [ProtoMember(2)]
        public int UserID { get; set; }

        /// <summary> Flag indicates this user is either a system-wide admin or an admin/curator
        /// of the particular collection being searched, in which case they can discover the hidden
        /// items as well </summary>
        [DataMember(Name = "admin")]
        [XmlAttribute("admin")]
        [ProtoMember(3)]
        public bool Admin { get; set; }

        /// <summary> List of user groups to which this user belongs, which can be used
        /// to grant discovery rights at the user group level </summary>
        [DataMember(Name = "userGroups")]
        [XmlArray("userGroups")]
        [XmlArrayItem("userGroup", typeof(int))]
        [ProtoMember(4)]
        public List<int> UserGroupID { get; set; }

        /// <summary> List of IP Ranges that this current user (whether logged in or not) 
        /// has discovery rights to </summary>
        [DataMember(Name = "ipMembership")]
        [XmlArray("ipMembership")]
        [XmlArrayItem("ipRange", typeof(int))]
        [ProtoMember(5)]
        public List<int> IpMembership { get; set; }
    }
}
