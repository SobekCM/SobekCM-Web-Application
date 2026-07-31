using System.Text.RegularExpressions;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;
using SobekCM.Resource_Object.Divisions;

namespace IiifImporter
{
    /// <summary> One physical page/canvas worth of image info, used both to build the METS
    /// fileSec entry and (optionally) to drive the actual image download, so the two always
    /// reference the same filename. Jp2Url/Jp2FileName are populated only when the source
    /// canvas advertises a direct JP2K master download link (David Rumsey's "Download N"
    /// metadata fields); they're simply null for sources that don't. </summary>
    public record PageImageInfo(string FileName, string? ImageServiceBaseUrl, int Width, int Height, string? Jp2FileName, string? Jp2Url);

    public class ItemBuildResult
    {
        public required SobekCM_Item Item { get; init; }
        public List<PageImageInfo> Pages { get; } = new();
        public List<string> UnmappedLabels { get; } = new();
    }

    /// <summary> Maps a David Rumsey IIIF manifest into a new, from-scratch SobekCM_Item.
    /// Modeled on SobekCM.Resource_Object.Testing.Test_Bib_Package as the canonical
    /// in-code (no template XML, no DB) construction example. </summary>
    public static class ItemBuilder
    {
        // Canvas metadata labels that are explicitly mapped below, or are intentionally
        // skipped (not bibliographic). Anything outside this set gets reported as unmapped.
        private static readonly HashSet<string> KnownLabels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Author", "Authors", "Date", "Short Title", "Full Title", "Publisher", "Publisher Location",
            "Type", "Obj Height cm", "Obj Width cm", "Scale 1", "Note", "Country", "Region", "City",
            "Subject", "Engraver or Printer",
            "List No", "Series No", "Image No",
            "Publication Author", "Pub Date", "Pub Title", "Pub Note", "Pub List No", "Pub Type",
            "Pub Height cm", "Pub Width cm", "Pub Reference", "Pub Maps",
            "Download 1", "Download 2",
            "Page No" // per-sheet identifier within a multi-canvas item; not item-level bibliographic data, intentionally skipped
        };

        public static ItemBuildResult Build(IiifManifest manifest, string objectId, string bibId, string vid, IReadOnlyList<string> aggregationCodes)
        {
            var item = new SobekCM_Item
            {
                BibID = bibId,
                VID = vid
            };

            item.Bib_Info.SobekCM_Type = TypeOfResource_SobekCM_Enum.Map;

            foreach (string code in aggregationCodes)
                item.Behaviors.Add_Aggregation(code);

            var result = new ItemBuildResult { Item = item };

            List<IiifCanvas> canvases = manifest.Sequences?.SelectMany(s => s.Canvases ?? new List<IiifCanvas>()).ToList()
                                         ?? new List<IiifCanvas>();

            List<IiifMetadataPair> metadata = canvases.FirstOrDefault()?.Metadata ?? new List<IiifMetadataPair>();

            Map_Bibliographic_Info(item.Bib_Info, manifest, metadata, result.UnmappedLabels);

            Build_Divisions(item, bibId, canvases, result.Pages);

            return result;
        }

        private static string? Get(List<IiifMetadataPair> metadata, params string[] labels)
        {
            foreach (string label in labels)
            {
                string? value = metadata
                    .FirstOrDefault(m => string.Equals(m.LabelText, label, StringComparison.OrdinalIgnoreCase))
                    ?.ValueText;

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        private static List<string> GetAll(List<IiifMetadataPair> metadata, string label)
        {
            return metadata
                .Where(m => string.Equals(m.LabelText, label, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.ValueText)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToList();
        }

        private static void Map_Bibliographic_Info(Bibliographic_Info bibInfo, IiifManifest manifest, List<IiifMetadataPair> metadata, List<string> unmappedLabels)
        {
            // ----- Title -----
            string? shortTitle = Get(metadata, "Short Title");
            string? fullTitle = Get(metadata, "Full Title");
            string title = !string.IsNullOrWhiteSpace(shortTitle) ? shortTitle
                : !string.IsNullOrWhiteSpace(manifest.Label) ? manifest.Label
                : fullTitle ?? "Untitled";
            bibInfo.Main_Title.Title = title;

            if (!string.IsNullOrWhiteSpace(fullTitle) && !string.Equals(fullTitle, title, StringComparison.OrdinalIgnoreCase))
            {
                bibInfo.Add_Other_Title(new Title_Info(fullTitle, Title_Type_Enum.Alternative));
            }

            // ----- Creator -----
            string? author = Get(metadata, "Author", "Authors");
            if (!string.IsNullOrWhiteSpace(author))
            {
                bibInfo.Main_Entity_Name.Name_Type = Name_Info_Type_Enum.Personal;
                bibInfo.Main_Entity_Name.Full_Name = author;
                bibInfo.Main_Entity_Name.Add_Role("Creator");
            }

            // ----- Date -----
            string? date = Get(metadata, "Date");
            if (!string.IsNullOrWhiteSpace(date))
                bibInfo.Origin_Info.Date_Issued = date;

            // ----- Publisher -----
            string? publisher = Get(metadata, "Publisher");
            if (!string.IsNullOrWhiteSpace(publisher))
            {
                Publisher_Info pubInfo = bibInfo.Add_Publisher(publisher);
                string? pubLocation = Get(metadata, "Publisher Location");
                if (!string.IsNullOrWhiteSpace(pubLocation))
                    pubInfo.Add_Place(pubLocation);
            }

            // ----- Genre / resource type -----
            string? type = Get(metadata, "Type");
            if (!string.IsNullOrWhiteSpace(type))
                bibInfo.Add_Genre(type);

            // ----- Engraver / printer (treated as a manufacturer, same as a print-shop role) -----
            string? engraver = Get(metadata, "Engraver or Printer");
            if (!string.IsNullOrWhiteSpace(engraver))
                bibInfo.Add_Manufacturer(engraver);

            // ----- Physical extent -----
            string? heightCm = Get(metadata, "Obj Height cm");
            string? widthCm = Get(metadata, "Obj Width cm");
            if (!string.IsNullOrWhiteSpace(heightCm) || !string.IsNullOrWhiteSpace(widthCm))
                bibInfo.Original_Description.Extent = $"{widthCm ?? "?"} x {heightCm ?? "?"} cm";

            // ----- Cartographic scale -----
            string? scale = Get(metadata, "Scale 1");
            if (!string.IsNullOrWhiteSpace(scale))
            {
                Subject_Info_Cartographics cartographics = bibInfo.Add_Cartographics_Subject();
                cartographics.Scale = "1:" + scale;
            }

            // ----- Note -----
            string? note = Get(metadata, "Note");
            if (!string.IsNullOrWhiteSpace(note))
                bibInfo.Add_Note(note);

            // ----- Geographic subject -----
            string? country = Get(metadata, "Country");
            string? region = Get(metadata, "Region");
            string? city = Get(metadata, "City");
            if (!string.IsNullOrWhiteSpace(country) || !string.IsNullOrWhiteSpace(region) || !string.IsNullOrWhiteSpace(city))
            {
                Subject_Info_Standard subject = bibInfo.Add_Subject();
                if (!string.IsNullOrWhiteSpace(country))
                    subject.Add_Geographic(country);
                if (!string.IsNullOrWhiteSpace(region))
                    subject.Add_Geographic(region);
                if (!string.IsNullOrWhiteSpace(city))
                    subject.Add_Geographic(city);
            }

            // ----- Topical subjects (repeatable) -----
            foreach (string topic in GetAll(metadata, "Subject"))
                bibInfo.Add_Subject().Add_Topic(topic);

            // ----- Local identifier -----
            string? listNo = Get(metadata, "List No");
            if (!string.IsNullOrWhiteSpace(listNo))
                bibInfo.Add_Identifier(listNo, "Local List No");

            // ----- Series number (no dedicated MODS slot; carry as a labeled note) -----
            string? seriesNo = Get(metadata, "Series No");
            if (!string.IsNullOrWhiteSpace(seriesNo))
                bibInfo.Add_Note(seriesNo, Note_Type_Enum.NONE, "Series No");

            // ----- Original publication (as a related "host" item) -----
            Map_Publication_Related_Item(bibInfo, metadata);

            // ----- Manifest-level attribution / related URL -----
            if (!string.IsNullOrWhiteSpace(manifest.AttributionText))
                bibInfo.Add_Note(manifest.AttributionText, Note_Type_Enum.Source, "Attribution");

            if (!string.IsNullOrWhiteSpace(manifest.RelatedUrl))
            {
                bibInfo.Location.Other_URL = manifest.RelatedUrl;
                bibInfo.Location.Other_URL_Display_Label = "David Rumsey Map Collection";
            }

            // ----- Report anything we didn't map -----
            foreach (IiifMetadataPair pair in metadata)
            {
                string? label = pair.LabelText;
                if (!string.IsNullOrWhiteSpace(label) && !KnownLabels.Contains(label))
                    unmappedLabels.Add($"{label} = {pair.ValueText}");
            }
        }

        private static void Map_Publication_Related_Item(Bibliographic_Info bibInfo, List<IiifMetadataPair> metadata)
        {
            string? pubTitle = Get(metadata, "Pub Title");
            string? pubAuthor = Get(metadata, "Publication Author");
            string? pubDate = Get(metadata, "Pub Date");
            string? pubNote = Get(metadata, "Pub Note");
            string? pubListNo = Get(metadata, "Pub List No");
            string? pubType = Get(metadata, "Pub Type");
            string? pubHeightCm = Get(metadata, "Pub Height cm");
            string? pubWidthCm = Get(metadata, "Pub Width cm");
            string? pubReference = Get(metadata, "Pub Reference");
            string? pubMaps = Get(metadata, "Pub Maps");

            bool hasAny = !string.IsNullOrWhiteSpace(pubTitle) || !string.IsNullOrWhiteSpace(pubAuthor)
                          || !string.IsNullOrWhiteSpace(pubDate) || !string.IsNullOrWhiteSpace(pubNote)
                          || !string.IsNullOrWhiteSpace(pubListNo) || !string.IsNullOrWhiteSpace(pubType)
                          || !string.IsNullOrWhiteSpace(pubHeightCm) || !string.IsNullOrWhiteSpace(pubWidthCm)
                          || !string.IsNullOrWhiteSpace(pubReference) || !string.IsNullOrWhiteSpace(pubMaps);

            if (!hasAny)
                return;

            var relatedItem = new Related_Item_Info { Relationship = Related_Item_Type_Enum.Host };

            if (!string.IsNullOrWhiteSpace(pubTitle))
                relatedItem.Main_Title.Title = pubTitle;

            if (!string.IsNullOrWhiteSpace(pubAuthor))
            {
                var pubAuthorName = new Name_Info { Name_Type = Name_Info_Type_Enum.Personal, Full_Name = pubAuthor };
                pubAuthorName.Add_Role("Creator");
                relatedItem.Add_Name(pubAuthorName);
            }

            if (!string.IsNullOrWhiteSpace(pubDate))
                relatedItem.Start_Date = pubDate;

            if (!string.IsNullOrWhiteSpace(pubNote))
                relatedItem.Add_Note(new Note_Info(pubNote));

            if (!string.IsNullOrWhiteSpace(pubListNo))
                relatedItem.Add_Identifier(pubListNo, "Local List No");

            if (!string.IsNullOrWhiteSpace(pubType))
                relatedItem.Add_Note(new Note_Info(pubType, Note_Type_Enum.NONE, "Pub Type"));

            if (!string.IsNullOrWhiteSpace(pubHeightCm) || !string.IsNullOrWhiteSpace(pubWidthCm))
                relatedItem.Add_Note(new Note_Info($"{pubWidthCm ?? "?"} x {pubHeightCm ?? "?"} cm", Note_Type_Enum.NONE, "Physical Description"));

            if (!string.IsNullOrWhiteSpace(pubReference))
                relatedItem.Add_Note(new Note_Info(pubReference, Note_Type_Enum.CitationReference));

            if (!string.IsNullOrWhiteSpace(pubMaps))
                relatedItem.Add_Note(new Note_Info(pubMaps, Note_Type_Enum.NONE, "Number of Maps"));

            bibInfo.Add_Related_Item(relatedItem);
        }

        private static void Build_Divisions(SobekCM_Item item, string bibId, List<IiifCanvas> canvases, List<PageImageInfo> pages)
        {
            var mainDivision = new Division_TreeNode("Main", string.Empty);
            item.Divisions.Physical_Tree.Roots.Add(mainDivision);

            int pageNumber = 0;
            foreach (IiifCanvas canvas in canvases)
            {
                pageNumber++;

                string label = !string.IsNullOrWhiteSpace(canvas.Label) ? canvas.Label : $"Page {pageNumber}";
                var pageNode = new Page_TreeNode(label);
                mainDivision.Nodes.Add(pageNode);

                string fileName = $"{bibId}_{pageNumber:0000}.jpg";

                IiifImageResource? resource = canvas.Images?.FirstOrDefault()?.Resource;
                int width = resource?.Width ?? canvas.Width;
                int height = resource?.Height ?? canvas.Height;
                string? serviceBaseUrl = resource?.Service?.Id;

                ushort clampedWidth = (ushort) Math.Clamp(width, 0, ushort.MaxValue);
                ushort clampedHeight = (ushort) Math.Clamp(height, 0, ushort.MaxValue);

                var fileInfo = new SobekCM_File_Info(fileName, clampedWidth, clampedHeight);
                pageNode.Files.Add(fileInfo);

                // David Rumsey specific: the canvas's own "Download N" metadata entries link
                // directly to the archival JP2K master (not exposed via the IIIF Image API).
                // Add it as a second file on the page when present, matching the usual SobekCM
                // pattern of multiple format derivatives (jp2 + jpg) per page.
                string? jp2Url = ExtractDavidRumseyJp2Url(canvas.Metadata ?? new List<IiifMetadataPair>());
                string? jp2FileName = null;
                if (!string.IsNullOrWhiteSpace(jp2Url))
                {
                    jp2FileName = $"{bibId}_{pageNumber:0000}.jp2";
                    var jp2FileInfo = new SobekCM_File_Info(jp2FileName, clampedWidth, clampedHeight);
                    pageNode.Files.Add(jp2FileInfo);
                }

                pages.Add(new PageImageInfo(fileName, serviceBaseUrl, width, height, jp2FileName, jp2Url));
            }
        }

        private static readonly Regex HrefRegex = new(@"href\s*=\s*[""']?([^\s""'>]+)", RegexOptions.IgnoreCase);

        /// <summary> David Rumsey specific: their IIIF canvases carry raw HTML anchors in
        /// "Download N" metadata fields (e.g. Download 1 = "&lt;a href=https://.../16908002.jp2 ...&gt;Full Image Download in JP2 Format&lt;/a&gt;").
        /// Scans those for a link ending in .jp2 and returns it, or null if this canvas doesn't
        /// advertise one (which is simply always the case for any other IIIF source). </summary>
        private static string? ExtractDavidRumseyJp2Url(List<IiifMetadataPair> canvasMetadata)
        {
            foreach (IiifMetadataPair pair in canvasMetadata)
            {
                string? label = pair.LabelText;
                string? value = pair.ValueText;
                if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                    continue;

                if (!label.StartsWith("Download", StringComparison.OrdinalIgnoreCase))
                    continue;

                Match match = HrefRegex.Match(value);
                if (match.Success && match.Groups[1].Value.EndsWith(".jp2", StringComparison.OrdinalIgnoreCase))
                    return match.Groups[1].Value;
            }

            return null;
        }
    }
}
