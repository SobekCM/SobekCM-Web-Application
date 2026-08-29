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

namespace SobekCM.MigrateFileSystem
{
    /// <summary> One-time/occasional bulk utility that walks every existing digital item in a SobekCM
    /// instance's database and either pushes their files up to GCS (--mode migrate) or, once that's been
    /// independently confirmed correct, deletes the now-redundant local copies of GCS-only master files
    /// (--mode cleanup). </summary>
    /// <remarks> Deliberately reuses <see cref="SobekFileSystem"/>/<see cref="Hybrid_FileSystem"/> directly
    /// rather than any separate upload/classification logic -- the same per-file routing rules, the same
    /// changed-file-skip optimization, and the same GCS-verification-before-delete safety check that the
    /// running application and Builder already use. Bootstraps settings the exact same lightweight way the
    /// app's own lazy <see cref="Engine_ApplicationCache_Gateway.Settings"/> property does, rather than the
    /// much heavier <c>Worker_BulkLoader.Refresh_Settings_And_Item_List</c>, which drags in microservice
    /// HTTP calls and plugin-assembly syncing this tool has no need for. </remarks>
    public class Program
    {
        /// <summary> Synchronizes console output once file-level work runs in parallel -- otherwise
        /// interleaved writes from concurrent threads garble each other mid-line </summary>
        private static readonly object consoleLock = new object();

        static int Main(string[] args)
        {
            string instancePath = null;
            string mode = null;
            bool execute = false;
            bool verbose = true;
            bool force = false;
            int threads = 8;
            string targetBibID = null;
            string targetVID = null;
            string targetFileName = null;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--instance-path":
                        if (i + 1 < args.Length)
                            instancePath = args[++i];
                        break;

                    case "--mode":
                        if (i + 1 < args.Length)
                            mode = args[++i];
                        break;

                    case "--execute":
                        execute = true;
                        break;

                    case "--verbose":
                        verbose = true;
                        break;

                    case "--quiet":
                        verbose = false;
                        break;

                    case "--force":
                        force = true;
                        break;

                    case "--bibid":
                        if (i + 1 < args.Length)
                            targetBibID = args[++i];
                        break;

                    case "--vid":
                        if (i + 1 < args.Length)
                            targetVID = args[++i];
                        break;

                    case "--file":
                        if (i + 1 < args.Length)
                            targetFileName = args[++i];
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

            if (string.IsNullOrEmpty(instancePath) || (mode != "migrate" && mode != "cleanup"))
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

            if (!string.IsNullOrEmpty(targetFileName) && string.IsNullOrEmpty(targetBibID))
            {
                Console.WriteLine("--file requires --bibid and --vid.");
                Show_Help();
                return 1;
            }

            if (!Directory.Exists(instancePath))
            {
                Console.WriteLine("Instance path does not exist: " + instancePath);
                return 1;
            }

            if (!execute)
            {
                Console.WriteLine("DRY RUN -- pass --execute to actually " + (mode == "migrate" ? "upload files." : "delete local files."));
                Console.WriteLine();
            }

            // Bootstrap settings exactly the way the running app does: point AppRoot_Gateway at the
            // instance's own directory, then just touch Settings -- it lazy-loads via
            // {instancePath}\config\sobekcm.config, which supplies the DB connection string
            AppRoot_Gateway.AppRootPath = instancePath;
            InstanceWide_Settings settings = Engine_ApplicationCache_Gateway.Settings;

            // "migrate" is meant to run BEFORE cutover, with the live site still serving files locally --
            // it never touches local files, so it only needs a bucket name configured, not File System Mode
            // already flipped to "GCS Hybrid". Pass ForceGcsHybrid so SobekFileSystem.Initialize builds
            // Hybrid_FileSystem regardless of the live mode setting (which would otherwise fall back to
            // plain local disk and silently do nothing useful).
            //
            // "cleanup" deletes local copies, so it stays gated on the site having actually cut over --
            // run it only once File System Mode is really "GCS Hybrid".
            if (mode == "migrate")
            {
                if (string.IsNullOrWhiteSpace(settings?.Servers?.GCS_Bucket_Name))
                {
                    Console.WriteLine("GCS Bucket Name is not configured for this instance -- nothing to migrate.");
                    Console.WriteLine("Set GCS Bucket Name and put the service account key file in place first.");
                    return 1;
                }

                SobekFileSystem.Initialize(settings, ForceGcsHybrid: true);
            }
            else
            {
                if (settings?.Servers?.File_System_Mode != "GCS Hybrid")
                {
                    Console.WriteLine("File System Mode is not \"GCS Hybrid\" for this instance -- nothing to clean up.");
                    Console.WriteLine("Cleanup deletes local files, so it only runs once the site has actually cut over.");
                    return 1;
                }

                SobekFileSystem.Initialize(settings);
            }

            List<(string BibID, string VID)> items;
            if (!string.IsNullOrEmpty(targetBibID))
            {
                // Single-item (or single-file) targeting -- e.g. re-running migrate for just the one file
                // cleanup left in place with a "no verified GCS copy" warning, because it was never
                // successfully uploaded (or uploaded with a size mismatch) in an earlier bulk migrate run.
                // Purely file-driven, same as the bulk path once it has a (BibID, VID) in hand -- no DB
                // lookup needed here, so this works even without database connectivity.
                items = new List<(string BibID, string VID)> { (targetBibID, targetVID) };
                Console.WriteLine("Targeting single item: " + targetBibID + ":" + targetVID +
                    (targetFileName != null ? "/" + targetFileName : " (all files)"));
            }
            else
            {
                try
                {
                    items = SobekCM_Item_Database.Get_All_BibID_VID_Pairs();
                }
                catch (Exception ee)
                {
                    Console.WriteLine("ERROR reading item list from the database: " + ee.Message);
                    return 1;
                }

                Console.WriteLine("Found " + items.Count + " item(s).");
            }
            Console.WriteLine();

            int itemsProcessed = 0, itemsMissingLocally = 0, itemsFailed = 0;
            int filesUploaded = 0, filesDeleted = 0, filesSkipped = 0;
            long bytesTransferred = 0;

            foreach ((string BibID, string VID) item in items)
            {
                string localFolder;
                try
                {
                    localFolder = SobekFileSystem.Resource_Network_Uri(item.BibID, item.VID);
                }
                catch (Exception ee)
                {
                    Console.WriteLine("ERROR resolving local folder for " + item.BibID + ":" + item.VID + " -- " + ee.Message);
                    itemsFailed++;
                    continue;
                }

                if (!Directory.Exists(localFolder))
                {
                    if (verbose)
                        Console.WriteLine("SKIP (no local folder) " + item.BibID + ":" + item.VID);
                    itemsMissingLocally++;
                    continue;
                }

                try
                {
                    bool requiresLocalFileBundle = Requires_Local_File_Bundle(localFolder, item.BibID, item.VID, verbose);

                    // Per-file uploads/deletes are each a separate network round-trip -- for an item with
                    // many small files (page images especially), that latency dominates over any single
                    // file's transfer time, so running several at once is a real win. Items themselves stay
                    // sequential; only the files within one item run in parallel.
                    var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = threads };

                    string[] filesToProcess = Directory.GetFiles(localFolder);
                    if (!string.IsNullOrEmpty(targetFileName))
                    {
                        filesToProcess = filesToProcess
                            .Where(file => string.Equals(Path.GetFileName(file), targetFileName, StringComparison.OrdinalIgnoreCase))
                            .ToArray();

                        if (filesToProcess.Length == 0)
                        {
                            Console.WriteLine("ERROR: " + targetFileName + " not found locally in " + localFolder);
                            itemsFailed++;
                            continue;
                        }
                    }

                    if (mode == "migrate")
                    {
                        Parallel.ForEach(filesToProcess, parallelOptions, file =>
                        {
                            string fileName = Path.GetFileName(file);
                            Migrate_One_File(file, item.BibID, item.VID, fileName, execute, verbose, force, requiresLocalFileBundle, ref filesUploaded, ref filesSkipped, ref bytesTransferred);
                        });
                    }
                    else
                    {
                        Parallel.ForEach(filesToProcess, parallelOptions, file =>
                        {
                            string fileName = Path.GetFileName(file);
                            Cleanup_One_File(item.BibID, item.VID, fileName, execute, verbose, requiresLocalFileBundle, ref filesDeleted, ref filesSkipped);
                        });
                    }

                    itemsProcessed++;
                }
                catch (AggregateException aee)
                {
                    string combined = string.Join("; ", aee.InnerExceptions.Select(inner => inner.Message));
                    Console.WriteLine("ERROR processing " + item.BibID + ":" + item.VID + " -- " + combined);
                    itemsFailed++;
                }
                catch (Exception ee)
                {
                    Console.WriteLine("ERROR processing " + item.BibID + ":" + item.VID + " -- " + ee.Message);
                    itemsFailed++;
                }
            }

            Console.WriteLine();
            Console.WriteLine("Items processed:         " + itemsProcessed);
            Console.WriteLine("Items with no local copy: " + itemsMissingLocally);
            Console.WriteLine("Items failed:             " + itemsFailed);
            if (mode == "migrate")
            {
                Console.WriteLine("Files uploaded/verified:  " + filesUploaded);
                Console.WriteLine("Files skipped:            " + filesSkipped + " (cache.protobuf, always skipped)");
                Console.WriteLine("Bytes transferred:        " + bytesTransferred);
            }
            else
            {
                Console.WriteLine("Files deleted locally:    " + filesDeleted);
                Console.WriteLine("Files skipped:            " + filesSkipped + " (not GCS-only, or no verified GCS copy)");
            }

            return 0;
        }

        /// <summary> Determines whether an item has a registered viewer (website/HTML/OpenTextbook) that
        /// resolves other files in its folder via same-origin relative paths rather than a signed URL --
        /// if so, its whole folder must stay local (<see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(System.Collections.Generic.IEnumerable{string})"/>)
        /// regardless of individual file extensions, and this tool must not GCS-only-classify (migrate mode)
        /// or delete the local copy of (cleanup mode) any of its files. Reads the item's own METS file, the
        /// same source of truth <see cref="SobekCM.Builder_Library.Modules.Items.PushMasterFilesToGcsModule"/>
        /// uses (via the full application's loaded item metadata) -- this tool has no such object in hand
        /// already, so it loads it directly, once per item. </summary>
        private static bool Requires_Local_File_Bundle(string LocalFolder, string BibID, string VID, bool Verbose)
        {
            string metsPath = Path.Combine(LocalFolder, BibID + "_" + VID + ".mets");
            if (!File.Exists(metsPath))
                return false;

            try
            {
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
                if (Verbose)
                    Console.WriteLine("  WARNING: could not read METS for " + BibID + ":" + VID + " to check for folder-relative viewers -- " + ee.Message);
                return false;
            }
        }

        /// <summary> Migrate mode, one file: upload/verify via SobekFileSystem.CopyFileIn, which routes by
        /// file category exactly as the running application does. Never deletes anything locally. </summary>
        /// <remarks> Runs concurrently across a whole item's files (see the <c>Parallel.ForEach</c> in
        /// <c>Main</c>), so the counter parameters are updated via <see cref="Interlocked"/> rather than
        /// plain increments, and console output is serialized through <see cref="consoleLock"/>. </remarks>
        private static void Migrate_One_File(string LocalPath, string BibID, string VID, string FileName,
            bool Execute, bool Verbose, bool Force, bool RequiresLocalFileBundle, ref int FilesUploaded, ref int FilesSkipped, ref long BytesTransferred)
        {
            if (string.Equals(FileName, "cache.protobuf", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref FilesSkipped);
                return;
            }

            long length = new FileInfo(LocalPath).Length;

            if (Verbose)
            {
                lock (consoleLock)
                    Console.WriteLine((Execute ? (Force ? "  re-uploading " : "  uploading ") : "  would upload ") + BibID + ":" + VID + "/" + FileName + " (" + length + " bytes)");
            }

            if (Execute)
                SobekFileSystem.CopyFileIn(LocalPath, BibID, VID, FileName, Force, RequiresLocalFileBundle);

            Interlocked.Increment(ref FilesUploaded);
            Interlocked.Add(ref BytesTransferred, length);
        }

        /// <summary> Cleanup mode, one file: only touches GCS-only files, and only deletes the local copy
        /// once GCS is verified to already have a matching copy -- never deletes from GCS, never touches
        /// dual-write/local-only files. </summary>
        /// <remarks> Runs concurrently across a whole item's files -- see the remarks on <see cref="Migrate_One_File"/>. </remarks>
        private static void Cleanup_One_File(string BibID, string VID, string FileName,
            bool Execute, bool Verbose, bool RequiresLocalFileBundle, ref int FilesDeleted, ref int FilesSkipped)
        {
            if (!Hybrid_FileSystem.IsGcsOnly(FileName, RequiresLocalFileBundle))
            {
                Interlocked.Increment(ref FilesSkipped);
                return;
            }

            if (!Execute)
            {
                if (Verbose)
                {
                    lock (consoleLock)
                        Console.WriteLine("  would verify+delete " + BibID + ":" + VID + "/" + FileName);
                }
                Interlocked.Increment(ref FilesDeleted);
                return;
            }

            bool deleted = SobekFileSystem.DeleteLocalCopyIfVerifiedInGcs(BibID, VID, FileName, RequiresLocalFileBundle);
            if (deleted)
            {
                Interlocked.Increment(ref FilesDeleted);
                if (Verbose)
                {
                    lock (consoleLock)
                        Console.WriteLine("  deleted " + BibID + ":" + VID + "/" + FileName);
                }
            }
            else
            {
                Interlocked.Increment(ref FilesSkipped);
                lock (consoleLock)
                    Console.WriteLine("  WARNING: no verified GCS copy for " + BibID + ":" + VID + "/" + FileName + " -- left in place");
            }
        }

        private static void Show_Help()
        {
            Console.WriteLine();
            Console.WriteLine("MigrateSobekFileSystem -- bulk-pushes an existing SobekCM instance's files to GCS,");
            Console.WriteLine("or (separately, later) cleans up local copies once a migration is confirmed correct.");
            Console.WriteLine();
            Console.WriteLine("Usage: MigrateSobekFileSystem --instance-path <path> --mode migrate|cleanup [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --instance-path <path>   Root folder of the target SobekCM deployment (where");
            Console.WriteLine("                            config\\sobekcm.config lives).");
            Console.WriteLine("  --mode migrate            Upload/verify every item's files in GCS. Never deletes");
            Console.WriteLine("                            anything locally. Requires GCS Bucket Name configured");
            Console.WriteLine("                            and the service account key in place -- runs fine even");
            Console.WriteLine("                            while File System Mode is still \"Local\" (pre-cutover).");
            Console.WriteLine("  --mode cleanup            Delete local copies of GCS-only master files, but only");
            Console.WriteLine("                            after verifying GCS already has a matching copy. Run");
            Console.WriteLine("                            this separately, later, once a migrate run is confirmed.");
            Console.WriteLine("                            Requires File System Mode already set to \"GCS Hybrid\".");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --execute                 Actually perform uploads/deletions. Without this flag,");
            Console.WriteLine("                            the tool always runs as a dry run and touches nothing.");
            Console.WriteLine("  --force                   migrate only: re-upload even if GCS already has a");
            Console.WriteLine("                            same-size object -- otherwise the changed-file-skip");
            Console.WriteLine("                            optimization treats matching size as \"already migrated\"");
            Console.WriteLine("                            and won't re-upload just to fix wrong object metadata");
            Console.WriteLine("                            (e.g. content type) on bytes that didn't actually change.");
            Console.WriteLine("  --quiet                   Per-item totals only, not per-file console output.");
            Console.WriteLine("                            Per-file output is on by default (--verbose is still");
            Console.WriteLine("                            accepted but is now a no-op, kept for compatibility).");
            Console.WriteLine("  --bibid <BibID> --vid <VID>");
            Console.WriteLine("                            Target just this one item instead of every item in the");
            Console.WriteLine("                            database -- e.g. to re-run migrate for a single file");
            Console.WriteLine("                            cleanup left in place with a \"no verified GCS copy\"");
            Console.WriteLine("                            warning. Must be used together. Skips the database item");
            Console.WriteLine("                            list lookup entirely -- works even without DB access.");
            Console.WriteLine("  --file <FileName>         Narrows a --bibid/--vid run to just this one file within");
            Console.WriteLine("                            that item, instead of every file in its folder. Requires");
            Console.WriteLine("                            --bibid and --vid.");
            Console.WriteLine("  --threads N               Number of files to upload/delete concurrently within");
            Console.WriteLine("                            a single item (default 8). Small files are mostly");
            Console.WriteLine("                            network-latency-bound, not bandwidth-bound, so raising");
            Console.WriteLine("                            this helps a lot on items with many small files (e.g.");
            Console.WriteLine("                            page images). Items themselves are still processed one");
            Console.WriteLine("                            at a time.");
            Console.WriteLine("  --help                    Shows these instructions.");
            Console.WriteLine();
        }
    }
}
