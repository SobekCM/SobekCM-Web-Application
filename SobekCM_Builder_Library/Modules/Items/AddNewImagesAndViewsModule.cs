#region Using directives

using System.IO;
using SobekCM.Resource_Object.Behaviors;

#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module adds only newly added images and views to the resource object </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class AddNewImagesAndViewsModule : abstractSubmissionPackageModule
    {
        /// <summary> Adds only newly added images and views to the resource object  </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            // Ensure all new image files are linked to the METS file
            foreach (string thisFile in Resource.NewImageFiles)
            {
                // Leave out the legacy QC images
                if ((thisFile.ToUpper().IndexOf(".QC.JPG") < 0) && (thisFile.ToUpper().IndexOf("THM.JPG") < 0))
                {
                    // Add this file
                    FileInfo thisFileInfo = new FileInfo(thisFile);
                    Resource.Metadata.Divisions.Physical_Tree.Add_File(thisFileInfo.Name);
                }
            }

            return true;
        }
    }
}
