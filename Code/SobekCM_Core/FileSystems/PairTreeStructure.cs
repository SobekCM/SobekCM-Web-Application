using SobekCM.Core.BriefItem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace SobekCM.Core.FileSystems
{
    /// <summary> Utility supports the pair tree file / directory structure which 
    /// the SObekCM instance uses by default to organize the digital resources </summary>
    public class PairTreeStructure : iFileSystem
    {
        private readonly string rootNetworkUri;
        private readonly string rootWebUri;
        private readonly char pathSeperator;
        private readonly char dirSeperator;

        /// <summary> Constructor for a new instance of the <see cref="PairTreeStructure"/> class </summary>
        /// <param name="RootNetworkUri"> Root network location for the digital resource files </param>
        /// <param name="RootWebUri"> Root web URL for the digital resource files folder </param>
        public PairTreeStructure(string RootNetworkUri, string RootWebUri)
        {
            rootNetworkUri = RootNetworkUri;
            rootWebUri = RootWebUri;

            // Set the environmental default
            pathSeperator = Path.PathSeparator;
            dirSeperator = Path.DirectorySeparatorChar;
        }


        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        public string ReadToEnd(BriefItemInfo DigitalResource, string FileName)
        {
            // Read the HTML file
            if ((FileName.IndexOf("http:") == 0) || (FileName.IndexOf("https:") == 0))
            {
                // the html retrieved from the page
                String strResult;
                using (var httpClient = new HttpClient())
                using (Stream responseStream = httpClient.GetStreamAsync(FileName).GetAwaiter().GetResult())
                using (var sr = new StreamReader(responseStream))
                {
                    strResult = sr.ReadToEnd();
                }
                return strResult;
            }

            // Just read the network
            string fullFilePath = Path.Combine(Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID), FileName);
            return File.ReadAllText(fullFilePath);
        }

        /// <summary> Return the WEB uri for a digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource)
        {
            if (rootWebUri[rootWebUri.Length - 1] == '/')
                return rootWebUri + DigitalResource.BibID.Substring(0, 2) + "/" + DigitalResource.BibID.Substring(2, 2) + "/" + DigitalResource.BibID.Substring(4, 2) + "/" + DigitalResource.BibID.Substring(6, 2) + "/" + DigitalResource.BibID.Substring(8, 2) + "/" + DigitalResource.VID + "/";

            return rootWebUri + "/" + DigitalResource.BibID.Substring(0, 2) + "/" + DigitalResource.BibID.Substring(2, 2) + "/" + DigitalResource.BibID.Substring(4, 2) + "/" + DigitalResource.BibID.Substring(6, 2) + "/" + DigitalResource.BibID.Substring(8, 2) + "/" + DigitalResource.VID + "/";
        }

        /// <summary> Return the WEB uri for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier for the resource in question </param>
        /// <param name="VID"> Volume identifier for the resource in question </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(string BibID, string VID)
        {
            if (rootWebUri[rootWebUri.Length - 1] == '/')
                return rootWebUri + BibID.Substring(0, 2) + "/" + BibID.Substring(2, 2) + "/" + BibID.Substring(4, 2) + "/" + BibID.Substring(6, 2) + "/" + BibID.Substring(8, 2) + "/" + VID + "/";

            return rootWebUri + "/" + BibID.Substring(0, 2) + "/" + BibID.Substring(2, 2) + "/" + BibID.Substring(4, 2) + "/" + BibID.Substring(6, 2) + "/" + BibID.Substring(8, 2) + "/" + VID + "/";
        }

        /// <summary> Return the WEB uri for a file within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return Resource_Web_Uri(DigitalResource) + FileName;
        }

        /// <summary> Return the WEB uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get the web URI for</param>
        /// <returns> URI for the web resource </returns>
        public string Resource_Web_Uri(string BibID, string VID, string FileName)
        {
            return Resource_Web_Uri(BibID, VID) + FileName;
        }

        /// <summary> Return a flag if the file specified exists within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to check for</param>
        /// <returns> URI for the web resource </returns>
        public bool FileExists(BriefItemInfo DigitalResource, string FileName)
        {
            string path = Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID);
            string filePath = Path.Combine(path, FileName);
            return File.Exists(filePath);
        }

        /// <summary> Return the NETWORK uri for a digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource)
        {
            return Resource_Network_Uri(DigitalResource.BibID, DigitalResource.VID);
        }

        /// <summary> Return the NETWORK uri for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public string Resource_Network_Uri(string BibID, string VID)
        {
            if ( String.IsNullOrEmpty(VID))
                return Path.Combine(rootNetworkUri, BibID.Substring(0, 2) + dirSeperator + BibID.Substring(2, 2) + dirSeperator + BibID.Substring(4, 2) + dirSeperator + BibID.Substring(6, 2) + dirSeperator + BibID.Substring(8, 2));

            return Path.Combine(rootNetworkUri, BibID.Substring(0, 2) + dirSeperator + BibID.Substring(2, 2) + dirSeperator + BibID.Substring(4, 2) + dirSeperator + BibID.Substring(6, 2) + dirSeperator + BibID.Substring(8, 2), VID);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public string Resource_Network_Uri(BriefItemInfo DigitalResource, string FileName)
        {
            return Path.Combine(Resource_Network_Uri(DigitalResource), FileName);
        }

        /// <summary> Return the NETWORK uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        public string Resource_Network_Uri(string BibID, string VID, string FileName)
        {
            return Path.Combine(Resource_Network_Uri(BibID, VID), FileName);
        }

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the 
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        public string AssociFilePath(BriefItemInfo DigitalResource)
        {
            return DigitalResource.BibID.Substring(0, 2) + dirSeperator + DigitalResource.BibID.Substring(2, 2) + dirSeperator + DigitalResource.BibID.Substring(4, 2) + dirSeperator + DigitalResource.BibID.Substring(6, 2) + dirSeperator + DigitalResource.BibID.Substring(8, 2) + dirSeperator + DigitalResource.VID + dirSeperator;
        }

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the 
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        /// <remarks>Why is this temporary?</remarks>
        public string AssociFilePath(string BibID, string VID)
        {
            return BibID.Substring(0, 2) + dirSeperator + BibID.Substring(2, 2) + dirSeperator + BibID.Substring(4, 2) + dirSeperator + BibID.Substring(6, 2) + dirSeperator + BibID.Substring(8, 2) + dirSeperator + VID + dirSeperator;
        }

        /// <summary> Gets the list of all the files associated with this digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object  </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        public List<SobekFileSystem_FileInfo> GetFiles(BriefItemInfo DigitalResource)
        {
            string directory = Resource_Network_Uri(DigitalResource);

            try
            {

                if (Directory.Exists(directory))
                {
                    FileInfo[] files = (new DirectoryInfo(directory)).GetFiles();
                    var returnValue = new List<SobekFileSystem_FileInfo>();
                    foreach (FileInfo thisFile in files)
                    {
                        var returnFile = new SobekFileSystem_FileInfo{
                            Name = thisFile.Name,
                            LastWriteTime = thisFile.LastWriteTime,
                            Extension = thisFile.Extension,
                            Length = thisFile.Length
                        };

                        returnValue.Add(returnFile);
                    }

                    return returnValue;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary> Ensure the folder for a digital resource (and any parent folders) exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        public void CreateDirectory(string BibID, string VID)
        {
            string directory = Resource_Network_Uri(BibID, VID);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// <summary> Write file content to a named file within a digital resource's folder, overwriting if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to write </param>
        /// <param name="Content"> Stream containing the file's content </param>
        public void SaveFile(string BibID, string VID, string FileName, Stream Content)
        {
            string path = Resource_Network_Uri(BibID, VID, FileName);
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                Content.CopyTo(fileStream);
            }
        }

        /// <summary> Copy a file already on local disk into a digital resource's folder as <paramref name="FileName"/>, overwriting if it exists </summary>
        /// <param name="SourceLocalPath"> Full local path of the source file (e.g. a per-user staging folder) </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name the file should have once copied into the digital resource's folder </param>
        public void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName)
        {
            string destination = Resource_Network_Uri(BibID, VID, FileName);

            // File.Copy throws if source and destination resolve to the same file, regardless of the
            // overwrite flag -- a no-op guard here matters for callers walking files already sitting at
            // their final production path (e.g. a bulk migration tool), not just staging-to-production copies
            if (string.Equals(Path.GetFullPath(SourceLocalPath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                return;

            File.Copy(SourceLocalPath, destination, true);
        }

        /// <summary> Delete a single named file within a digital resource's folder, if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        public void DeleteFile(string BibID, string VID, string FileName)
        {
            string path = Resource_Network_Uri(BibID, VID, FileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        /// <summary> Not supported: bulk-download from GCS is meaningless when every file is already local </summary>
        /// <exception cref="NotSupportedException"> Always thrown -- only relevant under Hybrid_FileSystem,
        /// which never delegates this call to a plain local file system </exception>
        public void DownloadAll(string BibID, string VID, string LocalDestinationFolder)
        {
            throw new NotSupportedException("PairTreeStructure has no GCS content to download -- DownloadAll only applies in GCS Hybrid mode.");
        }

        /// <summary> Not supported: there's no GCS copy to verify against, since every file here is local-only </summary>
        /// <exception cref="NotSupportedException"> Always thrown -- only relevant under Hybrid_FileSystem,
        /// which never delegates this call to a plain local file system </exception>
        public bool DeleteLocalCopyIfVerifiedInGcs(string BibID, string VID, string FileName)
        {
            throw new NotSupportedException("PairTreeStructure has no GCS copy to verify against -- DeleteLocalCopyIfVerifiedInGcs only applies in GCS Hybrid mode.");
        }

    }
}
