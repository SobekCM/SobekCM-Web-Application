using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SobekCM.Tools
{
    /// <summary> Shared JSON serializer configuration, used everywhere Jil used to be. Jil's
    /// InlineDeserializer&lt;T&gt; reflects for TimeSpan.FromSeconds with no parameter-type
    /// disambiguation in its own static type initializer; .NET 7+ added a TimeSpan.FromSeconds(long)
    /// overload alongside the original TimeSpan.FromSeconds(double), so that reflection now throws
    /// AmbiguousMatchException (surfaced as TypeInitializationException) for ANY type deserialized
    /// through Jil, not just ones with a TimeSpan property. </summary>
    /// <remarks> System.Text.Json doesn't natively honor [DataMember(Name = "...")]/EmitDefaultValue
    /// the way Jil did, so this resolver replicates that behavior by reading the DataContract
    /// attributes already present on every DTO in this codebase, preserving the exact wire format
    /// (property names, omitted-when-default fields) instead of requiring every touched class to be
    /// hand-annotated with [JsonPropertyName]. </remarks>
    public static class Json_Options
    {
        /// <summary> Shared options instance -- use for every JsonSerializer.Serialize/Deserialize
        /// call that replaces a former Jil JSON.Serialize/Deserialize call </summary>
        public static readonly JsonSerializerOptions Default = Create();

        private static JsonSerializerOptions Create()
        {
            var resolver = new DefaultJsonTypeInfoResolver();
            resolver.Modifiers.Add(Apply_DataContract_Attributes);

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = resolver
            };

            // Enums default to serializing as their underlying number in System.Text.Json; every DTO
            // in this codebase expects the member name instead (e.g. "InputError", not 2), matching
            // Jil's default behavior
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        private static void Apply_DataContract_Attributes(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            // [DataContract] is opt-in: only members explicitly marked [DataMember] should serialize
            // at all -- everything else (including [IgnoreDataMember] members, and plain unmarked
            // helper properties like the *_AsString/UrlSegments ones scattered through these DTOs)
            // must be excluded entirely, not just left with default System.Text.Json behavior. Types
            // with no [DataContract] (e.g. anonymous types used for small ad hoc JSON responses) are
            // left completely alone.
            bool isDataContract = typeInfo.Type.GetCustomAttributes(typeof(DataContractAttribute), true).Length > 0;

            for (int i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                JsonPropertyInfo property = typeInfo.Properties[i];

                DataMemberAttribute dataMember = null;
                if (property.AttributeProvider != null)
                {
                    object[] dataMemberAttributes = property.AttributeProvider.GetCustomAttributes(typeof(DataMemberAttribute), true);
                    if (dataMemberAttributes.Length > 0)
                        dataMember = (DataMemberAttribute)dataMemberAttributes[0];
                }

                if (dataMember == null)
                {
                    if (isDataContract)
                        typeInfo.Properties.RemoveAt(i);
                    continue;
                }

                if (!string.IsNullOrEmpty(dataMember.Name))
                    property.Name = dataMember.Name;

                if (!dataMember.EmitDefaultValue)
                {
                    // Nullable<T> boxes to a real null when HasValue is false, so this comparand
                    // works uniformly for reference types, nullable value types, and plain value types
                    object defaultValue = property.PropertyType.IsValueType ? Activator.CreateInstance(property.PropertyType) : null;
                    property.ShouldSerialize = (obj, value) => !Equals(value, defaultValue);
                }

                // { get; private set; } is the norm on these DTOs (e.g. RestResponseMessage's
                // ErrorTypeEnum/Message/Details/URI). System.Text.Json only wires up public setters
                // by default, so without this, deserializing into a private-set DataMember property
                // silently no-ops -- no exception, the property just keeps its default value -- exactly
                // like ProtoBuf/XmlSerializer/Jil already reached past the access modifier for the same
                // properties via reflection.
                if ((property.Set == null) && (property.AttributeProvider is PropertyInfo reflectedProperty) && reflectedProperty.CanWrite)
                {
                    property.Set = (obj, value) => reflectedProperty.SetValue(obj, value);
                }
            }
        }
    }
}
