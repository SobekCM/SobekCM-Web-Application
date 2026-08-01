#region Using directives

using System;
using System.IO;
using System.Text.RegularExpressions;

#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module deletes any files in the resource folder
    /// matching the configured post-archive delete pattern, once they are no longer needed
    /// (e.g. temporary files kept only long enough to be picked up by <see cref="CopyToArchiveFolderModule"/>) </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class DeleteNonRetainedFilesModule : abstractSubmissionPackageModule
    {
        /// <summary> Deletes any files in the resource folder matching the configured post-archive delete pattern </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            if (String.IsNullOrEmpty(Settings.Archive.PostArchive_Files_To_Delete))
                return true;

            string resourceFolder = Resource.Resource_Folder;

            string[] files = Directory.GetFiles(resourceFolder);
            foreach (string thisFile in files)
            {
                var thisFileInfo = new FileInfo(thisFile);
                if (Regex.Match(thisFileInfo.Name, Settings.Archive.PostArchive_Files_To_Delete, RegexOptions.IgnoreCase).Success)
                {
                    File.Delete(thisFile);
                }
            }

            return true;
        }
    }
}
