#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using SobekCM.Core.Aggregations;
using SobekCM.Core.Client;
using SobekCM.Core.Configuration;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Items;
using SobekCM.Core.Navigation;
using SobekCM.Core.Skins;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Items;
using SobekCM.Library.Database;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_File_ReaderWriters;
using SobekCM.Tools;
using SobekCM.Tools.Logs;

#endregion

namespace SobekCM.Library
{
    /// <summary> Class builds the static HTML page for a digital resource to allow indexing by search engines </summary>
    public class Static_Pages_Builder
    {
        private readonly SobekCM_Assistant assistant;
        private readonly Navigation_Object currentMode;

        private int errors;

        private readonly string primaryWebServerUrl;
        private readonly string staticSobekcmDataLocation;
		private readonly string staticSobekcmLocation;
        private readonly Custom_Tracer tracer;

	    private readonly string defaultSkin;


    //    /// <summary> Constructor for a new instance of the Static_Pages_Builder class </summary>
    //    /// <param name="Primary_Web_Server_URL"> URL for the primary web server </param>
    //    /// <param name="Static_Data_Location"> Network location for the data directory </param>
    //    /// <param name="Default_Skin"> Default skin code </param>
    //    public Static_Pages_Builder(string Primary_Web_Server_URL, string Static_Data_Location, string Default_Skin)
    //    {
    //        primaryWebServerUrl = Primary_Web_Server_URL;
    //        staticSobekcmDataLocation = Static_Data_Location;
    //        staticSobekcmLocation = UI_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network;

    //        tracer = new Custom_Tracer();
    //        assistant = new SobekCM_Assistant();

    //        // Save all the objects needed by the SobekCM Library
    //        defaultSkin = Default_Skin;

    //        // Create the mode object
    //        currentMode = new Navigation_Object
    //                          {
    //                              ViewerCode = "citation",
    //                              Skin = String.Empty,
    //                              Mode = Display_Mode_Enum.Item_Display,
    //                              Language = Web_Language_Enum.English,
    //                              Base_URL = primaryWebServerUrl
    //                          };

    //        // Set some constant settings
    //        // SobekCM.Library.UI_ApplicationCache_Gateway.Settings.Watermarks_URL = primary_web_server_url + "/design/wordmarks/";
    //        UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative = primaryWebServerUrl;

    //        // Ensure all the folders exist
    //        if (!Directory.Exists(staticSobekcmDataLocation))
    //            Directory.CreateDirectory(staticSobekcmDataLocation);
    //        if (!Directory.Exists(staticSobekcmDataLocation + "\\rss"))
    //            Directory.CreateDirectory(staticSobekcmDataLocation + "\\rss");

    //    }



    //    /// <summary> Rebuilds all MarcXML files </summary>
    //    /// <param name="ResourceDirectory"> Directory under which all the resources within this SobekCM library exist </param>
    //    /// <returns> The number of encountered errors </returns>
    //    public int Rebuild_All_MARC_Files( string ResourceDirectory )
    //    {
    //        // Set the item for the current mode
    //        errors = 0;
    //        int successes = 0;

    //        DataSet item_list_table = SobekCM_Database.Get_Item_List(false, null);

    //        // Get the item list and build the hashtable 
    //        Item_Lookup_Object itemList = new Item_Lookup_Object();
    //        Engine_Database.Populate_Item_Lookup_Object(false, itemList, tracer);


    //        foreach (DataRow thisRow in item_list_table.Tables[0].Rows)
    //        {
    //            string bibid = thisRow["BibID"].ToString();
    //            string vid = thisRow["VID"].ToString();

    //            if (!Create_MarcXML_File(bibid, vid, ResourceDirectory, itemList))
    //            {
    //                errors++;
    //            }
    //            else
    //            {
    //                successes++;
    //                if (successes % 1000 == 0 )
    //                    Console.WriteLine(@"{0} complete", successes);
    //            }
    //        }

    //        return errors;
    //    }

    //    /// <summary> Rebuilds all static pages, including the RSS feeds and site maps </summary>
    //    /// <param name="Logger"> Log file to record progress </param>
    //    /// <param name="BuildAllCitationPages"> Flag indicates to build the individual static HTML pages for each digital resource </param>
    //    /// <param name="InstanceName"> Name of this instance </param>
    //    /// <param name="PrimaryLogId"> Log ID in the case this is the builder and it has been pre-logged </param>
    //    /// <returns> The number of encountered errors </returns>
    //    public int Rebuild_All_Static_Pages( LogFileXhtml Logger, bool BuildAllCitationPages, string InstanceName, long PrimaryLogId )
    //    {


    //        return errors;

    //    }

    //    /// <summary> Build the browse all static HTML file that includes links to all items in a particular collection </summary>
    //    /// <param name="Aggregation"> Aggregation object for which to build the browse ALL static html page </param>
    //    /// <param name="AllItems"> List of all items linked to that aggregation/collection </param>
    //    /// <returns> TRUE if successful, otherwise FALSE </returns>
    //    public bool Build_All_Browse(Item_Aggregation Aggregation, DataSet AllItems)
    //    {
    //        if (AllItems == null)
    //            return false;

    //        // Set the default web skin
    //        currentMode.Skin = defaultSkin;
    //        currentMode.Aggregation = Aggregation.Code;
    //        currentMode.Aggregation_Type = Aggregation_Type_Enum.Home;
    //        currentMode.Is_Robot = true;

    //        if (staticSobekcmLocation.Length > 0)
    //            UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory = staticSobekcmLocation;

    //        // Pull the item aggregation object
    //        if (( Aggregation.Web_Skins != null ) && ( Aggregation.Web_Skins.Count > 0))
    //            currentMode.Skin = Aggregation.Web_Skins[0];

    //       // Get the skin object

    //        Custom_Tracer tracer = new Custom_Tracer();
    //        Web_Skin_Object skinObject = assistant.Get_HTML_Skin(currentMode, Engine_ApplicationCache_Gateway.Web_Skin_Collection, false, tracer);
    //        if (skinObject == null)
    //            return true;
  
    //        StreamWriter writer = new StreamWriter(staticSobekcmDataLocation + Aggregation.Code.ToLower() + "_all.html", false);
    //        writer.WriteLine("<!DOCTYPE html>");
    //        writer.WriteLine("<html>");
    //        writer.WriteLine("<head>");
    //        writer.WriteLine("  <title>" + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + " - " + Aggregation.Name + "</title>");
    //        writer.WriteLine();
    //        writer.WriteLine("  <!-- " + UI_ApplicationCache_Gateway.Settings.System.System_Name + " : SobekCM Digital Repository -->");
    //        writer.WriteLine();
    //        writer.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/SobekCM.min.css\" rel=\"stylesheet\" type=\"text/css\" />");
    //        writer.WriteLine("  <script type=\"text/javascript\" src=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/scripts/jquery/jquery-1.10.2.min.js\"></script>");
    //        writer.WriteLine("  <script type=\"text/javascript\" src=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/scripts/sobekcm_full.min.js\"></script>");
    //        writer.WriteLine("  <meta name=\"robots\" content=\"index, follow\" />");
    //        if (!(String.IsNullOrEmpty(skinObject.CSS_Style)))
    //        {
    //            writer.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + skinObject.CSS_Style + "\" rel=\"stylesheet\" type=\"text/css\" />");
    //        }
    //        if ( !String.IsNullOrEmpty(Aggregation.CSS_File))
    //        {
    //            writer.WriteLine("  <link href=\"" + currentMode.Base_Design_URL + "aggregations/" + skinObject.Skin_Code + "/" + skinObject.CSS_Style + "\" rel=\"stylesheet\" type=\"text/css\" />");
    //        }
    //        writer.WriteLine("</head>");
    //        writer.WriteLine("<body>");

    //        writer.WriteLine("<div id=\"container-inner\">");

    //        string banner = "<div id=\"sbkHmw_BannerDiv\"><a alt=\"" + Aggregation.Name + "\" href=\"" + primaryWebServerUrl + Aggregation.Code.ToLower() + "\" style=\"padding-bottom:0px;margin-bottom:0px\"><img id=\"mainBanner\" src=\"" + primaryWebServerUrl + "design/aggregations/" + Aggregation.Code + "/images/banners/coll.jpg\" alt=\"\" /></a></div>";
    //        //Display_Header(writer, skinObject, banner);


    ////		MainMenus_Helper_HtmlSubWriter.Add_Aggregation_Main_Menu(writer, currentMode, null, Aggregation, translations, codeManager );


    //        writer.WriteLine("<div class=\"sbkPrsw_DescPanel sbkPrsw_BrowseDescPanel\">");
    //        writer.WriteLine("  <h1>All Items</h1>");
    //        writer.WriteLine("</div>");

    //        writer.WriteLine("<div class=\"sbkPrsw_ResultsNavBarImbed\">");
    //        writer.WriteLine("  <br />");
    //        writer.WriteLine("  " + AllItems.Tables[0].Rows.Count + " of " + AllItems.Tables[0].Rows.Count + " matching titles");
    //        writer.WriteLine("</div>");
    //        writer.WriteLine();
 
    //        writer.WriteLine("<div id=\"sbkPrsw_ViewTypeSelectRow\">");
    //        writer.WriteLine("  <ul class=\"sbk_FauxDownwardTabsList\">");
    //        writer.WriteLine("    <li>BRIEF VIEW</li>");
    //        writer.WriteLine("    <li class=\"current\">TABLE VIEW</li>");
    //        writer.WriteLine("    <li>THUMBNAIL VIEW</li>");
    //        writer.WriteLine("  </ul>");
    //        writer.WriteLine("</div>");
    //        writer.WriteLine();

    //        writer.WriteLine("<div class=\"sbkPrsw_ResultsPanel\" id=\"main-content\">");

    //        writer.WriteLine("<br />");
    //        writer.WriteLine("<br />");

    //        // Add links for each item
    //        if (AllItems.Tables[0].Rows.Count > 0)
    //        {
    //            foreach (DataRow thisRow in AllItems.Tables[0].Rows)
    //            {
    //                // Determine the folder 
    //                string bibid = thisRow["BibID"].ToString();
    //                string vid = thisRow["VID"].ToString();

    //                writer.WriteLine("<a href=\"" + primaryWebServerUrl + bibid + "/" + vid + "\">" + thisRow["Title"] + "</a><br />");
    //            }
    //        }
    //        else
    //        {
    //            writer.WriteLine("<h1>NO ITEMS IN AGGREGATION</h1>");
    //        }

    //        writer.WriteLine("<br />");
    //        writer.WriteLine("<br />");

    //        writer.WriteLine("</div>");

    //        //Display_Footer(writer, skinObject);

    //        writer.WriteLine("</div>");

    //        writer.WriteLine("</body>");
    //        writer.WriteLine("</html>");

    //        writer.Flush();
    //        writer.Close();

    //        return true;
    //    }

    //    /// <summary> Forces this builder clsas to refresh all of the data values from the database </summary>
    //    public void Refresh()
    //    {
    //        try
    //        {
    //            // Build the code manager
    //            Engine_ApplicationCache_Gateway.RefreshCodes();
    //        }
    //        catch(Exception)
    //        {
    //            // Do nothing in this case
    //        }
    //    }

    //    #region Code for creating static citation item HTML

    //    /// <summary> Create the static HTML citation page for a single digital resource </summary>
    //    /// <param name="Current_Item"> Digital resource to write as static citaion html </param>
    //    /// <param name="Static_FileName"> Name of the resulting html file </param>
    //    /// <param name="Text_File_Directory"> Directory where any text files may exist for this resource </param>
    //    /// <returns> TRUE if successful, otherwise FALSE </returns>
    //    public bool Create_Item_Citation_HTML(SobekCM_Item Current_Item, string Static_FileName, string Text_File_Directory )
    //    {

    //        // Set the item for the current mode
    //        currentMode.BibID = Current_Item.BibID;
    //        currentMode.VID = Current_Item.VID;
    //        currentMode.ViewerCode = "citation";
    //        currentMode.Skin = defaultSkin;
    //        currentMode.Mode = Display_Mode_Enum.Item_Display;
    //        currentMode.Language = Web_Language_Enum.English;
    //        currentMode.Trace_Flag = Trace_Flag_Type_Enum.No;

    //        // Get the current page
    //        int current_page_index = currentMode.Page.HasValue ? Math.Max(currentMode.Page.Value, ((ushort)1)) : 1;
    //        Page_TreeNode currentPage = SobekCM_Item_Factory.Get_Current_Page(Current_Item, current_page_index, null);

    //        // Finish writing this
    //        Finish_writing_html(Current_Item, currentPage, Static_FileName, Text_File_Directory);

    //        UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative = String.Empty;
    //        return true;

    //    }

    //    /// <summary> Creates the static MarcXML file for a digital resource </summary>
    //    /// <param name="BIBID"> Bibliographic identifier ( BibID )</param>
    //    /// <param name="VID"> Volume identifier ( VID ) </param>
    //    /// <param name="DestinationDirectory"> Directory where the resultant MarcXML file should be written </param>
    //    /// <param name="Item_List"> Item lookup object </param>
    //    /// <returns>  This will read the currently live METS file and build the digital object with additional
    //    /// information from the database before writing the MarcXML file </returns>
    //    public bool Create_MarcXML_File(string BIBID, string VID, string DestinationDirectory, Item_Lookup_Object Item_List )
    //    {
    //        // Clear the current tracer
    //        tracer.Clear();

    //        try
    //        {

    //            // Set the item for the current mode
    //            currentMode.BibID = BIBID;
    //            currentMode.VID = VID;
    //            currentMode.ViewerCode = "citation";
    //            currentMode.Skin = "";
    //            currentMode.Mode = Display_Mode_Enum.Item_Display;
    //            currentMode.Language = Web_Language_Enum.English;
    //            currentMode.Trace_Flag = Trace_Flag_Type_Enum.No;

    //            // Get the item
    //            SobekCM_Item currentItem;
    //            Page_TreeNode currentPage;
    //            SobekCM_Items_In_Title itemsInTitle;
    //            assistant.Get_Item(String.Empty, currentMode, Item_List, UI_ApplicationCache_Gateway.Settings.Servers.Image_URL, UI_ApplicationCache_Gateway.Icon_List, tracer, null, out currentItem, out currentPage, out itemsInTitle);
    //            currentMode.Aggregation = String.Empty;
    //            if (currentItem == null)
    //                return false;

    //            if (currentItem.Behaviors.Aggregation_Count > 0)
    //                currentMode.Aggregation = currentItem.Behaviors.Aggregations[0].Code;


    //            string marcFile = DestinationDirectory + currentItem.Web.File_Root + "\\" + currentItem.VID + "\\marc.xml";

    //            // Create the options dictionary used when saving information to the database, or writing MarcXML
    //            Dictionary<string, object> options = new Dictionary<string, object>();
    //            if (UI_ApplicationCache_Gateway.Settings.MarcGeneration != null)
    //            {
    //                options["MarcXML_File_ReaderWriter:MARC Cataloging Source Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Cataloging_Source_Code;
    //                options["MarcXML_File_ReaderWriter:MARC Location Code"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Location_Code;
    //                options["MarcXML_File_ReaderWriter:MARC Reproduction Agency"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Agency;
    //                options["MarcXML_File_ReaderWriter:MARC Reproduction Place"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.Reproduction_Place;
    //                options["MarcXML_File_ReaderWriter:MARC XSLT File"] = UI_ApplicationCache_Gateway.Settings.MarcGeneration.XSLT_File;
    //            }
    //            options["MarcXML_File_ReaderWriter:System Name"] = UI_ApplicationCache_Gateway.Settings.System.System_Name;
    //            options["MarcXML_File_ReaderWriter:System Abbreviation"] = UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation;


    //            MarcXML_File_ReaderWriter marcWriter = new MarcXML_File_ReaderWriter();
    //            string errorMessage;
    //            return marcWriter.Write_Metadata(marcFile, currentItem, options, out errorMessage);
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }

    //    /// <summary> Create the static HTML citation page for a single digital resource </summary>
    //    /// <param name="BIBID"> Bibliographic identifier for the digital resource </param>
    //    /// <param name="VID"> Volume idenfitier for the digital resource </param>
    //    /// <param name="Static_FileName"> Name of the resulting html file </param>
    //    /// <param name="Text_File_Directory"> Directory where any text files may exist for this resource </param>
    //    /// <param name="Item_List"> Item lookup object </param>
    //    /// <returns> TRUE if successful, otherwise FALSE </returns>
    //    /// <remarks> THis is generally called by the SobekCM Builder windows application/scheduled task</remarks>
    //    public bool Create_Item_Citation_HTML(string BIBID, string VID, string Static_FileName, string Text_File_Directory, Item_Lookup_Object Item_List )
    //    {
    //        // Clear the current tracer
    //        tracer.Clear();

    //        // Set the item for the current mode
    //        currentMode.BibID = BIBID;
    //        currentMode.VID = VID;
    //        currentMode.ViewerCode = "citation";
    //        currentMode.Skin = defaultSkin;
    //        currentMode.Mode = Display_Mode_Enum.Item_Display;
    //        currentMode.Language = Web_Language_Enum.English;
    //        currentMode.Trace_Flag = Trace_Flag_Type_Enum.No;

    //        // Get the item
    //        SobekCM_Item currentItem;
    //        Page_TreeNode currentPage;
    //        SobekCM_Items_In_Title itemsInTitle;
    //        assistant.Get_Item(String.Empty, currentMode, Item_List, UI_ApplicationCache_Gateway.Settings.Servers.Image_URL, UI_ApplicationCache_Gateway.Icon_List, tracer, null, out currentItem, out currentPage, out itemsInTitle);
    //        if (currentItem == null)
    //            return false;

    //        if (currentItem.Behaviors.Aggregation_Count > 0)
    //            currentMode.Aggregation = currentItem.Behaviors.Aggregations[0].Code;

    //        // Get the current page
    //        int current_page_index = currentMode.Page.HasValue ? Math.Max(currentMode.Page.Value, ((ushort)1)) : 1;
    //        currentPage = SobekCM_Item_Factory.Get_Current_Page(currentItem, current_page_index, tracer);

    //        // Finish writing this
    //        Finish_writing_html(currentItem, currentPage, Static_FileName, Text_File_Directory);

    //        return true;
    //    }

    //    private void Finish_writing_html(SobekCM_Item CurrentItem, Page_TreeNode CurrentPage, string Filename, string TextFileLocation )
    //    {
    //        //bool textSearchable = CurrentItem.Behaviors.Text_Searchable;
    //        //CurrentItem.Behaviors.Text_Searchable = false;
    //        //if (staticSobekcmLocation.Length > 0)
    //        //    UI_ApplicationCache_Gateway.Settings.Servers.Base_Directory = staticSobekcmLocation;

    //        //// Get the skin
    //        //if ((CurrentItem.Behaviors.Web_Skin_Count > 0) && ( !CurrentItem.Behaviors.Web_Skins.Contains( defaultSkin.ToUpper())))
    //        //    currentMode.Skin = CurrentItem.Behaviors.Web_Skins[0];

    //        //// Get the skin object
    //        //Web_Skin_Object skinObject = skinsCollection[currentMode.Skin];
    //        //if (skinObject == null)
    //        //{
    //        //    skinObject = assistant.Get_HTML_Skin(currentMode, skinsCollection, false, null);
    //        //    skinsCollection.Add(skinObject);
    //        //}

    //        //// Create the HTML writer
    //        //Item_HtmlSubwriter itemWriter = new Item_HtmlSubwriter(CurrentItem, CurrentPage, null, codeManager, translations, true, true, currentMode, null, String.Empty, null, tracer) { Mode = currentMode, Skin = skinObject };
    //        //UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative = currentMode.Base_URL;
    //        //if ((UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative.Length == 0) || (UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative.Contains("localhost")))
    //        //{
    //        //    UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative = primaryWebServerUrl;
    //        //    currentMode.Base_URL = UI_ApplicationCache_Gateway.Settings.Servers.Base_SobekCM_Location_Relative;
    //        //}

    //        //// Now that the item viewer is built, set the robot flag to suppress some checks
    //        //currentMode.Is_Robot = true;

    //        //// Create the TextWriter
    //        //StreamWriter writer = new StreamWriter(Filename, false, Encoding.UTF8);

    //        //writer.WriteLine("<!DOCTYPE html>");
    //        //writer.WriteLine("<html>");
    //        //writer.WriteLine("<head>");
    //        //writer.WriteLine("  <title>" + CurrentItem.Bib_Info.Main_Title + "</title>");
    //        //writer.WriteLine();
    //        //writer.WriteLine("  <!-- " + UI_ApplicationCache_Gateway.Settings.System.System_Name + " : SobekCM Digital Repository -->");
    //        //writer.WriteLine();
    //        //writer.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/SobekCM.min.css\" rel=\"stylesheet\" type=\"text/css\" />");
    //        //writer.WriteLine("  <script type=\"text/javascript\" src=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/scripts/jquery/jquery-1.10.2.min.js\"></script>");
    //        //writer.WriteLine("  <script type=\"text/javascript\" src=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/scripts/sobekcm_full.min.js\"></script>");
    //        //writer.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + "default/SobekCM_Item.min.css\" rel=\"stylesheet\" type=\"text/css\" />");

    //        //writer.WriteLine("  <meta name=\"robots\" content=\"index, follow\" />");
    //        //if (skinObject.CSS_Style.Length > 0)
    //        //{
    //        //    writer.WriteLine("  <link href=\"" + UI_ApplicationCache_Gateway.Settings.Servers.System_Base_URL + skinObject.CSS_Style + "\" rel=\"stylesheet\" type=\"text/css\" />");
    //        //}

    //        //string image_src = currentMode.Base_URL + "/" + CurrentItem.Web.AssocFilePath + "/" + CurrentItem.Behaviors.Main_Thumbnail;
    //        //writer.WriteLine("  <link rel=\"image_src\" href=\"" + image_src.Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://") + "\" />");




    //        //writer.WriteLine("</head>");
    //        //writer.WriteLine("<body>");

    //        //// Is this item DARK or PRIVATE
    //        //if ((CurrentItem.Behaviors.Dark_Flag) || ( CurrentItem.Behaviors.IP_Restriction_Membership < 0 ))
    //        //{
    //        //    writer.WriteLine("THIS ITEM IS CURRENTLY DARK OR PRIVATE");
    //        //    writer.WriteLine("</body>");
    //        //    writer.WriteLine("</html>");
    //        //    writer.Flush();
    //        //    writer.Close();
    //        //    return;
    //        //}


    //        //// Add the header
    //        //Display_Header(writer, itemWriter.Skin, CurrentItem);

    //        //// Begin to write the item view
    //        //itemWriter.Write_HTML(writer, tracer);

    //        //if ((CurrentItem.Behaviors.Wordmark_Count > 0) || ((CurrentItem.Web.Static_PageCount > 1) && (CurrentItem.Web.Static_Division_Count > 1)))
    //        //{
    //        //    writer.WriteLine("<nav id=\"sbkIsw_Leftnavbar\" style=\"padding-top:3px\">");

    //        //    // Write the table of contents as static HTML, rather than the TreeView web control
    //        //    if ((CurrentItem.Web.Static_PageCount > 1) && (CurrentItem.Web.Static_Division_Count > 1))
    //        //    {
    //        //        writer.WriteLine("  <div class=\"sbkIsw_ShowTocRow\">");
    //        //        writer.WriteLine("    <div class=\"sbkIsw_UpToc\">HIDE TABLE OF CONTENTS</div>");
    //        //        writer.WriteLine("  </div>");

    //        //        writer.WriteLine("<div class=\"sbkIsw_TocTreeView\">");

    //        //        // load the table of contents in the tree
    //        //        TreeView treeView1 = new TreeView();
    //        //        itemWriter.Create_TreeView_From_Divisions(treeView1);

    //        //        // Step through all the parent nodes
    //        //        writer.WriteLine("  <table cellspacing=\"4px\">");
    //        //        foreach (TreeNode thisNode in treeView1.Nodes)
    //        //        {
    //        //            writer.WriteLine("    <tr><td width=\"9px\">&nbsp;</td><td>" + thisNode.Text.Replace("sbkIsw_SelectedTocTreeViewItem", "sbkIsw_TocTreeViewItem") + "</td></tr>");
    //        //        }
    //        //        writer.WriteLine("  </table>");
    //        //        writer.WriteLine("</div>");
    //        //    }

    //        //}


    //        //itemWriter.Write_Additional_HTML(writer, tracer);

    //        ////Literal citationLiteral = (Literal)placeHolder.Controls[0];
    //        ////writer.WriteLine(citationLiteral.Text);
    //        ////placeHolder.Controls.Clear();

    //        //writer.WriteLine("<!-- COMMENT HERE -->");

    //        //// Close out this tables and form
    //        //writer.WriteLine("       </tr>");

    //        //// If this is IP restricted, show nothing else
    //        //if (CurrentItem.Behaviors.IP_Restriction_Membership == 0)
    //        //{
    //        //    // Add the download list if there are some
    //        //    if (CurrentItem.Divisions.Download_Tree.Has_Files)
    //        //    {
    //        //        writer.WriteLine("       <tr>");
    //        //        // Create the downloads viewer to ouput the html
    //        //        Download_ItemViewer downloadViewer = new Download_ItemViewer {CurrentItem = CurrentItem, CurrentMode = currentMode};

    //        //        // Add the HTML for this now
    //        //        downloadViewer.Write_Main_Viewer_Section(writer, tracer);
    //        //        writer.WriteLine("       </tr>");
    //        //    }

    //        //    // If there is a table of contents write it again, this time it will be complete
    //        //    // and also show a hierarchy if there is one
    //        //    if ((CurrentItem.Web.Static_PageCount > 1) && (CurrentItem.Web.Static_Division_Count > 1))
    //        //    {
    //        //        writer.WriteLine("       <tr>");
    //        //        writer.WriteLine("         <td align=\"left\"><span class=\"SobekViewerTitle\">Table of Contents</span></td>");
    //        //        writer.WriteLine("       </tr>");

    //        //        writer.WriteLine("       <tr>");
    //        //        writer.WriteLine("          <td>");
    //        //        writer.WriteLine("            <div class=\"sbkCiv_Citation\">");

    //        //        foreach (abstract_TreeNode treeNode in CurrentItem.Divisions.Physical_Tree.Roots)
    //        //        {
    //        //            recursively_write_toc(writer, treeNode, "&nbsp; &nbsp; ");
    //        //        }

    //        //        writer.WriteLine("            </div>");
    //        //        writer.WriteLine("          </td>");
    //        //        writer.WriteLine("       </tr>");
    //        //    }

    //        //    // Is the text file location included, in which case any full text should be appended to the end?
    //        //    if ((TextFileLocation.Length > 0) && (Directory.Exists(TextFileLocation)))
    //        //    {
    //        //        // Get the list of all TXT files in this division
    //        //        string[] text_files = Directory.GetFiles(TextFileLocation, "*.txt");
    //        //        Dictionary<string, string> text_files_existing = new Dictionary<string, string>();
    //        //        foreach (string thisTextFile in text_files)
    //        //        {
    //        //            string text_filename = (new FileInfo(thisTextFile)).Name.ToUpper();
    //        //            text_files_existing[text_filename] = text_filename;
    //        //        }

    //        //        // Are there ANY text files?
    //        //        if (text_files.Length > 0)
    //        //        {
    //        //            // If this has page images, check for related text files 
    //        //            List<string> text_files_included = new List<string>();
    //        //            bool started = false;
    //        //            if (CurrentItem.Divisions.Physical_Tree.Has_Files)
    //        //            {
    //        //                // Go through the first 100 text pages
    //        //                List<abstract_TreeNode> pages = CurrentItem.Divisions.Physical_Tree.Pages_PreOrder;
    //        //                int page_count = 0;
    //        //                foreach (Page_TreeNode thisPage in pages)
    //        //                {
    //        //                    // Keep track of the page count
    //        //                    page_count++;

    //        //                    // Look for files in this page
    //        //                    if (thisPage.Files.Count > 0)
    //        //                    {
    //        //                        bool found_non_thumb_file = false;
    //        //                        foreach (SobekCM_File_Info thisFile in thisPage.Files)
    //        //                        {
    //        //                            // Make sure this is not a thumb
    //        //                            if (thisFile.System_Name.ToLower().IndexOf("thm.jpg") < 0)
    //        //                            {
    //        //                                found_non_thumb_file = true;
    //        //                                string root = thisFile.File_Name_Sans_Extension;
    //        //                                if (text_files_existing.ContainsKey(root.ToUpper() + ".TXT"))
    //        //                                {
    //        //                                    string text_file = TextFileLocation + "\\" + thisFile.File_Name_Sans_Extension + ".txt";

    //        //                                    // SInce this is marked to be included, save this name
    //        //                                    text_files_included.Add(root.ToUpper() + ".TXT");

    //        //                                    // For size reasons, we only include the text from the first 100 pages
    //        //                                    if (page_count <= 100)
    //        //                                    {
    //        //                                        if (!started)
    //        //                                        {
    //        //                                            writer.WriteLine("       <tr>");
    //        //                                            writer.WriteLine("         <td align=\"left\"><span class=\"SobekViewerTitle\">Full Text</span></td>");
    //        //                                            writer.WriteLine("       </tr>");
    //        //                                            writer.WriteLine("       <tr>");
    //        //                                            writer.WriteLine("          <td>");
    //        //                                            writer.WriteLine("            <div class=\"sbkCiv_Citation\">");

    //        //                                            started = true;
    //        //                                        }

    //        //                                        try
    //        //                                        {
    //        //                                            StreamReader reader = new StreamReader(text_file);
    //        //                                            string text_line = reader.ReadLine();
    //        //                                            while (text_line != null)
    //        //                                            {
    //        //                                                writer.WriteLine(text_line + "<br />");
    //        //                                                text_line = reader.ReadLine();
    //        //                                            }
    //        //                                            reader.Close();
    //        //                                        }
    //        //                                        catch
    //        //                                        {
    //        //                                            writer.WriteLine("Unable to read file: " + text_file);
    //        //                                        }

    //        //                                        writer.WriteLine("<br /><br />");
    //        //                                    }
    //        //                                }

    //        //                            }

    //        //                            // If a suitable file was found, break here
    //        //                            if (found_non_thumb_file)
    //        //                                break;
    //        //                        }
    //        //                    }
    //        //                }

    //        //                // End this if it was ever started
    //        //                if (started)
    //        //                {
    //        //                    writer.WriteLine("            </div>");
    //        //                    writer.WriteLine("          </td>");
    //        //                    writer.WriteLine("       </tr>");
    //        //                }
    //        //            }

    //        //            // Now, check for any other valid text files 
    //        //            List<string> additional_text_files = text_files_existing.Keys.Where(ThisTextFile => (!text_files_included.Contains(ThisTextFile.ToUpper())) && (ThisTextFile.ToUpper() != "AGREEMENT.TXT") && (ThisTextFile.ToUpper().IndexOf("REQUEST") != 0)).ToList();

    //        //            // Now, include any additional text files, which would not be page text files, possiblye 
    //        //            // full text for included PDFs, Powerpoint, Word Doc, etc..
    //        //            started = false;
    //        //            foreach (string thisTextFile in additional_text_files)
    //        //            {
    //        //                if (!started)
    //        //                {
    //        //                    writer.WriteLine("       <tr>");
    //        //                    writer.WriteLine("         <td align=\"left\"><span class=\"SobekViewerTitle\">Full Text</span></td>");
    //        //                    writer.WriteLine("       </tr>");
    //        //                    writer.WriteLine("       <tr>");
    //        //                    writer.WriteLine("          <td>");
    //        //                    writer.WriteLine("            <div class=\"sbkCiv_Citation\">");

    //        //                    started = true;
    //        //                }

    //        //                string text_file = TextFileLocation + "\\" + thisTextFile;

    //        //                try
    //        //                {


    //        //                    StreamReader reader = new StreamReader(text_file);
    //        //                    string text_line = reader.ReadLine();
    //        //                    while (text_line != null)
    //        //                    {
    //        //                        writer.WriteLine(text_line + "<br />");
    //        //                        text_line = reader.ReadLine();
    //        //                    }
    //        //                    reader.Close();
    //        //                }
    //        //                catch
    //        //                {
    //        //                    writer.WriteLine("Unable to read file: " + text_file);
    //        //                }

    //        //                writer.WriteLine("<br /><br />");
    //        //            }

    //        //            // End this if it was ever started
    //        //            if (started)
    //        //            {
    //        //                writer.WriteLine("            </div>");
    //        //                writer.WriteLine("          </td>");
    //        //                writer.WriteLine("       </tr>");
    //        //            }
    //        //        }
    //        //    }
    //        //}

    //        //writer.WriteLine("      </table>");
    //        //writer.WriteLine("      </div>");

    //        //// Write the footer
    //        //Display_Footer(writer, itemWriter.Skin);

    //        //writer.WriteLine("</body>");
    //        //writer.WriteLine("</html>");

    //        //writer.Flush();
    //        //writer.Close();

    //        //// Restore the text searchable flag and robot flag
    //        //currentMode.Is_Robot = false;
    //        //CurrentItem.Behaviors.Text_Searchable = textSearchable;
    //    }

    //    private void recursively_write_toc(StreamWriter Writer, abstract_TreeNode TreeNode, string Indent)
    //    {
    //        // Write this label, regardless of whether it is a page or a division (some page names have useful info)
    //        if ( TreeNode.Label.Length > 0 )
    //            Writer.WriteLine(Indent + TreeNode.Label + "<br />");
    //        else
    //            Writer.WriteLine(Indent + TreeNode.Type + "<br />");

    //        // If not a page, recurse more
    //        if (!TreeNode.Page)
    //        {
    //            Division_TreeNode divNode = (Division_TreeNode)TreeNode;
    //            foreach (abstract_TreeNode childNode in divNode.Nodes)
    //            {
    //                recursively_write_toc(Writer, childNode, Indent + "&nbsp; &nbsp; ");
    //            }
    //        }
    //    }


    //    /// <summary> Writes the static header to go at the top of the static digital resource page </summary>
    //    /// <param name="Writer"> Open stream to write the HTML header to </param>
    //    /// <param name="HTMLSkin"> Default html web skin/interface</param>
    //    /// <param name="CurrentItem"> Current item, to include the aggregationPermissions in the breadcrumbs </param>
    //    public void Display_Header(TextWriter Writer, Web_Skin_Object HTMLSkin, SobekCM_Item CurrentItem )
    //    {
    //        StringBuilder breadcrumb_builder = new StringBuilder("<a href=\"" + currentMode.Base_URL + "\">" + UI_ApplicationCache_Gateway.Settings.System.System_Abbreviation + " Home</a>");

    //        int codes_added = 0;
    //            if (CurrentItem.Behaviors.Aggregation_Count > 0)
    //            {
    //                foreach (Aggregation_Info aggregation in CurrentItem.Behaviors.Aggregations)
    //                {
    //                    string aggrCode = aggregation.Code;
    //                    if (aggrCode.ToLower() != currentMode.Aggregation)
    //                    {
    //                        if ((aggrCode.ToUpper() != "I" + CurrentItem.Bib_Info.Source.Code.ToUpper()) &&
    //                            (aggrCode.ToUpper() != "I" + CurrentItem.Bib_Info.Location.Holding_Code.ToUpper()))
    //                        {
    //                            Item_Aggregation_Related_Aggregations thisAggr = UI_ApplicationCache_Gateway.Aggregations[aggrCode];
    //                            if ((thisAggr != null) && (thisAggr.Active))
    //                            {
    //                                breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + currentMode.Base_URL +
    //                                                          aggrCode.ToLower() + "\">" +
    //                                                          thisAggr.ShortName +
    //                                                          "</a>");
    //                                codes_added++;
    //                            }
    //                        }
    //                    }
    //                    if (codes_added == 5)
    //                        break;
    //                }
    //            }

    //        if (codes_added < 5)
    //        {
    //            if (CurrentItem.Bib_Info.Source.Code.Length > 0) 
    //            {
    //                // Add source code
    //                string source_code = CurrentItem.Bib_Info.Source.Code;
    //                if ((source_code[0] != 'i') && (source_code[0] != 'I'))
    //                    source_code = "I" + source_code;
    //                Item_Aggregation_Related_Aggregations thisSourceAggr = UI_ApplicationCache_Gateway.Aggregations[source_code];
    //                if ((thisSourceAggr != null) && (!thisSourceAggr.Hidden) && (thisSourceAggr.Active))
    //                {
    //                    string source_name = thisSourceAggr.ShortName;
    //                    if (source_name.ToUpper() != "ADDED AUTOMATICALLY")
    //                    {
    //                        breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + currentMode.Base_URL +
    //                                                  source_code.ToLower() + "\">" +
    //                                                  source_name + "</a>");
    //                    }
    //                }

    //                // Add the holding code
    //                if ((CurrentItem.Bib_Info.Location.Holding_Code.Length > 0) &&
    //                    (CurrentItem.Bib_Info.Location.Holding_Code != CurrentItem.Bib_Info.Source.Code))
    //                {
    //                    // Add holding code
    //                    string holding_code = CurrentItem.Bib_Info.Location.Holding_Code;
    //                    if ((holding_code[0] != 'i') && (holding_code[0] != 'I'))
    //                        holding_code = "I" + holding_code;

    //                    Item_Aggregation_Related_Aggregations thisAggr = UI_ApplicationCache_Gateway.Aggregations[holding_code];
    //                    if ((thisAggr != null) && (!thisAggr.Hidden) && (thisAggr.Active))
    //                    {
    //                        string holding_name = thisAggr.ShortName;

    //                        if (holding_name.ToUpper() != "ADDED AUTOMATICALLY")
    //                        {
    //                            breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + currentMode.Base_URL +
    //                                                      holding_code.ToLower() + "\">" +
    //                                                      holding_name + "</a>");
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                if (CurrentItem.Bib_Info.Location.Holding_Code.Length > 0)
    //                {
    //                    // Add holding code
    //                    string holding_code = CurrentItem.Bib_Info.Location.Holding_Code;
    //                    if ((holding_code[0] != 'i') && (holding_code[0] != 'I'))
    //                        holding_code = "I" + holding_code;
    //                    string holding_name = UI_ApplicationCache_Gateway.Aggregations.Get_Collection_Short_Name(holding_code);
    //                    if (holding_name.ToUpper() != "ADDED AUTOMATICALLY")
    //                    {
    //                        breadcrumb_builder.Append(" &nbsp;|&nbsp; <a href=\"" + currentMode.Base_URL +
    //                                                  holding_code.ToLower() + "\">" +
    //                                                  holding_name + "</a>");
    //                    }
    //                }
    //            }
    //        }
    //        string breadcrumbs = breadcrumb_builder.ToString();

    //        Writer.WriteLine(HTMLSkin.Header_Item_HTML.Replace("<%URLOPTS%>", "").Replace("<%?URLOPTS%>", "").Replace("<%&URLOPTS%>", "").Replace("<%BREADCRUMBS%>", breadcrumbs).Replace("<%MYSOBEK%>", "").Replace("<%ENGLISH%>", "").Replace("<%FRENCH%>", "").Replace("<%SPANISH%>", "").Replace("<%LOWGRAPHICS%>", "").Replace("<%HIGHGRAPHICS%>", "").Replace("<%BASEURL%>", currentMode.Base_URL).Replace("<%BANNER%>",String.Empty));
    //        Writer.WriteLine(String.Empty);
    //    }

    //    ///// <summary> Writes the static header to go at the top of the static digital resource page </summary>
    //    ///// <param name="Writer"> Open stream to write the HTML header to </param>
    //    ///// <param name="HTMLSkin"> Default html web skin/interface</param>
    //    ///// <param name="Banner"> Banner HTML</param>
    //    //public void Display_Header(TextWriter Writer, Web_Skin_Object HTMLSkin, string Banner)
    //    //{
    //    //    string breadcrumbs = "<a href=\"" + currentMode.Base_URL + "\">" + UI_ApplicationCache_Gateway.Settings.System.System_Name + " Home</a>";
    //    //    Writer.WriteLine(HTMLSkin.Header_Item_HTML.Replace("<%URLOPTS%>", "").Replace("<%?URLOPTS%>", "").Replace("<%&URLOPTS%>", "").Replace("<%BREADCRUMBS%>", breadcrumbs).Replace("<%MYSOBEK%>", "").Replace("<%ENGLISH%>", "").Replace("<%FRENCH%>", "").Replace("<%SPANISH%>", "").Replace("<%LOWGRAPHICS%>", "").Replace("<%HIGHGRAPHICS%>", "").Replace("<%BASEURL%>", currentMode.Base_URL).Replace("<%BANNER%>", Banner));
    //    //    Writer.WriteLine(String.Empty);
    //    //}

    //    ///// <summary> Writes the static footer to go at the bottom of the static digital resource page </summary>
    //    ///// <param name="Writer"> Open stream to write the HTML footer to </param>
    //    ///// <param name="HTMLSkin"> Default html web skin/interface</param>
    //    //public void Display_Footer(TextWriter Writer, Web_Skin_Object HTMLSkin)
    //    //{

    //    //    // Get the current contact URL
    //    //    Display_Mode_Enum thisMode = currentMode.Mode;
    //    //    currentMode.Mode = Display_Mode_Enum.Contact;
    //    //    string contact = UrlWriterHelper.Redirect_URL(currentMode);

    //    //    // Restore the old mode
    //    //    currentMode.Mode = thisMode;

    //    //    Writer.WriteLine(currentMode.Mode == Display_Mode_Enum.Item_Display
    //    //                         ? HTMLSkin.Footer_Item_HTML.Replace("<%CONTACT%>", contact).Replace("<%URLOPTS%>", "").Replace("<%?URLOPTS%>", "").Replace("<%&URLOPTS%>", "").Replace("<%VERSION%>", "").Replace("<%BASEURL%>", currentMode.Base_URL).Trim()
    //    //                         : HTMLSkin.Footer_HTML.Replace("<%CONTACT%>", contact).Replace("<%URLOPTS%>", "").Replace("<%?URLOPTS%>", "").Replace("<%&URLOPTS%>", "").Replace("<%VERSION%>", "").Replace("<%BASEURL%>", currentMode.Base_URL).Trim());
    //    //}

    //    #endregion

    //    #region Code for creating the RSS feeds 

    //    /// <summary> Create the RSS feed files necessary </summary>
    //    /// <param name="Collection_Code"> Aggregation Code for this collection </param>
    //    /// <param name="RssFeedLocation"> Location for the updated RSS feed to be updated </param>
    //    /// <param name="Collection_Title"> Title of this aggregation/collection </param>
    //    /// <param name="AllItems"> DataSet of all items within this aggregation </param>
    //    /// <returns> TRUE if successful, otherwise FALSE </returns>
    //    public bool Create_RSS_Feed(string Collection_Code, string RssFeedLocation, string Collection_Title, DataSet AllItems)
    //    {
    //        try
    //        {
    //            if (AllItems == null)
    //                return false;

    //            int recordNumber = 0;
    //            int final_most_recent = 20;
    //            if (Collection_Code == "all")
    //                final_most_recent = 100;

    //            DataView viewer = new DataView(AllItems.Tables[0]) {Sort = "CreateDate DESC"};

    //            StreamWriter rss_writer = new StreamWriter(RssFeedLocation + Collection_Code + "_rss.xml", false, Encoding.UTF8);
    //            rss_writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    //            rss_writer.WriteLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
    //            rss_writer.WriteLine("<channel>");
    //            rss_writer.WriteLine("<title>" + Collection_Title.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " (All Items) </title>");
    //            rss_writer.WriteLine("<link>" + primaryWebServerUrl+ "data/rss/" + Collection_Code + "_rss.xml</link>");
    //            rss_writer.WriteLine("<atom:link href=\"" + primaryWebServerUrl + "rss/" + Collection_Code + "_rss.xml\" rel=\"self\" type=\"application/rss+xml\" />");
    //            rss_writer.WriteLine("<description></description>");
    //            rss_writer.WriteLine("<language>en</language>");
    //            rss_writer.WriteLine("<copyright>Copyright " + DateTime.Now.Year + "</copyright>");
    //            rss_writer.WriteLine("<lastBuildDate>" + DateTime_Helper.ToRfc822(DateTime.Now) + "</lastBuildDate>");

    //            rss_writer.WriteLine("");

    //            StreamWriter short_rss_writer = new StreamWriter(RssFeedLocation + Collection_Code + "_short_rss.xml", false, Encoding.UTF8);
    //            short_rss_writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
    //            short_rss_writer.WriteLine("<rss version=\"2.0\" xmlns:atom=\"http://www.w3.org/2005/Atom\">");
    //            short_rss_writer.WriteLine("<channel>");
    //            short_rss_writer.WriteLine("<title>" + Collection_Title.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + " (Most Recent Items) </title>");
    //            short_rss_writer.WriteLine("<link>" + primaryWebServerUrl + "rss/" + Collection_Code + "_short_rss.xml</link>");
    //            short_rss_writer.WriteLine("<atom:link href=\"" + primaryWebServerUrl + "rss/" + Collection_Code + "_short_rss.xml\" rel=\"self\" type=\"application/rss+xml\" />");
    //            short_rss_writer.WriteLine("<description></description>");
    //            short_rss_writer.WriteLine("<language>en</language>");
    //            short_rss_writer.WriteLine("<copyright>Copyright " + DateTime.Now.Year + "</copyright>");
    //            short_rss_writer.WriteLine("<lastBuildDate>" + DateTime_Helper.ToRfc822(DateTime.Now) + "</lastBuildDate>");
    //            short_rss_writer.WriteLine("");

    //            foreach (DataRowView thisRowView in viewer)
    //            {
    //                // Determine the folder name
    //                string bibid = thisRowView.Row["BibID"].ToString();
    //                string vid = thisRowView["VID"].ToString();

    //                recordNumber++;
    //                if (recordNumber <= final_most_recent)
    //                {
    //                    short_rss_writer.WriteLine("<item>");
    //                    short_rss_writer.WriteLine("<title>" + thisRowView.Row["Title"].ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</title>");
    //                    short_rss_writer.WriteLine("<description></description>");

    //                    short_rss_writer.WriteLine("<link>" + primaryWebServerUrl + bibid + "/" + vid + "</link>");

    //                    string create_date_string = thisRowView.Row["CreateDate"].ToString();
    //                    DateTime dateTime;
    //                    if (DateTime.TryParse(create_date_string, out dateTime))
    //                    {
    //                        string formattedDate = DateTime_Helper.ToRfc822(dateTime);
    //                        short_rss_writer.WriteLine("<pubDate>" + formattedDate + " </pubDate>");
    //                    }

    //                    short_rss_writer.WriteLine("<guid>" + primaryWebServerUrl + bibid + "/" + vid + "</guid>");
    //                    short_rss_writer.WriteLine("</item>");
    //                    short_rss_writer.WriteLine("");
    //                }

    //                rss_writer.WriteLine("<item>");
    //                rss_writer.WriteLine("<title>" + thisRowView.Row["Title"].ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") + "</title>");
    //                rss_writer.WriteLine("<description></description>");
    //                rss_writer.WriteLine("<link>" + primaryWebServerUrl + bibid + "/" + vid + "</link>");

    //                string create_date_string2 = thisRowView.Row["CreateDate"].ToString();
    //                DateTime dateTime2;
    //                if (DateTime.TryParse(create_date_string2, out dateTime2))
    //                {
    //                    string formattedDate = DateTime_Helper.ToRfc822(dateTime2);
    //                    rss_writer.WriteLine("<pubDate>" + formattedDate + " </pubDate>");
    //                }

    //                rss_writer.WriteLine("<guid>" + primaryWebServerUrl + bibid + "/" + vid + "</guid>");
    //                rss_writer.WriteLine("</item>");
    //                rss_writer.WriteLine("");
    //            }

    //            rss_writer.WriteLine("</channel>");
    //            rss_writer.WriteLine("</rss>");
    //            rss_writer.Flush();
    //            rss_writer.Close();

    //            short_rss_writer.WriteLine("</channel>");
    //            short_rss_writer.WriteLine("</rss>");
    //            short_rss_writer.Flush();
    //            short_rss_writer.Close();

    //            return AllItems.Tables[0].Rows.Count > 0;
    //        }
    //        catch
    //        {
    //           // Console.WriteLine(@"ERROR BUILDING RSS FEED {0}", Collection_Code);
    //            return false;
    //        }
    //    }

    //    #endregion

    }
}
