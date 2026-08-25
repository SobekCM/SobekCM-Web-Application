using SobekCM.Core.BriefItem;
using System.Collections.Generic;
using System.IO;

namespace SobekCM.Core.FileSystems
{
    public interface iFileSystem
    {



        /// <summary> Read to the end of a (text-based) file and return the contents </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the file to open, and read </param>
        /// <returns> Full contexts of the text-based file </returns>
        string ReadToEnd(BriefItemInfo DigitalResource, string FileName);


        /// <summary> Return the WEB uri for a digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the web resource </returns>
        string Resource_Web_Uri(BriefItemInfo DigitalResource);

        /// <summary> Return the WEB uri for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier for the resource in question </param>
        /// <param name="VID"> Volume identifier for the resource in question </param>
        /// <returns> URI for the web resource </returns>
        string Resource_Web_Uri(string BibID, string VID);

        /// <summary> Return the WEB uri for a file within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Name of the resource file </param>
        /// <returns> URI for the web resource </returns>
        string Resource_Web_Uri(BriefItemInfo DigitalResource, string FileName);

        /// <summary> Return the WEB uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get the web URI for</param>
        /// <returns> URI for the web resource </returns>
        string Resource_Web_Uri(string BibID, string VID, string FileName);

        /// <summary> Return a flag if the file specified exists within the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to check for</param>
        /// <returns> URI for the web resource </returns>
        bool FileExists(BriefItemInfo DigitalResource, string FileName);

        /// <summary> Return the NETWORK uri for a digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        string Resource_Network_Uri(BriefItemInfo DigitalResource);

        /// <summary> Return the NETWORK uri for a digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        string Resource_Network_Uri(string BibID, string VID);

        /// <summary> Return the NETWORK uri for a single file in the digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        string Resource_Network_Uri(BriefItemInfo DigitalResource, string FileName);

        /// <summary> Return the NETWORK uri for a single file in the digital resource </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Filename to get network URI for</param>
        /// <returns> URI for the network resource </returns>
        /// <remarks> This makes some presumptions on the type of system in the background </remarks>
        string Resource_Network_Uri(string BibID, string VID, string FileName);

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the 
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="DigitalResource"> The digital resource object </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        string AssociFilePath(BriefItemInfo DigitalResource);

        /// <summary> [TEMPORARY] Get the associated file path (which is essentially the part of the 
        /// path that appears UNDER the root imaging spot </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <returns> Part of the file path, derived from the BibID and VID </returns>
        /// <remarks>Why is this temporary?</remarks>
        string AssociFilePath(string BibID, string VID);

        /// <summary> Gets the list of all the files associated with this digital resource </summary>
        /// <param name="DigitalResource"> The digital resource object  </param>
        /// <returns> List of the file information for this digital resource, or NULL if this does not exist somehow </returns>
        List<SobekFileSystem_FileInfo> GetFiles(BriefItemInfo DigitalResource);

        /// <summary> Ensure the folder for a digital resource (and any parent folders) exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        void CreateDirectory(string BibID, string VID);

        /// <summary> Write file content to a named file within a digital resource's folder, overwriting if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to write </param>
        /// <param name="Content"> Stream containing the file's content </param>
        void SaveFile(string BibID, string VID, string FileName, Stream Content);

        /// <summary> Copy a file already on local disk into a digital resource's folder as <paramref name="FileName"/>, overwriting if it exists </summary>
        /// <param name="SourceLocalPath"> Full local path of the source file (e.g. a per-user staging folder) </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name the file should have once copied into the digital resource's folder </param>
        /// <remarks> Distinct from <see cref="SaveFile"/>: this takes a local source path (matching every
        /// current File.Copy(staging, dest, true) call site) rather than requiring the caller to open a
        /// Stream first. The source is always a local path -- never itself routed through <see cref="iFileSystem"/> --
        /// since every real call site copies from a per-user local staging folder, never from another
        /// digital resource's own storage. </remarks>
        void CopyFileIn(string SourceLocalPath, string BibID, string VID, string FileName);

        /// <summary> Delete a single named file within a digital resource's folder, if it exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete </param>
        void DeleteFile(string BibID, string VID, string FileName);

        /// <summary> Downloads every object under a digital resource's folder into a local destination folder.
        /// Only meaningful in GCS Hybrid mode -- other implementations throw <see cref="System.NotSupportedException"/>,
        /// since this is only ever called after a mode check has already confirmed Hybrid is active. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="LocalDestinationFolder"> Local folder every object should be downloaded into </param>
        void DownloadAll(string BibID, string VID, string LocalDestinationFolder);

        /// <summary> Deletes ONLY the local copy of a GCS-only file, and only after verifying GCS already has
        /// a matching-size copy -- never deletes anything from GCS, and never touches a dual-write/local-only
        /// file. Only meaningful in GCS Hybrid mode -- other implementations throw <see cref="System.NotSupportedException"/>,
        /// since this is only ever called after a mode check has already confirmed Hybrid is active. </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for a title within a SobekCM instance </param>
        /// <param name="VID"> Volume identifier (VID) for an item within a SobekCM title </param>
        /// <param name="FileName"> Name of the file to delete locally </param>
        /// <returns> TRUE if the local file was deleted (or was already gone), FALSE if it was left in place
        /// because GCS did not have a verified matching copy, or because the file isn't GCS-only </returns>
        bool DeleteLocalCopyIfVerifiedInGcs(string BibID, string VID, string FileName);

    }
}
