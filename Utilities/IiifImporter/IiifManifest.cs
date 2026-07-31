using System.Text.Json;
using System.Text.Json.Serialization;

namespace IiifImporter
{
    /// <summary> Minimal DTOs for the subset of IIIF Presentation API 2.0 used by the
    /// David Rumsey Map Collection manifests (https://www.davidrumsey.com/luna/servlet/iiif/m/{id}/manifest) </summary>
    public class IiifManifest
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("attribution")]
        public JsonElement? Attribution { get; set; }

        [JsonPropertyName("related")]
        public JsonElement? Related { get; set; }

        [JsonPropertyName("sequences")]
        public List<IiifSequence>? Sequences { get; set; }

        public string? AttributionText => IiifTextHelper.ExtractText(Attribution);

        public string? RelatedUrl => IiifTextHelper.ExtractIdOrText(Related);
    }

    public class IiifSequence
    {
        [JsonPropertyName("canvases")]
        public List<IiifCanvas>? Canvases { get; set; }
    }

    public class IiifCanvas
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("metadata")]
        public List<IiifMetadataPair>? Metadata { get; set; }

        [JsonPropertyName("images")]
        public List<IiifImageAnnotation>? Images { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class IiifMetadataPair
    {
        [JsonPropertyName("label")]
        public JsonElement? Label { get; set; }

        [JsonPropertyName("value")]
        public JsonElement? Value { get; set; }

        public string? LabelText => IiifTextHelper.ExtractText(Label);

        public string? ValueText => IiifTextHelper.ExtractText(Value);
    }

    public class IiifImageAnnotation
    {
        [JsonPropertyName("resource")]
        public IiifImageResource? Resource { get; set; }
    }

    public class IiifImageResource
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }

        [JsonPropertyName("service")]
        public IiifImageService? Service { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class IiifImageService
    {
        [JsonPropertyName("@id")]
        public string? Id { get; set; }
    }

    /// <summary> IIIF presentation JSON allows several fields (attribution, related, metadata labels/values)
    /// to be either a plain string, a language-tagged object ({"@value": "...", "@language": "en"}), an
    /// object with just an "@id" (a link), or an array of any of those. This normalizes all of those shapes
    /// down to a single display string so the rest of the importer can treat them uniformly. </summary>
    internal static class IiifTextHelper
    {
        public static string? ExtractText(JsonElement? element)
        {
            if (element == null) return null;
            return ExtractText(element.Value);
        }

        private static string? ExtractText(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Array:
                    var parts = new List<string>();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        string? text = ExtractText(item);
                        if (!string.IsNullOrWhiteSpace(text))
                            parts.Add(text);
                    }
                    return parts.Count > 0 ? string.Join("; ", parts) : null;

                case JsonValueKind.Object:
                    if (element.TryGetProperty("@value", out JsonElement valueProp))
                        return ExtractText(valueProp);
                    if (element.TryGetProperty("@id", out JsonElement idProp))
                        return ExtractText(idProp);
                    return null;

                case JsonValueKind.Number:
                    return element.ToString();

                default:
                    return null;
            }
        }

        public static string? ExtractIdOrText(JsonElement? element)
        {
            if (element == null) return null;
            JsonElement value = element.Value;
            if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("@id", out JsonElement idProp))
                return ExtractText(idProp);
            return ExtractText(value);
        }
    }
}
