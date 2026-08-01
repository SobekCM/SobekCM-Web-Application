using System.Text.RegularExpressions;
using SobekCM.Resource_Object.Configuration;

namespace IiifImporter
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            CliOptions? options = CliOptions.Parse(args);
            if (options == null)
            {
                CliOptions.PrintUsage();
                return 1;
            }

            // The METS writer pulls its writing profile (which dmdSec/amdSec sections to emit)
            // from ResourceObjectSettings.MetadataConfig. The main SobekCM web app populates this
            // by reading an XML config file at startup; this standalone tool has none, so fall back
            // to the library's own baked-in defaults (this is exactly what Set_Default_Values() is
            // for - "in case there is no file to be read"). Without this, Save_METS() writes only
            // the bare XML declaration and silently discards the failure.
            ResourceObjectSettings.MetadataConfig.Set_Default_Values();
            ResourceObjectSettings.MetadataConfig.Finalize_Metadata_Configuration();

            if (!File.Exists(options.CsvPath))
            {
                Console.WriteLine($"CSV file not found: {options.CsvPath}");
                return 1;
            }

            List<string> objectIds = File.ReadAllLines(options.CsvPath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uniqueObjectIds = new List<string>();
            int duplicateCount = 0;

            foreach (string objectId in objectIds)
            {
                if (seen.Add(objectId))
                {
                    uniqueObjectIds.Add(objectId);
                }
                else
                {
                    duplicateCount++;
                    Console.WriteLine($"Skipping duplicate object id: {objectId}");
                }
            }

            Console.WriteLine($"Loaded {uniqueObjectIds.Count} unique object IDs ({duplicateCount} duplicate(s) skipped) from {options.CsvPath}");

            if (options.Limit.HasValue && options.Limit.Value < uniqueObjectIds.Count)
            {
                uniqueObjectIds = uniqueObjectIds.Take(options.Limit.Value).ToList();
                Console.WriteLine($"Limiting run to the first {uniqueObjectIds.Count} unique object ID(s) (--limit {options.Limit.Value})");
            }

            Directory.CreateDirectory(options.OutputFolder);

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SobekCM-IiifImporter/1.0");

            var fetcher = new ManifestFetcher(http);
            var imageDownloader = new ImageDownloader(http, options.MaxImageSize);

            (string bibPrefix, int bibNumber, int bibDigits) = ParseStartBibId(options.StartBibId);

            int successCount = 0;
            int failureCount = 0;
            int itemIndex = 0;
            var random = new Random();

            foreach (string objectId in uniqueObjectIds)
            {
                itemIndex++;
                string bibId = bibPrefix + bibNumber.ToString().PadLeft(bibDigits, '0');
                const string vid = "00001";
                string itemFolder = Path.Combine(options.OutputFolder, $"{bibId}_{vid}");

                try
                {
                    Directory.CreateDirectory(itemFolder);

                    (IiifManifest manifest, string rawJson) = await fetcher.FetchAsync(objectId);
                    await File.WriteAllTextAsync(Path.Combine(itemFolder, "manifest.json"), rawJson);

                    ItemBuildResult result = ItemBuilder.Build(manifest, objectId, bibId, vid, options.AggregationCodes);

                    foreach (string unmapped in result.UnmappedLabels)
                        Console.WriteLine($"  [{bibId}] unmapped metadata label: {unmapped}");

                    if (options.DownloadImages)
                    {
                        foreach (PageImageInfo page in result.Pages)
                        {
                            if (string.IsNullOrWhiteSpace(page.ImageServiceBaseUrl))
                                Console.WriteLine($"  [{bibId}] no image service found for page {page.FileName}, skipping download");
                            else
                                await imageDownloader.DownloadAsync(page.ImageServiceBaseUrl, Path.Combine(itemFolder, page.FileName));

                            if (!string.IsNullOrWhiteSpace(page.Jp2Url) && page.Jp2FileName != null)
                            {
                                Console.WriteLine($"  [{bibId}] downloading JP2 master for {page.Jp2FileName}");
                                await imageDownloader.DownloadFileAsync(page.Jp2Url, Path.Combine(itemFolder, page.Jp2FileName));
                            }
                        }

                        if (result.Pages.Count > 0)
                            result.Item.Behaviors.Main_Thumbnail = result.Pages[0].FileName;
                    }

                    result.Item.Source_Directory = itemFolder;
                    result.Item.Save_METS();

                    Console.WriteLine($"[{bibId}] {objectId} -> {itemFolder}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED {objectId} ({bibId}): {ex.Message}");
                    failureCount++;
                }

                bibNumber++;

                if (options.DelayMs > 0 && itemIndex < uniqueObjectIds.Count)
                {
                    // Jittered: somewhere between 1x and 2x the requested delay, so a batch run
                    // doesn't hit the remote server on a perfectly predictable cadence.
                    int jitteredDelayMs = (int) (options.DelayMs * (1.0 + random.NextDouble()));
                    await Task.Delay(jitteredDelayMs);
                }
            }

            Console.WriteLine($"Done. {successCount} succeeded, {failureCount} failed.");
            return failureCount == 0 ? 0 : 2;
        }

        private static (string Prefix, int Number, int Digits) ParseStartBibId(string startBibId)
        {
            Match match = Regex.Match(startBibId, @"^([A-Za-z]+)(\d+)$");
            if (!match.Success)
                throw new ArgumentException($"--start-bibid '{startBibId}' must be a letter prefix followed by digits (e.g. DR00000001)");

            string prefix = match.Groups[1].Value;
            string digitsText = match.Groups[2].Value;
            return (prefix, int.Parse(digitsText), digitsText.Length);
        }
    }
}
