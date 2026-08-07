#region Using directives

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace SobekCM.Engine_Library.Solr
{
    /// <summary> Formats DateTime values the way Solr's date field type requires -- UTC, exactly three
    /// fractional-second digits, and a literal trailing 'Z' -- rather than System.Text.Json's default
    /// round-trip format ( which uses up to seven fractional digits and a numeric +00:00-style offset
    /// instead of 'Z', both of which Solr rejects with "Invalid Date String" ). Registering this once
    /// on <see cref="Solr_Http_Client"/>'s serializer options also covers DateTime? properties, since
    /// System.Text.Json wraps a registered value-type converter for its nullable counterpart automatically. </summary>
    public class Solr_DateTime_Converter : JsonConverter<DateTime>
    {
        private const string Solr_Date_Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override DateTime Read(ref Utf8JsonReader Reader, Type TypeToConvert, JsonSerializerOptions Options)
        {
            return Reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter Writer, DateTime Value, JsonSerializerOptions Options)
        {
            // Solr dates are always UTC. Unspecified-kind values (the norm for timestamps read back from
            // ADO.NET) are treated as already being UTC, rather than reinterpreted through the server's
            // local time zone, since that's the value's actual origin and intent throughout this codebase.
            DateTime utcValue = Value.Kind switch
            {
                DateTimeKind.Utc => Value,
                DateTimeKind.Local => Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(Value, DateTimeKind.Utc)
            };

            Writer.WriteStringValue(utcValue.ToString(Solr_Date_Format, CultureInfo.InvariantCulture));
        }
    }
}
