using System;
using System.IO;

namespace SobekCM.Core.FileSystems
{
    /// <summary> Copies a digital resource's current METS file into its backup subfolder before it is overwritten </summary>
    /// <remarks> Both the read and the write go through <see cref="SobekFileSystem"/>, so this works the same under
    /// Local, GCS Hybrid, and GCS Full file system modes. </remarks>
    public static class Mets_Backup_Helper
    {
        /// <summary> Backs up the current METS file for a digital resource, if one exists </summary>
        /// <param name="BibID"> Bibliographic identifier (BibID) for the resource </param>
        /// <param name="VID"> Volume identifier (VID) for the resource </param>
        /// <param name="MetsFileName"> File name of the METS within the resource folder (e.g. "AA00000001_00001.mets.xml") </param>
        /// <param name="BackupSubfolderName"> Name of the backup subfolder (Settings.Resources.Backup_Files_Folder_Name) </param>
        /// <returns> Name (relative to the resource folder) of the backup written, or NULL if there was no METS to back up </returns>
        public static string Backup_Current_Mets(string BibID, string VID, string MetsFileName, string BackupSubfolderName)
        {
            // A GCS-only METS (GCS Full mode) comes back as a copy downloaded into a temp cache
            bool downloaded = SobekFileSystem.IsGcsOnly(MetsFileName);

            string localPath;
            try
            {
                localPath = SobekFileSystem.Ensure_Local_Copy(BibID, VID, MetsFileName);
            }
            catch (Exception)
            {
                // No current METS to back up (or it could not be read)
                return null;
            }

            if (!File.Exists(localPath))
                return null;

            try
            {
                string backupName = Path.Combine(BackupSubfolderName, Path.GetFileNameWithoutExtension(MetsFileName).Replace(".mets", "") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mets.bak");
                SobekFileSystem.CopyFileIn(localPath, BibID, VID, backupName);
                return backupName;
            }
            finally
            {
                // The temp-cache copy would be stale once the METS is overwritten, so drop it
                // and let the next read fetch the fresh copy
                if (downloaded)
                {
                    try { File.Delete(localPath); } catch { }
                }
            }
        }
    }
}
