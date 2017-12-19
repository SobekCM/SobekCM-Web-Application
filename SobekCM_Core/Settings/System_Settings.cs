using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using ProtoBuf;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;

namespace SobekCM.Core.Settings
{
    /// <summary> Eumeration of the two different search systems </summary>
    public enum Search_System_Enum : byte
    {
        /// <summary> Legacy search system used up through verrsion 4 </summary>
        Legacy = 0,

        /// <summary> Beta of the search system that will be used version 5 </summary>
        Beta
    }

    /// <summary> Top-level settings that control basic operation and appearance of the entire SobekCM instance </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("SystemSettings")]
    public class System_Settings
    {
        /// <summary> Constructor for a new instance of the System_Settings class </summary>
        public System_Settings()
        {
            Include_Partners_On_System_Home = false;
            Include_TreeView_On_System_Home = false;
            Default_UI_Language = Web_Language_Enum.English;
            Metadata_Help_URL_Base = String.Empty;
            Help_URL_Base = String.Empty;

            Search_System = Search_System_Enum.Legacy;
        }

        /// <summary> Returns the default user interface language </summary>
        [DataMember(Name = "defaultUiLanguage")]
        [XmlElement("defaultUiLanguage")]
        [ProtoMember(1)]
        public Web_Language_Enum Default_UI_Language { get; set; }

        /// <summary> Flag determines if the detailed view of user permissions for items in an aggregation should show </summary>
        [DataMember(Name = "detailedUserAggregationPermissions")]
        [XmlElement("detailedUserAggregationPermissions")]
        [ProtoMember(2)]
        public bool Detailed_User_Aggregation_Permissions { get; set; }

        /// <summary> Flag indicates if logon has been restricted to system admins </summary>
        [DataMember(Name = "disableStandardUserLogonFlag")]
        [XmlElement("disableStandardUserLogonFlag")]
        [ProtoMember(3)]
        public bool Disable_Standard_User_Logon_Flag { get; set; }

        /// <summary> Message to go with the logon restriction </summary>
        [DataMember(Name = "disableStandarduserLogonMessage", EmitDefaultValue = false)]
        [XmlElement("disableStandarduserLogonMessage")]
        [ProtoMember(4)]
        public string Disable_Standard_User_Logon_Message { get; set; }

        /// <summary> Flag indicates if the partners browse should be displayed on the home page </summary>
        [DataMember(Name = "includePartnersOnSystemHome")]
        [XmlElement("includePartnersOnSystemHome")]
        [ProtoMember(5)]
        public bool Include_Partners_On_System_Home { get; set; }

        /// <summary> Flag indicates if the tree view should be displayed on the home page </summary>
        [DataMember(Name = "includeTreeViewOnSystemHome")]
        [XmlElement("includeTreeViewOnSystemHome")]
        [ProtoMember(6)]
        public bool Include_TreeView_On_System_Home { get; set; }

        /// <summary> Gets the abbrevation used to refer to this digital library </summary>
        [DataMember(Name = "systemAbbreviation")]
        [XmlElement("systemAbbreviation")]
        [ProtoMember(7)]
        public string System_Abbreviation { get; set; }

        /// <summary> Gets the base name for this system </summary>
        [DataMember(Name = "systemName")]
        [XmlElement("systemName")]
        [ProtoMember(8)]
        public string System_Name { get; set; }

        /// <summary> List of possible page image extensions </summary>
        [DataMember(Name = "pageImageExtensions", EmitDefaultValue = false)]
        [XmlArray("pageImageExtensions")]
        [XmlArrayItem("extenstion", typeof(string))]
        [ProtoMember(9)]
        public List<string> Page_Image_Extensions { get; set; }

        /// <summary> Base URL for the metadata help </summary>
        [DataMember(Name = "metadataHelpUrlBase", EmitDefaultValue = false)]
        [XmlElement("metadataHelpUrlBase")]
        [ProtoMember(10)]
        public string Metadata_Help_URL_Base { get; set; }

        /// <summary> Base URL for most the help pages </summary>
        [DataMember(Name = "helpUrlBase", EmitDefaultValue = false)]
        [XmlElement("helpUrlBase")]
        [ProtoMember(11)]
        public string Help_URL_Base { get; set; }

        /// <summary> Overall search system to be used ( i.e., Legacy or Beta version 5 ) </summary>
        [DataMember(Name = "searchSystem", EmitDefaultValue = false)]
        [XmlElement("searchSystem")]
        [ProtoMember(17)]
        public Search_System_Enum Search_System { get; set; }

        /// <summary> Set the search system, by passing in a string </summary>
        [XmlIgnore]
        public string Search_System_String
        {
            set {
                Search_System = String.Equals(value, "beta", StringComparison.OrdinalIgnoreCase) ? Search_System_Enum.Beta : Search_System_Enum.Legacy;
            }
            get
            {
                switch (Search_System)
                {
                    case Search_System_Enum.Beta:
                        return "Beta";

                    default:
                        return "Legacy";
                }
            }
        }

        /// <summary> Get the URL for all metadata help pages which are used when users request 
        /// help while submitting a new item or editing an existing item </summary>
        /// <param name="Current_Base_URL"> Current base url for the current user's request </param>
        /// <returns> Base URL to use for the metadata help page links </returns>
        public string Metadata_Help_URL(string Current_Base_URL)
        {
            return String.IsNullOrEmpty(Metadata_Help_URL_Base) ? Current_Base_URL : Help_URL_Base;
        }

        /// <summary> URL used for the main help pages about this system's basic functionality </summary>
        /// <param name="Current_Base_URL"> Current base url for the current user's request </param>
        /// <returns> Base URL to use for the main help page links </returns>
        public string Help_URL(string Current_Base_URL)
        {
            return String.IsNullOrEmpty(Help_URL_Base) ? Current_Base_URL : Help_URL_Base; 
        }

        /// <summary> API Key for the Google Map display features within SobekCM </summary>
        [DataMember(Name = "googleMapApiKey", EmitDefaultValue = false)]
        [XmlElement("googleMapApiKey")]
        [ProtoMember(12)]
        public string Google_Map_API_Key { get; set; }

        /// <summary> Custom regular expression used to check if a string is a valid BibID </summary>
        [DataMember(Name = "customBibIdRegex", EmitDefaultValue = false)]
        [XmlElement("customBibIdRegex")]
        [ProtoMember(13)]
        public string Custom_BibID_RegEx { get; set; }

        /// <summary> Set the default UI language, by passing in a string </summary>
        [XmlIgnore]
        public string Default_UI_Language_String
        {
            set
            {
                Default_UI_Language = Web_Language_Enum_Converter.Code_To_Enum(Web_Language_Enum_Converter.Name_To_Code(value));
            }
            get { return Web_Language_Enum_Converter.Enum_To_Name(Default_UI_Language); }
        }
        
        #region Methods that controls XML serialization

        /// <summary> Method suppresses XML Serialization of the Disable_Standard_User_Logon_Message property if it is empty </summary>
        /// <returns> TRUE if the property should be serialized, otherwise FALSE </returns>
        public bool ShouldSerializeDisable_Standard_User_Logon_Message()
        {
            return (!String.IsNullOrEmpty(Disable_Standard_User_Logon_Message)) && (Disable_Standard_User_Logon_Flag = true);
        }

        #endregion
    }
}
