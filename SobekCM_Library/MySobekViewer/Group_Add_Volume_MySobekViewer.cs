﻿#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web;
using SobekCM.Core.Client;
using SobekCM.Core.Items;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Items;
using SobekCM.Library.AdminViewer;
using SobekCM.Library.Citation;
using SobekCM.Library.Citation.Template;
using SobekCM.Library.HTML;
using SobekCM.Library.MainWriters;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Bib_Info;
using SobekCM.Resource_Object.Database;
using SobekCM.Resource_Object.Metadata_File_ReaderWriters;
using SobekCM.Tools;
using SobekCM_Resource_Database;

#endregion

namespace SobekCM.Library.MySobekViewer
{
    /// <summary> Class allows an authenticated user to add a new item/volume to an existing title </summary>
    /// <remarks> This class extends the <see cref="abstract_MySobekViewer"/> class.<br /><br />
    /// MySobek Viewers are used for registration and authentication with mySobek, as well as performing any task which requires
    /// authentication, such as online submittal, metadata editing, and system administrative tasks.<br /><br />
    /// During a valid html request, the following steps occur:
    /// <ul>
    /// <li>Application state is built/verified by the Application_State_Builder </li>
    /// <li>Request is analyzed by the QueryString_Analyzer and output as a <see cref="Navigation_Object"/>  </li>
    /// <li>Main writer is created for rendering the output, in his case the <see cref="Html_MainWriter"/> </li>
    /// <li>The HTML writer will create the necessary subwriter.  Since this action requires authentication, an instance of the  <see cref="MySobek_HtmlSubwriter"/> class is created. </li>
    /// <li>The mySobek subwriter creates an instance of this viewer to allow the user to add a new item/volume to an existing title </li>
    /// </ul></remarks>
    public class Group_Add_Volume_MySobekViewer : abstract_MySobekViewer
    {
        private readonly bool bornDigital;
        private readonly string date;
        private readonly short dispositionAdvice;
        private readonly string dispositionAdviceNotes;
        private bool hierarchyCopiedFromDate;
        private readonly short ipRestrict;
        private readonly string level1;
        private readonly int level1Order;
        private readonly string level2;
        private readonly int level2Order;
        private readonly string level3;
        private readonly int level3Order;
        private readonly DateTime? materialRecdDate;
        private readonly string materialRecdNotes;
        private string message;
        private readonly CompleteTemplate completeTemplate;
        private readonly string title;
        private readonly string trackingBox;

        private readonly List<Item_Hierarchy_Details> allVolumes;
        private readonly SobekCM_Item currentItem;


        /// <summary> Constructor for a new instance of the Group_Add_Volume_MySobekViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        public Group_Add_Volume_MySobekViewer(RequestCache RequestSpecificValues)  : base(RequestSpecificValues)
        {
            RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", String.Empty);

            // If no user then that is an error
            if ((RequestSpecificValues.Current_User == null) || (!RequestSpecificValues.Current_User.LoggedOn))
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Aggregation;
                RequestSpecificValues.Current_Mode.Aggregation = String.Empty;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Ensure BibID provided
            RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "Validate provided bibid");
            if (String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.BibID))
            {
                RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "BibID was not provided!");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = "Invalid Request : BibID missing in item group metadata edit request";
                return;
            }

            RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "Try to pull this sobek complete item group");
            currentItem = SobekEngineClient.Items.Get_Sobek_Item_Group(RequestSpecificValues.Current_Mode.BibID, RequestSpecificValues.Tracer);
            if (currentItem == null)
            {
                RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "Unable to build complete item group");
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = "Invalid Request : Unable to build complete item";
                return;
            }

            // Pull the list of items tied to this group - Get the list of other volumes
            RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "Get the list of items under " + RequestSpecificValues.Current_Mode.BibID);
            try
            {
                allVolumes = SobekEngineClient.Items.Get_Multiple_Volumes(RequestSpecificValues.Current_Mode.BibID, RequestSpecificValues.Tracer);
            }
            catch (Exception)
            {
                RequestSpecificValues.Tracer.Add_Trace("Tracking_ItemViewer.Constructor", "Unable to pull volumes under " + RequestSpecificValues.Current_Mode.BibID);
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Error;
                RequestSpecificValues.Current_Mode.Error_Message = "Internal Error : Unable to pull volumes under " + RequestSpecificValues.Current_Mode.BibID;
                return;
            }

            // Set some defaults
            ipRestrict = -1;
            title = String.Empty;
            date = String.Empty;
            level1 = String.Empty;
            level2 = String.Empty;
            level3 = String.Empty;
            level1Order = -1;
            level2Order = -1;
            level3Order = -1;
            hierarchyCopiedFromDate = false;
            message = String.Empty;
            trackingBox = String.Empty;
            bornDigital = false;
            materialRecdDate = null;
            materialRecdNotes = String.Empty;
            dispositionAdvice = -1;
            dispositionAdviceNotes = String.Empty;


            // If the user cannot edit this item, go back
            if (!RequestSpecificValues.Current_User.Can_Edit_This_Item(currentItem.BibID, currentItem.Bib_Info.SobekCM_Type_String, currentItem.Bib_Info.Source.Code, currentItem.Bib_Info.HoldingCode, currentItem.Behaviors.Aggregation_Code_List))
            {
                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Home;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                return;
            }

            // Determine the default CompleteTemplate code 
            string template_code = "addvolume";
            if (!RequestSpecificValues.Current_User.Include_Tracking_In_Standard_Forms)
                template_code = "addvolume_notracking";

            // Load this CompleteTemplate
            completeTemplate = Template_MemoryMgmt_Utility.Retrieve_Template(template_code, RequestSpecificValues.Tracer);
            if (completeTemplate != null)
            {
                RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "Found CompleteTemplate in cache");
            }
            else
            {
                RequestSpecificValues.Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Constructor", "Reading CompleteTemplate file");

                // Look in the user-defined templates portion first
                string user_template = UI_ApplicationCache_Gateway.Settings.Servers.Base_MySobek_Directory + "templates\\user\\standard\\" + template_code + ".xml";
                if (!File.Exists(user_template))
                    user_template = UI_ApplicationCache_Gateway.Settings.Servers.Base_MySobek_Directory + "templates\\default\\standard\\" + template_code + ".xml";

                // Read this template
                Template_XML_Reader reader = new Template_XML_Reader();
                completeTemplate = new CompleteTemplate();
                reader.Read_XML(user_template, completeTemplate, true);

                // Save this into the cache
                Template_MemoryMgmt_Utility.Store_Template(template_code, completeTemplate, RequestSpecificValues.Tracer);
            }

            // See if there was a hidden request
            string hidden_request = HttpContext.Current.Request.Form["action"] ?? String.Empty;

            // If this was a cancel request do that
            if (hidden_request == "cancel")
            {
                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
            }
            else if (hidden_request.IndexOf("save") == 0 )
            {
                // Get the VID that used as a source for this
                string vid = HttpContext.Current.Request.Form["base_volume"];

                if (string.IsNullOrEmpty(vid))
                {
                     message = "<span style=\"color: red\"><strong>No base volume selected!</strong></span>";
                }
                else
                {
                    try
                    {
                        // Get a new instance of this item
                        Tuple<SobekCM_Item, SobekCM_Item_Error> itemAndError = SobekCM_Item_Factory.Get_Item(RequestSpecificValues.Current_Mode.BibID, vid, UI_ApplicationCache_Gateway.Icon_List, RequestSpecificValues.Tracer);
                        SobekCM_Item saveItem = itemAndError.Item1;
                        
                        // Clear some values for this item
                        saveItem.VID = String.Empty;
                        saveItem.Divisions.Clear();
                        saveItem.Behaviors.Serial_Info.Clear();
                        saveItem.Bib_Info.Series_Part_Info.Clear();
                        saveItem.Behaviors.Clear_Ticklers();
                        saveItem.Tracking.Internal_Comments = String.Empty;
				        saveItem.Bib_Info.Location.PURL = String.Empty;
						saveItem.Behaviors.Main_Thumbnail = String.Empty;
						saveItem.METS_Header.Create_Date = DateTime.Now;
						saveItem.METS_Header.Modify_Date = saveItem.METS_Header.Create_Date;
	                    saveItem.METS_Header.Creator_Software = "SobekCM Web - Online add a volume (derived from VID " + vid + ")";
						saveItem.METS_Header.Clear_Creator_Individual_Notes();
	                    saveItem.METS_Header.Creator_Individual = RequestSpecificValues.Current_User.Full_Name;
	                    saveItem.Bib_Info.Location.Other_URL = String.Empty;
						saveItem.Bib_Info.Location.Other_URL_Display_Label = String.Empty;
						saveItem.Bib_Info.Location.Other_URL_Note = String.Empty;

                        // Save the CompleteTemplate changes to this item
                        completeTemplate.Save_To_Bib(saveItem, RequestSpecificValues.Current_User, 1);

                        // Save this item and copy over
                        complete_item_submission(saveItem, RequestSpecificValues.Tracer);

                        // Clear the volume list
                        CachedDataManager.Items.Remove_Items_In_Title(saveItem.BibID, RequestSpecificValues.Tracer);
                        CachedDataManager.Items.Remove_Items_List(saveItem.BibID, RequestSpecificValues.Tracer);

                        // Also clear the engine
                        SobekEngineClient.Items.Clear_Item_Group_Cache(saveItem.BibID, RequestSpecificValues.Tracer);

                        // Also clear any searches or browses ( in the future could refine this to only remove those
                        // that are impacted by this save... but this is good enough for now )
                        CachedDataManager.Clear_Search_Results_Browses();

                        // Forward differently depending on request
                        switch (hidden_request)
                        {
                            case "save_edit":
                                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
                                RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.Edit_Item_Metadata;
                                RequestSpecificValues.Current_Mode.VID = saveItem.VID;
                                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                                break;

                            case "save_again":
                                // No redirect, but save values
                                date = saveItem.Bib_Info.Origin_Info.Date_Issued;
                                ipRestrict = saveItem.Behaviors.IP_Restriction_Membership;
                                trackingBox = saveItem.Tracking.Tracking_Box;
                                bornDigital = saveItem.Tracking.Born_Digital;
                                dispositionAdvice = saveItem.Tracking.Disposition_Advice;
                                dispositionAdviceNotes = saveItem.Tracking.Disposition_Advice_Notes;
                                materialRecdDate = saveItem.Tracking.Material_Received_Date;
                                materialRecdNotes = saveItem.Tracking.Material_Received_Notes;
                                if (!hierarchyCopiedFromDate)
                                {
                                    if (saveItem.Behaviors.Serial_Info.Count > 0)
                                    {
                                        level1 = saveItem.Behaviors.Serial_Info[0].Display;
                                        level1Order = saveItem.Behaviors.Serial_Info[0].Order;
                                    }
                                    if (saveItem.Behaviors.Serial_Info.Count > 1)
                                    {
                                        level2 = saveItem.Behaviors.Serial_Info[1].Display;
                                        level2Order = saveItem.Behaviors.Serial_Info[1].Order;
                                    }
                                    if (saveItem.Behaviors.Serial_Info.Count > 2)
                                    {
                                        level3 = saveItem.Behaviors.Serial_Info[2].Display;
                                        level3Order = saveItem.Behaviors.Serial_Info[2].Order;
                                    }
                                }
                                message = message + "<span style=\"color: blue\"><strong>Saved new volume ( " + saveItem.BibID + " : " + saveItem.VID + ")</strong></span>";
                                break;

                            case "save_addfiles":
								RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.My_Sobek;
								RequestSpecificValues.Current_Mode.My_Sobek_Type = My_Sobek_Type_Enum.File_Management;
                                RequestSpecificValues.Current_Mode.VID = saveItem.VID;
                                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                                break;

                            default:
                                RequestSpecificValues.Current_Mode.Mode = Display_Mode_Enum.Item_Display;
                                RequestSpecificValues.Current_Mode.VID = saveItem.VID;
                                UrlWriterHelper.Redirect(RequestSpecificValues.Current_Mode);
                                break;
                        }

                    }
                    catch ( Exception ee )
                    {
                        message = message + "<br /><span style=\"color: red\"><strong>EXCEPTION CAUGHT!<br /><br />" + ee.Message + "<br /><br />" + ee.StackTrace.Replace("\n","<br />") + "</strong></span>";

                    }
                }
            }
        }

        #region Method commpletes the item submission on the way to the congratulations screen

        private void complete_item_submission(SobekCM_Item Item_To_Complete, Custom_Tracer Tracer)
        {
            // If this is a newspaper type, and the pubdate has a value, try to use that for the serial heirarchy
            if ((Item_To_Complete.Behaviors.Serial_Info.Count == 0) && (Item_To_Complete.Bib_Info.Origin_Info.Date_Issued.Length > 0) && (Item_To_Complete.Bib_Info.SobekCM_Type == TypeOfResource_SobekCM_Enum.Newspaper ))
            {
                    DateTime asDateTime;
                    if (DateTime.TryParse(Item_To_Complete.Bib_Info.Origin_Info.Date_Issued, out asDateTime))
                    {
                        hierarchyCopiedFromDate = true;
                        Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(1, asDateTime.Year, asDateTime.Year.ToString());
                        switch (asDateTime.Month)
                        {
                            case 1:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "January");
                                break;

                            case 2:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "February");
                                break;

                            case 3:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "March");
                                break;

                            case 4:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "April");
                                break;

                            case 5:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "May");
                                break;

                            case 6:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "June");
                                break;

                            case 7:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "July");
                                break;

                            case 8:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "August");
                                break;

                            case 9:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "September");
                                break;

                            case 10:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "October");
                                break;

                            case 11:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "November");
                                break;

                            case 12:
                                Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(2, asDateTime.Month, "December");
                                break;
                        }

                        Item_To_Complete.Behaviors.Serial_Info.Add_Hierarchy(3, asDateTime.Day, asDateTime.Day.ToString());
                    }
            }

            // Determine the in process directory for this
            string user_in_process_directory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" + RequestSpecificValues.Current_User.UserName.Replace(".", "").Replace("@", "") + "\\newitem";
            if (RequestSpecificValues.Current_User.ShibbID.Trim().Length > 0)
                user_in_process_directory = UI_ApplicationCache_Gateway.Settings.Servers.In_Process_Submission_Location + "\\" + RequestSpecificValues.Current_User.ShibbID + "\\newitem";

            // Ensure this directory exists
            if (!Directory.Exists(user_in_process_directory))
                Directory.CreateDirectory(user_in_process_directory);

            // Now, delete all the files in the processing directory
            string[] all_files = Directory.GetFiles(user_in_process_directory);
            foreach (string thisFile in all_files)
            {
                File.Delete(thisFile);
            }

            // Save to the database
            Item_To_Complete.Web.File_Root = Item_To_Complete.BibID.Substring(0, 2) + "\\" + Item_To_Complete.BibID.Substring(2, 2) + "\\" + Item_To_Complete.BibID.Substring(4, 2) + "\\" + Item_To_Complete.BibID.Substring(6, 2) + "\\" + Item_To_Complete.BibID.Substring(8, 2);
            SobekCM_Item_Database.Save_New_Digital_Resource(Item_To_Complete, false, false, RequestSpecificValues.Current_User.UserName, String.Empty, -1);

            // Assign the file root and assoc file path
            Item_To_Complete.Web.AssocFilePath = Item_To_Complete.Web.File_Root + "\\" + Item_To_Complete.VID + "\\";

            // Create the static html pages
            string base_url = RequestSpecificValues.Current_Mode.Base_URL;

            // Save the rest of the metadata
            Item_To_Complete.Source_Directory = user_in_process_directory;
            Item_To_Complete.Save_SobekCM_METS();

            Database.SobekCM_Database.Add_Item_To_User_Folder(RequestSpecificValues.Current_User.UserID, "Submitted Items", Item_To_Complete.BibID, Item_To_Complete.VID, 0, String.Empty, Tracer);

            // Save Bib_Level METS?
            //SobekCM.Resource_Object.Writers.OAI_Writer oaiWriter = new SobekCM.Resource_Object.Writers.OAI_Writer();
            //oaiWriter.Save_OAI_File(bibPackage, resource_folder + "\\oai_dc.xml", bibPackage.Processing_Parameters.Collection_Primary.ToLower(), createDate);

            // If there was no match, try to save to the tracking database
            Engine_Database.Tracking_Online_Submit_Complete(Item_To_Complete.Web.ItemID, RequestSpecificValues.Current_User.Full_Name, String.Empty);

            // Create the options dictionary used when saving information to the database, or writing MarcXML
            Dictionary<string, object> options = new Dictionary<string, object>();
            if (UI_ApplicationCache_Gateway.Settings.MarcGeneration != null)
            {
                options["MarcXML_File_ReaderWriter:MARC Cataloging Source Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Cataloging_Source_Code;
                options["MarcXML_File_ReaderWriter:MARC Location Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Location_Code;
                options["MarcXML_File_ReaderWriter:MARC Reproduction Agency"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Agency;
                options["MarcXML_File_ReaderWriter:MARC Reproduction Place"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Place;
                options["MarcXML_File_ReaderWriter:MARC XSLT File"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.XSLT_File;
            }
            options["MarcXML_File_ReaderWriter:System Name"] = UI_ApplicationCache_Gateway.Settings.System.System_Name;
            options["MarcXML_File_ReaderWriter:System Abbreviation"] = UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation;

			// Save the MARC file
            MarcXML_File_ReaderWriter marcWriter = new MarcXML_File_ReaderWriter();
            string errorMessage;
            marcWriter.Write_Metadata(Item_To_Complete.Source_Directory + "\\marc.xml", Item_To_Complete, options, out errorMessage);          

            // Copy all the files over to the server 
            string serverNetworkFolder = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + Item_To_Complete.Web.AssocFilePath;

            // Create the folder
            if (!Directory.Exists(serverNetworkFolder))
                Directory.CreateDirectory(serverNetworkFolder);
            if (!Directory.Exists(serverNetworkFolder + "\\" + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name))
                Directory.CreateDirectory(serverNetworkFolder + "\\" + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name);

			// Copy the static HTML page over first
			if (File.Exists(user_in_process_directory + "\\" + currentItem.BibID + "_" + currentItem.VID + ".html"))
			{
                File.Copy(user_in_process_directory + "\\" + currentItem.BibID + "_" + currentItem.VID + ".html", serverNetworkFolder + "\\" + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name + "\\" + currentItem.BibID + "_" + currentItem.VID + ".html", true);
				File.Delete(user_in_process_directory + "\\" + currentItem.BibID + "_" + currentItem.VID + ".html");
			}

			// Copy all the files 
			string[] allFiles = Directory.GetFiles(user_in_process_directory);
            foreach (string thisFile in allFiles)
            {
                string destination_file = serverNetworkFolder + "\\" + (new FileInfo(thisFile)).Name;
                File.Copy(thisFile, destination_file, true);
            }

            // Incrememnt the count of number of items submitted by this user
            RequestSpecificValues.Current_User.Items_Submitted_Count++;

            // Delete any remaining items
            all_files = Directory.GetFiles(user_in_process_directory);
            foreach (string thisFile in all_files)
            {
                File.Delete(thisFile);
            }
        }

        #endregion


        /// <summary> Navigation type to be displayed (mostly used by the mySobek viewers) </summary>
        /// <value> This returns none since this viewer writes all the necessary navigational elements </value>
        /// <remarks> This is set to NONE if the viewer will write its own navigation and ADMIN if the standard
        /// administrative tabs should be included as well.  </remarks>
        public override MySobek_Admin_Included_Navigation_Enum Standard_Navigation_Type { get { return MySobek_Admin_Included_Navigation_Enum.NONE; } }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <value> This returns the value 'Add New Volume' </value>
        public override string Web_Title
        {
            get { return "Add New Volume"; }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of the forms)</summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> This class does nothing, since the CompleteTemplate html is added in the <see cref="Write_ItemNavForm_Closing" /> method </remarks>
        public override void Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Write_HTML", "Do nothing");
        }

	    /// <summary> Add the HTML to be displayed in the main SobekCM viewer area </summary>
	    /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
	    /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
	    public override void Write_ItemNavForm_Closing(TextWriter Output, Custom_Tracer Tracer)
	    {
		    const string NEWVOLUME = "NEW VOLUME";

		    Tracer.Add_Trace("Group_Add_Volume_MySobekViewer.Write_ItemNavForm_Closing", "");

		    Output.WriteLine("<!-- Hidden field is used for postbacks to add new form elements (i.e., new name, new other titles, etc..) -->");
		    Output.WriteLine("<input type=\"hidden\" id=\"action\" name=\"action\" value=\"\" />");

		    Output.WriteLine("<div id=\"sbkIsw_Titlebar\">");

            string grouptitle = currentItem.Behaviors.GroupTitle;
		    if (grouptitle.Length > 125)
		    {
			    Output.WriteLine("\t<h1 itemprop=\"name\"><abbr title=\"" + grouptitle + "\">" + grouptitle.Substring(0, 120) + "...</abbr></h1>");
		    }
		    else
		    {
			    Output.WriteLine("\t<h1 itemprop=\"name\">" + grouptitle + "</h1>");
		    }

		    Output.WriteLine("</div>");
			Output.WriteLine("<div class=\"sbkMenu_Bar\" id=\"sbkIsw_MenuBar\" style=\"height:20px\">&nbsp;</div>");

		    Output.WriteLine("<div id=\"container-inner1000\">");
		    Output.WriteLine("<div id=\"pagecontainer\">");


		    Output.WriteLine("<!-- Group_Add_Volume_MySobekViewer.Write_ItemNavForm_Closing -->");
		    Output.WriteLine("<div class=\"sbkMySobek_HomeText\">");
		    Output.WriteLine("  <br />");
		    Output.WriteLine("  <h2>Add a new volume to this existing title/item group</h2>");
		    Output.WriteLine("    <ul>");
		    Output.WriteLine("      <li>Only enter data that you wish to override the data in the existing base volume.</li>");
		    //Output.WriteLine("      <li>Clicking on the green plus button ( <img class=\"repeat_button\" src=\"" + RequestSpecificValues.Current_Mode.Base_URL + "default/images/new_element_demo.jpg\" /> ) will add another instance of the element, if the element is repeatable.</li>");
		    Output.WriteLine("      <li>Click <a href=\"" + UI_ApplicationCache_Gateway.Settings.System.Help_URL(RequestSpecificValues.Current_Mode.Base_URL) + "help/addvolume\" target=\"_EDIT_INSTRUCTIONS\">here for detailed instructions</a> on adding new volumes online.</li>");
		    Output.WriteLine("     </ul>");
		    Output.WriteLine("</div>");
		    Output.WriteLine();

		    if (message.Length > 0)
		    {
			    Output.WriteLine("" + message + "<br />");
		    }

		    Output.WriteLine("<a name=\"CompleteTemplate\"> </a>");
		    Output.WriteLine("<div id=\"tabContainer\" class=\"fulltabs\">");
		    Output.WriteLine("  <div class=\"tabs\">");
		    Output.WriteLine("    <ul>");
		    Output.WriteLine("      <li id=\"tabHeader_1\" class=\"tabActiveHeader\">" + NEWVOLUME + "</li>");
		    Output.WriteLine("    </ul>");
		    Output.WriteLine("  </div>");
		    Output.WriteLine("  <div class=\"graytabscontent\">");
		    Output.WriteLine("    <div class=\"tabpage\" id=\"tabpage_1\">");

		    Output.WriteLine("      <!-- Add SAVE and CANCEL buttons to top of form -->");
		    Output.WriteLine("      <script src=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Js + "\" type=\"text/javascript\"></script>");
		    Output.WriteLine();

		    Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
		    Output.WriteLine("        <button onclick=\"addvolume_cancel_form(); return false;\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
		    Output.WriteLine("        <button onclick=\"addvolume_save_form(''); return false;\" class=\"sbkMySobek_BigButton\"> SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button> &nbsp; &nbsp; ");
		    Output.WriteLine("        <button onclick=\"addvolume_save_form('_again'); return false;\" class=\"sbkMySobek_BigButton\">SAVE & ADD ANOTHER</button>");
		    Output.WriteLine("      </div>");
		    Output.WriteLine("      <br /><br />");
		    Output.WriteLine();

		    // Output.WriteLine("    <td width=\"460px\"> Import metadata and behaviors from existing volume: &nbsp; ");
		    Output.WriteLine("      <div style=\"text-align:left;padding-left:58px; padding-bottom: 10px;\">Import from existing volume: &nbsp; ");
		    Output.WriteLine("        <select id=\"base_volume\" name=\"base_volume\" class=\"addvolume_base_volume\">");

            SortedList<string, string> sortList = new SortedList<string, string>();
	        foreach (Item_Hierarchy_Details itemRowView in allVolumes)
	        {
                sortList.Add(itemRowView.VID, itemRowView.VID);
	        }

            // Select the LAST vid by default
	        int i = 1;
            IList<string> sortListValues = sortList.Values;
            foreach (string thisVid in sortListValues)
		    {
                if (i == sortListValues.Count )
			    {
                    Output.WriteLine("          <option value=\"" + thisVid + "\" selected=\"selected\">" + thisVid + "</option>");
			    }
			    else
			    {
                    Output.WriteLine("          <option value=\"" + thisVid + "\">" + thisVid + "</option>");
			    }
		        i++;
		    }
		    Output.WriteLine("        </select>");
		    Output.WriteLine("      </div>");
		    Output.WriteLine();

	        bool isMozilla = ((!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.Browser_Type)) && (RequestSpecificValues.Current_Mode.Browser_Type.ToUpper().IndexOf("FIREFOX") >= 0));

		    // Create a new blank item for display purposes
		    SobekCM_Item displayItem = new SobekCM_Item {BibID = currentItem.BibID};
		    displayItem.Behaviors.IP_Restriction_Membership = ipRestrict;
		    displayItem.Behaviors.Serial_Info.Clear();
		    displayItem.Tracking.Born_Digital = bornDigital;
		    displayItem.Tracking.Tracking_Box = trackingBox;
		    displayItem.Tracking.Material_Received_Notes = materialRecdNotes;
		    displayItem.Tracking.Material_Received_Date = materialRecdDate;
		    displayItem.Tracking.Disposition_Advice = dispositionAdvice;
		    displayItem.Tracking.Disposition_Advice_Notes = dispositionAdviceNotes;
		    if (title.Length > 0)
		    {
			    displayItem.Bib_Info.Main_Title.Clear();
			    displayItem.Bib_Info.Main_Title.Title = title;
		    }
		    if (date.Length > 0)
			    displayItem.Bib_Info.Origin_Info.Date_Issued = date;
		    if ((level1.Length > 0) && (level1Order >= 0))
		    {
			    displayItem.Behaviors.Serial_Info.Add_Hierarchy(1, level1Order, level1);
			    if ((level2.Length > 0) && (level2Order >= 0))
			    {
				    displayItem.Behaviors.Serial_Info.Add_Hierarchy(2, level2Order, level2);
				    if ((level3.Length > 0) && (level3Order >= 0))
				    {
					    displayItem.Behaviors.Serial_Info.Add_Hierarchy(3, level3Order, level3);
				    }
			    }
		    }

            completeTemplate.Render_Template_HTML(Output, displayItem, RequestSpecificValues.Current_Mode.Skin == RequestSpecificValues.Current_Mode.Default_Skin ? RequestSpecificValues.Current_Mode.Skin.ToUpper() : RequestSpecificValues.Current_Mode.Skin, isMozilla, RequestSpecificValues.Current_User, RequestSpecificValues.Current_Mode.Language, UI_ApplicationCache_Gateway.Translation, RequestSpecificValues.Current_Mode.Base_URL, 1);

		    // Add the second buttons at the bottom of the form
		    Output.WriteLine("      <!-- Add SAVE and CANCEL buttons to bottom of form -->");
		    Output.WriteLine("      <div class=\"sbkMySobek_RightButtons\">");
		    Output.WriteLine("        <button onclick=\"addvolume_cancel_form(); return false;\" class=\"sbkMySobek_BigButton\"><img src=\"" + Static_Resources_Gateway.Button_Previous_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_LeftImg\" alt=\"\" /> CANCEL </button> &nbsp; &nbsp; ");
		    Output.WriteLine("        <button onclick=\"addvolume_save_form(''); return false;\" class=\"sbkMySobek_BigButton\"> SAVE <img src=\"" + Static_Resources_Gateway.Button_Next_Arrow_Png + "\" class=\"sbkMySobek_RoundButton_RightImg\" alt=\"\" /></button> &nbsp; &nbsp; ");
		    Output.WriteLine("      </div>");
		    Output.WriteLine();

		    Output.WriteLine("      <br /><br />");
		    Output.WriteLine("      <hr />");
		    Output.WriteLine();

		    Output.WriteLine("      <p>In addition, the following actions are available:</p>");
		    Output.WriteLine("      <button onclick=\"addvolume_save_form('_edit'); return false;\" class=\"sbkMySobek_RoundButton\">SAVE & EDIT ITEM</button> &nbsp; &nbsp; ");
		    Output.WriteLine("      <button onclick=\"addvolume_save_form('_addfiles'); return false;\" class=\"sbkMySobek_RoundButton\">SAVE & ADD FILES</button> &nbsp; &nbsp; ");
		    Output.WriteLine("      <button onclick=\"addvolume_save_form('_again'); return false;\" class=\"sbkMySobek_RoundButton\">SAVE & ADD ANOTHER</button>");

		    Output.WriteLine("    </div>");
		    Output.WriteLine("  </div>");
		    Output.WriteLine("</div>");
		    Output.WriteLine("</div>");
		    Output.WriteLine("</div>");
	    }

	    /// <summary> Gets the collection of special behaviors which this admin or mySobek viewer
		/// requests from the main HTML subwriter. </summary>
		/// <value> This tells the HTML and mySobek writers to mimic the item viewer </value>
		public override List<HtmlSubwriter_Behaviors_Enum> Viewer_Behaviors
		{
			get
			{
				return new List<HtmlSubwriter_Behaviors_Enum>
				{
					HtmlSubwriter_Behaviors_Enum.MySobek_Subwriter_Mimic_Item_Subwriter,
					HtmlSubwriter_Behaviors_Enum.Suppress_Banner
				};
			}
		}

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        public override string Container_CssClass { get { return "container-inner1000"; } }


        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        public override bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("  <link href=\"" + Static_Resources_Gateway.Sobekcm_Metadata_Css + "\" rel=\"stylesheet\" type=\"text/css\" />");
            return false;
        }
    }
}
