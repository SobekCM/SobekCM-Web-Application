using ProtoBuf;
using System;
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
        }

        /// <summary> Configuration information related to using Shibboleth configuration </summary>
        [DataMember(Name = "dropbox", EmitDefaultValue = false)]
        [XmlElement("shibboleth")]
        [ProtoMember(1)]
        public Shibboleth_Configuration Shibboleth { get; set; }


    }
}
