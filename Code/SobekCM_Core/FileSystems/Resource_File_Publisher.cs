using SobekCM.Tools;
using System.IO;

namespace SobekCM.Core.FileSystems
{
    /// <summary> Shared "publish staged item files to production" step used by every new-item/add-volume
    /// submission flow -- copies every file out of a staging directory into a resource's destination
    /// folder, backing up the item's static HTML page into a Backup subfolder first, then clears the
    /// staging directory. </summary>
    /// <remarks> Consolidates what used to be three independently-duplicated copy loops
    /// (<c>New_TEI_MySobekViewer</c>, <c>New_Group_And_Item_MySobekViewer</c>, <c>Group_Add_Volume_MySobekViewer</c>),
    /// two of which built destination paths via raw string concatenation rather than
    /// <see cref="PathTraversalGuard"/> -- all three now go through the guard here. <br /><br />
    /// The main resource folder and its files are routed through <see cref="SobekFileSystem"/> (addressed by
    /// BibID/VID/FileName), so this is storage-agnostic for the resource's own files. The Backup subfolder
    /// is a local-disk-only concept with no <see cref="iFileSystem"/> equivalent, so it's still resolved
    /// directly off <paramref name="DestinationDirectory"/>via raw <see cref="System.IO"/> calls. </remarks>
    public static class Resource_File_Publisher
    {
        /// <summary> Copies every file out of a staging directory into a resource's destination folder,
        /// backing up the item's static HTML page into a Backup subfolder first, then clears the staging
        /// directory. </summary>
        /// <param name="StagingDirectory"> Directory containing the freshly-submitted files (mets, images, html, etc.) </param>
        /// <param name="BibID"> Bibliographic identifier (BibID) for the resource being published </param>
        /// <param name="VID"> Volume identifier (VID) for the resource being published </param>
        /// <param name="DestinationDirectory"> Root network folder for this resource (Image_Server_Network + AssocFilePath) -- used only to resolve the local-disk-only Backup subfolder </param>
        /// <param name="BackupSubfolderName"> Name of the backup subfolder under <paramref name="DestinationDirectory"/> (Settings.Resources.Backup_Files_Folder_Name) </param>
        /// <param name="StaticHtmlFileName"> File name of the static HTML page to back up separately (e.g. "BibID_VID.html"), or null/empty if none exists </param>
        /// <param name="DeleteStagingFilesAfterCopy"> Whether to delete the remaining staging files once copied (true for every current call site) </param>
        public static void Publish_Staged_Files(string StagingDirectory, string BibID, string VID,
            string DestinationDirectory, string BackupSubfolderName, string StaticHtmlFileName,
            bool DeleteStagingFilesAfterCopy = true)
        {
            // Ensure the resource's folder exists (routed through the abstraction) and its Backup
            // subfolder exists (local-disk-only concept, resolved directly)
            SobekFileSystem.CreateDirectory(BibID, VID);

            string backupDirectory = Path.Combine(DestinationDirectory, BackupSubfolderName);
            if (!Directory.Exists(backupDirectory))
                Directory.CreateDirectory(backupDirectory);

            // Back up the static HTML page into the Backup subfolder first, then remove it from staging so
            // it isn't also picked up by the general copy loop below
            if (!string.IsNullOrEmpty(StaticHtmlFileName))
            {
                string safeHtmlFileName = PathTraversalGuard.SanitizeFileName(StaticHtmlFileName);
                string stagedHtmlPath = Path.Combine(StagingDirectory, safeHtmlFileName);

                if (File.Exists(stagedHtmlPath))
                {
                    SobekFileSystem.CopyFileIn(stagedHtmlPath, BibID, VID, Path.Combine(BackupSubfolderName, safeHtmlFileName));
                    File.Delete(stagedHtmlPath);
                }
            }

            // Copy every remaining staged file into the destination folder
            string[] allFiles = Directory.GetFiles(StagingDirectory);
            foreach (string thisFile in allFiles)
            {
                string safeFileName = PathTraversalGuard.SanitizeFileName(new FileInfo(thisFile).Name);
                SobekFileSystem.CopyFileIn(thisFile, BibID, VID, safeFileName);
            }

            // Clear the staging directory now that everything has been published
            if (DeleteStagingFilesAfterCopy)
            {
                string[] remainingFiles = Directory.GetFiles(StagingDirectory);
                foreach (string thisFile in remainingFiles)
                {
                    try
                    {
                        File.Delete(thisFile);
                    }
                    catch
                    {
                        // Not much to do here -- matches existing swallow-and-continue behavior at every call site
                    }
                }
            }
        }
    }
}
