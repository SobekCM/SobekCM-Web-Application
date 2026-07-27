using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Configuration information related to authentication and logging on
    /// through the web application </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("AuthenticationConfig")]
    public class Authentication_Configuration
    {
        /// <summary> Constructor for a new instance of the Authentication_Configuration class </summary>
        public Authentication_Configuration()
        {
            //Shibboleth = new Shibboleth_Configuration();
            Oidc = new List<Oidc_Configuration>();
            Saml = new List<Saml_Configuration>();
            AllowLocalAuth = true;
        }

        /// <summary> Flag indicates whether the built-in SobekCM username/password authentication (registration
        /// and logon against the local database) is allowed. Defaults to TRUE - only disabled when the
        /// "AllowLocalAuth" attribute is explicitly present and set to "false" on the &lt;Authentication&gt;
        /// element, e.g. for an instance that only wants to allow sign-in through OIDC/SAML </summary>
        [DataMember(Name = "allowLocalAuth", EmitDefaultValue = false)]
        [XmlAttribute("allowLocalAuth")]
        [ProtoMember(4)]
        public bool AllowLocalAuth { get; set; }

        /// <summary> Configuration information related to using Shibboleth configuration </summary>
        [DataMember(Name = "dropbox", EmitDefaultValue = false)]
        [XmlElement("shibboleth")]
        [ProtoMember(1)]
        public Shibboleth_Configuration Shibboleth { get; set; }

        /// <summary> Configuration for each registered OpenID Connect identity provider </summary>
        [DataMember(Name = "oidc", EmitDefaultValue = false)]
        [XmlArray("oidc")]
        [XmlArrayItem("provider", typeof(Oidc_Configuration))]
        [ProtoMember(2)]
        public List<Oidc_Configuration> Oidc { get; set; }

        /// <summary> Configuration for each registered SAML identity provider </summary>
        [DataMember(Name = "saml", EmitDefaultValue = false)]
        [XmlArray("saml")]
        [XmlArrayItem("provider", typeof(Saml_Configuration))]
        [ProtoMember(3)]
        public List<Saml_Configuration> Saml { get; set; }
    }
}
