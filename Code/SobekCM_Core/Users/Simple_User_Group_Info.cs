using System.Runtime.Serialization;

namespace SobekCM.Core.Users
{
    /// <summary> Class contains very basic information about a single user group that a
    /// user is associated with </summary>
    [DataContract]
    public class Simple_User_Group_Info
    {
        /// <summary> Name for this SobekCM user group </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary> UserGroupID (or primary key) to this user group from the database </summary>
        [DataMember]
        public int UserGroupID { get; set; }

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
