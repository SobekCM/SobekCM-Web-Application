using SobekCM.Core.BriefItem;
using SobekCM.Core.Settings;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace SobekCM.Core.FileSystems
{
    /// <summary> Class provides uniform access to the sobek file system
    /// which contains all the digital resources </summary>
    public static class SobekFileSystem
    {
        private static iFileSystem fileSystem;

        /// <summary> Initializes the file system, choosing between local disk and GCS Hybrid based on
        /// <see cref="Server_Settings.File_System_Mode"/> </summary>
        /// <param name="Settings"> Instance-wide settings -- both call sites already have this object in hand.
        /// Takes the full object (not just <see cref="InstanceWide_Settings.Servers"/>) because GCS Hybrid mode
        /// also needs <see cref="InstanceWide_Settings.System"/>'s <see cref="System_Settings.System_Code"/> for
        /// the GCS object key prefix. </param>
        /// <param name="GcsServiceAccountJsonPathOverride"> Override for where the GCS service account key
        /// lives, sourced from the caller's own local config rather than the shared DB-backed instance
        /// settings -- the web application reads this from appsettings.json ("GCS:ServiceAccountJsonPath"),
        /// while the Builder reads it per-instance from its own sobekcm.config
        /// (<see cref="SobekCM.Builder_Library.Settings.Single_Instance_Configuration.Gcs_Service_Account_Json_Path"/>),
        /// letting different Builder-managed instances use different keys. Wins over the legacy
        /// Base_Directory-relative default when set. </param>
        /// <param name="ForceGcsHybrid"> When TRUE, builds <see cref="Hybrid_FileSystem"/> even if
        /// <see cref="Server_Settings.File_System_Mode"/> is not yet "GCS Hybrid" -- for the pre-cutover
        /// migration utility, which needs to push files to GCS while the live site is still reading/writing
        /// locally. Not used by the running application or Builder. </param>
        /// <param name="ForceGcsFull"> Same as <paramref name="ForceGcsHybrid"/>, but builds
        /// <see cref="GCS_Full_FileSystem"/> instead -- for the migration utility's pre-cutover run against
        /// an instance headed for "GCS Full" rather than "GCS Hybrid". Mutually exclusive with
        /// <paramref name="ForceGcsHybrid"/>; if both are TRUE, GCS Full wins. </param>
        /// <param name="Tracer"> Optional trace object -- records which concrete <see cref="iFileSystem"/> got
        /// selected, since this runs on every request (see <see cref="SobekCM.Endpoints"/> request pipeline)
        /// and the choice is otherwise invisible in an error trace route </param>
        /// <remarks> Falls back to plain <see cref="PairTreeStructure"/> for any mode value other than
        /// exactly "GCS Hybrid" or "GCS Full" -- a typo'd or not-yet-migrated setting degrades to
        /// always-safe local behavior instead of throwing at startup. </remarks>
        public static void Initialize(InstanceWide_Settings Settings, string GcsServiceAccountJsonPathOverride = null, bool ForceGcsHybrid = false, bool ForceGcsFull = false, Custom_Tracer Tracer = null)
        {
            Server_Settings servers = Settings?.Servers;

            if (ForceGcsFull || servers?.File_System_Mode == "GCS Full")
            {
                string keyPath = Resolve_GCS_Key_Path(servers, GcsServiceAccountJsonPathOverride);
                fileSystem = new GCS_Full_FileSystem(servers.Image_Server_Network, servers.Image_URL,
                    servers.GCS_Bucket_Name, Settings.System?.System_Code, keyPath, TimeSpan.FromMinutes(servers.GCS_Signed_Url_Expiration_Minutes));
            }
            else if (ForceGcsHybrid || servers?.File_System_Mode == "GCS Hybrid")
            {
                string keyPath = Resolve_GCS_Key_Path(servers, GcsServiceAccountJsonPathOverride);
                fileSystem = new Hybrid_FileSystem(servers.Image_Server_Network, servers.Image_URL,
                    servers.GCS_Bucket_Name, Settings.System?.System_Code, keyPath, TimeSpan.FromMinutes(servers.GCS_Signed_Url_Expiration_Minutes));
            }
            else
            {
                fileSystem = new PairTreeStructure(servers?.Image_Server_Network ?? "", servers?.Image_URL ?? "");
            }

            Tracer?.Add_Trace("SobekFileSystem.Initialize", "File_System_Mode='" + (servers?.File_System_Mode ?? "Local") + "', using " + fileSystem.GetType().Name);
        }

        /// <summary> Adds a trace line naming the currently active <see cref="iFileSystem"/> implementation </summary>
        /// <param name="Tracer"> Trace object to record the active file system into </param>
        /// <remarks> For request-scoped callers whose <see cref="Custom_Tracer"/> is constructed after
        /// <see cref="Initialize"/> already ran earlier in the pipeline (e.g. <c>QueryInitializer</c>'s
        /// constructor, which builds its <see cref="Custom_Tracer"/> after <c>RequestContextMiddleware</c>
        /// has already called <see cref="Initialize"/> once per request) -- avoids re-running
        /// <see cref="Initialize"/> just to get a trace line out of it. </remarks>
        public static void Log_Active_File_System(Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("SobekFileSystem.Log_Active_File_System", "Using " + (fileSystem?.GetType().Name ?? "(not yet initialized)"));
        }

        /// <summary> Picks the GCS service account key path: the caller's own local override if given,
        /// otherwise the legacy Base_Directory-relative default. </summary>
        private static string Resolve_GCS_Key_Path(Server_Settings servers, string localOverride)
        {
            if (!String.IsNullOrEmpty(localOverride))
                return localOverride;

            return Path.Combine(servers.Base_Directory, "config", "user", "gcs-service-account.json");
        }

        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public static string ReadToEnd(BriefItemInfo DigitalResource, string FileName)
        {
            return fileSystem.ReadToEnd(DigitalResource, FileName);
        }

        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public static string ReadToEnd(string BibID, string VID, string FileName)
        {
            return fileSystem.ReadToEnd(BibID, VID, FileName);
        }

        /// <summary> Return the WEB uri for a digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the web resource </returns>
        public static string Resource_Web_Uri(BriefItemInfo DigitalResource)
        {
            return fileSystem.Resource_Web_Uri(DigitalResource);
        }

        /// <summary> Return the WEB uri for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier for the resource in question </param>
        /// <param name="VID"> Volume identifier for the resource in question </param>
        /// <returns> URI for the web resource </returns>
        public static string Resource_Web_Uri(string BibID, string VID)
        {
            return fileSystem.Resource_Web_Uri(BibID, VID);
        }

        /// <summary> Return the WEB uri for a file within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> URI for the web resource </returns>
        public static string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return fileSystem.Resource_Web_Uri(DigitalResource, FileName);
        }

        /// <summary> Return the WEB uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get the web URI for</param>
        /// <returns> URI for the web resource </returns>
        public static string Resource_Web_Uri(string BibID, string VID, string FileName)
        {
            return fileSystem.Resource_Web_Uri(BibID, VID, FileName);
        }

        /// <summary> Return the NETWORK uri for a digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public static string Resource_Network_Uri(BriefItemInfo DigitalResource)
        {
            return fileSystem.Resource_Network_Uri(DigitalResource);
        }

        /// <summary> Return the NETWORK uri for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public static string Resource_Network_Uri(string BibID, string VID)
        {
            return fileSystem.Resource_Network_Uri(BibID, VID);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public static string Resource_Network_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return fileSystem.Resource_Network_Uri(DigitalResource, FileName);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public static string Resource_Network_Uri(string BibID, string VID, string FileName)
        {
            return fileSystem.Resource_Network_Uri(BibID, VID, FileName);
        }

        /// <summary> Return a flag if the file specified exists within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to check for</param>
        /// <returns> URI for the web resource </returns>
        public static bool FileExists(BriefItemInfo DigitalResource, string FileName)
        {
            return fileSystem.FileExists(DigitalResource, FileName);
        }

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the 
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        /// <remarks>Why is this temporary?</remarks>
        public static string AssociFilePath(BriefItemInfo DigitalResource)
        {
            return fileSystem.AssociFilePath(DigitalResource);
        }

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the 
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        /// <remarks>Why is this temporary?</remarks>
        public static string AssociFilePath(string BibID, string VID)
        {
            return fileSystem.AssociFilePath(BibID, VID);
        }

        /// <summary> Gets the list of all the files associated with this digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object  </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public static List<SobekFileSystem_FileInfo> GetFiles(BriefItemInfo DigitalResource)
        {
            return fileSystem.GetFiles(DigitalResource);
        }

        /// <summary> Gets the list of all the files associated with this digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public static List<SobekFileSystem_FileInfo> GetFiles(string BibID, string VID)
        {
            return fileSystem.GetFiles(BibID, VID);
        }

        /// <summary> Ensure the folder for a digital resource (and any parent folders) exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        public static void CreateDirectory(string BibID, string VID)
        {
            fileSystem.CreateDirectory(BibID, VID);
        }

        /// <summary> Write file content to a named file within a digital resource's folder, overwriting if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to write </param>
        /// <param name="Content"> Stream containing the file's content </param>
        public static void SaveFile(string BibID, string VID, string FileName, Stream Content)
        {
            fileSystem.SaveFile(BibID, VID, FileName, Content);
        }

        /// <summary> Copy a file already on local disk into a digital resource's folder as <paramref name="FileName"/>, overwriting if it exists </summary>
        /// <param name="SourceLocalPath"> Full local path of the source file (e.g. a per-user staging folder) </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name the file should have once copied into the digital resource's folder </param>
        public static void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName, bool Force = false, bool RequiresLocalFileBundle = false)
        {
            fileSystem.CopyFileIn(SourceLocalPath, BibID, VID, FileName, Force, RequiresLocalFileBundle);
        }

        /// <summary> Delete a single named file within a digital resource's folder, if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        public static void DeleteFile(string BibID, string VID, string FileName)
        {
            fileSystem.DeleteFile(BibID, VID, FileName);
        }

        /// <summary> Downloads every object under a digital resource's folder into a local destination folder.
        /// Only meaningful in GCS Hybrid mode. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="LocalDestinationFolder"> Local folder every object should be downloaded into </param>
        public static void DownloadAll(string BibID, string VID, string LocalDestinationFolder)
        {
            fileSystem.DownloadAll(BibID, VID, LocalDestinationFolder);
        }

        /// <summary> Deletes ONLY the local copy of a GCS-only file, and only after verifying GCS already has
        /// a matching-size copy. Only meaningful in GCS Hybrid mode. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete locally </param>
        /// <returns> TRUE if the local file was deleted (or was already gone), FALSE otherwise </returns>
        public static bool DeleteLocalCopyIfVerifiedInGcs(string BibID, string VID, string FileName, bool RequiresLocalFileBundle = false)
        {
            return fileSystem.DeleteLocalCopyIfVerifiedInGcs(BibID, VID, FileName, RequiresLocalFileBundle);
        }

        /// <summary> TRUE if this file has no permanent local copy under whichever file system is currently
        /// active -- lets a caller ask "is this GCS-only" without knowing or branching on the active mode </summary>
        /// <param name="FileName"> File name to classify. May include a subfolder prefix (e.g. "Backup\x.html") </param>
        /// <param name="RequiresLocalFileBundle"> See the matching parameter on <see cref="CopyFileIn"/> </param>
        /// <returns> TRUE if this file belongs only in GCS with no permanent local copy, otherwise FALSE </returns>
        public static bool IsGcsOnly(string FileName, bool RequiresLocalFileBundle = false)
        {
            return fileSystem.IsGcsOnly(FileName, RequiresLocalFileBundle);
        }

        /// <summary> Downloads a single named object from a digital resource's folder into a specific local
        /// destination path </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to download </param>
        /// <param name="LocalDestinationPath"> Full local path the file should be written to </param>
        public static void DownloadFile(string BibID, string VID, string FileName, string LocalDestinationPath)
        {
            fileSystem.DownloadFile(BibID, VID, FileName, LocalDestinationPath);
        }

        /// <summary> Returns a real, usable local file-system path guaranteed to contain this file's current
        /// bytes -- the file's own permanent local path if it already has one, otherwise a materialized copy
        /// downloaded into a deterministic temp cache location </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to materialize a local copy of </param>
        /// <param name="RequiresLocalFileBundle"> See the matching parameter on <see cref="CopyFileIn"/> </param>
        /// <returns> A local file-system path safe to hand to an ordinary, non-abstracted local-file API </returns>
        /// <remarks> For a GCS-only file, the download is skipped if a same-size copy is already sitting in
        /// the temp cache from an earlier call -- cheap re-use within one request/process run, not a durable
        /// cache across runs (the temp folder is not cleaned up here; it's ordinary OS temp space). </remarks>
        public static string Ensure_Local_Copy(string BibID, string VID, string FileName, bool RequiresLocalFileBundle = false)
        {
            if (!IsGcsOnly(FileName, RequiresLocalFileBundle))
                return fileSystem.Resource_Network_Uri(BibID, VID, FileName);

            string cacheFolder = Path.Combine(Path.GetTempPath(), "SobekCM_FileCache", BibID, VID);
            string cachePath = Path.Combine(cacheFolder, FileName);

            if (!File.Exists(cachePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                fileSystem.DownloadFile(BibID, VID, FileName, cachePath);
            }

            return cachePath;
        }
    }
}