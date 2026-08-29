#region Using directives

using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Core.FileSystems
{
    /// <summary> iFileSystem implementation which keeps virtually every digital resource file in Google Cloud
    /// Storage only, with no permanent local copy of anything except the derived <c>cache.protobuf</c>
    /// performance cache -- for an instance with no local/network disk footprint of real content at all
    /// (e.g. a developer's workstation with no access to the shared image server). </summary>
    /// <remarks>
    /// <para> This is <see cref="Hybrid_FileSystem"/>'s sibling, and reuses its <see cref="Hybrid_FileSystem.FileCategory"/>
    /// enum and <see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(BriefItemInfo)"/> helper directly. The
    /// one real difference in classification is that METS, marc.xml, and thumbnails are no longer automatically
    /// kept local -- Hybrid's "always dual-write these three" special case is gone. Two categories, by
    /// <see cref="Classify"/>: </para>
    /// <list type="bullet">
    /// <item> <see cref="ResourceObjectSettings.Metadata_Cache_FileName"/> (cache.protobuf) -- local only, never
    /// written to GCS. Same rationale as <see cref="Hybrid_FileSystem"/>: a derived/regenerable performance
    /// artifact with no archival value. </item>
    /// <item> Everything else -- GCS only, no permanent local copy -- <b>unless</b> the owning item has a
    /// registered viewer that resolves other files in its folder via same-origin relative paths (website/HTML/
    /// OpenTextbook), in which case its whole folder stays local AND is archived to GCS, for the same reason
    /// <see cref="Hybrid_FileSystem"/> carves this case out: GCS has no mechanism to serve a bucket the way a
    /// local folder can be browsed relatively. </item>
    /// </list>
    /// <para> Because METS/thumbnails are no longer guaranteed local, every read-path method here -- unlike
    /// <see cref="Hybrid_FileSystem"/>'s, which always delegate to local disk -- has to dispatch by
    /// <see cref="Classify"/> just like the write-path methods already do. </para>
    /// </remarks>
    public class GCS_Full_FileSystem : iFileSystem
    {
        private readonly PairTreeStructure localFileSystem;
        private readonly GCS_FileSystem gcsFileSystem;

        /// <summary> Constructor for a new instance of the GCS_Full_FileSystem class </summary>
        /// <param name="RootNetworkUri"> Root network location for the digital resource files kept locally
        /// (cache.protobuf, and any folder-relative-viewer item's whole bundle) </param>
        /// <param name="RootWebUri"> Root web URL for the digital resource files kept locally </param>
        /// <param name="GcsBucketName"> Name of the GCS bucket every other digital resource file is stored under </param>
        /// <param name="SystemCode"> Code identifying this SobekCM instance, used as the top-level GCS object
        /// key prefix (see <see cref="GCS_FileSystem"/>'s remarks) -- falls back to "SOBEK" if not provided </param>
        /// <param name="GcsServiceAccountJsonKeyPath"> Full path to a service account JSON key file, with read/write
        /// access to <paramref name="GcsBucketName"/> and the ability to sign URLs </param>
        /// <param name="SignedUrlDuration"> How long a generated signed web URL should remain valid before expiring </param>
        /// <exception cref="FileNotFoundException"> Thrown if <paramref name="GcsServiceAccountJsonKeyPath"/> does
        /// not exist -- this is the most likely first-deploy misconfiguration, so it's checked here with an
        /// actionable message rather than left to surface as an opaque credential-loading error </exception>
        public GCS_Full_FileSystem(string RootNetworkUri, string RootWebUri,
            string GcsBucketName, string SystemCode, string GcsServiceAccountJsonKeyPath, TimeSpan SignedUrlDuration)
        {
            if (!File.Exists(GcsServiceAccountJsonKeyPath))
                throw new FileNotFoundException("GCS Full mode requires a service account key file at: " + GcsServiceAccountJsonKeyPath);

            localFileSystem = new PairTreeStructure(RootNetworkUri, RootWebUri);
            gcsFileSystem = new GCS_FileSystem(GcsBucketName, SystemCode, GcsServiceAccountJsonKeyPath, SignedUrlDuration);
        }

        /// <summary> Classifies a file name into local-only or GCS-only -- see the class remarks </summary>
        /// <param name="FileName"> File name to classify. May include a subfolder prefix (e.g. "Backup\x.html") </param>
        /// <param name="RequiresLocalFileBundle"> Precomputed result of <see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(BriefItemInfo)"/>
        /// (or the <see cref="IEnumerable{T}"/> overload) for the file's owning item -- callers without cheap
        /// access to the item's viewer list can safely leave this FALSE; classification just falls back to
        /// GCS-only without the whole-folder override. </param>
        /// <returns> The file's category </returns>
        internal static Hybrid_FileSystem.FileCategory Classify(string FileName, bool RequiresLocalFileBundle = false)
        {
            string leaf = FileName.Replace('\\', '/');
            int lastSlash = leaf.LastIndexOf('/');
            if (lastSlash >= 0)
                leaf = leaf.Substring(lastSlash + 1);

            if (string.Equals(leaf, ResourceObjectSettings.Metadata_Cache_FileName, StringComparison.OrdinalIgnoreCase))
                return Hybrid_FileSystem.FileCategory.LocalOnly;        // cache.protobuf: local-only, never GCS

            if (RequiresLocalFileBundle)
                return Hybrid_FileSystem.FileCategory.DualWrite;       // whole item needs local-folder serving

            // Unlike Hybrid_FileSystem, METS/marc.xml/thumbnails get no special case here -- under GCS Full
            // there is no permanent local copy of anything except cache.protobuf and a folder-bundle item.
            return Hybrid_FileSystem.FileCategory.GcsOnly;
        }

        /// <summary> Convenience wrapper over <see cref="Classify"/> for callers that only need the 2-way
        /// "does this belong in GCS at all" answer </summary>
        /// <param name="FileName"> File name to classify. May include a subfolder prefix (e.g. "Backup\x.html") </param>
        /// <param name="RequiresLocalFileBundle"> See <see cref="Classify"/> </param>
        /// <returns> TRUE if this file belongs only in GCS with no permanent local copy, otherwise FALSE </returns>
        public static bool IsGcsOnly(string FileName, bool RequiresLocalFileBundle = false)
        {
            return Classify(FileName, RequiresLocalFileBundle) == Hybrid_FileSystem.FileCategory.GcsOnly;
        }

        /// <summary> Instance-method form of <see cref="IsGcsOnly(string, bool)"/> -- see
        /// <see cref="SobekFileSystem.IsGcsOnly"/> </summary>
        bool iFileSystem.IsGcsOnly(string FileName, bool RequiresLocalFileBundle)
        {
            return IsGcsOnly(FileName, RequiresLocalFileBundle);
        }

        /// <summary> Normalizes a file name to use "/" separators, since GCS object keys are flat strings
        /// that use "/" purely as a readable-nesting convention, never a platform path separator </summary>
        private static string NormalizeForGcs(string FileName)
        {
            return FileName.Replace('\\', '/');
        }

        /// <summary> Read to the end of a (text-based) file and return the contents -- dispatches to GCS or
        /// local, by file category </summary>
        /// <param name="DigitalResource"> The digital resource object, used both to address the file and to
        /// check <see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(BriefItemInfo)"/> </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public string ReadToEnd(BriefItemInfo DigitalResource, string FileName)
        {
            if (IsGcsOnly(FileName, Hybrid_FileSystem.Requires_Local_File_Bundle(DigitalResource)))
                return gcsFileSystem.ReadToEnd(DigitalResource.BibID, DigitalResource.VID, NormalizeForGcs(FileName));

            return localFileSystem.ReadToEnd(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Read to the end of a (text-based) file and return the contents -- dispatches to GCS or
        /// local, by file category. No item context available at this overload, so the folder-relative-viewer
        /// override in <see cref="Classify"/> can't apply. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public string ReadToEnd(string BibID, string VID, string FileName)
        {
            if (IsGcsOnly(FileName))
                return gcsFileSystem.ReadToEnd(BibID, VID, NormalizeForGcs(FileName));

            return localFileSystem.ReadToEnd(BibID, VID, FileName);
        }

        /// <summary> Bare (no-filename) overload -- always resolves locally, same reasoning as
        /// <see cref="Resource_Web_Uri(string, string)"/> </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource)
        {
            return localFileSystem.Resource_Web_Uri(DigitalResource);
        }

        /// <summary> Bare (no-filename) overload -- always resolves locally. Safe because the only real call
        /// sites that concatenate a filename onto this result belong to the same folder-relative viewer types
        /// that <see cref="Classify"/> always forces to <c>DualWrite</c> (locally-served) for their whole item --
        /// never a GCS-only file. Same reasoning as <see cref="Hybrid_FileSystem.Resource_Web_Uri(string, string)"/>. </summary>
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
        /// check <see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(BriefItemInfo)"/> </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            if (IsGcsOnly(FileName, Hybrid_FileSystem.Requires_Local_File_Bundle(DigitalResource)))
                return gcsFileSystem.Resource_Web_Uri(DigitalResource.BibID, DigitalResource.VID, NormalizeForGcs(FileName));

            return localFileSystem.Resource_Web_Uri(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Return the WEB uri for a single file in the digital resource -- dispatches to GCS
        /// (signed URL) or local, by file category. No item context available at this overload, so the
        /// folder-relative-viewer override in <see cref="Classify"/> can't apply. </summary>
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

        /// <summary> Return a flag if the file specified exists within the digital resource -- checks GCS or
        /// local, by file category </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to check for</param>
        /// <returns> TRUE if the file exists, otherwise FALSE </returns>
        public bool FileExists(BriefItemInfo DigitalResource, string FileName)
        {
            if (IsGcsOnly(FileName, Hybrid_FileSystem.Requires_Local_File_Bundle(DigitalResource)))
                return gcsFileSystem.FileExists(DigitalResource, FileName);

            return localFileSystem.FileExists(DigitalResource, FileName);
        }

        /// <summary> Return the NETWORK uri for a digital resource -- always local (there is no single
        /// meaningful network location once files may be split between local and GCS) </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource)
        {
            return localFileSystem.Resource_Network_Uri(DigitalResource);
        }

        /// <summary> Return the NETWORK uri for a digital resource -- always local, see remarks on the
        /// <see cref="BriefItemInfo"/> overload </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(string BibID, string VID)
        {
            return localFileSystem.Resource_Network_Uri(BibID, VID);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource -- dispatches to GCS
        /// (a <c>gs://</c> style, informative-only location) or a real local path, by file category </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            if (IsGcsOnly(FileName, Hybrid_FileSystem.Requires_Local_File_Bundle(DigitalResource)))
                return gcsFileSystem.Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID, NormalizeForGcs(FileName));

            return localFileSystem.Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource -- dispatches to GCS
        /// (a <c>gs://</c> style, informative-only location) or a real local path, by file category. No item
        /// context available at this overload, so the folder-relative-viewer override in <see cref="Classify"/>
        /// can't apply. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        public string Resource_Network_Uri(string BibID, string VID, string FileName)
        {
            if (IsGcsOnly(FileName))
                return gcsFileSystem.Resource_Network_Uri(BibID, VID, NormalizeForGcs(FileName));

            return localFileSystem.Resource_Network_Uri(BibID, VID, FileName);
        }

        /// <summary> [TEMPORARY] Get the associated file path -- always local, same simplification
        /// <see cref="Hybrid_FileSystem"/> makes (this is a legacy, rarely-used accessor) </summary>
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

        /// <summary> Gets the list of all the files associated with this digital resource -- lists GCS
        /// objects, plus a local cache.protobuf entry if one exists, or (for a folder-bundle item) lists the
        /// local folder directly </summary>
        /// <param name="DigitalResource"> The digital resource object  </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public List<SobekFileSystem_FileInfo> GetFiles(BriefItemInfo DigitalResource)
        {
            if (Hybrid_FileSystem.Requires_Local_File_Bundle(DigitalResource))
                return localFileSystem.GetFiles(DigitalResource);

            List<SobekFileSystem_FileInfo> gcsFiles = gcsFileSystem.GetFiles(DigitalResource) ?? new List<SobekFileSystem_FileInfo>();

            string localCachePath = localFileSystem.Resource_Network_Uri(DigitalResource, ResourceObjectSettings.Metadata_Cache_FileName);
            if (File.Exists(localCachePath))
            {
                var cacheFileInfo = new FileInfo(localCachePath);
                gcsFiles.Add(new SobekFileSystem_FileInfo
                {
                    Name = cacheFileInfo.Name,
                    LastWriteTime = cacheFileInfo.LastWriteTime,
                    Extension = cacheFileInfo.Extension,
                    Length = cacheFileInfo.Length
                });
            }

            return gcsFiles;
        }

        /// <summary> Gets the list of all the files associated with this digital resource -- lists GCS
        /// objects, plus a local cache.protobuf entry if one exists. No item context available at this
        /// overload, so the folder-relative-viewer override can't apply -- a folder-bundle item's files would
        /// be listed as if GCS-only. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public List<SobekFileSystem_FileInfo> GetFiles(string BibID, string VID)
        {
            List<SobekFileSystem_FileInfo> gcsFiles = gcsFileSystem.GetFiles(BibID, VID) ?? new List<SobekFileSystem_FileInfo>();

            string localCachePath = localFileSystem.Resource_Network_Uri(BibID, VID, ResourceObjectSettings.Metadata_Cache_FileName);
            if (File.Exists(localCachePath))
            {
                var cacheFileInfo = new FileInfo(localCachePath);
                gcsFiles.Add(new SobekFileSystem_FileInfo
                {
                    Name = cacheFileInfo.Name,
                    LastWriteTime = cacheFileInfo.LastWriteTime,
                    Extension = cacheFileInfo.Extension,
                    Length = cacheFileInfo.Length
                });
            }

            return gcsFiles;
        }

        /// <summary> Ensure the local folder for a digital resource (and any parent folders) exists -- cheap,
        /// and still needed for cache.protobuf and folder-bundle-item writes </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        public void CreateDirectory(string BibID, string VID)
        {
            localFileSystem.CreateDirectory(BibID, VID);
        }

        /// <summary> Write file content to a named file within a digital resource's folder, overwriting if it
        /// exists -- routed to GCS or local, by file category </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to write </param>
        /// <param name="Content"> Stream containing the file's content </param>
        /// <remarks> Does not get the changed-file-skip optimization <see cref="CopyFileIn"/> gets -- has
        /// zero real call sites today, so that complexity isn't justified yet, matching <see cref="Hybrid_FileSystem.SaveFile"/> </remarks>
        public void SaveFile(string BibID, string VID, string FileName, Stream Content)
        {
            Hybrid_FileSystem.FileCategory category = Classify(FileName);

            if (category == Hybrid_FileSystem.FileCategory.GcsOnly)
            {
                gcsFileSystem.SaveFile(BibID, VID, NormalizeForGcs(FileName), Content);
                return;
            }

            localFileSystem.SaveFile(BibID, VID, FileName, Content);

            if (category == Hybrid_FileSystem.FileCategory.LocalOnly)
                return;

            if (Content.CanSeek)
                Content.Position = 0;
            gcsFileSystem.SaveFile(BibID, VID, NormalizeForGcs(FileName), Content);
        }

        /// <summary> Copy a file already on local disk into a digital resource's folder as <paramref name="FileName"/>,
        /// overwriting if it exists -- routed to GCS or local, by file category </summary>
        /// <param name="SourceLocalPath"> Full local path of the source file (e.g. a per-user staging folder) </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name the file should have once copied into the digital resource's folder </param>
        public void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName, bool Force = false, bool RequiresLocalFileBundle = false)
        {
            Hybrid_FileSystem.FileCategory category = Classify(FileName, RequiresLocalFileBundle);

            if (category != Hybrid_FileSystem.FileCategory.GcsOnly)
                localFileSystem.CopyFileIn(SourceLocalPath, BibID, VID, FileName);

            if (category == Hybrid_FileSystem.FileCategory.LocalOnly)
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
        /// GCS or local, by file category </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        public void DeleteFile(string BibID, string VID, string FileName)
        {
            Hybrid_FileSystem.FileCategory category = Classify(FileName);

            if (category == Hybrid_FileSystem.FileCategory.GcsOnly)
            {
                gcsFileSystem.DeleteFile(BibID, VID, NormalizeForGcs(FileName));
                // Best-effort local delete too, in case a stray scratch copy exists
                try { localFileSystem.DeleteFile(BibID, VID, FileName); } catch { }
                return;
            }

            localFileSystem.DeleteFile(BibID, VID, FileName);

            if (category == Hybrid_FileSystem.FileCategory.LocalOnly)
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

        /// <summary> Downloads a single named file into a specific local destination path -- from GCS if the
        /// file is GCS-only, otherwise a plain local copy </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to download </param>
        /// <param name="LocalDestinationPath"> Full local path the file should be written to </param>
        public void DownloadFile(string BibID, string VID, string FileName, string LocalDestinationPath)
        {
            if (IsGcsOnly(FileName))
            {
                gcsFileSystem.DownloadFile(BibID, VID, NormalizeForGcs(FileName), LocalDestinationPath);
                return;
            }

            localFileSystem.DownloadFile(BibID, VID, FileName, LocalDestinationPath);
        }

        /// <summary> Deletes ONLY the local copy of a GCS-only file, and only after verifying GCS already has
        /// a matching-size copy. Used by the migration utility's cleanup mode. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete locally </param>
        /// <param name="RequiresLocalFileBundle"> Precomputed <see cref="Hybrid_FileSystem.Requires_Local_File_Bundle(BriefItemInfo)"/>
        /// result for the file's owning item -- see <see cref="Classify"/> </param>
        /// <returns> TRUE if the local file was deleted (or was already gone), FALSE if it was left in place
        /// because GCS did not have a verified matching copy, or because the file isn't GCS-only </returns>
        public bool DeleteLocalCopyIfVerifiedInGcs(string BibID, string VID, string FileName, bool RequiresLocalFileBundle = false)
        {
            if (Classify(FileName, RequiresLocalFileBundle) != Hybrid_FileSystem.FileCategory.GcsOnly)
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
