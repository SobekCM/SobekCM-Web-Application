#region Using directives

using SobekCM.Resource_Object.Configuration;
using SobekCM.Resource_Object.Divisions;
using System;
using System.IO;
using System.Text.RegularExpressions;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module adds ALL non-image files to the digital resource, 
    /// regardless if they were newly added or not </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class AttachAllNonImageFilesModule : abstractSubmissionPackageModule
    {
        /// <summary> Adds ALL non-image files to the digital resource, regardless if they were newly added or not </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("AttachAllNonImageFilesModule.DoWork");

            // Nothing to do for a metadata-only update -- no resource files accompany it
            if (Resource.METS_Only_Package)
                return true;

            // Ensure all non-image files are linked to the METS file
            string[] all_files = Directory.GetFiles(Resource.Resource_Folder);
            foreach (string thisFile in all_files)
            {
                var thisFileInfo = new FileInfo(thisFile);

                if ((!ResourceObjectSettings.Is_File_Excluded_From_Package(thisFileInfo.Name)) && (!Regex.Match(thisFileInfo.Name, Settings.Resources.Files_To_Exclude_From_Downloads, RegexOptions.IgnoreCase).Success) && (String.Compare(thisFileInfo.Name, Resource.BibID + "_" + Resource.VID + ".html", StringComparison.OrdinalIgnoreCase) != 0))
                {
                    // Exclude any other XML file that happens to include the BibID in its name
                    if ((thisFileInfo.Name.IndexOf(".xml", StringComparison.OrdinalIgnoreCase) < 0) || (thisFileInfo.Name.IndexOf(Resource.BibID, StringComparison.OrdinalIgnoreCase) < 0))
                    {
                        Resource.Metadata.Divisions.Download_Tree.Add_File(thisFileInfo.Name);
                    }
                }
            }

            return true;
        }
    }
}
