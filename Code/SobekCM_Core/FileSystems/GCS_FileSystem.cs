#region Using directives

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using SobekCM.Core.BriefItem;
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
    /// <para> Object keys mirror the same pairtree BibID/VID folder convention <see cref="PairTreeStructure"/>
    /// uses for local/network paths, just expressed with "/" (GCS object keys are flat strings, not real
    /// directories) instead of a platform path separator. </para>
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
        private readonly TimeSpan signedUrlDuration;

        /// <summary> Constructor for a new instance of the GCS_FileSystem class </summary>
        /// <param name="BucketName"> Name of the GCS bucket all digital resource files are stored under </param>
        /// <param name="ServiceAccountJsonKeyPath"> Full path to a service account JSON key file, with read
        /// access to <paramref name="BucketName"/> and the ability to sign URLs </param>
        /// <param name="SignedUrlDuration"> How long a generated web URL should remain valid before expiring.
        /// Defaults to 4 hours if not provided. </param>
        public GCS_FileSystem(string BucketName, string ServiceAccountJsonKeyPath, TimeSpan? SignedUrlDuration = null)
        {
            bucketName = BucketName;
            signedUrlDuration = SignedUrlDuration ?? TimeSpan.FromHours(4);

            GoogleCredential credential = CredentialFactory.FromFile<ServiceAccountCredential>(ServiceAccountJsonKeyPath).ToGoogleCredential();
            storageClient = StorageClient.Create(credential);

            // Signing needs a credential capable of signing bytes with the account's private key
            urlSigner = UrlSigner.FromCredential(credential);
        }

        /// <summary> Computes the pairtree-style object key prefix for an item's folder within the bucket,
        /// matching the same BibID/VID folder convention <see cref="PairTreeStructure"/> uses for local/network
        /// storage, just expressed as a GCS object key instead of a filesystem path </summary>
        private static string object_key_prefix(string BibID, string VID)
        {
            return BibID.Substring(0, 2) + "/" + BibID.Substring(2, 2) + "/" + BibID.Substring(4, 2) + "/" + BibID.Substring(6, 2) + "/" + BibID.Substring(8, 2) + "/" + VID + "/";
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
            string prefix = object_key_prefix(DigitalResource.BibID, DigitalResource.VID);

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
    }
}
