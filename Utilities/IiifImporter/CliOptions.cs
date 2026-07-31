namespace IiifImporter
{
    public class CliOptions
    {
        public required string CsvPath { get; init; }
        public required string OutputFolder { get; init; }
        public string StartBibId { get; init; } = "DR00000001";
        public List<string> AggregationCodes { get; init; } = new();
        public bool DownloadImages { get; init; }

        /// <summary> Max width/height in pixels to request when downloading images.
        /// Null means no limit - request the image at full resolution. </summary>
        public int? MaxImageSize { get; init; }

        /// <summary> Maximum number of unique object IDs to process, after de-duplication.
        /// Null means no limit - process every unique ID in the CSV. </summary>
        public int? Limit { get; init; }

        /// <summary> Pause, in milliseconds, between items (before starting the next object's
        /// manifest fetch/downloads) so as not to hammer the remote IIIF server. </summary>
        public int DelayMs { get; init; } = 500;

        public static CliOptions? Parse(string[] args)
        {
            var positional = new List<string>();
            string startBibId = "DR00000001";
            var aggregationCodes = new List<string>();
            bool downloadImages = false;
            int? maxImageSize = null;
            int? limit = null;
            int delayMs = 500;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--start-bibid":
                        if (++i >= args.Length) return null;
                        startBibId = args[i];
                        break;

                    case "--aggregation":
                        if (++i >= args.Length) return null;
                        aggregationCodes.Add(args[i]);
                        break;

                    case "--download-images":
                        downloadImages = true;
                        break;

                    case "--max-image-size":
                        if (++i >= args.Length) return null;
                        if (!int.TryParse(args[i], out int parsedMaxImageSize)) return null;
                        maxImageSize = parsedMaxImageSize;
                        break;

                    case "--limit":
                        if (++i >= args.Length) return null;
                        if (!int.TryParse(args[i], out int parsedLimit)) return null;
                        limit = parsedLimit;
                        break;

                    case "--delay-ms":
                        if (++i >= args.Length) return null;
                        if (!int.TryParse(args[i], out delayMs) || delayMs < 0) return null;
                        break;

                    case "-h":
                    case "--help":
                        return null;

                    default:
                        positional.Add(arg);
                        break;
                }
            }

            if (positional.Count != 2)
                return null;

            return new CliOptions
            {
                CsvPath = positional[0],
                OutputFolder = positional[1],
                StartBibId = startBibId,
                AggregationCodes = aggregationCodes,
                DownloadImages = downloadImages,
                MaxImageSize = maxImageSize,
                Limit = limit,
                DelayMs = delayMs
            };
        }

        public static void PrintUsage()
        {
            Console.WriteLine("""
                IiifImporter - imports IIIF manifests into SobekCM METS packages

                Usage:
                  IiifImporter <csv-path> <output-folder> [options]

                Arguments:
                  <csv-path>          Path to a text file with one LUNA object ID per line
                  <output-folder>     Folder to write "{BibID}_{VID}" item folders into

                Options:
                  --start-bibid ID    First BibID to assign (default: DR00000001)
                  --aggregation CODE  Aggregation code to add to every item (repeatable)
                  --download-images   Download full images via the IIIF Image API (default: off)
                  --max-image-size N  Max width/height in pixels when downloading images (default: no limit - full resolution)
                  --limit N           Only process the first N unique object IDs (default: no limit - process all); useful for a quick test run
                  --delay-ms N        Pause N milliseconds between items, to go easy on the remote server (default: 500)
                """);
        }
    }
}
