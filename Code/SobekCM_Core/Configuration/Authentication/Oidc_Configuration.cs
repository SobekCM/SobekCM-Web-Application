using ProtoBuf;
using SobekCM.Core.Users;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Configuration for a single OpenID Connect identity provider. One instance of this class
    /// is registered per configured IdP (e.g. Google, Microsoft Entra, Okta); <see cref="Provider_Code"/>
    /// is used both as the ASP.NET Core authentication scheme name and as the URL segment identifying
    /// this provider (e.g. "/my/oidc/{Provider_Code}") </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("OidcProviderConfig")]
    public class Oidc_Configuration
    {
        [XmlIgnore]
        private Dictionary<string, User_Object_Attribute_Mapping_Enum> attributeMappingDictionary;

        /// <summary> Constructor for a new instance of the Oidc_Configuration class </summary>
        public Oidc_Configuration()
        {
            AttributeMapping = new List<Attribute_Mapping_Entry>();
            Constants = new List<Attribute_Mapping_Entry>();

            Provider_Code = String.Empty;
            Display_Label = String.Empty;
            Authority = String.Empty;
            ClientId = String.Empty;
            ClientSecret = String.Empty;
            Enabled = true;
        }

        /// <summary> Unique code for this provider — used as the authentication scheme name and as
        /// the "/my/oidc/{Provider_Code}" URL segment </summary>
        [DataMember(Name = "providerCode")]
        [XmlAttribute("providerCode")]
        [ProtoMember(1)]
        public string Provider_Code { get; set; }

        /// <summary> Label for this provider, displayed to the user on the logon page (e.g. "Google", "Microsoft") </summary>
        [DataMember(Name = "label")]
        [XmlAttribute("label")]
        [ProtoMember(2)]
        public string Display_Label { get; set; }

        /// <summary> Flag indicates if this provider is currently enabled </summary>
        [DataMember(Name = "enabled")]
        [XmlAttribute("enabled")]
        [ProtoMember(3)]
        public bool Enabled { get; set; }

        /// <summary> OpenID Connect authority (issuer) URL — the app fetches discovery metadata from
        /// "{Authority}/.well-known/openid-configuration" </summary>
        [DataMember(Name = "authority")]
        [XmlAttribute("authority")]
        [ProtoMember(4)]
        public string Authority { get; set; }

        /// <summary> Client ID issued by the identity provider for this application </summary>
        [DataMember(Name = "clientId")]
        [XmlAttribute("clientId")]
        [ProtoMember(5)]
        public string ClientId { get; set; }

        /// <summary> Client secret issued by the identity provider for this application </summary>
        [DataMember(Name = "clientSecret")]
        [XmlAttribute("clientSecret")]
        [ProtoMember(6)]
        public string ClientSecret { get; set; }

        /// <summary> List of all the constants to assign to a new user established through this provider </summary>
        [DataMember(Name = "constants", EmitDefaultValue = false)]
        [XmlArray("constants")]
        [XmlArrayItem("constant", typeof(Attribute_Mapping_Entry))]
        [ProtoMember(7)]
        public List<Attribute_Mapping_Entry> Constants { get; private set; }

        /// <summary> Add a new constant mapping for all new users established using this provider </summary>
        /// <param name="UserAttribute"> Attribute within the SobekCM user object </param>
        /// <param name="ConstantValue"> Constant value to apply for all new users established using this provider </param>
        public void Add_Constant(User_Object_Attribute_Mapping_Enum UserAttribute, string ConstantValue)
        {
            Constants.Add(new Attribute_Mapping_Entry(UserAttribute, ConstantValue));
        }

        /// <summary> List of all the claim mapping, where claims returned by the identity provider are
        /// mapped to the SobekCM user object </summary>
        [DataMember(Name = "attributeMapping", EmitDefaultValue = false)]
        [XmlArray("attributeMapping")]
        [XmlArrayItem("mapping", typeof(Attribute_Mapping_Entry))]
        [ProtoMember(8)]
        public List<Attribute_Mapping_Entry> AttributeMapping { get; private set; }

        /// <summary> Add a new mapping between a claim returned from the identity provider and a user attribute </summary>
        /// <param name="ClaimName"> Claim name from the OIDC token/userinfo response </param>
        /// <param name="UserAttribute"> Attribute within the SobekCM user object </param>
        public void Add_Attribute_Mapping(string ClaimName, User_Object_Attribute_Mapping_Enum UserAttribute)
        {
            AttributeMapping.Add(new Attribute_Mapping_Entry(UserAttribute, ClaimName));
            attributeMappingDictionary = null;
        }

        /// <summary> Get the mapping from a claim name into the new user object </summary>
        /// <param name="ClaimName"> Claim name from the OIDC token/userinfo response </param>
        /// <returns> Mapping into the user object ( or NONE ) </returns>
        public User_Object_Attribute_Mapping_Enum Get_User_Object_Mapping(string ClaimName)
        {
            return Attribute_Mapping_Helper.Get_Mapping(AttributeMapping, ref attributeMappingDictionary, ClaimName);
        }
    }
}
