using System.Xml.Linq;

namespace RijksmuseumImporter
{
    /// <summary> The small, fixed Dublin Core field set returned by Rijksmuseum's OAI-PMH
    /// GetRecord (metadataPrefix=oai_dc) - confirmed live to include everything needed:
    /// bibliographic fields plus the IIIF image URL in dc:relation. </summary>
    public class RijksmuseumRecord
    {
        public string? Title { get; set; }
        public string? Creator { get; set; }
        public string? Date { get; set; }
        public string? Description { get; set; }
        public List<string> Formats { get; } = new();
        public string? AccessionNumber { get; set; }
        public List<string> ImageUrls { get; } = new();
        public string? Rights { get; set; }
        public string? Type { get; set; }
        public List<string> Subjects { get; } = new();
        public string? Coverage { get; set; }

        /// <summary> Every setSpec code this record belongs to, from its own GetRecord header
        /// (e.g. "260239", "26112") - resolved to human-readable set names via SetNameLookup. </summary>
        public List<string> SetSpecs { get; } = new();

        /// <summary> Any dc:* element present on this record that isn't one of the fields above
        /// (e.g. dc:publisher, dc:contributor, dc:source, dc:language - none observed in samples
        /// checked so far, but oai_dc doesn't guarantee that holds for every record). Reported
        /// so nothing silently vanishes without at least a console trail. </summary>
        public List<string> UnmappedFields { get; } = new();

        private static readonly HashSet<string> KnownElementNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "title", "creator", "date", "description", "rights", "type", "coverage",
            "format", "subject", "relation", "identifier"
        };

        public static RijksmuseumRecord Parse(XDocument doc)
        {
            XNamespace dc = "http://purl.org/dc/elements/1.1/";

            XElement? headerElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "header");
            XElement? dcElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "dc");
            if (dcElement == null)
                throw new InvalidOperationException("GetRecord response did not contain an oai_dc:dc element.");

            var record = new RijksmuseumRecord
            {
                Title = dcElement.Element(dc + "title")?.Value,
                Creator = dcElement.Element(dc + "creator")?.Value,
                Date = dcElement.Element(dc + "date")?.Value,
                Description = dcElement.Element(dc + "description")?.Value,
                Rights = dcElement.Element(dc + "rights")?.Value,
                Type = dcElement.Element(dc + "type")?.Value,
                Coverage = dcElement.Element(dc + "coverage")?.Value
            };

            record.Formats.AddRange(dcElement.Elements(dc + "format").Select(e => e.Value).Where(v => !string.IsNullOrWhiteSpace(v)));
            record.Subjects.AddRange(dcElement.Elements(dc + "subject").Select(e => e.Value).Where(v => !string.IsNullOrWhiteSpace(v)));
            record.ImageUrls.AddRange(dcElement.Elements(dc + "relation").Select(e => e.Value).Where(v => !string.IsNullOrWhiteSpace(v)));

            // dc:identifier is the museum accession number (e.g. "SK-A-4673") in every
            // sample observed; guard against a URL ever showing up there instead.
            record.AccessionNumber = dcElement.Elements(dc + "identifier")
                .Select(e => e.Value)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v) && !v.StartsWith("http", StringComparison.OrdinalIgnoreCase));

            if (headerElement != null)
            {
                record.SetSpecs.AddRange(headerElement.Elements()
                    .Where(e => e.Name.LocalName == "setSpec")
                    .Select(e => e.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v)));
            }

            foreach (XElement element in dcElement.Elements())
            {
                if (!KnownElementNames.Contains(element.Name.LocalName) && !string.IsNullOrWhiteSpace(element.Value))
                    record.UnmappedFields.Add($"dc:{element.Name.LocalName} = {element.Value}");
            }

            return record;
        }
    }
}
