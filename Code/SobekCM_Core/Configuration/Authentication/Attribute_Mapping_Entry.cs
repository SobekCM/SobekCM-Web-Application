using ProtoBuf;
using SobekCM.Core.Users;
using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Stores the link between a mapping to a user object attribute and a string value, used
    /// to configure claim mapping for any federated authentication provider (OIDC, SAML, ...) </summary>
    /// <remarks> Generic sibling of <see cref="Shibboleth_Configuration_Mapping"/> — introduced so new
    /// provider config doesn't depend on the Shibboleth-specific class; Shibboleth's own config is left
    /// untouched. Used to hold both constants (a fixed value -> mapping) and claim mapping (an incoming
    /// claim/attribute name -> mapping) </remarks>
    [Serializable, DataContract, ProtoContract]
    public class Attribute_Mapping_Entry
    {
        /// <summary> Indicates a mapping to an attribute under the user class </summary>
        [DataMember(Name = "attribute", EmitDefaultValue = false)]
        [XmlAttribute("attribute")]
        [ProtoMember(1)]
        public User_Object_Attribute_Mapping_Enum Mapping { get; set; }

        /// <summary> Value, either a constant to put in the user via the mapping, or the incoming
        /// claim/attribute name, used to find a user-specific value and map to the user class </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        [XmlAttribute("value")]
        [ProtoMember(2)]
        public string Value { get; set; }

        /// <summary> Constructor for a new instance of the Attribute_Mapping_Entry class </summary>
        public Attribute_Mapping_Entry()
        {
            // Empty constructor for serialization purposes
        }

        /// <summary> Constructor for a new instance of the Attribute_Mapping_Entry class </summary>
        /// <param name="Mapping"> Indicates a mapping to an attribute under the user class </param>
        /// <param name="Value"> Value, either a constant to put in the user via the mapping, or the
        /// incoming claim/attribute name, used to find a user-specific value and map to the user class </param>
        public Attribute_Mapping_Entry(User_Object_Attribute_Mapping_Enum Mapping, string Value)
        {
            this.Mapping = Mapping;
            this.Value = Value;
        }
    }
}
