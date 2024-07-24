﻿#region Using directives

using SobekCM.Resource_Object.Divisions;
using System;
using System.IO;
using System.Text.RegularExpressions;

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
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            // Ensure all non-image files are linked to the METS file
            string[] all_files = Directory.GetFiles(Resource.Resource_Folder);
            foreach (string thisFile in all_files)
            {
                FileInfo thisFileInfo = new FileInfo(thisFile);

                if ((!Regex.Match(thisFileInfo.Name, Settings.Resources.Files_To_Exclude_From_Downloads, RegexOptions.IgnoreCase).Success) && (String.Compare(thisFileInfo.Name, Resource.BibID + "_" + Resource.VID + ".html", StringComparison.OrdinalIgnoreCase) != 0))
                {
                    // Some last checks here
                    if ((thisFileInfo.Name.IndexOf("marc.xml", StringComparison.OrdinalIgnoreCase) != 0) && (thisFileInfo.Name.IndexOf("doc.xml", StringComparison.OrdinalIgnoreCase) != 0) && (thisFileInfo.Name.IndexOf(".mets", StringComparison.OrdinalIgnoreCase) < 0) && (thisFileInfo.Name.IndexOf("citation_mets.xml", StringComparison.OrdinalIgnoreCase) < 0) &&
                        (thisFileInfo.Name.IndexOf("ufdc_mets.xml", StringComparison.OrdinalIgnoreCase) < 0) && (thisFileInfo.Name.IndexOf("agreement.txt", StringComparison.OrdinalIgnoreCase) < 0) &&
                        ((thisFileInfo.Name.IndexOf(".xml", StringComparison.OrdinalIgnoreCase) < 0) || (thisFileInfo.Name.IndexOf(Resource.BibID, StringComparison.OrdinalIgnoreCase) < 0)))
                    {
                        Resource.Metadata.Divisions.Download_Tree.Add_File(thisFileInfo.Name);
                    }
                }
            }

            return true;
        }
    }
}
