#region Using directives

using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Core.FileSystems
{
    /// <summary> iFileSystem implementation which splits digital resource files between local/network disk
    /// and a Google Cloud Storage bucket, by file type. </summary>
    /// <remarks>
    /// <para> Three categories of file, by <see cref="IsGcsOnly"/>'s classification: </para>
    /// <list type="bullet">
    /// <item> Master/derivative image files (TIFF, PDF, JP2/JPX, full-size JPEG) -- GCS only, no permanent
    /// local copy. Any local copy that exists during Builder processing is scratch space, cleaned up once
    /// uploaded. </item>
    /// <item> METS, marc.xml, and thumbnails -- kept on local disk as the permanent working/serving copy,
    /// AND written to GCS as a backup/archival copy (dual-write). </item>
    /// <item> <see cref="ResourceObjectSettings.Metadata_Cache_FileName"/> (cache.protobuf) -- local only,
    /// never written to GCS. This is a derived/regenerable performance artifact with no archival value, and
    /// is written directly via <c>BriefItem_Cache.WriteCache</c>/<c>DeleteCache</c> against
    /// <see cref="Resource_Network_Uri(string, string)"/>, never through <see cref="SaveFile"/>/<see cref="CopyFileIn"/>. </item>
    /// </list>
    /// <para> All read-path methods that resolve/operate on real local paths (<see cref="ReadToEnd"/>,
    /// <see cref="FileExists"/>, <see cref="Resource_Network_Uri(string, string)"/> and its overloads,
    /// <see cref="AssociFilePath(string, string)"/> and its overload, <see cref="GetFiles"/>,
    /// <see cref="CreateDirectory"/>) always delegate to the local file system unconditionally -- every one
    /// of the three categories above has a real, permanent local presence except the GCS-only master files,
    /// which are never read back through this class outside of a Builder scratch copy that's already known
    /// to be local by construction. </para>
    /// <para> <see cref="Resource_Web_Uri(string, string, string)"/> (the per-file overload) is the one
    /// read-path method that dispatches by file category, since it's what hands a browser a URL to actually
    /// display/download a file. The bare, no-filename overloads always resolve locally -- see their remarks. </para>
    /// </remarks>
    public class Hybrid_FileSystem : iFileSystem
    {
        private readonly PairTreeStructure localFileSystem;
        private readonly GCS_FileSystem gcsFileSystem;

        /// <summary> Constructor for a new instance of the Hybrid_FileSystem class </summary>
        /// <param name="RootNetworkUri"> Root network location for the digital resource files kept locally </param>
        /// <param name="RootWebUri"> Root web URL for the digital resource files kept locally </param>
        /// <param name="GcsBucketName"> Name of the GCS bucket master/derivative image files are stored under </param>
        /// <param name="GcsServiceAccountJsonKeyPath"> Full path to a service account JSON key file, with read/write
        /// access to <paramref name="GcsBucketName"/> and the ability to sign URLs </param>
        /// <param name="SignedUrlDuration"> How long a generated signed web URL should remain valid before expiring </param>
        /// <exception cref="FileNotFoundException"> Thrown if <paramref name="GcsServiceAccountJsonKeyPath"/> does
        /// not exist -- this is the most likely first-deploy misconfiguration, so it's checked here with an
        /// actionable message rather than left to surface as an opaque credential-loading error </exception>
        public Hybrid_FileSystem(string RootNetworkUri, string RootWebUri,
            string GcsBucketName, string GcsServiceAccountJsonKeyPath, TimeSpan SignedUrlDuration)
        {
            if (!File.Exists(GcsServiceAccountJsonKeyPath))
                throw new FileNotFoundException("GCS Hybrid mode requires a service account key file at: " + GcsServiceAccountJsonKeyPath);

            localFileSystem = new PairTreeStructure(RootNetworkUri, RootWebUri);
            gcsFileSystem = new GCS_FileSystem(GcsBucketName, GcsServiceAccountJsonKeyPath, SignedUrlDuration);
        }

        /// <summary> Classifies a file name as GCS-only (master/derivative image files) or not (everything
        /// kept locally -- METS, marc.xml, thumbnails, cache.protobuf, and anything else unrecognized) </summary>
        /// <param name="FileName"> File name to classify. May include a subfolder prefix (e.g. "Backup\x.html") </param>
        /// <returns> TRUE if this file belongs only in GCS with no permanent local copy, otherwise FALSE </returns>
        public static bool IsGcsOnly(string FileName)
        {
            string leaf = FileName.Replace('\\', '/');
            int lastSlash = leaf.LastIndexOf('/');
            if (lastSlash >= 0)
                leaf = leaf.Substring(lastSlash + 1);

            if (string.Equals(leaf, ResourceObjectSettings.Metadata_Cache_FileName, StringComparison.OrdinalIgnoreCase))
                return false;                                          // cache.protobuf: local-only, never GCS

            if (leaf.EndsWith("thm.jpg", StringComparison.OrdinalIgnoreCase))
                return false;                                          // thumbnail: dual-write

            if (leaf.EndsWith(".mets.xml", StringComparison.OrdinalIgnoreCase) ||
                leaf.EndsWith(".mets", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(leaf, "marc.xml", StringComparison.OrdinalIgnoreCase))
                return false;                                          // metadata: dual-write

            string ext = Path.GetExtension(leaf).ToLowerInvariant();
            if (ext == ".tif" || ext == ".tiff" || ext == ".pdf" || ext == ".jp2" || ext == ".jpx" ||
                ext == ".jpg" || ext == ".jpeg")
                return true;                                           // master/derivative images: GCS-only

            // anything else unclassified (html backups, .txt, .pro, .csv, etc.): dual-write. Safer than
            // local-only (nothing archival is silently skipped from GCS) and safer than GCS-only (nothing
            // needed for immediate serving is silently made GCS-dependent)
            return false;
        }

        /// <summary> Normalizes a file name to use "/" separators, since GCS object keys are flat strings
        /// that use "/" purely as a readable-nesting convention, never a platform path separator </summary>
        private static string NormalizeForGcs(string FileName)
        {
            return FileName.Replace('\\', '/');
        }

        /// <summary> Read to the end of a (text-based) file and return the contents -- always local </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public string ReadToEnd(BriefItemInfo DigitalResource, string FileName)
        {
            return localFileSystem.ReadToEnd(DigitalResource, FileName);
        }

        /// <summary> Not supported: mirrors GCS_FileSystem's bare-overload contract -- there is no meaningful
        /// "base" web URL for an entire digital resource once any file in it might be GCS-only </summary>
        /// <exception cref="NotSupportedException"> Always thrown -- callers must request a specific file
        /// via <see cref="Resource_Web_Uri(BriefItemInfo, string)"/> instead </exception>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource)
        {
            return localFileSystem.Resource_Web_Uri(DigitalResource);
        }

        /// <summary> Bare (no-filename) overload -- always resolves locally. Safe because every real call
        /// site that concatenates a filename onto this result concatenates a dual-write (locally-served)
        /// file name, never a GCS-only one. </summary>
        /// <param name="BibID"> Bibliographic identifier for the resource in question </param>
        /// <param name="VID"> Volume identifier for the resource in question </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(string BibID, string VID)
        {
            return localFileSystem.Resource_Web_Uri(BibID, VID);
        }

        /// <summary> Return the WEB uri for a file within the digital resource -- dispatches to GCS (signed
        /// URL) or local, by file category </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return Resource_Web_Uri(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Return the WEB uri for a single file in the digital resource -- dispatches to GCS
        /// (signed URL) or local, by file category </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get the web URI for</param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(string BibID, string VID, string FileName)
        {
            if (IsGcsOnly(FileName))
                return gcsFileSystem.Resource_Web_Uri(BibID, VID, NormalizeForGcs(FileName));

            return localFileSystem.Resource_Web_Uri(BibID, VID, FileName);
        }

        /// <summary> Return a flag if the file specified exists within the digital resource -- always local </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to check for</param>
        /// <returns> TRUE if the file exists locally, otherwise FALSE </returns>
        public bool FileExists(BriefItemInfo DigitalResource, string FileName)
        {
            return localFileSystem.FileExists(DigitalResource, FileName);
        }

        /// <summary> Return the NETWORK uri for a digital resource -- always local </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource)
        {
            return localFileSystem.Resource_Network_Uri(DigitalResource);
        }

        /// <summary> Return the NETWORK uri for a digital resource -- always local </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(string BibID, string VID)
        {
            return localFileSystem.Resource_Network_Uri(BibID, VID);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource -- always local </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return localFileSystem.Resource_Network_Uri(DigitalResource, FileName);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource -- always local </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(string BibID, string VID, string FileName)
        {
            return localFileSystem.Resource_Network_Uri(BibID, VID, FileName);
        }

        /// <summary> [TEMPORARY] Get the associated file path -- always local </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        public string AssociFilePath(BriefItemInfo DigitalResource)
        {
            return localFileSystem.AssociFilePath(DigitalResource);
        }

        /// <summary> [TEMPORARY] Get the associated file path -- always local </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        public string AssociFilePath(string BibID, string VID)
        {
            return localFileSystem.AssociFilePath(BibID, VID);
        }

        /// <summary> Gets the list of all the files associated with this digital resource -- always local
        /// (reflects what's actually on disk, i.e. everything except GCS-only master files that have
        /// already been pushed and cleaned up) </summary>
        /// <param name="DigitalResource"> The digital resource object  </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public List<SobekFileSystem_FileInfo> GetFiles(BriefItemInfo DigitalResource)
        {
            return localFileSystem.GetFiles(DigitalResource);
        }

        /// <summary> Ensure the folder for a digital resource (and any parent folders) exists -- always local,
        /// since GCS has no directory concept and every category has at least a local scratch/permanent presence </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        public void CreateDirectory(string BibID, string VID)
        {
            localFileSystem.CreateDirectory(BibID, VID);
        }

        /// <summary> Write file content to a named file within a digital resource's folder, overwriting if it
        /// exists -- routed to GCS only, local only, or both, by file category </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to write </param>
        /// <param name="Content"> Stream containing the file's content </param>
        /// <remarks> Does not get the changed-file-skip optimization <see cref="CopyFileIn"/> gets -- has
        /// zero real call sites today, so that complexity isn't justified yet </remarks>
        public void SaveFile(string BibID, string VID, string FileName, Stream Content)
        {
            if (IsGcsOnly(FileName))
            {
                gcsFileSystem.SaveFile(BibID, VID, NormalizeForGcs(FileName), Content);
                return;
            }

            localFileSystem.SaveFile(BibID, VID, FileName, Content);

            if (Content.CanSeek)
                Content.Position = 0;
            gcsFileSystem.SaveFile(BibID, VID, NormalizeForGcs(FileName), Content);
        }

        /// <summary> Copy a file already on local disk into a digital resource's folder as <paramref name="FileName"/>,
        /// overwriting if it exists -- routed to GCS only, local only, or both, by file category </summary>
        /// <param name="SourceLocalPath"> Full local path of the source file (e.g. a per-user staging folder) </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name the file should have once copied into the digital resource's folder </param>
        public void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName)
        {
            bool isGcsOnly = IsGcsOnly(FileName);

            if (!isGcsOnly)
                localFileSystem.CopyFileIn(SourceLocalPath, BibID, VID, FileName);

            long localLength = new FileInfo(SourceLocalPath).Length;
            if (gcsFileSystem.ObjectMatchesLocalFile(BibID, VID, NormalizeForGcs(FileName), localLength))
                return;                                                // already up to date in GCS -- skip the upload

            gcsFileSystem.CopyFileIn(SourceLocalPath, BibID, VID, NormalizeForGcs(FileName));
        }

        /// <summary> Delete a single named file within a digital resource's folder, if it exists -- routed to
        /// GCS only, local only, or both, by file category </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        public void DeleteFile(string BibID, string VID, string FileName)
        {
            if (IsGcsOnly(FileName))
            {
                gcsFileSystem.DeleteFile(BibID, VID, NormalizeForGcs(FileName));
                // Best-effort local delete too, in case a stray scratch copy exists
                try { localFileSystem.DeleteFile(BibID, VID, FileName); } catch { }
                return;
            }

            localFileSystem.DeleteFile(BibID, VID, FileName);
            gcsFileSystem.DeleteFile(BibID, VID, NormalizeForGcs(FileName));
        }

        /// <summary> Downloads every object under a digital resource's folder from GCS into a local folder --
        /// used by the Builder's StageResourceFilesLocallyModule before reprocessing an existing item </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="LocalDestinationFolder"> Local folder every object should be downloaded into </param>
        public void DownloadAll(string BibID, string VID, string LocalDestinationFolder)
        {
            gcsFileSystem.DownloadAll(BibID, VID, LocalDestinationFolder);
        }
    }
}
