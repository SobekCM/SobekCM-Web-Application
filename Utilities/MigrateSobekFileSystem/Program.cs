#region Using directives

using SobekCM.Core.FileSystems;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM_Resource_Database;
using System;
using System.Collections.Generic;
using System.IO;

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
        static int Main(string[] args)
        {
            string instancePath = null;
            string mode = null;
            bool execute = false;
            bool verbose = false;
            bool force = false;

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

                    case "--force":
                        force = true;
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
                    foreach (string file in Directory.GetFiles(localFolder))
                    {
                        string fileName = Path.GetFileName(file);

                        if (mode == "migrate")
                            Migrate_One_File(file, item.BibID, item.VID, fileName, execute, verbose, force, ref filesUploaded, ref filesSkipped, ref bytesTransferred);
                        else
                            Cleanup_One_File(item.BibID, item.VID, fileName, execute, verbose, ref filesDeleted, ref filesSkipped);
                    }

                    itemsProcessed++;
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

        /// <summary> Migrate mode, one file: upload/verify via SobekFileSystem.CopyFileIn, which routes by
        /// file category exactly as the running application does. Never deletes anything locally. </summary>
        private static void Migrate_One_File(string LocalPath, string BibID, string VID, string FileName,
            bool Execute, bool Verbose, bool Force, ref int FilesUploaded, ref int FilesSkipped, ref long BytesTransferred)
        {
            if (string.Equals(FileName, "cache.protobuf", StringComparison.OrdinalIgnoreCase))
            {
                FilesSkipped++;
                return;
            }

            long length = new FileInfo(LocalPath).Length;

            if (Verbose)
                Console.WriteLine((Execute ? (Force ? "  re-uploading " : "  uploading ") : "  would upload ") + BibID + ":" + VID + "/" + FileName + " (" + length + " bytes)");

            if (Execute)
                SobekFileSystem.CopyFileIn(LocalPath, BibID, VID, FileName, Force);

            FilesUploaded++;
            BytesTransferred += length;
        }

        /// <summary> Cleanup mode, one file: only touches GCS-only files, and only deletes the local copy
        /// once GCS is verified to already have a matching copy -- never deletes from GCS, never touches
        /// dual-write/local-only files. </summary>
        private static void Cleanup_One_File(string BibID, string VID, string FileName,
            bool Execute, bool Verbose, ref int FilesDeleted, ref int FilesSkipped)
        {
            if (!Hybrid_FileSystem.IsGcsOnly(FileName))
            {
                FilesSkipped++;
                return;
            }

            if (!Execute)
            {
                if (Verbose)
                    Console.WriteLine("  would verify+delete " + BibID + ":" + VID + "/" + FileName);
                FilesDeleted++;
                return;
            }

            bool deleted = SobekFileSystem.DeleteLocalCopyIfVerifiedInGcs(BibID, VID, FileName);
            if (deleted)
            {
                FilesDeleted++;
                if (Verbose)
                    Console.WriteLine("  deleted " + BibID + ":" + VID + "/" + FileName);
            }
            else
            {
                FilesSkipped++;
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
            Console.WriteLine("  --verbose                 Per-file console output, not just per-item totals.");
            Console.WriteLine("  --help                    Shows these instructions.");
            Console.WriteLine();
        }
    }
}
