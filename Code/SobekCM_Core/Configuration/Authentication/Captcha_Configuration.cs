using ProtoBuf;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Configuration for the captcha (currently only Cloudflare Turnstile) shown on the anonymous
    /// forms - self-registration and the contact form </summary>
    [Serializable, DataContract, ProtoContract]
    [XmlRoot("CaptchaConfig")]
    public class Captcha_Configuration
    {
        /// <summary> Constructor for a new instance of the Captcha_Configuration class </summary>
        public Captcha_Configuration()
        {
            Provider = "turnstile";
            SiteKey = String.Empty;
            SecretKey = String.Empty;
            Enabled = true;
        }

        /// <summary> Captcha provider. Only "turnstile" (Cloudflare Turnstile) is supported </summary>
        [DataMember(Name = "provider")]
        [XmlAttribute("provider")]
        [ProtoMember(1)]
        public string Provider { get; set; }

        /// <summary> Flag indicates if the captcha is turned on. Defaults to TRUE, but the captcha is still
        /// not shown unless both keys are present - see <see cref="Is_Active"/> </summary>
        [DataMember(Name = "enabled")]
        [XmlAttribute("enabled")]
        [ProtoMember(2)]
        public bool Enabled { get; set; }

        /// <summary> Public site key, written into the page for the widget </summary>
        [DataMember(Name = "siteKey")]
        [XmlAttribute("siteKey")]
        [ProtoMember(3)]
        public string SiteKey { get; set; }

        /// <summary> Secret key used to verify each response with the provider. Never serialized to XML or JSON </summary>
        [XmlIgnore]
        [ProtoMember(4)]
        public string SecretKey { get; set; }

        /// <summary> Flag indicates if the captcha should actually be shown and enforced: it is enabled, the
        /// provider is supported, and both keys are configured </summary>
        [XmlIgnore]
        [IgnoreDataMember]
        public bool Is_Active =>
            Enabled &&
            String.Equals(Provider, "turnstile", StringComparison.OrdinalIgnoreCase) &&
            !String.IsNullOrWhiteSpace(SiteKey) &&
            !String.IsNullOrWhiteSpace(SecretKey);
    }
}
