using SobekCM.Core.BriefItem;
using SobekCM.Core.Settings;
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
        /// <param name="ForceGcsHybrid"> When TRUE, builds <see cref="Hybrid_FileSystem"/> even if
        /// <see cref="Server_Settings.File_System_Mode"/> is not yet "GCS Hybrid" -- for the pre-cutover
        /// migration utility, which needs to push files to GCS while the live site is still reading/writing
        /// locally. Not used by the running application or Builder. </param>
        /// <remarks> Falls back to plain <see cref="PairTreeStructure"/> for any mode value other than
        /// exactly "GCS Hybrid" (not just "Local") -- a typo'd or not-yet-migrated setting degrades to
        /// always-safe local behavior instead of throwing at startup. </remarks>
        public static void Initialize(InstanceWide_Settings Settings, bool ForceGcsHybrid = false)
        {
            Server_Settings servers = Settings?.Servers;

            if (ForceGcsHybrid || servers?.File_System_Mode == "GCS Hybrid")
            {
                string keyPath = Path.Combine(servers.Base_Directory, "config", "user", "gcs-service-account.json");
                fileSystem = new Hybrid_FileSystem(servers.Image_Server_Network, servers.Image_URL,
                    servers.GCS_Bucket_Name, Settings.System?.System_Code, keyPath, TimeSpan.FromMinutes(servers.GCS_Signed_Url_Expiration_Minutes));
            }
            else
            {
                fileSystem = new PairTreeStructure(servers?.Image_Server_Network ?? "", servers?.Image_URL ?? "");
            }
        }

        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public static string ReadToEnd(BriefItemInfo DigitalResource, string FileName)
        {
            return fileSystem.ReadToEnd(DigitalResource, FileName);
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
    }
}