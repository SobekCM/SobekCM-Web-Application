#region Using directives

using SobekCM.Core.FileSystems;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Behaviors;
using SobekCM_Resource_Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SobekCM.RestoreLocalFileCache
{
    /// <summary> One-time/occasional bulk utility that rebuilds the LOCAL half of GCS Hybrid mode --
    /// thumbnails, METS, and marc.xml -- for every item already living in a GCS bucket, or downloads
    /// everything for a true full restore (<c>--full</c>). Two intended uses: disaster recovery if an
    /// instance's local disk is ever lost (GCS Hybrid means the bucket already holds the complete data,
    /// so nothing is actually gone), and hydrating a brand new instance (a freshly restored database
    /// pointed at an existing bucket, e.g. testing.sobeklibrary.com or an ephemeral e2e environment) that
    /// has never had any local files at all. </summary>
    /// <remarks> The inverse of <c>MigrateSobekFileSystem</c> (which pushes local files up to GCS) --
    /// deliberately follows the same structure/bootstrap/conventions as that tool. Reuses
    /// <see cref="SobekFileSystem"/> directly rather than any separate download/classification logic, so
    /// this always agrees with whatever the running application itself considers "local" under GCS
    /// Hybrid. </remarks>
    public class Program
    {
        /// <summary> Synchronizes console/log output once file-level work runs in parallel -- otherwise
        /// interleaved writes from concurrent threads garble each other mid-line </summary>
        private static readonly object consoleLock = new object();

        /// <summary> Opened once <c>instancePath</c> is known valid, so every run also leaves a persistent
        /// record on disk next to the instance itself -- console output alone (especially a GCE VM's serial
        /// console, a size-limited ring buffer) has proven unreliable for diagnosing exactly what a run did
        /// after the fact. Null until then (e.g. during <see cref="Show_Help"/>). </summary>
        private static StreamWriter logWriter;

        /// <summary> Writes one line to both the console and the persistent log file (once open), safe to
        /// call from multiple threads. Every place in this file that used to call <c>Console.WriteLine</c>
        /// directly calls this instead, so nothing is ever visible on screen but missing from the log or vice
        /// versa. </summary>
        private static void Log(string message = "")
        {
            lock (consoleLock)
            {
                Console.WriteLine(message);
                logWriter?.WriteLine(message);
            }
        }

        static int Main(string[] args)
        {
            string instancePath = null;
            bool execute = false;
            bool quiet = false;
            bool force = false;
            bool full = false;
            int threads = 8;
            string targetBibID = null;
            string targetVID = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--instance-path":
                        if (i + 1 < args.Length)
                            instancePath = args[++i];
                        break;

                    case "--execute":
                        execute = true;
                        break;

                    case "--quiet":
                        quiet = true;
                        break;

                    case "--force":
                        force = true;
                        break;

                    case "--full":
                        full = true;
                        break;

                    case "--bibid":
                        if (i + 1 < args.Length)
                            targetBibID = args[++i];
                        break;

                    case "--vid":
                        if (i + 1 < args.Length)
                            targetVID = args[++i];
                        break;

                    case "--threads":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int parsedThreads) && parsedThreads > 0)
                            threads = parsedThreads;
                        else
                        {
                            Console.WriteLine("--threads requires a positive integer.");
                            Show_Help();
                            return 1;
                        }
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

            if (string.IsNullOrEmpty(instancePath))
            {
                Show_Help();
                return 1;
            }

            if (string.IsNullOrEmpty(targetBibID) != string.IsNullOrEmpty(targetVID))
            {
                Console.WriteLine("--bibid and --vid must be used together.");
                Show_Help();
                return 1;
            }

            if (!Directory.Exists(instancePath))
            {
                Console.WriteLine("Instance path does not exist: " + instancePath);
                return 1;
            }

            string logDirectory = Path.Combine(instancePath, "logs");
            string logPath = Path.Combine(logDirectory, "RestoreLocalFileCache-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            try
            {
                Directory.CreateDirectory(logDirectory);
                logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
            }
            catch (Exception ee)
            {
                // Not fatal -- fall back to console-only rather than losing the whole run over a logging
                // problem (e.g. a permissions issue on logDirectory).
                Console.WriteLine("WARNING: could not open log file at " + logPath + " -- " + ee.Message);
                Console.WriteLine("Continuing with console output only.");
            }

            if (logWriter != null)
                Log("Log file: " + logPath);
            else
                Log("Log file disabled (could not open " + logPath + ")");
            Log();

            if (!execute)
            {
                Log("DRY RUN -- pass --execute to actually download files.");
                Log();
            }

            // Bootstrap settings exactly the way the running app does: point AppRoot_Gateway at the
            // instance's own directory, then just touch Settings -- it lazy-loads via
            // {instancePath}\config\sobekcm.config, which supplies the DB connection string
            AppRoot_Gateway.AppRootPath = instancePath;
            InstanceWide_Settings settings = Engine_ApplicationCache_Gateway.Settings;

            if (string.IsNullOrWhiteSpace(settings?.Servers?.GCS_Bucket_Name))
            {
                Log("GCS Bucket Name is not configured for this instance -- nothing to restore from.");
                Log("Set GCS Bucket Name and put the service account key file in place first.");
                logWriter?.Dispose();
                return 1;
            }

            // Force Hybrid classification regardless of the instance's live File System Mode -- this is
            // exactly the point of the tool (e.g. a brand new instance still set to "Local" pending its
            // first restore, or an ephemeral e2e VM whose startup script hasn't flipped the setting yet).
            // --full doesn't care about classification at all (it downloads everything unconditionally),
            // but Initialize still needs to run so GetFiles/DownloadFile/Resource_Network_Uri all work.
            SobekFileSystem.Initialize(settings, ForceGcsHybrid: true);

            List<(string BibID, string VID)> items;
            if (!string.IsNullOrEmpty(targetBibID))
            {
                items = new List<(string BibID, string VID)> { (targetBibID, targetVID) };
                Log("Targeting single item: " + targetBibID + ":" + targetVID);
            }
            else
            {
                try
                {
                    items = SobekCM_Item_Database.Get_All_BibID_VID_Pairs();
                }
                catch (Exception ee)
                {
                    Log("ERROR reading item list from the database: " + ee.Message);
                    logWriter?.Dispose();
                    return 1;
                }

                Log("Found " + items.Count + " item(s).");
            }
            Log();

            int itemsProcessed = 0, itemsWithNothingInGcs = 0, itemsFailed = 0, itemsRequiringFullBundle = 0;
            int filesDownloaded = 0, filesSkipped = 0;
            long bytesTransferred = 0;

            foreach ((string BibID, string VID) item in items)
            {
                List<SobekFileSystem_FileInfo> knownFiles;
                try
                {
                    knownFiles = SobekFileSystem.GetFiles(item.BibID, item.VID);
                }
                catch (Exception ee)
                {
                    Log("ERROR listing files for " + item.BibID + ":" + item.VID + " -- " + ee.Message);
                    itemsFailed++;
                    continue;
                }

                if (knownFiles == null || knownFiles.Count == 0)
                {
                    if (!quiet)
                        Log("SKIP (nothing known locally or in GCS) " + item.BibID + ":" + item.VID);
                    itemsWithNothingInGcs++;
                    continue;
                }

                string localFolder;
                try
                {
                    localFolder = SobekFileSystem.Resource_Network_Uri(item.BibID, item.VID);
                }
                catch (Exception ee)
                {
                    Log("ERROR resolving local folder for " + item.BibID + ":" + item.VID + " -- " + ee.Message);
                    itemsFailed++;
                    continue;
                }

                try
                {
                    if (execute)
                        SobekFileSystem.CreateDirectory(item.BibID, item.VID);
                    bool requiresLocalFileBundle = full || Requires_Local_File_Bundle(localFolder, item.BibID, item.VID, knownFiles, execute, quiet);
                    if (requiresLocalFileBundle && !full)
                        itemsRequiringFullBundle++;

                    // Per-file downloads are each a separate network round-trip -- for an item with many
                    // small files (page images especially, under --full), that latency dominates over any
                    // single file's transfer time, so running several at once is a real win. Items
                    // themselves stay sequential; only the files within one item run in parallel.
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threads };

                    Parallel.ForEach(knownFiles, parallelOptions, file =>
                    {
                        Restore_One_File(file, item.BibID, item.VID, localFolder, full, requiresLocalFileBundle,
                            execute, quiet, force, ref filesDownloaded, ref filesSkipped, ref bytesTransferred);
                    });

                    itemsProcessed++;
                }
                catch (AggregateException aee)
                {
                    string combined = string.Join("; ", aee.InnerExceptions.Select(inner => inner.Message));
                    Log("ERROR processing " + item.BibID + ":" + item.VID + " -- " + combined);
                    itemsFailed++;
                }
                catch (Exception ee)
                {
                    Log("ERROR processing " + item.BibID + ":" + item.VID + " -- " + ee.Message);
                    itemsFailed++;
                }
            }

            Log();
            Log("Items processed:            " + itemsProcessed);
            Log("Items with nothing found:   " + itemsWithNothingInGcs);
            Log("Items failed:                " + itemsFailed);
            if (!full)
                Log("Items needing full bundle:  " + itemsRequiringFullBundle + " (folder-relative viewer -- every file restored, not just the local half)");
            Log("Files downloaded:            " + filesDownloaded);
            Log("Files skipped:               " + filesSkipped + " (already present locally, or GCS-only under " + (full ? "--full, which shouldn't skip anything but a same-name existing file" : "GCS Hybrid classification") + ")");
            Log("Bytes transferred:           " + bytesTransferred);

            logWriter?.Dispose();

            // Previously always returned 0 here regardless of itemsFailed -- a real per-item failure (caught
            // above) never surfaced as a non-zero exit code, so the calling startup script's
            // `if ($LASTEXITCODE -ne 0) { throw ... }` check could never actually catch one.
            return itemsFailed > 0 ? 1 : 0;
        }

        /// <summary> Determines whether an item has a registered viewer (website/HTML/OpenTextbook) that
        /// resolves other files in its folder via same-origin relative paths rather than a signed URL --
        /// if so, its whole folder must be restored (<see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(System.Collections.Generic.IEnumerable{string})"/>),
        /// not just the usual thumbnail/METS/marc.xml local half. Unlike <c>MigrateSobekFileSystem</c>'s
        /// equivalent helper, the METS file may not exist locally yet at all (that's exactly what this
        /// tool might be restoring) -- so this downloads it first if needed, then reads it. </summary>
        private static bool Requires_Local_File_Bundle(string LocalFolder, string BibID, string VID, List<SobekFileSystem_FileInfo> KnownFiles, bool Execute, bool Quiet)
        {
            SobekFileSystem_FileInfo metsFile = KnownFiles.FirstOrDefault(f =>
                f.Name.EndsWith(".mets.xml", StringComparison.OrdinalIgnoreCase) ||
                f.Name.EndsWith(".mets", StringComparison.OrdinalIgnoreCase));

            if (metsFile == null)
                return false;

            string metsPath = Path.Combine(LocalFolder, metsFile.Name);

            try
            {
                if (!File.Exists(metsPath))
                {
                    if (!Execute)
                        return false; // dry run -- can't download to inspect, assume no bundle rather than guess

                    SobekFileSystem.DownloadFile(BibID, VID, metsFile.Name, metsPath);
                }

                SobekCM_Item item = SobekCM_Item.Read_METS(metsPath);
                var viewerTypes = new List<string>();
                if (item.Behaviors.Views_Count > 0)
                {
                    foreach (View_Object view in item.Behaviors.Views)
                        viewerTypes.Add(view.View_Type);
                }

                return Hybrid_FileSystem.Requires_Local_File_Bundle(viewerTypes);
            }
            catch (Exception ee)
            {
                if (!Quiet)
                    Log("  WARNING: could not read METS for " + BibID + ":" + VID + " to check for folder-relative viewers -- " + ee.Message);
                return false;
            }
        }

        /// <summary> One file: downloads it if it belongs local under the active classification (or
        /// unconditionally under <c>--full</c>) and isn't already sitting on disk. Never deletes or
        /// touches GCS -- purely additive. </summary>
        /// <remarks> Runs concurrently across a whole item's files (see the <c>Parallel.ForEach</c> in
        /// <c>Main</c>), so the counter parameters are updated via <see cref="Interlocked"/> rather than
        /// plain increments, and console output is serialized through <see cref="consoleLock"/>. </remarks>
        private static void Restore_One_File(SobekFileSystem_FileInfo File, string BibID, string VID, string LocalFolder,
            bool Full, bool RequiresLocalFileBundle, bool Execute, bool Quiet, bool Force,
            ref int FilesDownloaded, ref int FilesSkipped, ref long BytesTransferred)
        {
            bool belongsLocal = Full || !SobekFileSystem.IsGcsOnly(File.Name, RequiresLocalFileBundle);
            if (!belongsLocal)
            {
                Interlocked.Increment(ref FilesSkipped);
                return;
            }

            string localPath = Path.Combine(LocalFolder, File.Name);
            if (!Force && System.IO.File.Exists(localPath))
            {
                Interlocked.Increment(ref FilesSkipped);
                return;
            }

            if (!Quiet)
                Log((Execute ? "  downloading " : "  would download ") + BibID + ":" + VID + "/" + File.Name + " (" + File.Length + " bytes)");

            if (Execute)
            {
                try
                {
                    SobekFileSystem.DownloadFile(BibID, VID, File.Name, localPath);
                }
                catch (Exception ee)
                {
                    Log("  ERROR downloading " + BibID + ":" + VID + "/" + File.Name + " -- " + ee.Message);
                    throw;
                }

                long actualLength = new FileInfo(localPath).Length;
                if (actualLength != File.Length)
                    Log("  WARNING: " + BibID + ":" + VID + "/" + File.Name + " downloaded as " + actualLength + " bytes, expected " + File.Length + " bytes");
            }

            Interlocked.Increment(ref FilesDownloaded);
            Interlocked.Add(ref BytesTransferred, File.Length);
        }

        private static void Show_Help()
        {
            Console.WriteLine();
            Console.WriteLine("RestoreLocalFileCache -- rebuilds the local half of GCS Hybrid mode (thumbnails,");
            Console.WriteLine("METS, marc.xml) for every item in a SobekCM instance, from data already sitting");
            Console.WriteLine("in its GCS bucket. Never deletes or modifies anything in GCS -- purely additive.");
            Console.WriteLine();
            Console.WriteLine("Usage: RestoreLocalFileCache --instance-path <path> [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --instance-path <path>   Root folder of the target SobekCM deployment (where");
            Console.WriteLine("                            config\\sobekcm.config lives). Must already have a");
            Console.WriteLine("                            GCS Bucket Name configured -- File System Mode does");
            Console.WriteLine("                            NOT need to already be \"GCS Hybrid\"; this tool");
            Console.WriteLine("                            targets Hybrid classification regardless, so it can");
            Console.WriteLine("                            run against a brand new instance before its first");
            Console.WriteLine("                            cutover.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --execute                 Actually download files. Without this flag, the tool");
            Console.WriteLine("                            always runs as a dry run and touches nothing (an item");
            Console.WriteLine("                            needing a full folder bundle can't be detected in a");
            Console.WriteLine("                            dry run if its METS isn't already local, since that");
            Console.WriteLine("                            check itself requires downloading the METS first).");
            Console.WriteLine("  --full                    Download EVERY file for every item -- master/derivative");
            Console.WriteLine("                            images and OCR text included, not just the usual");
            Console.WriteLine("                            thumbnail/METS/marc.xml local half. For a true full");
            Console.WriteLine("                            disaster-recovery restore of a completely lost local");
            Console.WriteLine("                            disk. Without this flag, restores only what GCS Hybrid");
            Console.WriteLine("                            mode actually needs kept locally.");
            Console.WriteLine("  --force                   Re-download even if a same-named local file already");
            Console.WriteLine("                            exists -- otherwise an existing local file of any size");
            Console.WriteLine("                            is left alone rather than overwritten.");
            Console.WriteLine("  --quiet                   Per-item/summary totals only, not per-file output.");
            Console.WriteLine("  --bibid <BibID> --vid <VID>");
            Console.WriteLine("                            Target just this one item instead of every item in the");
            Console.WriteLine("                            database. Must be used together. Skips the database");
            Console.WriteLine("                            item list lookup entirely -- works even without DB");
            Console.WriteLine("                            access, as long as the GCS bucket has the item's files.");
            Console.WriteLine("  --threads N               Number of files to download concurrently within a");
            Console.WriteLine("                            single item (default 8). Small files are mostly");
            Console.WriteLine("                            network-latency-bound, not bandwidth-bound, so raising");
            Console.WriteLine("                            this helps a lot on items with many small files (e.g.");
            Console.WriteLine("                            page images under --full). Items themselves are still");
            Console.WriteLine("                            processed one at a time.");
            Console.WriteLine("  --help                    Shows these instructions.");
            Console.WriteLine();
        }
    }
}
