using ProtoBuf;
using SobekCM.Core.Users;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Configuration for a single SAML identity provider (via Sustainsys.Saml2). One instance
    /// of this class is registered per configured IdP; <see cref="Provider_Code"/> is used both as the
    /// ASP.NET Core authentication scheme name and as the URL segment identifying this provider
    /// (e.g. "/my/saml/{Provider_Code}") </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("SamlProviderConfig")]
    public class Saml_Configuration
    {
        [XmlIgnore]
        private Dictionary<string, User_Object_Attribute_Mapping_Enum> attributeMappingDictionary;

        /// <summary> Constructor for a new instance of the Saml_Configuration class </summary>
        public Saml_Configuration()
        {
            AttributeMapping = new List<Attribute_Mapping_Entry>();
            Constants = new List<Attribute_Mapping_Entry>();

            Provider_Code = String.Empty;
            Display_Label = String.Empty;
            IdpMetadataUrl = String.Empty;
            EntityId = String.Empty;
            Enabled = true;
        }

        /// <summary> Unique code for this provider — used as the authentication scheme name and as
        /// the "/my/saml/{Provider_Code}" URL segment </summary>
        [DataMember(Name = "providerCode")]
        [XmlAttribute("providerCode")]
        [ProtoMember(1)]
        public string Provider_Code { get; set; }

        /// <summary> Label for this provider, displayed to the user on the logon page </summary>
        [DataMember(Name = "label")]
        [XmlAttribute("label")]
        [ProtoMember(2)]
        public string Display_Label { get; set; }

        /// <summary> Flag indicates if this provider is currently enabled </summary>
        [DataMember(Name = "enabled")]
        [XmlAttribute("enabled")]
        [ProtoMember(3)]
        public bool Enabled { get; set; }

        /// <summary> URL to the identity provider's SAML metadata document </summary>
        [DataMember(Name = "idpMetadataUrl")]
        [XmlAttribute("idpMetadataUrl")]
        [ProtoMember(4)]
        public string IdpMetadataUrl { get; set; }

        /// <summary> Entity ID for this application (the service provider) as registered with the identity provider </summary>
        [DataMember(Name = "entityId")]
        [XmlAttribute("entityId")]
        [ProtoMember(5)]
        public string EntityId { get; set; }

        /// <summary> List of all the constants to assign to a new user established through this provider </summary>
        [DataMember(Name = "constants", EmitDefaultValue = false)]
        [XmlArray("constants")]
        [XmlArrayItem("constant", typeof(Attribute_Mapping_Entry))]
        [ProtoMember(6)]
        public List<Attribute_Mapping_Entry> Constants { get; private set; }

        /// <summary> Add a new constant mapping for all new users established using this provider </summary>
        /// <param name="UserAttribute"> Attribute within the SobekCM user object </param>
        /// <param name="ConstantValue"> Constant value to apply for all new users established using this provider </param>
        public void Add_Constant(User_Object_Attribute_Mapping_Enum UserAttribute, string ConstantValue)
        {
            Constants.Add(new Attribute_Mapping_Entry(UserAttribute, ConstantValue));
        }

        /// <summary> List of all the attribute mapping, where SAML assertion attributes are mapped to
        /// the SobekCM user object </summary>
        [DataMember(Name = "attributeMapping", EmitDefaultValue = false)]
        [XmlArray("attributeMapping")]
        [XmlArrayItem("mapping", typeof(Attribute_Mapping_Entry))]
        [ProtoMember(7)]
        public List<Attribute_Mapping_Entry> AttributeMapping { get; private set; }

        /// <summary> Add a new mapping between a SAML assertion attribute and a user attribute </summary>
        /// <param name="AttributeName"> Attribute name/URI from the SAML assertion </param>
        /// <param name="UserAttribute"> Attribute within the SobekCM user object </param>
        public void Add_Attribute_Mapping(string AttributeName, User_Object_Attribute_Mapping_Enum UserAttribute)
        {
            AttributeMapping.Add(new Attribute_Mapping_Entry(UserAttribute, AttributeName));
            attributeMappingDictionary = null;
        }

        /// <summary> Get the mapping from a SAML assertion attribute name into the new user object </summary>
        /// <param name="AttributeName"> Attribute name/URI from the SAML assertion </param>
        /// <returns> Mapping into the user object ( or NONE ) </returns>
        public User_Object_Attribute_Mapping_Enum Get_User_Object_Mapping(string AttributeName)
        {
            return Attribute_Mapping_Helper.Get_Mapping(AttributeMapping, ref attributeMappingDictionary, AttributeName);
        }
    }
}
