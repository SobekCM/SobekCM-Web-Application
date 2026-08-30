#region Using directives

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using SobekCM.Core.BriefItem;
using SobekCM.Resource_Object.Divisions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

#endregion

namespace SobekCM.Core.FileSystems
{
    /// <summary> iFileSystem implementation which stores and serves every digital resource file from a single
    /// Google Cloud Storage bucket, rather than local/network disk. </summary>
    /// <remarks>
    /// <para> This assumes an all-or-nothing deployment: either every resource file for this instance lives in
    /// GCS (this class), or every resource file lives on local/network disk (<see cref="PairTreeStructure"/>)
    /// -- never a mix within one instance. </para>
    /// <para> Collection/aggregation-level files (banners, design assets, skins, etc.) are NOT covered by this
    /// class. Those continue to be served directly from the web server regardless of which iFileSystem is
    /// active for item/digital-resource files. </para>
    /// <para> To configure this file system, you need to collect: </para>
    /// <list type="bullet">
    /// <item> The GCS bucket name digital resource files are stored in </item>
    /// <item> A service account JSON key file with read access to that bucket, and with rights to sign URLs
    /// (the service account needs the "Service Account Token Creator" role on itself, or the key must include
    /// a private key directly, since generating a signed URL requires something capable of signing bytes) </item>
    /// <item> How long a generated, signed web URL should remain valid before it expires -- this is the
    /// "encrypting access to each image" piece: nothing is served from a permanent public URL, every URL handed
    /// to a browser is time-limited and specific to that one object </item>
    /// </list>
    /// <para> Object keys are "{SystemCode}/{BibID}/{VID}/{FileName}" -- deliberately NOT the pairtree-split
    /// BibID convention <see cref="PairTreeStructure"/> uses for local/network paths. The pairtree split
    /// exists purely to keep a traditional filesystem directory from holding an unmanageably large number of
    /// entries; GCS has no such constraint (a flat namespace with millions of objects under one prefix
    /// performs identically to one with a handful), so there's no reason to carry that local-disk-motivated
    /// convention into GCS keys. The "/" characters are still used deliberately, though -- GCS itself treats
    /// object keys as flat strings either way, but the Cloud Console and `gsutil` specifically render objects
    /// sharing a "/"-delimited prefix as nested folders, which is what makes {SystemCode} show up as a visible
    /// top-level "folder" when browsing the bucket. </para>
    /// <para> The bare, no-filename overloads of Resource_Web_Uri (<see cref="Resource_Web_Uri(BriefItemInfo)"/>
    /// and <see cref="Resource_Web_Uri(string, string)"/>) intentionally throw -- GCS signs individual objects,
    /// not folders, so there is no such thing as a "base" web URL under this file system. Every caller has to
    /// resolve one specific file at a time. If something still calls the bare overloads, that is a real call
    /// site that needs to be tracked down and fixed before this can go live, not something to paper over here. </para>
    /// </remarks>
    public class GCS_FileSystem : iFileSystem
    {
        private readonly StorageClient storageClient;
        private readonly UrlSigner urlSigner;
        private readonly string bucketName;
        private readonly string systemCode;
        private readonly TimeSpan signedUrlDuration;

        /// <summary> Constructor for a new instance of the GCS_FileSystem class </summary>
        /// <param name="BucketName"> Name of the GCS bucket all digital resource files are stored under </param>
        /// <param name="SystemCode"> Code identifying this SobekCM instance (<see cref="SobekCM.Core.Settings.System_Settings.System_Code"/>),
        /// used as the top-level prefix for every object key so multiple instances can share one bucket and
        /// still browse as separate top-level "folders" in the Cloud Console. Falls back to "SOBEK" if not
        /// provided. </param>
        /// <param name="ServiceAccountJsonKeyPath"> Full path to a service account JSON key file, with read
        /// access to <paramref name="BucketName"/> and the ability to sign URLs </param>
        /// <param name="SignedUrlDuration"> How long a generated web URL should remain valid before expiring.
        /// Defaults to 4 hours if not provided. </param>
        public GCS_FileSystem(string BucketName, string SystemCode, string ServiceAccountJsonKeyPath, TimeSpan? SignedUrlDuration = null)
        {
            bucketName = BucketName;
            systemCode = string.IsNullOrEmpty(SystemCode) ? "SOBEK" : SystemCode;
            signedUrlDuration = SignedUrlDuration ?? TimeSpan.FromHours(4);

            GoogleCredential credential = CredentialFactory.FromFile<ServiceAccountCredential>(ServiceAccountJsonKeyPath).ToGoogleCredential();
            storageClient = StorageClient.Create(credential);

            // Signing needs a credential capable of signing bytes with the account's private key
            urlSigner = UrlSigner.FromCredential(credential);
        }

        /// <summary> Computes the object key prefix for an item's folder within the bucket -- see the class
        /// remarks for why this is "{SystemCode}/{BibID}/{VID}/", not a pairtree-split BibID </summary>
        private string object_key_prefix(string BibID, string VID)
        {
            return systemCode + "/" + BibID + "/" + VID + "/";
        }

        /// <summary> Determines the content type to store on a GCS object from its file name's extension, so
        /// browsers render inline-viewable types (PDF, images, etc.) instead of always forcing a download --
        /// GCS serves whatever content type is stored as object metadata, unlike local disk where the running
        /// web server infers it from the extension at request time. Falls back to "application/octet-stream"
        /// for an extension-less file name or one <see cref="SobekCM_File_Info.MIME_Type"/> doesn't recognize. </summary>
        private static string content_type_for(string FileName)
        {
            string extension = Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();
            string mimeType = string.IsNullOrEmpty(extension) ? string.Empty : SobekCM_File_Info.MIME_Type(extension);
            return string.IsNullOrEmpty(mimeType) || mimeType.StartsWith("unknown/", StringComparison.OrdinalIgnoreCase)
                ? "application/octet-stream"
                : mimeType;
        }

        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public string ReadToEnd(BriefItemInfo DigitalResource, string FileName)
        {
            // Some callers pass a full external URL in as the "file name" (matching PairTreeStructure's
            // behavior), rather than a file that actually lives in this file system
            if ((FileName.IndexOf("http:") == 0) || (FileName.IndexOf("https:") == 0))
            {
                using (var httpClient = new HttpClient())
                using (Stream responseStream = httpClient.GetStreamAsync(FileName).GetAwaiter().GetResult())
                using (var sr = new StreamReader(responseStream))
                {
                    return sr.ReadToEnd();
                }
            }

            string objectName = object_key_prefix(DigitalResource.BibID, DigitalResource.VID) + FileName;

            using (var stream = new MemoryStream())
            {
                storageClient.DownloadObject(bucketName, objectName, stream);
                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public string ReadToEnd(string BibID, string VID, string FileName)
        {
            if ((FileName.IndexOf("http:") == 0) || (FileName.IndexOf("https:") == 0))
            {
                using (var httpClient = new HttpClient())
                using (Stream responseStream = httpClient.GetStreamAsync(FileName).GetAwaiter().GetResult())
                using (var sr = new StreamReader(responseStream))
                {
                    return sr.ReadToEnd();
                }
            }

            string objectName = object_key_prefix(BibID, VID) + FileName;

            using (var stream = new MemoryStream())
            {
                storageClient.DownloadObject(bucketName, objectName, stream);
                stream.Position = 0;
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary> Not supported: GCS signs individual objects, not folders, so there is no meaningful
        /// "base" web URL for an entire digital resource under this file system </summary>
        /// <exception cref="NotSupportedException"> Always thrown -- callers must request a specific file
        /// via <see cref="Resource_Web_Uri(BriefItemInfo, string)"/> instead </exception>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource)
        {
            throw new NotSupportedException("GCS_FileSystem cannot return a web URI for an entire digital resource -- GCS signs individual objects, not folders. Call Resource_Web_Uri(DigitalResource, FileName) for a specific file instead.");
        }

        /// <summary> Not supported: GCS signs individual objects, not folders, so there is no meaningful
        /// "base" web URL for an entire digital resource under this file system </summary>
        /// <exception cref="NotSupportedException"> Always thrown -- callers must request a specific file
        /// via <see cref="Resource_Web_Uri(string, string, string)"/> instead </exception>
        public string Resource_Web_Uri(string BibID, string VID)
        {
            throw new NotSupportedException("GCS_FileSystem cannot return a web URI for an entire digital resource -- GCS signs individual objects, not folders. Call Resource_Web_Uri(BibID, VID, FileName) for a specific file instead.");
        }

        /// <summary> Return a time-limited, signed WEB uri for a file within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> Signed URI for the web resource, valid for the configured signed URL duration </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return Resource_Web_Uri(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Return a time-limited, signed WEB uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get the web URI for</param>
        /// <returns> Signed URI for the web resource, valid for the configured signed URL duration </returns>
        public string Resource_Web_Uri(string BibID, string VID, string FileName)
        {
            string objectName = object_key_prefix(BibID, VID) + FileName;
            return urlSigner.Sign(bucketName, objectName, signedUrlDuration, HttpMethod.Get);
        }

        /// <summary> Return a flag if the file specified exists within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to check for</param>
        /// <returns> TRUE if the object exists in the bucket, otherwise FALSE </returns>
        public bool FileExists(BriefItemInfo DigitalResource, string FileName)
        {
            string objectName = object_key_prefix(DigitalResource.BibID, DigitalResource.VID) + FileName;

            try
            {
                storageClient.GetObject(bucketName, objectName);
                return true;
            }
            catch (Google.GoogleApiException gae) when (gae.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <summary> Return the "network" location for a digital resource -- for this file system, this is the
        /// gs:// style object location, informative for logging purposes, but not directly usable by generic
        /// file APIs the way a real local/network path would be </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> gs://bucket/object-key style URI for the resource's folder </returns>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource)
        {
            return Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID);
        }

        /// <summary> Return the "network" location for a digital resource -- see remarks on
        /// <see cref="Resource_Network_Uri(BriefItemInfo)"/> </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> gs://bucket/object-key style URI for the resource's folder </returns>
        public string Resource_Network_Uri(string BibID, string VID)
        {
            return "gs://" + bucketName + "/" + object_key_prefix(BibID, VID);
        }

        /// <summary> Return the "network" location for a single file in the digital resource -- see remarks on
        /// <see cref="Resource_Network_Uri(BriefItemInfo)"/> </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to get the network URI for</param>
        /// <returns> gs://bucket/object-key style URI for the file </returns>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID, FileName);
        }

        /// <summary> Return the "network" location for a single file in the digital resource -- see remarks on
        /// <see cref="Resource_Network_Uri(BriefItemInfo)"/> </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get the network URI for</param>
        /// <returns> gs://bucket/object-key style URI for the file </returns>
        public string Resource_Network_Uri(string BibID, string VID, string FileName)
        {
            return "gs://" + bucketName + "/" + object_key_prefix(BibID, VID) + FileName;
        }

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        public string AssociFilePath(BriefItemInfo DigitalResource)
        {
            return object_key_prefix(DigitalResource.BibID, DigitalResource.VID);
        }

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        public string AssociFilePath(string BibID, string VID)
        {
            return object_key_prefix(BibID, VID);
        }

        /// <summary> Gets the list of all the files associated with this digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object  </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public List<SobekFileSystem_FileInfo> GetFiles(BriefItemInfo DigitalResource)
        {
            return GetFiles(DigitalResource.BibID, DigitalResource.VID);
        }

        /// <summary> Gets the list of all the files associated with this digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public List<SobekFileSystem_FileInfo> GetFiles(string BibID, string VID)
        {
            string prefix = object_key_prefix(BibID, VID);

            try
            {
                var returnValue = new List<SobekFileSystem_FileInfo>();
                foreach (Google.Apis.Storage.v1.Data.Object thisObject in storageClient.ListObjects(bucketName, prefix))
                {
                    // Skip the "folder placeholder" object itself, if one exists, and anything in a
                    // deeper nested prefix (this should mirror a flat DirectoryInfo.GetFiles() call)
                    string relativeName = thisObject.Name.Substring(prefix.Length);
                    if ((relativeName.Length == 0) || (relativeName.IndexOf("/") >= 0))
                        continue;

                    var returnFile = new SobekFileSystem_FileInfo
                    {
                        Name = relativeName,
                        LastWriteTime = thisObject.UpdatedDateTimeOffset?.DateTime,
                        Extension = Path.GetExtension(relativeName),
                        Length = thisObject.Size.HasValue ? (long)thisObject.Size.Value : 0
                    };

                    returnValue.Add(returnFile);
                }

                return returnValue;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary> No-op -- GCS has no real directory concept, objects are just flat keys </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        public void CreateDirectory(string BibID, string VID)
        {
            // Nothing to do -- GCS object keys don't require their "folder" to exist first
        }

        /// <summary> Write file content to a named object within a digital resource's folder, overwriting if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to write </param>
        /// <param name="Content"> Stream containing the file's content </param>
        public void SaveFile(string BibID, string VID, string FileName, Stream Content)
        {
            string objectName = object_key_prefix(BibID, VID) + FileName;
            storageClient.UploadObject(bucketName, objectName, content_type_for(FileName), Content);
        }

        /// <summary> Copy a file already on local disk up into a digital resource's folder as <paramref name="FileName"/>, overwriting if it exists </summary>
        /// <param name="SourceLocalPath"> Full local path of the source file </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name the file should have once uploaded into the digital resource's folder </param>
        public void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName, bool Force = false, bool RequiresLocalFileBundle = false)
        {
            string objectName = object_key_prefix(BibID, VID) + FileName;
            using (var stream = File.OpenRead(SourceLocalPath))
            {
                storageClient.UploadObject(bucketName, objectName, content_type_for(FileName), stream);
            }
        }

        /// <summary> Delete a single named object within a digital resource's folder, if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        public void DeleteFile(string BibID, string VID, string FileName)
        {
            string objectName = object_key_prefix(BibID, VID) + FileName;
            try
            {
                storageClient.DeleteObject(bucketName, objectName);
            }
            catch (Google.GoogleApiException gae) when (gae.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // Already gone -- nothing to do
            }
        }

        /// <summary> Checks whether an existing GCS object's size matches a local file's length, as a cheap
        /// (no-download) way to decide whether an upload can be skipped because the object is already
        /// up to date </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to check </param>
        /// <param name="LocalFileLength"> Length, in bytes, of the local file being compared against </param>
        /// <returns> TRUE if the object exists in the bucket and its size matches <paramref name="LocalFileLength"/>, otherwise FALSE </returns>
        internal bool ObjectMatchesLocalFile(string BibID, string VID, string FileName, long LocalFileLength)
        {
            string objectName = object_key_prefix(BibID, VID) + FileName;
            try
            {
                Google.Apis.Storage.v1.Data.Object existing = storageClient.GetObject(bucketName, objectName);
                return existing.Size.HasValue && (long)existing.Size.Value == LocalFileLength;
            }
            catch (Google.GoogleApiException gae) when (gae.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        /// <summary> Downloads every object under a digital resource's folder into a local destination folder
        /// -- skipping any file that already exists there. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="LocalDestinationFolder"> Local folder every object should be downloaded into </param>
        /// <remarks> The skip-if-present check matters: the caller (<see cref="StageResourceFilesLocallyModule"/>)
        /// runs this against the incoming submission's own working folder, which may already contain a
        /// depositor's replacement for a file this item already has in GCS (a corrected page image, an
        /// updated METS, etc). Downloading unconditionally would silently overwrite that just-submitted file
        /// with the stale GCS copy before any other module ever saw it -- this only fills in files the
        /// incoming submission didn't already provide. </remarks>
        public void DownloadAll(string BibID, string VID, string LocalDestinationFolder)
        {
            string prefix = object_key_prefix(BibID, VID);

            if (!Directory.Exists(LocalDestinationFolder))
                Directory.CreateDirectory(LocalDestinationFolder);

            foreach (Google.Apis.Storage.v1.Data.Object thisObject in storageClient.ListObjects(bucketName, prefix))
            {
                string relativeName = thisObject.Name.Substring(prefix.Length);
                if ((relativeName.Length == 0) || (relativeName.IndexOf("/") >= 0))
                    continue;

                string localPath = Path.Combine(LocalDestinationFolder, relativeName);
                if (File.Exists(localPath))
                    continue;

                download_object_to_path(thisObject.Name, localPath);
            }
        }

        /// <summary> Downloads a single named object from a digital resource's folder into a specific local
        /// destination path </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to download </param>
        /// <param name="LocalDestinationPath"> Full local path the file should be written to </param>
        public void DownloadFile(string BibID, string VID, string FileName, string LocalDestinationPath)
        {
            string objectName = object_key_prefix(BibID, VID) + FileName;
            download_object_to_path(objectName, LocalDestinationPath);
        }

        /// <summary> Downloads one GCS object, by its full object key, to a local path -- creating the
        /// destination directory first if needed. Shared by <see cref="DownloadAll"/> and <see cref="DownloadFile"/>. </summary>
        private void download_object_to_path(string ObjectName, string LocalDestinationPath)
        {
            string destinationDirectory = Path.GetDirectoryName(LocalDestinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory) && !Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            using (var fileStream = new FileStream(LocalDestinationPath, FileMode.Create, FileAccess.Write))
            {
                storageClient.DownloadObject(bucketName, ObjectName, fileStream);
            }
        }

        /// <summary> Not supported: this class has no separate local copy to delete -- every file lives only
        /// in GCS under this implementation </summary>
        /// <exception cref="NotSupportedException"> Always thrown -- only relevant under Hybrid_FileSystem,
        /// which never delegates this call to a plain GCS-only file system </exception>
        public bool DeleteLocalCopyIfVerifiedInGcs(string BibID, string VID, string FileName, bool RequiresLocalFileBundle = false)
        {
            throw new NotSupportedException("GCS_FileSystem has no separate local copy to delete -- DeleteLocalCopyIfVerifiedInGcs only applies in GCS Hybrid mode.");
        }

        /// <summary> Always TRUE -- every file lives in GCS only under this file system, with no permanent
        /// local copy of anything </summary>
        public bool IsGcsOnly(string FileName, bool RequiresLocalFileBundle = false)
        {
            return true;
        }
    }
}
