﻿#region Using directives

using System;
using System.IO;
using System.Net;

#endregion



namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module creates a static version for serving to search 
    /// engine robots to provide as much indexable data as possible </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class CreateStaticVersionModule : abstractSubmissionPackageModule
    {
        //private Static_Pages_Builder staticBuilder;

        /// <summary> Method releases all resources </summary>
        /// <remarks> This overrides the base implemenation of this method to also clear the static pages builder </remarks>
        public override void ReleaseResources()
        {
            //staticBuilder = null;
            Settings = null;
        }

        /// <summary> Creates a static version for serving to search engine robots to provide as much indexable data as possible </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource)
        {
            string source_url = Settings.Servers.Application_Server_URL + Resource.BibID + "/" + Resource.VID + "/robot";

            try
            {
                using (WebClient client = new WebClient())
                {
                    string downloadString = client.DownloadString(source_url);

                    // Save the static page and then copy to all the image servers
                    try
                    {
                        if (!Directory.Exists(Resource.Resource_Folder + "\\" + Settings.Resources.Backup_Files_Folder_Name))
                            Directory.CreateDirectory(Resource.Resource_Folder + "\\" + Settings.Resources.Backup_Files_Folder_Name);

                        string static_file = Resource.Resource_Folder + "\\" + Settings.Resources.Backup_Files_Folder_Name + "\\" + Resource.Metadata.BibID + "_" + Resource.Metadata.VID + ".html";
                        using (StreamWriter writer = new StreamWriter(static_file, false))
                        {
                            writer.Write(downloadString);
                            writer.Flush();
                            writer.Close();
                        }
                        
                        if (!File.Exists(static_file))
                        {
                            OnError("Error creating static page for this resource", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                        }
                        else
                        {
                            // Also copy to the static page location server
                            string web_server_file_version = Settings.Servers.Static_Pages_Location + Resource.File_Root + "\\" + Resource.BibID + "_" + Resource.VID + ".html";
                            if (!Directory.Exists(Settings.Servers.Static_Pages_Location + Resource.File_Root))
                                Directory.CreateDirectory(Settings.Servers.Static_Pages_Location + Resource.File_Root);
                            File.Copy(static_file, web_server_file_version, true);
                        }
                    }
                    catch
                    {
                        OnError("Error creating static page for this resource", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                    }
                }

            }
            catch (Exception )
            {
                OnProcess("Error pulling the robot version", "CreateStaticVersionModule", Resource.BibID + "_" + Resource.VID, Resource.METS_Type_String, -1);
            }



            //// Only build the statyic builder when needed 
            //if (staticBuilder == null)
            //{
            //    // Create the new statics page builder
            //    staticBuilder = new Static_Pages_Builder(Settings.Application_Server_URL, Settings.Static_Pages_Location, Settings.Application_Server_Network);
            //}

            //// Save the static page and then copy to all the image servers
            //try
            //{
            //    if (!Directory.Exists(Resource.Resource_Folder + "\\" + Settings.Backup_Files_Folder_Name))
            //        Directory.CreateDirectory(Resource.Resource_Folder + "\\" + Settings.Backup_Files_Folder_Name);

            //    string static_file = Resource.Resource_Folder + "\\" + Settings.Backup_Files_Folder_Name + "\\" + Resource.Metadata.BibID + "_" + Resource.Metadata.VID + ".html";
            //    staticBuilder.Create_Item_Citation_HTML(Resource.Metadata, static_file, Resource.Resource_Folder);

            //    if (!File.Exists(static_file))
            //    {
            //        OnError("Error creating static page for this resource", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
            //    }
            //    else
            //    {
            //        // Also copy to the static page location server
            //        string web_server_file_version = Settings.Static_Pages_Location + Resource.File_Root + "\\" + Resource.BibID + "_" + Resource.VID + ".html";
            //        if (!Directory.Exists(Settings.Static_Pages_Location + Resource.File_Root))
            //            Directory.CreateDirectory(Settings.Static_Pages_Location + Resource.File_Root);
            //        File.Copy(static_file, web_server_file_version, true);
            //    }
            //}
            //catch
            //{
            //    OnError("Error creating static page for this resource", Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
            //}

            return true;
        }
    }
}
