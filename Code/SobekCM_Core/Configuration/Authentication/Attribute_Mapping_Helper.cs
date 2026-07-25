using SobekCM.Core.Users;
using System;
using System.Collections.Generic;

namespace SobekCM.Core.Configuration.Authentication
{
    /// <summary> Shared lookup logic for provider claim-mapping lists (<see cref="Attribute_Mapping_Entry"/>),
    /// used by <see cref="Oidc_Configuration"/> and <see cref="Saml_Configuration"/> so each doesn't
    /// duplicate its own caching dictionary </summary>
    public static class Attribute_Mapping_Helper
    {
        /// <summary> Get the user object mapping for a given incoming claim/attribute name, rebuilding
        /// the lookup cache first if the backing list has changed since it was last built </summary>
        /// <param name="AttributeMapping"> Configured list of claim-name -> user-attribute mappings </param>
        /// <param name="Cache"> Lookup cache, rebuilt in place when stale (pass by ref) </param>
        /// <param name="AttributeName"> Incoming claim/attribute name to look up </param>
        /// <returns> Mapped user attribute, or NONE if unmapped </returns>
        public static User_Object_Attribute_Mapping_Enum Get_Mapping(List<Attribute_Mapping_Entry> AttributeMapping, ref Dictionary<string, User_Object_Attribute_Mapping_Enum> Cache, string AttributeName)
        {
            if ((Cache == null) || (Cache.Count != AttributeMapping.Count))
            {
                Cache = new Dictionary<string, User_Object_Attribute_Mapping_Enum>(StringComparer.OrdinalIgnoreCase);
                foreach (Attribute_Mapping_Entry entry in AttributeMapping)
                    Cache[entry.Value] = entry.Mapping;
            }

            return Cache.TryGetValue(AttributeName, out User_Object_Attribute_Mapping_Enum mapping) ? mapping : User_Object_Attribute_Mapping_Enum.NONE;
        }
    }
}
