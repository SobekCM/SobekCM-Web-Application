#region Using directives

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

#endregion

namespace SobekCM.PullGcsFiles
{
    /// <summary> Settings read from appsettings.json -- the bucket, credential, and instance code are
    /// deployment-wide constants, so they live in config rather than being passed on the command line every
    /// run. The list file and output folder are the two things that actually change from run to run, so
    /// those stay command-line arguments. </summary>
    public class GcsSettings
    {
        /// <summary> Name of the GCS bucket resource files are stored under </summary>
        public string GCS_Bucket_Name { get; set; }

        /// <summary> Full path to a service account JSON key file with read access to <see cref="GCS_Bucket_Name"/> </summary>
        public string GCS_Service_Account_Json_Key_Path { get; set; }

        /// <summary> Code identifying the SobekCM instance the bucket's objects are organized under, e.g.
        /// "DEMO" -- the top-level "folder" in the bucket, matching <c>Server_Settings.Instance_Code</c> and
        /// the object key convention used by <c>GCS_FileSystem</c> ("{InstanceCode}/{BibID}/{VID}/{FileName}") </summary>
        public string Instance_Code { get; set; }
    }

    /// <summary> Command-line utility that reads a list of BibID (optionally BibID:VID) values and downloads
    /// every file present in each item's GCS folder down to a local output folder, one subfolder per item
    /// named "BibID_VID". </summary>
    /// <remarks> Deliberately a small standalone tool rather than a consumer of <c>SobekCM_Core</c>'s
    /// <c>GCS_FileSystem</c>/<c>SobekFileSystem</c> -- this only ever reads, never needs signed URLs, item
    /// metadata, or database access, so it talks to <see cref="StorageClient"/> directly using the same
    /// object key convention ("{InstanceCode}/{BibID}/{VID}/{FileName}", see <c>GCS_FileSystem</c> remarks). </remarks>
    public class Program
    {
        static int Main(string[] args)
        {
            string listPath = null;
            string outputPath = null;
            string bucketOverride = null;
            string keyPathOverride = null;
            string instanceCodeOverride = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--list":
                        if (i + 1 < args.Length)
                            listPath = args[++i];
                        break;

                    case "--output":
                        if (i + 1 < args.Length)
                            outputPath = args[++i];
                        break;

                    case "--bucket":
                        if (i + 1 < args.Length)
                            bucketOverride = args[++i];
                        break;

                    case "--key-path":
                        if (i + 1 < args.Length)
                            keyPathOverride = args[++i];
                        break;

                    case "--instance-code":
                        if (i + 1 < args.Length)
                            instanceCodeOverride = args[++i];
                        break;

                    case "--help":
                    case "-help":
                    case "?":
                        Show_Help();
                        return 0;

                    default:
                        Console.WriteLine("Unrecognized argument: " + args[i]);
                        Show_Help();
                        return 1;
                }
            }

            if (string.IsNullOrEmpty(listPath) || string.IsNullOrEmpty(outputPath))
            {
                Show_Help();
                return 1;
            }

            if (!File.Exists(listPath))
            {
                Console.WriteLine("List file does not exist: " + listPath);
                return 1;
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            GcsSettings settings = configuration.Get<GcsSettings>() ?? new GcsSettings();

            if (!string.IsNullOrEmpty(bucketOverride))
                settings.GCS_Bucket_Name = bucketOverride;
            if (!string.IsNullOrEmpty(keyPathOverride))
                settings.GCS_Service_Account_Json_Key_Path = keyPathOverride;
            if (!string.IsNullOrEmpty(instanceCodeOverride))
                settings.Instance_Code = instanceCodeOverride;

            if (string.IsNullOrWhiteSpace(settings.GCS_Bucket_Name))
            {
                Console.WriteLine("GCS_Bucket_Name is not configured (appsettings.json or --bucket).");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(settings.GCS_Service_Account_Json_Key_Path))
            {
                Console.WriteLine("GCS_Service_Account_Json_Key_Path is not configured (appsettings.json or --key-path).");
                return 1;
            }

            if (!File.Exists(settings.GCS_Service_Account_Json_Key_Path))
            {
                Console.WriteLine("GCS service account key file does not exist: " + settings.GCS_Service_Account_Json_Key_Path);
                return 1;
            }

            if (string.IsNullOrWhiteSpace(settings.Instance_Code))
            {
                Console.WriteLine("Instance_Code is not configured (appsettings.json or --instance-code).");
                return 1;
            }

            List<(string BibID, string VID)> items = Read_Item_List(listPath);
            if (items.Count == 0)
            {
                Console.WriteLine("No items found in list file: " + listPath);
                return 1;
            }

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            StorageClient storageClient;
            try
            {
                GoogleCredential credential = CredentialFactory.FromFile<ServiceAccountCredential>(settings.GCS_Service_Account_Json_Key_Path).ToGoogleCredential();
                storageClient = StorageClient.Create(credential);
            }
            catch (Exception ee)
            {
                Console.WriteLine("ERROR creating GCS storage client: " + ee.Message);
                return 1;
            }

            Console.WriteLine("Found " + items.Count + " item(s) in " + listPath);
            Console.WriteLine("Bucket: " + settings.GCS_Bucket_Name + "   Instance: " + settings.Instance_Code);
            Console.WriteLine("Output: " + outputPath);
            Console.WriteLine();

            int itemsWithFiles = 0, itemsWithNoFiles = 0, itemsFailed = 0, filesDownloaded = 0;
            long bytesDownloaded = 0;

            foreach ((string BibID, string VID) item in items)
            {
                string prefix = settings.Instance_Code + "/" + item.BibID + "/" + item.VID + "/";
                string destinationFolder = Path.Combine(outputPath, item.BibID + "_" + item.VID);

                try
                {
                    int filesForItem = 0;
                    foreach (Google.Apis.Storage.v1.Data.Object gcsObject in storageClient.ListObjects(settings.GCS_Bucket_Name, prefix))
                    {
                        // Skip the "folder placeholder" object itself, if one exists, and anything in a
                        // deeper nested prefix -- mirrors the flat, one-level file listing GCS_FileSystem
                        // uses for a resource's own folder
                        string relativeName = gcsObject.Name.Substring(prefix.Length);
                        if ((relativeName.Length == 0) || (relativeName.IndexOf("/") >= 0))
                            continue;

                        if (filesForItem == 0)
                            Directory.CreateDirectory(destinationFolder);

                        string localPath = Path.Combine(destinationFolder, relativeName);
                        Console.WriteLine("  " + item.BibID + ":" + item.VID + "/" + relativeName);

                        using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write))
                        {
                            storageClient.DownloadObject(gcsObject, fileStream);
                        }

                        filesForItem++;
                        filesDownloaded++;
                        bytesDownloaded += gcsObject.Size.HasValue ? (long)gcsObject.Size.Value : 0;
                    }

                    if (filesForItem > 0)
                        itemsWithFiles++;
                    else
                    {
                        Console.WriteLine("  WARNING: no files found for " + item.BibID + ":" + item.VID + " under " + prefix);
                        itemsWithNoFiles++;
                    }
                }
                catch (Exception ee)
                {
                    Console.WriteLine("  ERROR downloading " + item.BibID + ":" + item.VID + " -- " + ee.Message);
                    itemsFailed++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Items with files downloaded: " + itemsWithFiles);
            Console.WriteLine("Items with no files found:   " + itemsWithNoFiles);
            Console.WriteLine("Items failed:                " + itemsFailed);
            Console.WriteLine("Files downloaded:             " + filesDownloaded);
            Console.WriteLine("Bytes downloaded:              " + bytesDownloaded);

            return 0;
        }

        /// <summary> Reads a list file of one BibID (optionally "BibID:VID") per line -- blank lines are
        /// skipped, and a BibID with no ":VID" suffix defaults to VID "00001" </summary>
        /// <param name="ListPath"> Full path to the list file </param>
        /// <returns> List of (BibID, VID) pairs, in the order they appeared in the file </returns>
        private static List<(string BibID, string VID)> Read_Item_List(string ListPath)
        {
            var items = new List<(string BibID, string VID)>();

            foreach (string rawLine in File.ReadAllLines(ListPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                int colonIndex = line.IndexOf(':');
                string bibId = colonIndex >= 0 ? line.Substring(0, colonIndex).Trim() : line;
                string vid = colonIndex >= 0 ? line.Substring(colonIndex + 1).Trim() : string.Empty;

                if (bibId.Length == 0)
                    continue;

                if (vid.Length == 0)
                    vid = "00001";

                items.Add((bibId, vid));
            }

            return items;
        }

        private static void Show_Help()
        {
            Console.WriteLine();
            Console.WriteLine("PullGcsFiles -- downloads every file present in each listed item's GCS folder");
            Console.WriteLine("down to a local output folder, one subfolder per item named \"BibID_VID\".");
            Console.WriteLine();
            Console.WriteLine("Usage: PullGcsFiles --list <path> --output <path> [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --list <path>             Text file with one BibID (or \"BibID:VID\") per line.");
            Console.WriteLine("                            A BibID with no VID defaults to \"00001\".");
            Console.WriteLine("  --output <path>           Root folder to download into. Each item is written to");
            Console.WriteLine("                            <output>\\<BibID>_<VID>\\. Created if it doesn't exist.");
            Console.WriteLine();
            Console.WriteLine("Options (override appsettings.json):");
            Console.WriteLine("  --bucket <name>           GCS bucket name.");
            Console.WriteLine("  --key-path <path>         Full path to the service account JSON key file.");
            Console.WriteLine("  --instance-code <code>    Instance code -- the top-level \"folder\" in the bucket");
            Console.WriteLine("                            objects are organized under (\"{Instance}/{BibID}/{VID}/...\").");
            Console.WriteLine("  --help                    Shows these instructions.");
            Console.WriteLine();
            Console.WriteLine("Bucket name, key path, and instance code are normally set once in appsettings.json");
            Console.WriteLine("next to the executable, since they don't change from run to run.");
            Console.WriteLine();
        }
    }
}
