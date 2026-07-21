#region Using directives

using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Users
{
    /// <summary> Holds the basic data about aggregationPermissions which may be editable by a user.  These objects are
    /// institutions, collections, subcollections, etc.. </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("Aggregation")]
    public class User_Permissioned_Aggregation
    {
        /// <summary> Code for this user editable object </summary>
        [DataMember(EmitDefaultValue = false, Name = "code")]
        [XmlAttribute("code")]
        [ProtoMember(1)]
        public string Code { get; set; }

        /// <summary> Name for this user editable object </summary>
        [DataMember(EmitDefaultValue = false, Name = "name")]
        [XmlAttribute("name")]
        [ProtoMember(2)]
        public string Name { get; set; }

        /// <summary> Constructor for a new instance of the User_Permissioned_Aggregation class </summary>
        /// <remark> parameterless  constructor for deserializing</remark>>
        public User_Permissioned_Aggregation() { }

        /// <summary> Constructor for a new instance of the User_Permissioned_Aggregation class </summary>
        /// <param name="Code"> Code for this user editable item aggregation</param>
        /// <param name="Name"> Name for this user editable item aggregation </param>
        /// <param name="CanSelect"> Flag indicates if this user can add items to this item aggregation</param>
        /// <param name="CanEditItems"> Flag indicates if this user can edit any items in this item aggregation</param>
        /// <param name="IsCurator"> Flag indicates if this user is listed as the curator or collection manager for this given digital aggregation </param>
        /// <param name="OnHomePage"> Flag indicates if this user is an admin over this aggregation, and can edit the aggregation home, browse, and info pages</param>
		/// <param name="IsAdmin"> Flag indicates if this user is listed as the administrator for this aggregation </param>
        public User_Permissioned_Aggregation(string Code, string Name, bool CanSelect, bool CanEditItems, bool IsCurator, bool OnHomePage, bool IsAdmin)
        {
            this.Code = Code;
            this.Name = Name;
            this.CanSelect = CanSelect;
            this.CanEditItems = CanEditItems;
            this.IsCurator = IsCurator;
            this.OnHomePage = OnHomePage;
            this.IsAdmin = IsAdmin;
            GroupDefined = false;
        }

        /// <summary> Flag indicates if this user is listed as the curator or collection manager for this given digital aggregation </summary>
        [DataMember(EmitDefaultValue = false, Name = "isCurator")]
        [XmlAttribute("isCurator")]
        [ProtoMember(3)]
        public bool IsCurator { get; set; }

        /// <summary> Flag indicates if this user is listed as the curator or collection manager for this given digital aggregation </summary>
        [DataMember(EmitDefaultValue = false, Name = "isAdmin")]
        [XmlAttribute("isAdmin")]
        [ProtoMember(4)]
        public bool IsAdmin { get; set; }

        /// <summary> Flag indicates if this user has asked to have this aggregation appear on their personalized home page </summary>
        [DataMember(EmitDefaultValue = false, Name = "onHomePage")]
        [XmlAttribute("onHomePage")]
        [ProtoMember(5)]
        public bool OnHomePage { get; set; }

        /// <summary> Flag indicates if this user can add items to this item aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canSelect")]
        [XmlAttribute("canSelect")]
        [ProtoMember(6)]
        public bool CanSelect { get; set; }

        /// <summary> Flag indicates if this user can edit the metadata for items in this aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canEditMetadata")]
        [XmlAttribute("canEditMetadata")]
        [ProtoMember(7)]
        public bool CanEditMetadata { get; set; }

        /// <summary> Flag indicates if this user can edit the behavior for items in this aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canEditBehaviors")]
        [XmlAttribute("canEditBehaviors")]
        [ProtoMember(8)]
        public bool CanEditBehaviors { get; set; }

        /// <summary> Flag indicates if this user can perform quality control for items in this aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canPerformQc")]
        [XmlAttribute("canPerformQc")]
        [ProtoMember(9)]
        public bool CanPerformQc { get; set; }

        /// <summary> Flag indicates if this user can upload files for items in this aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canUploadFiles")]
        [XmlAttribute("canUploadFiles")]
        [ProtoMember(10)]
        public bool CanUploadFiles { get; set; }

        /// <summary> Flag indicates if this user can change the visibility of items ( PRIVATE, PUBLIC, etc.. ) in this aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canChangeVisibility")]
        [XmlAttribute("canChangeVisibility")]
        [ProtoMember(11)]
        public bool CanChangeVisibility { get; set; }

        /// <summary> Flag indicates if this user can delete any items in this aggregation</summary>
        [DataMember(EmitDefaultValue = false, Name = "canDelete")]
        [XmlAttribute("canDelete")]
        [ProtoMember(12)]
        public bool CanDelete { get; set; }

        /// <summary> Flag indicates that this is a group defined link  </summary>
        [DataMember(EmitDefaultValue = false, Name = "groupDefined")]
        [XmlAttribute("groupDefined")]
        [ProtoMember(13)]
        public bool GroupDefined { get; set; }

        /// <summary> Flag indicates if this user can edit any items in this item aggregation</summary>
        [IgnoreDataMember]
        [XmlIgnore]
        public bool CanEditItems
        {
            get
            {
                return CanEditMetadata && CanEditBehaviors && CanPerformQc && CanUploadFiles && CanChangeVisibility && CanDelete;
            }
            set
            {
                CanEditMetadata = value;
                CanEditBehaviors = value;
                CanPerformQc = value;
                CanUploadFiles = value;
                CanChangeVisibility = value;
                CanDelete = value;
            }
        }
    }
}
