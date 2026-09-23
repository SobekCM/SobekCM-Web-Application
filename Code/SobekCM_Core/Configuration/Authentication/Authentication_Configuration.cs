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
            AllowSelfRegistration = true;
            ShowLocalLogon = true;
        }

        /// <summary> Master switch for the built-in SobekCM username/password authentication (registration
        /// and logon against the local database). Defaults to TRUE - only disabled when the "AllowLocalAuth"
        /// attribute is explicitly present and set to "false" on the &lt;Authentication&gt; element.  When
        /// FALSE, no local account can log on anywhere (including the hidden my/logon/local page) and
        /// self-registration is off, regardless of <see cref="AllowSelfRegistration"/> and <see cref="ShowLocalLogon"/> </summary>
        [DataMember(Name = "allowLocalAuth", EmitDefaultValue = false)]
        [XmlAttribute("allowLocalAuth")]
        [ProtoMember(4)]
        public bool AllowLocalAuth { get; set; }

        /// <summary> Flag indicates whether new users can register a local account themselves (the "Register Now"
        /// links and the register viewers).  Defaults to TRUE.  Only has any effect while <see cref="AllowLocalAuth"/>
        /// is TRUE - use <see cref="Self_Registration_Enabled"/> to check both at once </summary>
        [DataMember(Name = "allowSelfRegistration", EmitDefaultValue = false)]
        [XmlAttribute("allowSelfRegistration")]
        [ProtoMember(5)]
        public bool AllowSelfRegistration { get; set; }

        /// <summary> Flag indicates whether the local username/password logon option is offered on the standard
        /// logon screen.  Defaults to TRUE.  When FALSE, local accounts can still log on through the unlinked
        /// my/logon/local page (e.g. for SobekDigital staff or automated smoke tests on an SSO-only instance),
        /// as long as <see cref="AllowLocalAuth"/> is TRUE.  Which local accounts can do so is controlled per-user
        /// with the existing isActive flag on the user record </summary>
        [DataMember(Name = "showLocalLogon", EmitDefaultValue = false)]
        [XmlAttribute("showLocalLogon")]
        [ProtoMember(6)]
        public bool ShowLocalLogon { get; set; }

        /// <summary> Flag indicates if self-registration is actually available, which requires both the
        /// <see cref="AllowLocalAuth"/> master switch and <see cref="AllowSelfRegistration"/> </summary>
        [XmlIgnore]
        [IgnoreDataMember]
        public bool Self_Registration_Enabled => AllowLocalAuth && AllowSelfRegistration;

        /// <summary> Flag indicates if the local logon option should be offered on the standard logon screen,
        /// which requires both the <see cref="AllowLocalAuth"/> master switch and <see cref="ShowLocalLogon"/> </summary>
        [XmlIgnore]
        [IgnoreDataMember]
        public bool Local_Logon_On_Standard_Screen => AllowLocalAuth && ShowLocalLogon;

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
