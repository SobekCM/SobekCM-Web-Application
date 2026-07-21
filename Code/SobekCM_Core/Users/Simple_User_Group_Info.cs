using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Users
{
    /// <summary> Class contains very basic information about a single user group that a
    /// user is associated with </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("UserGroup")]
    public class Simple_User_Group_Info
    {
        /// <summary> Name for this SobekCM user group </summary>
        [DataMember(EmitDefaultValue = false, Name = "name")]
        [XmlAttribute("name")]
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary> UserGroupID (or primary key) to this user group from the database </summary>
        [DataMember(EmitDefaultValue = false, Name = "userGroupID")]
        [XmlAttribute("userGroupID")]
        [ProtoMember(2)]
        public int UserGroupID { get; set; }

        /// <summary> Constructor for a new instance of the Simple_User_Group_Info  class </summary>
        /// <remark> parameterless  constructor for deserializing</remark>>
        public Simple_User_Group_Info() {   }

        /// <summary> Constructor for a new instance of the Simple_User_Group_Info  class </summary>
        /// <param name="UserGroupID"> UserGroupID (or primary key) to this user group from the database </param>
        /// <param name="Name"> Name for this SobekCM user group </param>
        public Simple_User_Group_Info(int UserGroupID, string Name)
        {
            this.Name = Name;
            this.UserGroupID = UserGroupID;
        }

    }
}
