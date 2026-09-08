#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Tools;

#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module adds ALL the image files to the digital resource,
    /// regardless if they were just uploaded or not </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class AttachImagesAllModule : abstractSubmissionPackageModule
    {
        /// <summary> Adds ALL the image files to the digital resource, regardless if they were just uploaded or not </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("AttachImagesAllModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return true;

            int jpeg_files = 0;

            // Ensure all non-image files are linked to the METS file
            string[] all_files = SobekCM_File_Utilities.GetFiles(Resource.Resource_Folder, "*.jp2|*.jpg");
            foreach (string thisFile in all_files)
            {
                string filename = Path.GetFileName(thisFile);
                string extension = Path.GetExtension(thisFile);

                if ((filename == null) || (extension == null))
                    continue;

                extension = extension.ToLower().Replace(".", "");

                // Also, check to see if this is a jpeg or jpeg2000
                if (extension == "jp2")
                {
                    // Try to just always add JPEG2000s
                    Resource.Metadata.Divisions.Physical_Tree.Add_File(filename);
                }
                if (extension == "jpg")
                {
                    // If this is a thumbnail jpeg, only add if a regular JPEG or JP2000 exists
                    // Otherwise, it is likely just a thumbnail for a PDF or PPT file or something
                    if (filename.IndexOf("thm.jpg", StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        string possible_jpeg = thisFile.ToLower().Replace("thm.jpg", ".jpg");
                        string possible_jpeg2000 = thisFile.ToLower().Replace("thm.jpg", "jp2");

                        if ((File.Exists(possible_jpeg)) || (File.Exists(possible_jpeg2000)))
                        {
                            Resource.Metadata.Divisions.Physical_Tree.Add_File(filename);
                        }
                    }
                    else
                    {
                        // Non thumbnail, so go ahead and add it
                        Resource.Metadata.Divisions.Physical_Tree.Add_File(filename);
                        jpeg_files++;
                    }
                }
            }

            // Just a sanity check to make sure thumbnails weren't added.. which would create a separate page in the METS structmap
            List<abstract_TreeNode> allPages = Resource.Metadata.Divisions.Physical_Tree.Pages_PreOrder;
            foreach (Page_TreeNode thisPage in allPages)
            {
                // If there is only one file attached, look for the thumbnails
                if (thisPage.Files.Count == 1)
                {
                    // If the only file is a thumbnail, just CLEAR this page.  That should
                    // cause the page to be skipped
                    string fileName = thisPage.Files[0].System_Name;
                    if (fileName.IndexOf("thm.jpg", StringComparison.OrdinalIgnoreCase) > 0)
                        thisPage.Files.Clear();
                }
            }

            return true;
        }
    }
}
