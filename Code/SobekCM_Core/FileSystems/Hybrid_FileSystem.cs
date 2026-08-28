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
    /// <para> Three categories of file, by <see cref="Classify"/>'s classification: </para>
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
        /// <param name="SystemCode"> Code identifying this SobekCM instance, used as the top-level GCS object
        /// key prefix (see <see cref="GCS_FileSystem"/>'s remarks) -- falls back to "SOBEK" if not provided </param>
        /// <param name="GcsServiceAccountJsonKeyPath"> Full path to a service account JSON key file, with read/write
        /// access to <paramref name="GcsBucketName"/> and the ability to sign URLs </param>
        /// <param name="SignedUrlDuration"> How long a generated signed web URL should remain valid before expiring </param>
        /// <exception cref="FileNotFoundException"> Thrown if <paramref name="GcsServiceAccountJsonKeyPath"/> does
        /// not exist -- this is the most likely first-deploy misconfiguration, so it's checked here with an
        /// actionable message rather than left to surface as an opaque credential-loading error </exception>
        public Hybrid_FileSystem(string RootNetworkUri, string RootWebUri,
            string GcsBucketName, string SystemCode, string GcsServiceAccountJsonKeyPath, TimeSpan SignedUrlDuration)
        {
            if (!File.Exists(GcsServiceAccountJsonKeyPath))
                throw new FileNotFoundException("GCS Hybrid mode requires a service account key file at: " + GcsServiceAccountJsonKeyPath);

            localFileSystem = new PairTreeStructure(RootNetworkUri, RootWebUri);
            gcsFileSystem = new GCS_FileSystem(GcsBucketName, SystemCode, GcsServiceAccountJsonKeyPath, SignedUrlDuration);
        }

        /// <summary> The three ways a file can be routed between local disk and GCS </summary>
        internal enum FileCategory
        {
            /// <summary> Master/derivative image files -- GCS only, no permanent local copy </summary>
            GcsOnly,
            /// <summary> METS, marc.xml, thumbnails, and anything else unrecognized -- kept locally AND backed up to GCS </summary>
            DualWrite,
            /// <summary> cache.protobuf -- local only, never written to or deleted from GCS </summary>
            LocalOnly
        }

        /// <summary> Viewer types that resolve other files in an item's folder via same-origin relative
        /// paths at runtime (an iframe'd HTML entry point letting the browser fetch its own sub-resources),
        /// rather than through a per-file signed-URL request -- confirmed by reading each one's Create_Viewer
        /// path: <see cref="SobekCM.Library.ItemViewer.Viewers.HTML_WebSite_ItemViewer"/>,
        /// <see cref="SobekCM.Library.ItemViewer.Viewers.HTML_ItemViewer"/>, the OpenTextbook viewer and its
        /// Divisions variant. An item registered with any of these needs its ENTIRE folder kept local,
        /// regardless of individual file extensions -- GCS has no mechanism to serve a bucket the way a local
        /// folder can be browsed relatively, and there's no per-request hook to rewrite an arbitrary relative
        /// fetch into a signed URL. </summary>
        private static readonly HashSet<string> FolderRelativeViewerTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WEBSITE", "HTML", "OPEN_TEXTBOOK", "OPEN_DIVISIONS" };

        /// <summary> TRUE if any of the given viewer types requires its item's whole file folder to stay
        /// local (see <see cref="FolderRelativeViewerTypes"/>). The general-purpose overload -- takes plain
        /// viewer-type strings rather than a specific item model, so callers holding either the current
        /// <see cref="BriefItemInfo"/> model or the older <c>SobekCM_Item</c> model (e.g. the Builder, which
        /// only ever has the latter) can both funnel through the same one hardcoded list. </summary>
        /// <param name="ViewerTypes"> Every viewer type registered on the item, or NULL if unavailable </param>
        public static bool Requires_Local_File_Bundle(IEnumerable<string> ViewerTypes)
        {
            if (ViewerTypes == null)
                return false;

            foreach (string type in ViewerTypes)
            {
                if (!string.IsNullOrEmpty(type) && FolderRelativeViewerTypes.Contains(type))
                    return true;
            }

            return false;
        }

        /// <summary> TRUE if this item has a registered viewer that requires its whole file folder to stay
        /// local (see <see cref="FolderRelativeViewerTypes"/>) </summary>
        /// <param name="Item"> The digital resource's metadata, or NULL if unavailable to the caller </param>
        public static bool Requires_Local_File_Bundle(BriefItemInfo Item)
        {
            if (Item?.Behaviors?.Viewers == null)
                return false;

            var viewerTypes = new List<string>();
            foreach (BriefItem_BehaviorViewer viewer in Item.Behaviors.Viewers)
                viewerTypes.Add(viewer.ViewerType);

            return Requires_Local_File_Bundle(viewerTypes);
        }

        /// <summary> Classifies a file name into one of the three <see cref="FileCategory"/> values </summary>
        /// <param name="FileName"> File name to classify. May include a subfolder prefix (e.g. "Backup\x.html") </param>
        /// <param name="RequiresLocalFileBundle"> Precomputed result of <see cref="Requires_Local_File_Bundle(BriefItemInfo)"/>
        /// (or the <see cref="IEnumerable{T}"/> overload) for the file's owning item -- callers without cheap
        /// access to the item's viewer list can safely leave this FALSE; classification just falls back to
        /// the flat rules below without the whole-folder override. </param>
        /// <returns> The file's category </returns>
        internal static FileCategory Classify(string FileName, bool RequiresLocalFileBundle = false)
        {
            string leaf = FileName.Replace('\\', '/');
            int lastSlash = leaf.LastIndexOf('/');
            if (lastSlash >= 0)
                leaf = leaf.Substring(lastSlash + 1);

            if (string.Equals(leaf, ResourceObjectSettings.Metadata_Cache_FileName, StringComparison.OrdinalIgnoreCase))
                return FileCategory.LocalOnly;                         // cache.protobuf: local-only, never GCS

            if (leaf.EndsWith("thm.jpg", StringComparison.OrdinalIgnoreCase))
                return FileCategory.DualWrite;                         // thumbnail: dual-write

            if (leaf.EndsWith(".mets.xml", StringComparison.OrdinalIgnoreCase) ||
                leaf.EndsWith(".mets", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(leaf, "marc.xml", StringComparison.OrdinalIgnoreCase))
                return FileCategory.DualWrite;                         // metadata: dual-write

            if (RequiresLocalFileBundle)
                return FileCategory.DualWrite;                         // whole item needs local-folder serving

            // Everything else defaults to GCS-only -- flipped from the old DualWrite-by-default so arbitrary
            // large downloads (ZIPs, audio, video, CAD files, whatever an agency submits next) actually get
            // their local copy cleaned up instead of accumulating on disk forever. The seven master/derivative
            // image extensions this used to allowlist explicitly need no special case any more -- they were
            // never anything but GcsOnly, which is now also the default for everything else.
            return FileCategory.GcsOnly;
        }

        /// <summary> Convenience wrapper over <see cref="Classify"/> for the read-path/cleanup callers that
        /// only ever need the 2-way "does this belong in GCS at all" answer </summary>
        /// <param name="FileName"> File name to classify. May include a subfolder prefix (e.g. "Backup\x.html") </param>
        /// <param name="RequiresLocalFileBundle"> See <see cref="Classify"/> </param>
        /// <returns> TRUE if this file belongs only in GCS with no permanent local copy, otherwise FALSE </returns>
        public static bool IsGcsOnly(string FileName, bool RequiresLocalFileBundle = false)
        {
            return Classify(FileName, RequiresLocalFileBundle) == FileCategory.GcsOnly;
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

        /// <summary> Bare (no-filename) overload -- always resolves locally. Safe because the only real call
        /// sites that concatenate a filename onto this result belong to the same folder-relative viewer types
        /// (<see cref="FolderRelativeViewerTypes"/>) that <see cref="Classify"/> already forces to <c>DualWrite</c>
        /// (locally-served) for their whole item -- never a GCS-only file. </summary>
        /// <param name="BibID"> Bibliographic identifier for the resource in question </param>
        /// <param name="VID"> Volume identifier for the resource in question </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(string BibID, string VID)
        {
            return localFileSystem.Resource_Web_Uri(BibID, VID);
        }

        /// <summary> Return the WEB uri for a file within the digital resource -- dispatches to GCS (signed
        /// URL) or local, by file category </summary>
        /// <param name="DigitalResource"> The digital resource object, used both to address the file and to
        /// check <see cref="Requires_Local_File_Bundle"/> </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            if (IsGcsOnly(FileName, Requires_Local_File_Bundle(DigitalResource)))
                return gcsFileSystem.Resource_Web_Uri(DigitalResource.BibID, DigitalResource.VID, NormalizeForGcs(FileName));

            return localFileSystem.Resource_Web_Uri(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Return the WEB uri for a single file in the digital resource -- dispatches to GCS
        /// (signed URL) or local, by file category. No item context available at this overload, so the
        /// folder-relative-viewer override in <see cref="Classify"/> can't apply -- only used today for
        /// thumbnails (see <see cref="SobekCM.Library.ItemViewer.Viewers.MultiVolumes_ItemViewer"/>), which
        /// classify <c>DualWrite</c> regardless via the <c>thm.jpg</c> check, so this is safe in practice. </summary>
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
            FileCategory category = Classify(FileName);

            if (category == FileCategory.GcsOnly)
            {
                gcsFileSystem.SaveFile(BibID, VID, NormalizeForGcs(FileName), Content);
                return;
            }

            localFileSystem.SaveFile(BibID, VID, FileName, Content);

            if (category == FileCategory.LocalOnly)
                return;

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
        public void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName, bool Force = false, bool RequiresLocalFileBundle = false)
        {
            FileCategory category = Classify(FileName, RequiresLocalFileBundle);

            if (category != FileCategory.GcsOnly)
                localFileSystem.CopyFileIn(SourceLocalPath, BibID, VID, FileName);

            if (category == FileCategory.LocalOnly)
                return;

            if (!Force)
            {
                long localLength = new FileInfo(SourceLocalPath).Length;
                if (gcsFileSystem.ObjectMatchesLocalFile(BibID, VID, NormalizeForGcs(FileName), localLength))
                    return;                                            // already up to date in GCS -- skip the upload
            }

            gcsFileSystem.CopyFileIn(SourceLocalPath, BibID, VID, NormalizeForGcs(FileName));
        }

        /// <summary> Delete a single named file within a digital resource's folder, if it exists -- routed to
        /// GCS only, local only, or both, by file category </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        public void DeleteFile(string BibID, string VID, string FileName)
        {
            FileCategory category = Classify(FileName);

            if (category == FileCategory.GcsOnly)
            {
                gcsFileSystem.DeleteFile(BibID, VID, NormalizeForGcs(FileName));
                // Best-effort local delete too, in case a stray scratch copy exists
                try { localFileSystem.DeleteFile(BibID, VID, FileName); } catch { }
                return;
            }

            localFileSystem.DeleteFile(BibID, VID, FileName);

            if (category == FileCategory.LocalOnly)
                return;

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

        /// <summary> Deletes ONLY the local copy of a GCS-only file, and only after verifying GCS already has
        /// a matching-size copy -- never deletes anything from GCS, and never touches a dual-write/local-only
        /// file. Used by the migration utility's cleanup mode. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete locally </param>
        /// <param name="RequiresLocalFileBundle"> Precomputed <see cref="Requires_Local_File_Bundle(BriefItemInfo)"/>
        /// result for the file's owning item -- see <see cref="Classify"/>. Passing this correctly matters a
        /// lot here specifically: leaving it FALSE for a folder-relative-viewer item (website/HTML/OpenTextbook)
        /// would misclassify it as GcsOnly under the flipped default and this method would delete local copies
        /// of files that must stay local. </param>
        /// <returns> TRUE if the local file was deleted (or was already gone), FALSE if it was left in place
        /// because GCS did not have a verified matching copy, or because the file isn't GCS-only </returns>
        public bool DeleteLocalCopyIfVerifiedInGcs(string BibID, string VID, string FileName, bool RequiresLocalFileBundle = false)
        {
            if (Classify(FileName, RequiresLocalFileBundle) != FileCategory.GcsOnly)
                return false;

            string localPath = localFileSystem.Resource_Network_Uri(BibID, VID, FileName);
            if (!File.Exists(localPath))
                return true;

            long localLength = new FileInfo(localPath).Length;
            if (!gcsFileSystem.ObjectMatchesLocalFile(BibID, VID, NormalizeForGcs(FileName), localLength))
                return false;

            File.Delete(localPath);
            return true;
        }
    }
}
