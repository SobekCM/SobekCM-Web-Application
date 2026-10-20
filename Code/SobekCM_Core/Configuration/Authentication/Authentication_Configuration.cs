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

        /// <summary> Flag makes this a private repository: every page, data feed (OAI-PMH, IIIF, JSON, XML, ...),
        /// /files download and /engine call requires a logged-on user.  Defaults to FALSE.  The logon,
        /// registration and SSO sign-in pages, the contact form and error pages stay reachable.  Engine calls
        /// from an IP inside any of the engine's configured restriction ranges (which must already include this
        /// web server, since the application calls its own engine) are still allowed without a logon </summary>
        /// <remarks> Unlike LoginOnlyMode:ManualMode = "Site" in appsettings.json (an emergency load-shedding
        /// switch, which leaves the data feeds open for harvesters), this is meant to stay on permanently </remarks>
        [DataMember(Name = "requireLogon", EmitDefaultValue = false)]
        [XmlAttribute("requireLogon")]
        [ProtoMember(8)]
        public bool RequireLogon { get; set; }

        /// <summary> Provider code of an enabled OIDC or SAML provider that the standard logon screen should send
        /// the user straight to, instead of showing the list of logon options.  NULL or empty (the default) always
        /// shows the logon screen.  The unlinked my/logon/local page is never redirected </summary>
        [DataMember(Name = "autoSignInProvider", EmitDefaultValue = false)]
        [XmlAttribute("autoSignInProvider")]
        [ProtoMember(9)]
        public string AutoSignInProvider { get; set; }

        /// <summary> Configuration information related to using Shibboleth configuration </summary>
        [DataMember(Name = "dropbox", EmitDefaultValue = false)]
        [XmlElement("shibboleth")]
        [ProtoMember(1)]
        public Shibboleth_Configuration Shibboleth { get; set; }

        /// <summary> Captcha configuration for the anonymous forms (self-registration and contact). NULL when no
        /// captcha element is present in the configuration, in which case those forms have no captcha </summary>
        [DataMember(Name = "captcha", EmitDefaultValue = false)]
        [XmlElement("captcha")]
        [ProtoMember(7)]
        public Captcha_Configuration Captcha { get; set; }

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
