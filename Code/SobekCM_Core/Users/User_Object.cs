#region Using directives

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

#endregion

namespace SobekCM.Core.Users
{
    #region User_Authenticaion_Type_Enum

    /// <summary> Enumeration used to indicate the way the current user has authenticated </summary>
    /// <remarks> This is primarily used the first time a user logs on and registers with the system </remarks>
    public enum User_Authentication_Type_Enum : byte
    {
        /// <summary> No authentication (or not indicated) </summary>
        NONE = 0,

        /// <summary> Authentication occurred using LDAP, primarily using Active Directory and IIS challenge </summary>
        LDAP,

        /// <summary> Authentication occurred using Shibboleth </summary>
        Shibboleth,

        /// <summary> Authentication occurred using the internal SobekCM authentication system </summary>
        Sobek,

        /// <summary> Authentication occurred using the basic IIS windows authentication pop-up </summary>
        Windows,

        /// <summary> Authentication occurred using an external OpenID Connect identity provider </summary>
        OpenIdConnect,

        /// <summary> Authentication occurred using an external SAML identity provider </summary>
        Saml
    }

    #endregion

    #region User_Object_Attribute_Mapping and helper class

    /// <summary> Enumeration of the main public elements associated with a user </summary>
    /// <remarks> This is used for mapping during authentication (usually the first time a user logs on) </remarks>
    public enum User_Object_Attribute_Mapping_Enum : byte
    {
        /// <summary> No mapping defined </summary>
        NONE = 0,

        /// <summary> Maps to the COLLEGE this user is associated with </summary>
        College,

        /// <summary> Maps to the DEPARTMENT this user is associated with </summary>
        Department,

        /// <summary> Maps to the EMAIL address of this user </summary>
        Email,

        /// <summary> Maps to the FIRSTNAME address of this user </summary>
        Firstname,

        /// <summary> Full name of this user ( FIRSTNAME followed by LASTNAME ) </summary>
        Fullname,

        /// <summary> Maps to the LASTNAME address of this user </summary>
        Lastname,

        /// <summary> Maps to the NICKNAME address of this user </summary>
        Nickname,

        /// <summary> Maps to the NOTES for this user, which are only visible to system and portal admins </summary>
        Notes,

        /// <summary> Maps to the ORGANIZATION this user is associated with </summary>
        Organization,

        /// <summary> Maps to the CODE for the ORGANIZATION this user is associated with </summary>
        OrgCode,

        /// <summary> Maps to the USERNAME address of this user </summary>
        Username
    }

    /// <summary> Static helper class is used to convert strings to the enumeration for
    /// the user object attribute mapping </summary>
    public static class User_Object_Attribute_Mapping_Enum_Converter
    {

        /// <summary> Convert a string to the enumeration for the user object attribute mapping </summary>
        /// <param name="Value"> String value </param>
        /// <returns> Enumeration value, or User_Object_Attribute_Mapping_Enum.NONE </returns>
        public static User_Object_Attribute_Mapping_Enum ToEnum(string Value)
        {
            switch (Value.ToUpper())
            {
                case "USERNAME":
                    return User_Object_Attribute_Mapping_Enum.Username;

                case "EMAIL":
                    return User_Object_Attribute_Mapping_Enum.Email;

                case "FIRSTNAME":
                    return User_Object_Attribute_Mapping_Enum.Firstname;

                case "LASTNAME":
                    return User_Object_Attribute_Mapping_Enum.Lastname;

                case "FULLNAME":
                    return User_Object_Attribute_Mapping_Enum.Fullname;

                case "NICKNAME":
                    return User_Object_Attribute_Mapping_Enum.Nickname;

                case "NOTES":
                    return User_Object_Attribute_Mapping_Enum.Notes;

                case "ORGANIZATION":
                    return User_Object_Attribute_Mapping_Enum.Organization;

                case "ORGCODE":
                    return User_Object_Attribute_Mapping_Enum.OrgCode;

                case "COLLEGE":
                    return User_Object_Attribute_Mapping_Enum.College;

                case "DEPARTMENT":
                    return User_Object_Attribute_Mapping_Enum.Department;

                default:
                    return User_Object_Attribute_Mapping_Enum.NONE;
            }
        }

        /// <summary> Convert the enumeration for the user object attribute mapping to a string </summary>
        /// <param name="Value"> Enumeration value </param>
        /// <returns> String value </returns>
        public static string ToString(User_Object_Attribute_Mapping_Enum Value)
        {
            switch (Value)
            {
                case User_Object_Attribute_Mapping_Enum.Username:
                    return "USERNAME";

                case User_Object_Attribute_Mapping_Enum.Email:
                    return "EMAIL";

                case User_Object_Attribute_Mapping_Enum.Firstname:
                    return "FIRSTNAME";

                case User_Object_Attribute_Mapping_Enum.Lastname:
                    return "LASTNAME";

                case User_Object_Attribute_Mapping_Enum.Nickname:
                    return "NICKNAME";

                case User_Object_Attribute_Mapping_Enum.Notes:
                    return "NOTES";

                case User_Object_Attribute_Mapping_Enum.Organization:
                    return "ORGANIZATION";

                case User_Object_Attribute_Mapping_Enum.OrgCode:
                    return "ORGCODE";

                case User_Object_Attribute_Mapping_Enum.College:
                    return "COLLEGE";

                case User_Object_Attribute_Mapping_Enum.Department:
                    return "DEPARTMENT";

                default:
                    return "NONE";
            }
        }
    }


    #endregion

    /// <summary> Represents a single mySobek user, including personal information, permissions,
    /// and preferences.  </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("User")]
    public class User_Object
    {
        #region Private class members 

        private User_Aggregation_Permissions aggregationPermissions;
        private SortedList<string, User_Folder> folders;

        private List<string> templates_from_groups;
        private List<string> defaultMetadataSetsFromGroups;

        #endregion

        #region Constructor

        /// <summary> Constructor for a new instance of the User_Object class </summary>
        public User_Object()
        {
            Family_Name = String.Empty;
            Given_Name = String.Empty;
            ShibbID = String.Empty;
            Email = String.Empty;
            Department = String.Empty;
            Nickname = String.Empty;
            Can_Submit = false;
            Is_Just_Registered = false;
            Send_Email_On_Submission = true;
            Is_Temporary_Password = false;
            Is_Internal_User = false;
            UserName = String.Empty;
            Preferred_Language = String.Empty;
            Templates = [];
            Default_Metadata_Sets = [];
            BibIDs = [];
            Bookshelf_Items = [];
            Items_Submitted_Count = 0;
            Organization = String.Empty;
            Department = String.Empty;
            Unit = String.Empty;
            College = String.Empty;
            Organization_Code = String.Empty;
            Edit_Template_Code_Simple = String.Empty;
            Edit_Template_Code_Complex = String.Empty;
            aggregationPermissions = new User_Aggregation_Permissions();
            Editable_Regular_Expressions = [];
            folders = new SortedList<string, User_Folder>();
            Default_Rights = String.Empty;
            Is_System_Admin = false;
            Is_Portal_Admin = false;
            Has_Descriptive_Tags = false;
            User_Groups = [];
            Receive_Stats_Emails = true;
            Has_Item_Stats = false;
            Include_Tracking_In_Standard_Forms = true;
            UserSettings = [];
            Can_Delete_All = false;
            Authentication_Type = User_Authentication_Type_Enum.NONE;
            defaultMetadataSetsFromGroups = [];
            templates_from_groups = [];
            LoggedOn = false;

        }

        #endregion

        /// <summary> Flag indicates if this user is logged on, or if this represents
        /// a non-logged on user's session-specific data </summary>
        [DataMember(EmitDefaultValue = false, Name = "loggedOn")]
        [XmlAttribute("loggedOn")]
        [ProtoMember(1)]
        public bool LoggedOn { get; set; } = false;

        /// <summary> Internal notes about this user, which are not viewable by the actual user </summary>
        /// <remarks> This can be used, in part, to put data from Shibboleth or the LDAP authentication process </remarks>
        [DataMember(EmitDefaultValue = false, Name = "notes")]
        [XmlAttribute("notes")]
        [ProtoMember(2)]
        public string Internal_Notes { get; set; } = String.Empty;

        /// <summary> Flag indicates if this user should appear as a possible scanning technician </summary>
        [DataMember(EmitDefaultValue = false, Name = "scanningTechnician")]
        [XmlAttribute("scanningTechnician")]
        [ProtoMember(3)]
        public bool Scanning_Technician { get; set; } = false;

        /// <summary> Flag indicates if this user should appear as a possible processing technician </summary>
        [DataMember(EmitDefaultValue = false, Name = "processingTechnician")]
        [XmlAttribute("processingTechnician")]
        [ProtoMember(4)]
        public bool Processing_Technician { get; set; } = false;

        #region User settings properties and methods

        /// <summary> User settings as a key/value dictionary </summary>
        [DataMember(EmitDefaultValue = false, Name = "settings")]
        [XmlIgnore]
        [ProtoMember(5)]
        public Dictionary<string, string> UserSettings { get; private set; }

        /// <summary> Get the number of settings </summary>
        public int SettingsCount => UserSettings?.Count ?? 0;

        /// <summary> Gets the list of all the keys for these settings </summary>
        public List<string> SettingsKeys => UserSettings?.Keys.ToList() ?? new List<string>();

        /// <summary> Get the user option by key, returning null if absent </summary>
        public string Get_Setting(string Option_Key) =>
            UserSettings.TryGetValue(Option_Key, out string v) ? v : null;

        /// <summary> Get the user option as a bool, returning the default if absent </summary>
        public bool Get_Setting(string Option_Key, bool Default_Value) =>
            UserSettings.TryGetValue(Option_Key, out string v)
                ? v.Equals("true", StringComparison.OrdinalIgnoreCase)
                : Default_Value;

        /// <summary> Get the user option as an integer, returning the default if absent </summary>
        public int Get_Setting(string Option_Key, int Default_Value) =>
            UserSettings.TryGetValue(Option_Key, out string v) && int.TryParse(v, out int i) ? i : Default_Value;

        /// <summary> Get the user option as a string, returning the default if absent </summary>
        public string Get_Setting(string Option_Key, string Default_Value) =>
            UserSettings.TryGetValue(Option_Key, out string v) ? v : Default_Value;

        /// <summary> Add or update a user option </summary>
        public void Add_Setting(string Option_Key, string Option_Value) =>
            Add_Setting(Option_Key, Option_Value, true);

        /// <summary> Add or update a user option </summary>
        public void Add_Setting(string Option_Key, string Option_Value, bool Update_Database)
        {
            if (!UserSettings.TryGetValue(Option_Key, out string existing) || existing != Option_Value)
                UserSettings[Option_Key] = Option_Value;
        }

        #endregion

        #region Public properties of this user object

        /// <summary> Flag indicates this user has chosen to receive statistics emails about their items </summary>
        [DataMember(EmitDefaultValue = false, Name = "receiveStatsEmails")]
        [XmlAttribute("receiveStatsEmails")]
        [ProtoMember(6)]
        public bool Receive_Stats_Emails { get; set; }

        /// <summary> Flag indicates this user has item statistics linked to their account </summary>
        [DataMember(EmitDefaultValue = false, Name = "hasItemStats")]
        [XmlAttribute("hasItemStats")]
        [ProtoMember(7)]
        public bool Has_Item_Stats { get; set; }

        /// <summary> Checks to see if this user is a collection manager or collection admin </summary>
        [XmlIgnore]
        public bool Is_A_Collection_Manager_Or_Admin
        {
            get { return aggregationPermissions != null && (aggregationPermissions.Aggregations != null) && aggregationPermissions.Aggregations.Any(Aggregation => Aggregation.IsCurator); }
        }

        /// <summary> Flag indicates if this user has descriptive tags associated with them </summary>
        [DataMember(EmitDefaultValue = false, Name = "hasDescriptiveTags")]
        [XmlAttribute("hasDescriptiveTags")]
        [ProtoMember(8)]
        public bool Has_Descriptive_Tags { get; set; }

        /// <summary> Flag is used when editing a users rights to indicate this user should be able to edit ALL items in the library </summary>
        [DataMember(EmitDefaultValue = false, Name = "shouldBeAbleToEditAllItems")]
        [XmlAttribute("shouldBeAbleToEditAllItems")]
        [ProtoMember(9)]
        public bool Should_Be_Able_To_Edit_All_Items { get; set; }

        /// <summary> Ordered list of submittal templates this user has access to </summary>
        /// <remarks>The first item in this list is the default template for this user </remarks>
        [DataMember(EmitDefaultValue = false, Name = "templates")]
        [XmlArray("templates")]
        [XmlArrayItem("template", typeof(string))]
        [ProtoMember(10)]
        public List<string> Templates { get; private set; }

        /// <summary> Stored value of the current template selection (raw, for serialization) </summary>
        [DataMember(EmitDefaultValue = false, Name = "currentTemplate")]
        [XmlAttribute("currentTemplate")]
        [ProtoMember(46)]
        public string Current_Template_Value { get; set; }

        /// <summary> Returns the effective current template, falling back to the first available template </summary>
        [XmlIgnore]
        public string Current_Template
        {
            get
            {
                if (!string.IsNullOrEmpty(Current_Template_Value))
                    return Current_Template_Value;
                return Templates is { Count: > 0 } ? Templates[0] : string.Empty;
            }
            set
            {
                if (Templates == null || Templates.Count == 0) return;
                if (string.IsNullOrEmpty(value) || Templates.Contains(value))
                    Current_Template_Value = value;
            }
        }

        /// <summary> Ordered list of default metadata sets this user has access to </summary>
        /// <remarks>The first item in this list is the default metadata set for this user </remarks>
        [DataMember(EmitDefaultValue = false, Name = "defaultMetadataSets")]
        [XmlArray("defaultMetadataSets")]
        [XmlArrayItem("metadataSet", typeof(string))]
        [ProtoMember(11)]
        public List<string> Default_Metadata_Sets { get; private set; }

        /// <summary> Stored value of the current default metadata set selection (raw, for serialization) </summary>
        [DataMember(EmitDefaultValue = false, Name = "currentMetadataSet")]
        [XmlAttribute("currentMetadataSet")]
        [ProtoMember(47)]
        public string Current_Default_Metadata_Value { get; set; }

        /// <summary> Returns the effective current default metadata set, falling back to the first available set </summary>
        [XmlIgnore]
        public string Current_Default_Metadata
        {
            get
            {
                if (!string.IsNullOrEmpty(Current_Default_Metadata_Value))
                    return Current_Default_Metadata_Value;
                return Default_Metadata_Sets is { Count: > 0 } ? Default_Metadata_Sets[0] : string.Empty;
            }
            set
            {
                if (Default_Metadata_Sets == null || Default_Metadata_Sets.Count == 0) return;
                if (string.IsNullOrEmpty(value) || Default_Metadata_Sets.Contains(value))
                    Current_Default_Metadata_Value = value;
            }
        }

        /// <summary> List of the BibID's for every item this user has submitted or been directly
        /// granted edit permissions against. </summary>
        [DataMember(EmitDefaultValue = false, Name = "bibids")]
        [XmlArray("bibids")]
        [XmlArrayItem("bibid", typeof(string))]
        [ProtoMember(12)]
        public List<string> BibIDs { get; private set; }

        /// <summary> List of BibID_VID identifiers for items in this user's bookshelves </summary>
        [DataMember(EmitDefaultValue = false, Name = "bookshelfItems")]
        [XmlArray("bookshelfItems")]
        [XmlArrayItem("item", typeof(string))]
        [ProtoMember(48)]
        public List<string> Bookshelf_Items { get; private set; }

        /// <summary> Code for the federated (OIDC/SAML) provider this user last authenticated through, matching
        /// an <c>Oidc_Configuration</c>/<c>Saml_Configuration</c> Provider_Code; NULL for Sobek/LDAP/Windows users </summary>
        /// <remarks> Generalized, multi-provider-capable sibling of <see cref="ShibbID"/> — added rather than
        /// reusing ShibbID so the dormant Shibboleth path is left completely untouched </remarks>
        [DataMember(EmitDefaultValue = false, Name = "externalProviderCode")]
        [XmlAttribute("externalProviderCode")]
        [ProtoMember(49)]
        public string External_Provider_Code { get; set; }

        /// <summary> Subject identifier (e.g. OIDC 'sub' claim, SAML NameID) this user was issued by
        /// <see cref="External_Provider_Code"/>; NULL for Sobek/LDAP/Windows users </summary>
        [DataMember(EmitDefaultValue = false, Name = "externalSubjectId")]
        [XmlAttribute("externalSubjectId")]
        [ProtoMember(50)]
        public string External_Subject_Id { get; set; }

        /// <summary> Human-readable description of how this user authenticates - "Registered" for a native
        /// SobekCM account, or "OpenID (Label)"/"SAML (Label)" for a federated account, where Label is the
        /// matching <c>Oidc_Configuration</c>/<c>Saml_Configuration</c> provider's Display_Label </summary>
        /// <remarks> Not computed by this class - <see cref="Authentication_Type"/> and
        /// <see cref="External_Provider_Code"/> alone don't carry the provider's Display_Label, so whichever
        /// method loads the user from the database is expected to look up the label and set this </remarks>
        [DataMember(EmitDefaultValue = false, Name = "authenticationSource")]
        [XmlAttribute("authenticationSource")]
        [ProtoMember(51)]
        public string Authentication_Source { get; set; }

        /// <summary> Number of items this user has submitted </summary>
        [DataMember(EmitDefaultValue = false, Name = "itemsSubmittedCount")]
        [XmlAttribute("itemsSubmittedCount")]
        [ProtoMember(13)]
        public int Items_Submitted_Count { get; set; }

        /// <summary> SobekCM username for this user </summary>
        [DataMember(EmitDefaultValue = false, Name = "userName")]
        [XmlAttribute("userName")]
        [ProtoMember(14)]
        public string UserName { get; set; }

        /// <summary> UserID (or primary key) to this user from the database </summary>
        [DataMember(Name = "userID")]
        [XmlAttribute("userID")]
        [ProtoMember(15)]
        public int UserID { get; set; }

        /// <summary> User's preferred language </summary>
        [DataMember(EmitDefaultValue = false, Name = "preferredLanguage")]
        [XmlAttribute("preferredLanguage")]
        [ProtoMember(16)]
        public string Preferred_Language { get; set; }

        /// <summary> Simple flag indicates if this user can submit items </summary>
        [DataMember(EmitDefaultValue = false, Name = "canSubmit")]
        [XmlAttribute("canSubmit")]
        [ProtoMember(17)]
        public bool Can_Submit { get; set; }

        /// <summary> Simple flag indicates if this user can delete any item in this repository </summary>
        [DataMember(EmitDefaultValue = false, Name = "canDeleteAll")]
        [XmlAttribute("canDeleteAll")]
        [ProtoMember(18)]
        public bool Can_Delete_All { get; set; }

        /// <summary> Default rights statement for this user </summary>
        [DataMember(EmitDefaultValue = false, Name = "defaultRights")]
        [XmlAttribute("defaultRights")]
        [ProtoMember(19)]
        public string Default_Rights { get; set; }

        /// <summary> Flag indicates whether user wishes to receive an email after submission </summary>
        [DataMember(EmitDefaultValue = false, Name = "sendEmailOnSubmission")]
        [XmlAttribute("sendEmailOnSubmission")]
        [ProtoMember(20)]
        public bool Send_Email_On_Submission { get; set; }

        /// <summary> Flag indicates if this is a temporary password </summary>
        /// <remarks>Temporary passwords must be changed once the user logs on </remarks>
        [DataMember(EmitDefaultValue = false, Name = "isTemporaryPassword")]
        [XmlAttribute("isTemporaryPassword")]
        [ProtoMember(21)]
        public bool Is_Temporary_Password { get; set; }

        /// <summary> Flag indicates if this is an internal user </summary>
        /// <remarks>This grants access to various tracking elements in SobekCM</remarks>
        [DataMember(EmitDefaultValue = false, Name = "isInternalUser")]
        [XmlAttribute("isInternalUser")]
        [ProtoMember(22)]
        public bool Is_Internal_User { get; set; }

        /// <summary> Flag indicates if this user has general admin rights over the entire system </summary>
        [DataMember(EmitDefaultValue = false, Name = "isSystemAdmin")]
        [XmlAttribute("isSystemAdmin")]
        [ProtoMember(23)]
        public bool Is_System_Admin { get; set; }

        /// <summary> Flag indicates if this user has general admin rights over the appearance of portions of the system </summary>
        [DataMember(EmitDefaultValue = false, Name = "isPortalAdmin")]
        [XmlAttribute("isPortalAdmin")]
        [ProtoMember(24)]
        public bool Is_Portal_Admin { get; set; }

        /// <summary> Flag indicates if this user is the host administrator, if this is a hosted instance </summary>
        [DataMember(EmitDefaultValue = false, Name = "isHostAdmin")]
        [XmlAttribute("isHostAdmin")]
        [ProtoMember(25)]
        public bool Is_Host_Admin { get; set; }

        /// <summary> Flag indicates if this user is a user administrator, able to manage users, user requests, and groups </summary>
        [DataMember(EmitDefaultValue = false, Name = "isUserAdmin")]
        [XmlAttribute("isUserAdmin")]
        [ProtoMember(26)]
        public bool Is_User_Admin { get; set; }

        /// <summary> Flag indicates if users should see the tracking information when adding a new volume
        /// or performing standard operations within the system </summary>
        [DataMember(EmitDefaultValue = false, Name = "includeTrackingInStandardForms")]
        [XmlAttribute("includeTrackingInStandardForms")]
        [ProtoMember(27)]
        public bool Include_Tracking_In_Standard_Forms { get; set; }

        /// <summary> User's family (or last) name </summary>
        [DataMember(EmitDefaultValue = false, Name = "familyName")]
        [XmlAttribute("familyName")]
        [ProtoMember(28)]
        public string Family_Name { get; set; }

        /// <summary> User's given (or first) name </summary>
        [DataMember(EmitDefaultValue = false, Name = "givenName")]
        [XmlAttribute("givenName")]
        [ProtoMember(29)]
        public string Given_Name { get; set; }

        /// <summary> User's nickname </summary>
        [DataMember(EmitDefaultValue = false, Name = "nickname")]
        [XmlAttribute("nickname")]
        [ProtoMember(30)]
        public string Nickname { get; set; }

        /// <summary> Returns the user's full name in [first name last name] order </summary>
        [XmlIgnore]
        public string Full_Name
        {
            get { return Given_Name + " " + Family_Name; }
        }

        /// <summary> Returns the user's full name in [last name, first name] format </summary>
        [XmlIgnore]
        public string Reversed_Full_Name
        {
            get { return Family_Name + ", " + Given_Name; }
        }

        /// <summary> User's shibboleth ID </summary>
        [DataMember(EmitDefaultValue = false, Name = "shibbID")]
        [XmlAttribute("shibbID")]
        [ProtoMember(31)]
        public string ShibbID { get; set; }

        /// <summary> User's organization affiliation information </summary>
        [DataMember(EmitDefaultValue = false, Name = "organization")]
        [XmlAttribute("organization")]
        [ProtoMember(32)]
        public string Organization { get; set; }

        /// <summary> User's organization code </summary>
        /// <remarks> This is used to tag any newly submitted items to the institution's aggregation </remarks>
        [DataMember(EmitDefaultValue = false, Name = "organizationCode")]
        [XmlAttribute("organizationCode")]
        [ProtoMember(33)]
        public string Organization_Code { get; set; }

        /// <summary> User's college affiliation information </summary>
        [DataMember(EmitDefaultValue = false, Name = "college")]
        [XmlAttribute("college")]
        [ProtoMember(34)]
        public string College { get; set; }

        /// <summary> User's department affiliation information </summary>
        [DataMember(EmitDefaultValue = false, Name = "department")]
        [XmlAttribute("department")]
        [ProtoMember(35)]
        public string Department { get; set; }

        /// <summary> User's unit affiliation information </summary>
        [DataMember(EmitDefaultValue = false, Name = "unit")]
        [XmlAttribute("unit")]
        [ProtoMember(36)]
        public string Unit { get; set; }

        /// <summary> User's email address </summary>
        [DataMember(EmitDefaultValue = false, Name = "email")]
        [XmlAttribute("email")]
        [ProtoMember(37)]
        public string Email { get; set; }

        /// <summary> User's template code for editing simple (non-MARC) records </summary>
        [DataMember(EmitDefaultValue = false, Name = "editTemplateCodeSimple")]
        [XmlAttribute("editTemplateCodeSimple")]
        [ProtoMember(38)]
        public string Edit_Template_Code_Simple { get; set; }

        /// <summary> User's template code editing complex (MARC) records </summary>
        [DataMember(EmitDefaultValue = false, Name = "editTemplateCodeComplex")]
        [XmlAttribute("editTemplateCodeComplex")]
        [ProtoMember(39)]
        public string Edit_Template_Code_Complex { get; set; }

        /// <summary> Enumeration indicates how the user authenticated with the system ( i.e., Sobek, Shibboleth, or LDAP ) </summary>
        [DataMember(EmitDefaultValue = false, Name = "authenticationType")]
        [XmlAttribute("authenticationType")]
        [ProtoMember(40)]
        public User_Authentication_Type_Enum Authentication_Type { get; set; }

        /// <summary> List of item aggregation permissions associated with this user </summary>
        [DataMember(EmitDefaultValue = false, Name = "aggregations")]
        [XmlArray("aggregations")]
        [XmlArrayItem("aggregation", typeof(User_Permissioned_Aggregation))]
        [ProtoMember(41)]
        public List<User_Permissioned_Aggregation> PermissionedAggregations
        {
            get
            {
                if (aggregationPermissions == null) return null;
                return aggregationPermissions.Aggregations;
            }
            set
            {
                if (aggregationPermissions == null)
                    aggregationPermissions = new User_Aggregation_Permissions();
                aggregationPermissions.Aggregations = value;
            }
        }

        /// <summary> List of regular expressions for checking for edit by bibid </summary>
        [DataMember(EmitDefaultValue = false, Name = "editableRegexes")]
        [XmlArray("editableRegexes")]
        [XmlArrayItem("regex", typeof(string))]
        [ProtoMember(42)]
        public List<string> Editable_Regular_Expressions { get; private set; }

        /// <summary> List of user groups to which this user belongs </summary>
        [DataMember(EmitDefaultValue = false, Name = "userGroups")]
        [XmlArray("userGroups")]
        [XmlArrayItem("userGroup", typeof(Simple_User_Group_Info))]
        [ProtoMember(43)]
        public List<Simple_User_Group_Info> User_Groups { get; private set; }

        /// <summary> List of folders associated with this user </summary>
        [XmlIgnore]
        public ReadOnlyCollection<User_Folder> Folders
        {
            get { return new ReadOnlyCollection<User_Folder>(folders.Values); }
        }

        /// <summary> List of folders associated with this user, exposed as a list for serialization </summary>
        [DataMember(EmitDefaultValue = false, Name = "folders")]
        [XmlArray("folders")]
        [XmlArrayItem("folder", typeof(User_Folder))]
        [ProtoMember(44)]
        public List<User_Folder> Folders_List
        {
            get { return folders.Values.ToList(); }
            set
            {
                folders.Clear();
                if (value != null)
                    foreach (User_Folder f in value)
                        folders[f.Folder_Name] = f;
            }
        }

        /// <summary> Return the number of templates tied to this user </summary>
        [XmlIgnore]
        public int Templates_Count => Templates?.Count ?? 0;

        /// <summary> Return the number of default metadata sets tied to this user </summary>
        [XmlIgnore]
        public int Default_Metadata_Sets_Count => Default_Metadata_Sets?.Count ?? 0;

        /// <summary> Return the number of aggregations tied to this user </summary>
        [XmlIgnore]
        public int PermissionedAggregations_Count
        {
            get { return ((aggregationPermissions == null) || (aggregationPermissions.Aggregations == null)) ? 0 : aggregationPermissions.Aggregations.Count; }
        }

        /// <summary> Flag indicates if this user was just registered </summary>
        /// <remarks> This flag is just used so mySobek does not say 'Welcome Back' the first time a user logs on </remarks>
        [DataMember(EmitDefaultValue = false, Name = "isJustRegistered")]
        [XmlAttribute("isJustRegistered")]
        [ProtoMember(45)]
        public bool Is_Just_Registered { get; set; }

        /// <summary> Gets the list of all folders, in alphabetical order </summary>
        [XmlIgnore]
        public ReadOnlyCollection<User_Folder> All_Folders
        {
            get
            {
                var folder_builder = new SortedList<string, User_Folder>();
                foreach (User_Folder thisFolder in folders.Values)
                {
                    folder_builder.Add(thisFolder.Folder_Name, thisFolder);
                    recurse_through_children(thisFolder, folder_builder);
                }

                if (folder_builder.Count == 0)
                    folder_builder.Add("My Bookshelf", new User_Folder("My Bookshelf", -1));

                return new ReadOnlyCollection<User_Folder>(folder_builder.Values);
            }
        }

        /// <summary> Removes an item from the list of items in the user's bookshelves </summary>
        /// <param name="BibID"> BibID for this item in a bookshelf</param>
        /// <param name="VID"> VID for this item in a bookshelf</param>
        public void Remove_From_Bookshelves(string BibID, string VID)
        {
            string objID = BibID.ToUpper() + "_" + VID;
            if (Bookshelf_Items.Contains(objID))
                Bookshelf_Items.Remove(objID);
        }

        /// <summary> Checks to see if an item exists in this user's bookshelf </summary>
        /// <param name="BibID"> BibID for this item in a bookshelf</param>
        /// <param name="VID"> VID for this item in a bookshelf</param>
        /// <returns> TRUE if the item is in the bookshelf, otherwise FALSE </returns>
        public bool Is_In_Bookshelf(string BibID, string VID)
        {
            return Bookshelf_Items.Contains(BibID.ToUpper() + "_" + VID);
        }

        /// <summary> Sets the flag that a particular aggregation exists on this user's home page </summary>
        /// <param name="Code"> Code for this item aggregation </param>
        /// <param name="Name"> Name of this item aggregation </param>
        /// <param name="Flag"> New flag </param>
        public void Set_Aggregation_Home_Page_Flag(string Code, string Name, bool Flag)
        {
            string aggrCodeUpper = Code.ToUpper();
            if (aggregationPermissions.Aggregations != null)
            {
                foreach (User_Permissioned_Aggregation thisAggregation in aggregationPermissions.Aggregations.Where(ThisAggregation => ThisAggregation.Code == aggrCodeUpper))
                {
                    thisAggregation.OnHomePage = Flag;
                    return;
                }
            }

            if (Flag)
            {
                aggregationPermissions.Add(Code, Name, false, false, false, false, false, false, false, false, true, false, false);
            }
        }

        /// <summary> Checks to see if an aggregation is currently listed on the user's personalized home page </summary>
        /// <param name="AggregationCode"> Code for this item aggregation </param>
        /// <returns> TRUE if on the home page currently, otherwise FALSE </returns>
        public bool Is_On_Home_Page(string AggregationCode)
        {
            string aggrCodeUpper = AggregationCode.ToUpper();
            if (aggregationPermissions != null && aggregationPermissions.Aggregations != null)
                return (from thisAggregation in aggregationPermissions.Aggregations where thisAggregation.Code == aggrCodeUpper select thisAggregation.OnHomePage).FirstOrDefault();
            return false;
        }

        /// <summary> Checks to see if this user can perform curatorial tasks against an item aggregation </summary>
        /// <param name="AggregationCode"> Code for this item aggregation </param>
        /// <returns> TRUE if this user is curator on either this aggregation or all of this library, otherwise FALSE </returns>
        public bool Is_Aggregation_Curator(string AggregationCode)
        {
            if ((Is_System_Admin) || (Is_Portal_Admin))
                return true;

            string aggrCodeUpper = AggregationCode.ToUpper();
            if (aggregationPermissions != null && aggregationPermissions.Aggregations != null)
                return (from thisAggregation in aggregationPermissions.Aggregations where thisAggregation.Code == aggrCodeUpper select thisAggregation.IsCurator).FirstOrDefault();
            return false;
        }

        /// <summary> Checks to see if this user can perform administrative tasks against an item aggregation </summary>
        /// <param name="AggregationCode"> Code for this item aggregation </param>
        /// <returns> TRUE if this user is admin on either this aggregation or all of this library, otherwise FALSE </returns>
        public bool Is_Aggregation_Admin(string AggregationCode)
        {
            if ((Is_System_Admin) || (Is_Portal_Admin))
                return true;

            string aggrCodeUpper = AggregationCode.ToUpper();

            if (aggregationPermissions != null && aggregationPermissions.Aggregations != null)
                return (from thisAggregation in aggregationPermissions.Aggregations where thisAggregation.Code == aggrCodeUpper select thisAggregation.IsAdmin).FirstOrDefault();
            return false;
        }

        /// <summary> Checks to see if this user can edit all the items within this aggregation </summary>
        /// <param name="AggregationCode"> Code for this item aggregation </param>
        /// <returns> TRUE if this user is set to edit all items either this aggregation or all of this library, otherwise FALSE </returns>
        public bool Can_Edit_All_Items(string AggregationCode)
        {
            if (Is_System_Admin)
                return true;

            string aggrCodeUpper = AggregationCode.ToUpper();

            if (aggregationPermissions != null && aggregationPermissions.Aggregations != null)
                return (from thisAggregation in aggregationPermissions.Aggregations where thisAggregation.Code == aggrCodeUpper select thisAggregation.CanEditItems).FirstOrDefault();
            return false;
        }

        /// <summary> This checks that the folder name exists, and returns the proper format </summary>
        /// <param name="NameVersion"> Version of the folder name to check </param>
        /// <returns> Folder name in proper format </returns>
        public string Folder_Name(string NameVersion)
        {
            User_Folder folderObject = Get_Folder(NameVersion);
            return folderObject == null ? String.Empty : folderObject.Folder_Name;
        }

        private void recurse_through_children(User_Folder ParentFolder, SortedList<string, User_Folder> FolderBuilder)
        {
            if (ParentFolder.Child_Count > 0)
            {
                foreach (User_Folder thisFolder in ParentFolder.Children)
                {
                    FolderBuilder.Add(thisFolder.Folder_Name, thisFolder);
                    recurse_through_children(thisFolder, FolderBuilder);
                }
            }
        }

        /// <summary> Get a folder obejct by folder name </summary>
        /// <param name="Folder_Name"> Name of the folder object to retrieve</param>
        /// <returns> Folder object by name </returns>
        public User_Folder Get_Folder(string Folder_Name)
        {
            string name_version_lower = Folder_Name.ToLower();
            return folders.Values.Select(ThisFolder => recurse_to_get_folder(ThisFolder, name_version_lower)).FirstOrDefault(ReturnValue => ReturnValue != null);
        }

        private User_Folder recurse_to_get_folder(User_Folder ParentFolder, string FolderName)
        {
            if (ParentFolder.Folder_Name.ToLower() == FolderName)
                return ParentFolder;
            if (ParentFolder.Children != null)
                return ParentFolder.Children.Select(ChildFolder => recurse_to_get_folder(ChildFolder, FolderName)).FirstOrDefault(ReturnValue => ReturnValue != null);
            return null;
        }

        #endregion

        #region public methods for modifying the collections of editable objects ( bibid, templates, projects, aggregationPermissions, etc..)

        /// <summary> Clear all the user groups associated with this user  </summary>
        public void Clear_UserGroup_Membership()
        {
            User_Groups.Clear();
        }

        /// <summary> Adds a user group to the list of user groups this user belongs to </summary>
        /// <param name="GroupName"> Name of the user group</param>
        public void Add_User_Group(int UserGroupID, string GroupName)
        {
            foreach (Simple_User_Group_Info existing in User_Groups)
            {
                if (existing.UserGroupID == UserGroupID)
                    return;
            }
            User_Groups.Add(new Simple_User_Group_Info(UserGroupID, GroupName));
        }

        /// <summary> Add an item to the list of items on the bookshelf for this user </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for this item </param>
        /// <param name="VID"> Volume identifier (VID) for this item </param>
        public void Add_Bookshelf_Item(string BibID, string VID)
        {
            string objid = BibID.ToUpper() + "_" + VID;
            if (!Bookshelf_Items.Contains(objid))
                Bookshelf_Items.Add(objid);
        }

        /// <summary> Clear the list of aggregation permissions associated with this user </summary>
        public void Clear_Aggregations()
        {
            aggregationPermissions.Clear();
        }

        /// <summary> Add a new item aggregation to this user's collection of item aggregationPermissions </summary>
        /// <param name="Code">Code for this user editable item aggregation</param>
        /// <param name="Name">Name for this user editable item aggregation </param>
        /// <param name="CanSelect">Flag indicates if this user can add items to this item aggregation</param>
        /// <param name="CanDelete"> Flag indicates if the user can delete items in this aggregation  </param>
        /// <param name="IsCurator"> Flag indicates if this user is listed as the curator or collection manager for this given digital aggregation </param>
        /// <param name="OnHomePage"> Flag indicates if this user has asked to have this aggregation appear on their personalized home page</param>
        /// <param name="IsAdmin"> Flag indicates if this user is listed athe admin for this aggregation </param>
        /// <param name="CanEditMetadata"> Flag indicates if the user can edit metadata for all items in this aggregation </param>
        /// <param name="CanEditBehaviors"> Flag indicates if the user can edit behaviors for all items in this aggregation  </param>
        /// <param name="CanPerformQc"> Flag indicates if the user can edit perform quality control for all items in this aggregation  </param>
        /// <param name="CanUploadFiles"> Flag indicates if the user can edit upload files for all items in this aggregation  </param>
        /// <param name="CanChangeVisibility"> Flag indicates if the user can change the visibility for all items in this aggregation  </param>
        /// <param name="GroupDefined"> Flag indicates if these permissions are derived from the group </param>
        public void Add_Aggregation(string Code, string Name, bool CanSelect, bool CanEditMetadata, bool CanEditBehaviors, bool CanPerformQc, bool CanUploadFiles, bool CanChangeVisibility, bool CanDelete, bool IsCurator, bool OnHomePage, bool IsAdmin, bool GroupDefined)
        {
            aggregationPermissions.Add(Code, Name, CanSelect, CanEditMetadata, CanEditBehaviors, CanPerformQc, CanUploadFiles, CanChangeVisibility, CanDelete, IsCurator, OnHomePage, IsAdmin, GroupDefined);
        }

        /// <summary> Adds a BibID to the list of bibid's this user can edit </summary>
        /// <param name="BibID">New BibID this user can edit</param>
        public void Add_BibID(string BibID)
        {
            BibIDs.Add(BibID);
        }

        /// <summary> Clears the list of templates associated with this user </summary>
        public void Clear_Templates()
        {
            Templates.Clear();
        }

        /// <summary> Adds a template to the list of templates this user can select </summary>
        /// <param name="Template">Code for this template</param>
        /// <param name="Group_Defined"> Indicates if this user has permissions to use this template through group membership </param>
        /// <remarks>This must match the name of one of the template XML files in the mySobek\templates folder</remarks>
        public void Add_Template(string Template, bool Group_Defined)
        {
            Templates.Add(Template);
            if (Group_Defined)
                templates_from_groups.Add(Template);
        }

        /// <summary> Sets the default template for this user </summary>
        /// <param name="Template">Code for this template</param>
        /// <remarks>This only sets this as the default template if it currently exists in the list of possible templates for this user </remarks>
        public void Set_Default_Template(string Template)
        {
            if (!Templates.Contains(Template) || Templates.IndexOf(Template) == 0) return;
            Templates.Remove(Template);
            Templates.Insert(0, Template);
        }

        /// <summary> Clears all default metadata sets associated with this user </summary>
        public void Clear_Default_Metadata_Sets()
        {
            Default_Metadata_Sets.Clear();
        }

        /// <summary> Adds a default metadata set to the list of sets this user can select </summary>
        /// <param name="MetadataSet">Code for this default metadata set</param>
        /// <param name="Group_Defined"> Defined at the user group level (versus at the instance level) </param>
        /// <remarks>This must match the name of one of the project METS (.pmets) files in the mySobek\projects folder</remarks>
        public void Add_Default_Metadata_Set(string MetadataSet, bool Group_Defined)
        {
            Default_Metadata_Sets.Add(MetadataSet);
            if (Group_Defined)
                defaultMetadataSetsFromGroups.Add(MetadataSet);
        }

        /// <summary> Sets the current default metadata set for this user </summary>
        /// <param name="MetadataSet">Code for this default metadata set</param>
        /// <remarks>This only sets this as the default metadata set if it currently exists in the list of possible projects for this user </remarks>
        public void Set_Current_Default_Metadata(string MetadataSet)
        {
            if (!Default_Metadata_Sets.Contains(MetadataSet) || Default_Metadata_Sets.IndexOf(MetadataSet) == 0) return;
            Default_Metadata_Sets.Remove(MetadataSet);
            Default_Metadata_Sets.Insert(0, MetadataSet);
        }

        /// <summary> Adds a regular expression to this user to determine which titles this user can edit </summary>
        /// <param name="Regular_Expression"> Regular expression used to compute if this user can edit a title, by BibID</param>
        public void Add_Editable_Regular_Expression(string Regular_Expression)
        {
            Editable_Regular_Expressions.Add(Regular_Expression);
        }

        /// <summary> Adds a folder to the list of folders associated with this user </summary>
        /// <param name="Folder"> Built folder object </param>
        public void Add_Folder(User_Folder Folder)
        {
            folders[Folder.Folder_Name] = Folder;
        }

        /// <summary> Adds a folder name to the list of folders associated with this user </summary>
        /// <param name="Folder_Name"> Name of the folder to add </param>
        /// <param name="Folder_ID"> Primary key for this folder </param>
        public void Add_Folder(string Folder_Name, int Folder_ID)
        {
            folders[Folder_Name] = new User_Folder(Folder_Name, Folder_ID);
        }

        /// <summary> Removes a folder name from the list of folders associated with this user </summary>
        /// <param name="Folder_Name"> Name of the folder to remove </param>
        public void Remove_Folder(string Folder_Name)
        {
            string delete_name_lower = Folder_Name.ToLower();
            for (int i = 0; i < folders.Count; i++)
            {
                if (folders.Values[i].Folder_Name.ToLower() != delete_name_lower) continue;

                folders.RemoveAt(i);
                break;
            }
        }

        /// <summary> Clear all the folders linked to this user object </summary>
        public void Clear_Folders()
        {
            folders.Clear();
        }

        #endregion

        /// <summary> Set a value on this user object, based on the user object attribute mapping enumeration </summary>
        /// <param name="Mapping"> Field to set in this user object </param>
        /// <param name="Value"> Value to set that field to </param>
	    public void Set_Value_By_Mapping(User_Object_Attribute_Mapping_Enum Mapping, string Value)
        {
            switch (Mapping)
            {
                case User_Object_Attribute_Mapping_Enum.Username:
                    UserName = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Email:
                    Email = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Firstname:
                    Given_Name = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Lastname:
                    Family_Name = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Nickname:
                    Nickname = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Notes:
                    Internal_Notes = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Organization:
                    Organization = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.OrgCode:
                    Organization_Code = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.College:
                    College = Value;
                    break;

                case User_Object_Attribute_Mapping_Enum.Department:
                    Department = Value;
                    break;
            }
        }

        /// <summary> Gets a value from this user object, based on the user object attribute mapping enumeration </summary>
        /// <param name="Mapping"> Field to get from this user object </param>
        public string Get_Value_By_Mapping(User_Object_Attribute_Mapping_Enum Mapping)
        {
            switch (Mapping)
            {
                case User_Object_Attribute_Mapping_Enum.Username:
                    return UserName;

                case User_Object_Attribute_Mapping_Enum.Email:
                    return Email;

                case User_Object_Attribute_Mapping_Enum.Firstname:
                    return Given_Name;

                case User_Object_Attribute_Mapping_Enum.Lastname:
                    return Family_Name;

                case User_Object_Attribute_Mapping_Enum.Nickname:
                    return Nickname;

                case User_Object_Attribute_Mapping_Enum.Fullname:
                    return Full_Name;

                case User_Object_Attribute_Mapping_Enum.Notes:
                    return Internal_Notes;

                case User_Object_Attribute_Mapping_Enum.Organization:
                    return Organization;

                case User_Object_Attribute_Mapping_Enum.OrgCode:
                    return Organization_Code;

                case User_Object_Attribute_Mapping_Enum.College:
                    return College;

                case User_Object_Attribute_Mapping_Enum.Department:
                    return Department;
            }

            return String.Empty;
        }

        /// <summary> Determines if this user can edit this item, based on several different criteria </summary>
        /// <param name="BibID"> BibID for the item </param>
        /// <param name="ItemType"> Type of the item </param>
        /// <param name="SourceCode"> Source code for the item </param>
        /// <param name="HoldingCode"> Holding code for the item </param>
        /// <param name="Aggregations"> List  of all aggregations codes linked to the item </param>
        /// <returns>TRUE if the user can edit this item, otherwise FALSE</returns>
        public bool Can_Edit_This_Item(string BibID, string ItemType, string SourceCode, string HoldingCode, ICollection<string> Aggregations)
        {
            //if (!InstanceWide_Settings_Singleton.Settings.Online_Edit_Submit_Enabled)
            //    return false;

            if (String.Compare(ItemType, "PROJECT", StringComparison.OrdinalIgnoreCase) == 0)
                return Is_Portal_Admin;

            if ((Is_Portal_Admin) || (Is_System_Admin))
                return true;

            if (BibIDs.Contains(BibID.ToUpper()))
                return true;

            if ((aggregationPermissions["I" + SourceCode.ToUpper()] != null) && (aggregationPermissions["I" + SourceCode.ToUpper()].CanEditMetadata))
                return true;

            if ((aggregationPermissions["I" + HoldingCode.ToUpper()] != null) && (aggregationPermissions["I" + HoldingCode.ToUpper()].CanEditMetadata))
                return true;

            if ((aggregationPermissions.Aggregations != null) && (Aggregations != null))
            {
                foreach (string thisAggr in Aggregations)
                {
                    if ((aggregationPermissions[thisAggr] != null) && (aggregationPermissions[thisAggr].CanEditMetadata))
                        return true;
                }
            }

            return Editable_Regular_Expressions.Select(RegexString => new Regex(RegexString)).Any(MyReg => MyReg.IsMatch(BibID.ToUpper()));
        }


        /// <summary> Determines if this user can edit this item, based on several different criteria </summary>
        /// <param name="BibID"> BibID for the item to check </param>
        /// <param name="SourceCode"> Source code for the item </param>
        /// <param name="HoldingCode"> Holding code for the item </param>
        /// <param name="Aggregations"> List  of all aggregations codes linked to the item </param>
        /// <returns>TRUE if the user can edit this item, otherwise FALSE</returns>
        public bool Can_Delete_This_Item(string BibID, string SourceCode, string HoldingCode, ICollection<string> Aggregations)
        {
            if ((Can_Delete_All) || (Is_System_Admin))
                return true;

            if ((aggregationPermissions["I" + SourceCode.ToUpper()] != null) && (aggregationPermissions["I" + SourceCode.ToUpper()].CanDelete))
                return true;

            if ((aggregationPermissions["I" + HoldingCode.ToUpper()] != null) && (aggregationPermissions["I" + HoldingCode.ToUpper()].CanDelete))
                return true;

            if ((aggregationPermissions.Aggregations != null) && (Aggregations != null))
            {
                foreach (string thisAggr in Aggregations)
                {
                    if ((aggregationPermissions[thisAggr] != null) && (aggregationPermissions[thisAggr].CanDelete))
                        return true;
                }
            }

            return false;
        }

        /// <summary> Returns the security hash based on IP for this user </summary>
        /// <param name="IP">IP Address for this user request</param>
        /// <returns>Security hash for comparison purposes or for encoding in the cookie</returns>
        /// <remarks>This is used to add another level of security on cookies coming in from a user request </remarks>
        public string Security_Hash(string IP)
        {
            string key = IP.Replace(".", "").PadRight(8, '%').Substring(0, 8);
            string source = Given_Name + "sobekh" + Family_Name;

            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(source));
            return Convert.ToBase64String(hash);
        }

    }
}
