﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Client;
using SobekCM.Core.Navigation;
using SobekCM.Core.Settings;
using SobekCM.Core.UI_Configuration;
using SobekCM.Core.UI_Configuration.StaticResources;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.HTML;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Library.UI;
using SobekCM.Resource_Object;
using SobekCM.Resource_Object.Database;
using SobekCM.Resource_Object.Divisions;
using SobekCM.Resource_Object.Metadata_Modules;
using SobekCM.Resource_Object.Metadata_Modules.GeoSpatial;
using SobekCM.Tools;
using SobekCM_Resource_Database;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Map editing (via the Google maps interface) item viewer prototyper, which is used to check to see if a user has access to 
    /// edit the coordinate information for this item, and to create the viewer itself if the user selects that option </summary>
    public class Google_Coordinate_Entry_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the Google_Coordinate_Entry_ItemViewer_Prototyper class </summary>
        public Google_Coordinate_Entry_ItemViewer_Prototyper()
        {
            ViewerType = "MAP_EDIT";
            ViewerCode = "mapedit";
        }

        /// <summary> Name of this viewer, which matches the viewer name from the database and 
        /// in the configuration files as well.  This is actually populate by the configuration information </summary>
        public string ViewerType { get; set; }

        /// <summary> Code for this viewer, which can also be set from the configuration information </summary>
        public string ViewerCode { get; set; }

        /// <summary> If this viewer is tied to certain files existing in the digital resource, this lists all the 
        /// possible file extensions this supports (from the configuration file usually) </summary>
        public string[] FileExtensions { get; set; }

        /// <summary> Indicates if the specified item matches the basic requirements for this viewer, or
        /// if this viewer should be ignored for this item </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer really should be included </param>
        /// <returns> TRUE if this viewer should generally be included with this item, otherwise FALSE </returns>
        public virtual bool Include_Viewer(BriefItemInfo CurrentItem)
        {
            // This should always be included (although it won't be accessible or shown to everyone)
            return true;
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since PDFs should never be shown if an item is checked out </returns>
        public virtual bool Override_On_Checkout(BriefItemInfo CurrentItem)
        {
            return false;
        }

        /// <summary> Flag indicates if the current user has access to this viewer for the item </summary>
        /// <param name="CurrentItem"> Digital resource to see if the current user has correct permissions to use this viewer </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public virtual bool Has_Access(BriefItemInfo CurrentItem, User_Object CurrentUser, bool IsRestricted)
        {
            // If there is no user (or they aren't logged in) then obviously, they can't edit this
            if ((CurrentUser == null) || (!CurrentUser.LoggedOn))
            {
                return false;
            }

            // See if this user can edit this item
            bool userCanEditItem = CurrentUser.Can_Edit_This_Item(CurrentItem.BibID, CurrentItem.Type, CurrentItem.Behaviors.Source_Institution_Aggregation, CurrentItem.Behaviors.Holding_Location_Aggregation, CurrentItem.Behaviors.Aggregation_Code_List);
            if (!userCanEditItem)
            {
                // Can't edit, so don't show and return FALSE
                return false;
            }

            // Apparently it can be shown
            return true;
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        public virtual void Add_Menu_Items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IsRestricted)
        {
            // Do nothing since this is already handed and added to the menu by the MANAGE MENU item viewer
        }

        /// <summary> Creates and returns the an instance of the <see cref="Google_Coordinate_Entry_ItemViewer"/> class for editing
        /// the coordinate information associated with this digtial resource within an online google maps interface </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="Google_Coordinate_Entry_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, RequestCache_RequestFlags CurrentFlags)
        {
            return new Google_Coordinate_Entry_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer );
        }
    }

    /// <summary> Item viewer allows the user to edit the coordinate information associated with this 
    /// digital resource within an online google maps interface </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Google_Coordinate_Entry_ItemViewer : abstractNoPaginationItemViewer
    {
        private SobekCM_Item currentItem;
        private GeoSpatial_Information geoInfo2;
        List<Coordinate_Polygon> allPolygons;
        List<Coordinate_Point> allPoints;
        List<Coordinate_Line> allLines;
        List<Coordinate_Circle> allCircles;

        private Dictionary<string, object> options;

        /// <summary> Constructor for a new instance of the Google_Coordinate_Entry_ItemViewer class, used to edit the 
        /// coordinate information associated with this digital resource within an online google maps interface </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public Google_Coordinate_Entry_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer )
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            try
            {

                // Get the full SobekCM item
                Tracer.Add_Trace("Google_Coordinate_Entry_ItemViewer.Constructor", "Try to pull this sobek complete item");
                currentItem = SobekEngineClient.Items.Get_Sobek_Item(CurrentRequest.BibID, CurrentRequest.VID, Tracer);
                if (currentItem == null)
                {
                    Tracer.Add_Trace("Google_Coordinate_Entry_ItemViewer.Constructor", "Unable to build complete item");
                    CurrentRequest.Mode = Display_Mode_Enum.Error;
                    CurrentRequest.Error_Message = "Invalid Request : Unable to build complete item";
                    return;
                }


                //string resource_directory = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + CurrentItem.Web.AssocFilePath;
                //string current_mets = resource_directory + CurrentItem.METS_Header.ObjectID + ".mets.xml";

                // If there is no user, send to the login
                if (CurrentUser == null)
                {
                    CurrentRequest.Mode = Display_Mode_Enum.My_Sobek;
                    CurrentRequest.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                    CurrentRequest.Return_URL = BriefItem.BibID + "/" + BriefItem.VID + "/mapedit";
                    UrlWriterHelper.Redirect(CurrentRequest);
                    return;
                }

                //holds actions from page
                string action = HttpContext.Current.Request.Form["action"] ?? String.Empty;
                string payload = HttpContext.Current.Request.Form["payload"] ?? String.Empty;

                // See if there were hidden requests
                if (!String.IsNullOrEmpty(action))
                {
                    if (action == "save")
                        SaveContent(payload);
                }

                ////create a backup of the mets
                //string backup_directory = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + Current_Item.Web.AssocFilePath + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name;
                //string backup_mets_name = backup_directory + "\\" + CurrentItem.METS_Header.ObjectID + "_" + DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + ".mets.bak";
                //File.Copy(current_mets, backup_mets_name);

            }
            catch (Exception ee)
            {
                //Custom_Tracer.Add_Trace("MapEdit Start Failure");
                throw new ApplicationException("MapEdit Start Failure\r\n" + ee.Message);
            }
        }

        /// <summary> parse and save incoming message  </summary>
        /// <param name="SendData"> message from page </param>
        public void SaveContent(String SendData)
        {
            try
            {

                //get rid of excess string 
                SendData = SendData.Replace("{\"sendData\": \"", "").Replace("{\"sendData\":\"", "");

                //validate
                if (SendData.Length == 0)
                    return;

                //ensure we have a geo-spatial module in the digital resource
                GeoSpatial_Information resourceGeoInfo = currentItem.Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
                //if there was no geo-spatial module
                if (resourceGeoInfo == null)
                {
                    //create new geo-spatial module, if we do not already have one
                    resourceGeoInfo = new GeoSpatial_Information();
                    currentItem.Add_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY, resourceGeoInfo);
                }

                //get the pages
                List<abstract_TreeNode> pages = currentItem.Divisions.Physical_Tree.Pages_PreOrder;

                //create a new list of all the polygons for a resource item
                Dictionary<string, Page_TreeNode> pageLookup = new Dictionary<string, Page_TreeNode>();
                int page_index = 1;
                foreach (var abstractTreeNode in pages)
                {
                    var pageNode = (Page_TreeNode)abstractTreeNode;
                    if (pageNode.Label.Length == 0)
                        pageLookup["Page " + page_index] = pageNode;
                    else
                        pageLookup[pageNode.Label] = pageNode;
                    page_index++;
                }

                //get the length of incoming message
                int index1 = SendData.LastIndexOf("~", StringComparison.Ordinal);

                //split into each save message
                string[] allSaves = SendData.Substring(0, index1).Split('~');

                //hold save type handle
                string saveTypeHandle;
                //go through each item to save and check for ovelrays and item only not pois (ORDER does matter because these will be saved to db before pois are saved)
                foreach (string t in allSaves)
                {
                    //get the length of save message
                    int index2 = t.LastIndexOf("|", StringComparison.Ordinal);
                    //split into save elements
                    string[] ar = t.Substring(0, index2).Split('|');
                    //determine the save type handle (position 0 in array)
                    saveTypeHandle = ar[0];
                    //determine the save type (position 1 in array)
                    string saveType = ar[1];
                    //based on saveType, parse into objects
                    if (saveTypeHandle == "save")
                    {
                        //handle save based on type
                        switch (saveType)
                        {
                            #region item
                            case "item":
                                //prep incoming lat/long
                                string[] temp1 = ar[2].Split(',');
                                double temp1Lat = Convert.ToDouble(temp1[0].Replace("(", ""));
                                double temp1Long = Convert.ToDouble(temp1[1].Replace(")", ""));
                                ////clear specific geo obj
                                //resourceGeoInfo.Clear_Specific(Convert.ToString(ar[3]));
                                //clear all the previous mains featureTypes (this will work for an item because there is only ever one item)
                                resourceGeoInfo.Clear_NonPOIs();
                                //add the point obj
                                Coordinate_Point newPoint = new Coordinate_Point(temp1Lat, temp1Long, currentItem.METS_Header.ObjectID, "main");
                                //add the new point 
                                resourceGeoInfo.Add_Point(newPoint);
                                //save to db
                                SobekCM_Item_Database.Save_Digital_Resource(currentItem, options);
                                break;
                            #endregion
                            #region overlay
                            case "overlay":
                                //parse the array id of the page
                                int arrayId = (Convert.ToInt32(ar[2]) - 1); //is this always true (minus one of the human page id)?
                                //add the label to page obj
                                pages[arrayId].Label = ar[3];
                                //get the geocoordinate object for that pageId
                                GeoSpatial_Information pageGeo = pages[arrayId].Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
                                //if there isnt any already there
                                if (pageGeo == null)
                                {
                                    //create new
                                    pageGeo = new GeoSpatial_Information();
                                    //create a polygon
                                    Coordinate_Polygon pagePolygon = new Coordinate_Polygon();
                                    //prep incoming bounds
                                    string[] temp2 = ar[4].Split(',');
                                    pagePolygon.Clear_Edge_Points();
                                    pagePolygon.Add_Edge_Point(Convert.ToDouble(temp2[0].Replace("(", "")), Convert.ToDouble(temp2[1].Replace(")", "")));
                                    pagePolygon.Add_Edge_Point(Convert.ToDouble(temp2[2].Replace("(", "")), Convert.ToDouble(temp2[3].Replace(")", "")));
                                    pagePolygon.Recalculate_Bounding_Box();
                                    //add the rotation
                                    double result;
                                    pagePolygon.Rotation = Double.TryParse(ar[6], out result) ? result : 0;
                                    //add the featureType (explicitly add to make sure it is there)
                                    pagePolygon.FeatureType = "main";
                                    //add the label
                                    pagePolygon.Label = ar[3];
                                    //add the polygon type
                                    pagePolygon.PolygonType = "rectangle";
                                    //add polygon to pagegeo
                                    pageGeo.Add_Polygon(pagePolygon);
                                }
                                else
                                {
                                    try
                                    {
                                        //get current polygon info
                                        Coordinate_Polygon pagePolygon = pageGeo.Polygons[0];
                                        //prep incoming bounds
                                        string[] temp2 = ar[4].Split(',');
                                        pagePolygon.Clear_Edge_Points();
                                        pagePolygon.Add_Edge_Point(Convert.ToDouble(temp2[0].Replace("(", "")), Convert.ToDouble(temp2[1].Replace(")", "")));
                                        pagePolygon.Add_Edge_Point(Convert.ToDouble(temp2[2].Replace("(", "")), Convert.ToDouble(temp2[3].Replace(")", "")));
                                        pagePolygon.Recalculate_Bounding_Box();
                                        //add the rotation
                                        double result;
                                        pagePolygon.Rotation = Double.TryParse(ar[6], out result) ? result : 0;
                                        //add the featureType (explicitly add to make sure it is there)
                                        pagePolygon.FeatureType = "main";
                                        //add the label
                                        pagePolygon.Label = ar[3];
                                        //add the polygon type
                                        pagePolygon.PolygonType = "rectangle";
                                        //clear all previous nonPOIs for this page (NOTE: this will only work if there is only one main page item)
                                        pageGeo.Clear_NonPOIs();
                                        //add polygon to pagegeo
                                        pageGeo.Add_Polygon(pagePolygon);
                                    }
                                    catch (Exception)
                                    {
                                        //there were no polygons
                                        try
                                        {
                                            //make a polygon
                                            Coordinate_Polygon pagePolygon = new Coordinate_Polygon();
                                            //prep incoming bounds
                                            string[] temp2 = ar[4].Split(',');
                                            pagePolygon.Clear_Edge_Points();
                                            pagePolygon.Add_Edge_Point(Convert.ToDouble(temp2[0].Replace("(", "")), Convert.ToDouble(temp2[1].Replace(")", "")));
                                            pagePolygon.Add_Edge_Point(Convert.ToDouble(temp2[2].Replace("(", "")), Convert.ToDouble(temp2[3].Replace(")", "")));
                                            pagePolygon.Recalculate_Bounding_Box();
                                            //add the rotation
                                            double result;
                                            pagePolygon.Rotation = Double.TryParse(ar[6], out result) ? result : 0;
                                            //add the featureType (explicitly add to make sure it is there)
                                            pagePolygon.FeatureType = "main";
                                            //add the label
                                            pagePolygon.Label = ar[3];
                                            //add the polygon type
                                            pagePolygon.PolygonType = "rectangle";
                                            //clear all previous nonPOIs for this page (NOTE: this will only work if there is only one main page item)
                                            pageGeo.Clear_NonPOIs();
                                            //add polygon to pagegeo
                                            pageGeo.Add_Polygon(pagePolygon);
                                        }
                                        catch (Exception)
                                        {
                                            //welp...
                                        }
                                    }
                                }
                                //add the pagegeo obj
                                pages[arrayId].Add_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY, pageGeo);
                                //save to db
                                SobekCM_Item_Database.Save_Digital_Resource(currentItem, options);
                                break;
                            #endregion
                        }
                    }
                    else
                    {
                        if (saveTypeHandle == "delete")
                        {
                            switch (saveType)
                            {
                                #region item
                                case "item":
                                    //clear nonpoipoints
                                    resourceGeoInfo.Clear_NonPOIPoints();
                                    //save to db
                                    SobekCM_Item_Database.Save_Digital_Resource(currentItem, options);
                                    break;
                                #endregion
                                #region overlay
                                case "overlay":
                                    try
                                    {
                                        //parse the array id of the page
                                        int arrayId = (Convert.ToInt32(ar[2]) - 1); //is this always true (minus one of the human page id)?
                                        //get the geocoordinate object for that pageId
                                        GeoSpatial_Information pageGeo = pages[arrayId].Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
                                        if (pageGeo != null)
                                        {
                                            Coordinate_Polygon pagePolygon = pageGeo.Polygons[0];

                                            //reset edgepoints
                                            pagePolygon.Clear_Edge_Points();
                                            //reset rotation
                                            pagePolygon.Rotation = 0;
                                            //add the featureType (explicitly add to make sure it is there)
                                            pagePolygon.FeatureType = "hidden";
                                            //add the polygon type
                                            pagePolygon.PolygonType = "hidden";
                                            //clear all previous nonPOIs for this page (NOTE: this will only work if there is only one main page item)
                                            pageGeo.Clear_NonPOIs();
                                            //add polygon to pagegeo
                                            pageGeo.Add_Polygon(pagePolygon);
                                        }

                                        ////if there isnt any already there
                                        //if (pageGeo != null)
                                        //    pageGeo.Remove_Polygon(pageGeo.Polygons[0]);

                                        //add the pagegeo obj
                                        pages[arrayId].Add_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY, pageGeo);

                                        //save to db
                                        SobekCM_Item_Database.Save_Digital_Resource(currentItem, options);
                                    }
                                    catch (Exception)
                                    {
                                        //
                                    }

                                    break;
                                #endregion
                            }
                        }
                    }
                }

                //check to see if save poi clear has already been fired...
                bool firedOnce = true;
                //go through each item to save and check for pois only
                foreach (string t in allSaves)
                {
                    //get the length of save message
                    int index2 = t.LastIndexOf("|", StringComparison.Ordinal);
                    //split into save elements
                    string[] ar = t.Substring(0, index2).Split('|');
                    //determine the save type handle (position 0 in array)
                    saveTypeHandle = ar[0];
                    //determine the save type (position 1 in array)
                    string saveType = ar[1];
                    //based on saveType, parse into objects
                    if (saveTypeHandle == "save")
                    {
                        //handle save based on type
                        switch (saveType)
                        {
                            #region poi
                            case "poi":
                                //fixes bug
                                if (firedOnce)
                                {
                                    //clear previous poi points
                                    resourceGeoInfo.Clear_POIs();
                                    firedOnce = false;
                                }
                                //get specific geometry (KML Standard)
                                switch (ar[2])
                                {
                                    case "marker":
                                        //prep incoming lat/long
                                        string[] temp2 = ar[4].Split(',');
                                        double temp2Lat = Convert.ToDouble(temp2[0].Replace("(", ""));
                                        double temp2Long = Convert.ToDouble(temp2[1].Replace(")", ""));
                                        //add the new point 
                                        resourceGeoInfo.Add_Point(temp2Lat, temp2Long, ar[3], "poi");
                                        break;
                                    case "circle":
                                        //create new circle
                                        Coordinate_Circle poiCircle = new Coordinate_Circle { Label = ar[3], Radius = Convert.ToDouble(ar[5]), FeatureType = "poi" };

                                        //add the incoming lat/long
                                        string[] temp3 = ar[4].Split(',');
                                        poiCircle.Latitude = Convert.ToDouble(temp3[0].Replace("(", ""));
                                        poiCircle.Longitude = Convert.ToDouble(temp3[1].Replace(")", ""));
                                        //add to the resource obj
                                        resourceGeoInfo.Add_Circle(poiCircle);
                                        break;
                                    case "rectangle":
                                        //create new polygon
                                        Coordinate_Polygon poiRectangle = new Coordinate_Polygon { Label = ar[3], FeatureType = "poi", PolygonType = "rectangle" };

                                        //add the incoming bounds
                                        string[] temp4 = ar[4].Split(',');
                                        poiRectangle.Add_Edge_Point(Convert.ToDouble(temp4[0].Replace("(", "")), Convert.ToDouble(temp4[1].Replace(")", "")));
                                        poiRectangle.Add_Edge_Point(Convert.ToDouble(temp4[2].Replace("(", "")), Convert.ToDouble(temp4[3].Replace(")", "")));
                                        poiRectangle.Recalculate_Bounding_Box();
                                        //add to resource obj
                                        resourceGeoInfo.Add_Polygon(poiRectangle);
                                        break;
                                    case "polygon":
                                        //create new polygon
                                        Coordinate_Polygon poiPolygon = new Coordinate_Polygon { Label = ar[3], FeatureType = "poi" };

                                        //add the edge points
                                        for (int i2 = 5; i2 < ar.Length; i2++)
                                        {
                                            string[] temp5 = ar[i2].Split(',');
                                            poiPolygon.Add_Edge_Point(Convert.ToDouble(temp5[0].Replace("(", "")), Convert.ToDouble(temp5[1].Replace(")", "")));
                                        }
                                        //add the polygon
                                        resourceGeoInfo.Add_Polygon(poiPolygon);
                                        break;
                                    case "polyline":
                                        //create new line
                                        Coordinate_Line poiLine = new Coordinate_Line { Label = ar[3], FeatureType = "poi" };

                                        //add the edge points
                                        for (int i2 = 5; i2 < ar.Length; i2++)
                                        {
                                            string[] temp5 = ar[i2].Split(',');
                                            poiLine.Add_Point(Convert.ToDouble(temp5[0].Replace("(", "")), Convert.ToDouble(temp5[1].Replace(")", "")), "");
                                        }
                                        //add the line
                                        resourceGeoInfo.Add_Line(poiLine);
                                        break;
                                }
                                break;
                            #endregion
                        }
                    }
                }

                #region prep saving dir
                //create inprocessing directory
                string userInProcessDirectory = UI_ApplicationCache_Gateway.Settings.User_InProcess_Directory(CurrentUser, "mapwork");
                string backupDirectory = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + currentItem.Web.AssocFilePath + UI_ApplicationCache_Gateway.Settings.Resources.Backup_Files_Folder_Name;

                //ensure the user's process directory exists
                if (!Directory.Exists(userInProcessDirectory))
                    Directory.CreateDirectory(userInProcessDirectory);
                //ensure the backup directory exists
                if (!Directory.Exists(backupDirectory))
                    Directory.CreateDirectory(backupDirectory);

                string resource_directory = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network + currentItem.Web.AssocFilePath;
                string current_mets = resource_directory + currentItem.METS_Header.ObjectID + ".mets.xml";
                string backup_mets = backupDirectory + "\\" + currentItem.METS_Header.ObjectID + "_" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + "_" + DateTime.Now.Hour.ToString() + DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() + ".mets.xml.BAK";
                string metsInProcessFile = userInProcessDirectory + "\\" + currentItem.BibID + "_" + currentItem.VID + ".mets.xml";
                #endregion

                #region Save mets and db
                //save the item to the temporary location
                currentItem.Save_METS(userInProcessDirectory + "\\" + currentItem.BibID + "_" + currentItem.VID + ".mets.xml");
                //move temp mets to prod
                File.Copy(metsInProcessFile, current_mets, true);
                //delete in process mets file 
                File.Delete(metsInProcessFile);
                //create a backup mets file
                File.Copy(current_mets, backup_mets, true);
                #endregion

            }
            catch (Exception)
            {
                //Custom_Tracer.Add_Trace("MapEdit Save Error");
                throw new ApplicationException("MapEdit Save Error");
                //throw;
            }

        }

        /// <summary> CSS ID for the viewer viewport for this particular viewer </summary>
        /// <value> This always returns the value 'sbkDiv_Viewer' </value>
        public override string ViewerBox_CssId
        {
            get { return "sbkDiv_Viewer"; }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Google_Coordinate_Entry_ItemViewer.Write_Main_Viewer_Section", "");
            }

            try
            {


                //page content
                Output.WriteLine("<td>");

                Output.WriteLine(" <input type=\"hidden\" id=\"action\" name=\"action\" value=\"\" /> ");
                Output.WriteLine(" <input type=\"hidden\" id=\"payload\" name=\"payload\" value=\"\" /> ");
                Output.WriteLine("  ");

                //loading blanket
                Output.WriteLine("  ");
                Output.WriteLine(" <div id=\"mapedit_blanket_loading\"><div>Loading...<br/><br/><img src=\"" + Static_Resources_Gateway.Ajax_Loader_Img + "\"></div></div> ");
                Output.WriteLine("  ");

                //standard css
                Output.WriteLine(" <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Jquery_Ui_Css + "\"/> ");
                Output.WriteLine(" <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Jquery_Searchbox_Css + "\"/> ");

                //custom css
                Output.WriteLine(" <link rel=\"stylesheet\" href=\"" + Static_Resources_Gateway.Sobekcm_Mapeditor_Css + "\"/> ");

                //standard js files
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Ui_1_10_3_Custom_Js + "\"></script> ");
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Migrate_1_1_1_Js + "\"></script> ");
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Rotate_Js + "\"></script> ");
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Knob_Js + "\"></script> ");
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Jquery_Json_2_4_Js + "\"></script> ");

                Output.WriteLine(" <script src=\"https://maps.googleapis.com/maps/api/js?key=" + UI_ApplicationCache_Gateway.Settings.System.Google_Map_API_Key + "&libraries=drawing\" type=\"text/javascript\"></script>");

          //      Output.WriteLine(" <script type=\"text/javascript\" src=\"https://maps.googleapis.com/maps/api/js?v=3.exp&sensor=false&key=AIzaSyCzliz5FjUlEI9D2605b33-etBrENSSBZM&libraries=drawing\"></script> ");
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + CurrentRequest.Base_URL + "default/scripts/mapeditor/gmaps-infobox.js\"></script> ");

                //custom js
                #region

                Output.WriteLine(" ");
                Output.WriteLine(" <script type=\"text/javascript\"> ");
                Output.WriteLine(" ");

                //setup server to client vars writer
                Output.WriteLine(" // Add Server Vars ");
                Output.WriteLine(" function initServerToClientVars(){ ");
                Output.WriteLine("  try{ ");
                Output.WriteLine("   MAPEDITOR.TRACER.addTracer(\"[INFO]: initServerToClientVars started...\"); ");
                Output.WriteLine("   MAPEDITOR.GLOBAL.DEFINES.baseURL = \"" + CurrentRequest.Base_URL + "\"; //add baseURL ");

                #region Get debug time (only while debugging)

#if DEBUG
            string filePath = Assembly.GetCallingAssembly().Location;
            const int C_PE_HEADER_OFFSET = 60;
            const int C_LINKER_TIMESTAMP_OFFSET = 8;
            byte[] b = new byte[2048];
            Stream s = null;
            try
            {
                s = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            catch (Exception)
            {
                Tracer.Add_Trace("Could Not Create Build Time");
                throw;
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }
            int i2 = BitConverter.ToInt32(b, C_PE_HEADER_OFFSET);
            int secondsSince1970 = BitConverter.ToInt32(b, i2 + C_LINKER_TIMESTAMP_OFFSET);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            string debugTime_buildTimestamp = dt.ToString();
            //get current timestamp
            TimeSpan span = (dt - new DateTime(1970, 1, 1, 0, 0, 0, 0));
            double debugTime_unixTimestamp = span.TotalSeconds;

            Output.WriteLine("   MAPEDITOR.GLOBAL.DEFINES.debugUnixTimeStamp = " + debugTime_unixTimestamp + "; //add debugTime ");
            Output.WriteLine("   MAPEDITOR.GLOBAL.DEFINES.debugBuildTimeStamp = \"" + debugTime_buildTimestamp + "\"; //add debugTimestamp ");
#endif

                #endregion

                //detemrine if debugging
                if (CurrentRequest.Base_URL.Contains("localhost"))
                    Output.WriteLine("   MAPEDITOR.GLOBAL.DEFINES.debuggerOn = true; //debugger flag ");
                else
                    Output.WriteLine("   MAPEDITOR.GLOBAL.DEFINES.debuggerOn = false; //debugger flag ");

                Output.WriteLine("   MAPEDITOR.TRACER.addTracer(\"[INFO]: initServerToClientVars completed...\"); ");
                Output.WriteLine("  }catch (err){ ");
                Output.WriteLine("  MAPEDITOR.TRACER.addTracer(\"[ERROR]: initServerToClientVars failed...\"); ");
                Output.WriteLine("  } ");
                Output.WriteLine(" } ");
                Output.WriteLine(" ");

                //config settings writer section 
                Output.WriteLine(" // Add Config Settings Objects ");
                Output.WriteLine(" function initConfigSettings(){ ");
                Output.WriteLine("  try{ ");
                Output.WriteLine("    MAPEDITOR.TRACER.addTracer(\"[INFO]: initConfigSettings started...\"); ");

                //add collection configs
                #region

                //read collectionIds from config.xml file
                try
                {
                    //get collectionIdsFromPage
                    List<string> collectionIdsFromPage = new List<string>();
                    collectionIdsFromPage.Add(currentItem.Behaviors.Aggregations[0].Code);
                    collectionIdsFromPage.Add(currentItem.BibID);

                    //get settings
                    List<Simple_Setting> settings = UI_ApplicationCache_Gateway.Configuration.UI.MapEditor.GetSettings(collectionIdsFromPage);

                    //loop through settings
                    foreach (Simple_Setting thisSetting in settings)
                    {
                        //write to page
                        Output.WriteLine("   MAPEDITOR.GLOBAL.DEFINES." + thisSetting.Key + " = " + thisSetting.Value + "; //adding " + thisSetting.Key);
                    }
                }
                catch (Exception)
                {
                    Tracer.Add_Trace("Could Not Load MapEdit Configs");
                    throw;
                }

                #endregion

                Output.WriteLine("    MAPEDITOR.TRACER.addTracer(\"[INFO]: initConfigSettings completed...\"); ");
                Output.WriteLine("  }catch (err){ ");
                Output.WriteLine("  MAPEDITOR.TRACER.addTracer(\"[ERROR]: initConfigSettings failed...\"); ");
                Output.WriteLine("  } ");
                Output.WriteLine(" } ");
                Output.WriteLine(" ");

                //geo objects writer section 
                Output.WriteLine(" // Add Geo Objects ");
                Output.WriteLine(" function initGeoObjects(){ ");
                Output.WriteLine("  try{ ");
                Output.WriteLine("    MAPEDITOR.TRACER.addTracer(\"[INFO]: initGeoObjects started...\"); ");
                Output.WriteLine(" ");

                // Add the geo info
                if (currentItem != null)
                {

                    allPolygons = new List<Coordinate_Polygon>();
                    allPoints = new List<Coordinate_Point>();
                    allLines = new List<Coordinate_Line>();
                    allCircles = new List<Coordinate_Circle>();

                    // Collect all the polygons, points, and lines
                    #region
                    GeoSpatial_Information geoInfo = currentItem.Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
                    if ((geoInfo != null) && (geoInfo.hasData))
                    {
                        if (geoInfo.Polygon_Count > 0)
                        {
                            foreach (Coordinate_Polygon thisPolygon in geoInfo.Polygons)
                                allPolygons.Add(thisPolygon);
                        }
                        if (geoInfo.Line_Count > 0)
                        {
                            foreach (Coordinate_Line thisLine in geoInfo.Lines)
                                allLines.Add(thisLine);
                        }
                        if (geoInfo.Point_Count > 0)
                        {
                            foreach (Coordinate_Point thisPoint in geoInfo.Points)
                                allPoints.Add(thisPoint);
                        }
                        if (geoInfo.Circle_Count > 0)
                        {
                            foreach (Coordinate_Circle thisCircle in geoInfo.Circles)
                                allCircles.Add(thisCircle);
                        }
                    }
                    #endregion

                    // Collect all the pages and their data
                    #region
                    List<abstract_TreeNode> pages = currentItem.Divisions.Physical_Tree.Pages_PreOrder;
                    for (int i = 0; i < pages.Count; i++)
                    {
                        abstract_TreeNode pageNode = pages[i];
                        geoInfo2 = pageNode.Get_Metadata_Module(GlobalVar.GEOSPATIAL_METADATA_MODULE_KEY) as GeoSpatial_Information;
                        if ((geoInfo2 != null) && (geoInfo2.hasData))
                        {
                            if (geoInfo2.Polygon_Count > 0)
                            {
                                foreach (Coordinate_Polygon thisPolygon in geoInfo2.Polygons)
                                {
                                    thisPolygon.Page_Sequence = (ushort)(i + 1);
                                    allPolygons.Add(thisPolygon);
                                }
                            }
                            if (geoInfo2.Line_Count > 0)
                            {
                                foreach (Coordinate_Line thisLine in geoInfo2.Lines)
                                {
                                    allLines.Add(thisLine);
                                }
                            }
                            if (geoInfo2.Point_Count > 0)
                            {
                                foreach (Coordinate_Point thisPoint in geoInfo2.Points)
                                {
                                    allPoints.Add(thisPoint);
                                }
                            }
                            if (geoInfo2.Circle_Count > 0)
                            {
                                foreach (Coordinate_Circle thisCircle in geoInfo2.Circles)
                                {
                                    allCircles.Add(thisCircle);
                                }
                            }
                        }
                    }
                    #endregion

                    // Add all the points to the page
                    #region
                    if (allPoints.Count > 0)
                    {
                        //add each point
                        for (int point = 0; point < allPoints.Count; point++)
                        {
                            //add the featureType
                            Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPointFeatureType[" + point + "] = \"" + allPoints[point].FeatureType + "\";");
                            //add the label
                            if (!String.IsNullOrEmpty(allPoints[point].Label))
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPointLabel[" + point + "] = \"" + Convert_String_To_XML_Safe(allPoints[point].Label) + "\"; ");
                            else
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPointLabel[" + point + "] = \"" + Convert_String_To_XML_Safe(currentItem.Bib_Title) + "\"; ");
                            //add the center point
                            Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPointCenter[" + point + "] = new google.maps.LatLng(" + allPoints[point].Latitude + "," + allPoints[point].Longitude + "); ");
                            //add the image url (if not a poi)
                            if (allPoints[point].FeatureType != "poi")
                            {
                                try
                                {
                                    //get image url myway
                                    string current_image_file = currentItem.Web.Source_URL + "/" + currentItem.VID + ".jpg";
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPointSourceURL[" + point + "] = \"" + current_image_file + "\"; ");
                                }
                                catch (Exception)
                                {
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPointSourceURL[" + point + "] = \"\"; ");
                                }
                            }
                        }
                        Output.WriteLine("      MAPEDITOR.GLOBAL.displayIncomingPoints();");
                        Output.WriteLine(" ");
                    }
                    #endregion

                    // Add all the circles to page
                    #region
                    if (allCircles.Count > 0)
                    {
                        //add each circle
                        for (int circle = 0; circle < allCircles.Count; circle++)
                        {
                            //add the featuretype
                            Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingCircleFeatureType[" + circle + "] = \"" + allCircles[circle].FeatureType + "\";");
                            //add the label
                            if (!String.IsNullOrEmpty(allCircles[circle].Label))
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingCircleLabel[" + circle + "] = \"" + Convert_String_To_XML_Safe(allCircles[circle].Label) + "\"; ");
                            else
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingCircleLabel[" + circle + "] = \"" + Convert_String_To_XML_Safe(currentItem.Bib_Title) + "\"; ");
                            //add the center point
                            Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingCircleCenter[" + circle + "] = new google.maps.LatLng(" + allCircles[circle].Latitude + "," + allCircles[circle].Longitude + "); ");
                            //add the radius
                            Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingCircleRadius[" + circle + "] = " + allCircles[circle].Radius + "; ");
                        }
                        Output.WriteLine(" ");
                        Output.WriteLine("      MAPEDITOR.GLOBAL.displayIncomingCircles();");
                        Output.WriteLine(" ");
                    }
                    #endregion

                    //Add all the Lines to page
                    #region
                    if (allLines.Count > 0)
                    {
                        //add each line
                        for (int line = 0; line < allLines.Count; line++)
                        {
                            //add the featuretype
                            Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingLineFeatureType[" + line + "] = \"" + allLines[line].FeatureType + "\";");
                            //add the label
                            if (!String.IsNullOrEmpty(allLines[line].Label))
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingLineLabel[" + line + "] = \"" + Convert_String_To_XML_Safe(allLines[line].Label) + "\"; ");
                            else
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingLineLabel[" + line + "] = \"" + Convert_String_To_XML_Safe(currentItem.Bib_Title) + "\"; ");
                            //add the Line path
                            Output.Write("      MAPEDITOR.GLOBAL.DEFINES.incomingLinePath[" + line + "] = [ ");
                            int linePointCurrentCount = 0;
                            foreach (Coordinate_Point thisPoint in allLines[line].Points)
                            {
                                linePointCurrentCount++;
                                //determine if this is the last edge point (fixes the js issue where the trailing , could cause older browsers to crash)
                                if (linePointCurrentCount == allLines[line].Point_Count)
                                    Output.Write("new google.maps.LatLng(" + thisPoint.Latitude + "," + thisPoint.Longitude + ") ");
                                else
                                    Output.Write("new google.maps.LatLng(" + thisPoint.Latitude + "," + thisPoint.Longitude + "), ");
                            }
                            Output.WriteLine("];");

                        }
                        Output.WriteLine(" ");
                        Output.WriteLine("      MAPEDITOR.GLOBAL.displayIncomingLines();");
                        Output.WriteLine(" ");
                    }
                    #endregion

                    // Add all the polygons to page
                    #region
                    //iteraters 
                    int totalAddedPolygonIndex = 0; //this holds the index counter or code/array counter (IE starts with 0)
                    int totalAddedPolygonCount = 0; //this holds the real (human) count not the index (IE starts at 1)
                    //go through and add the existing polygons
                    if ((allPolygons.Count > 0) && (allPolygons[0].Edge_Points_Count > 1))
                    {

                        #region Add overlays first! this fixes index issues wtotalAddedPolygonIndexhin (thus index is always id minus 1)
                        foreach (Coordinate_Polygon itemPolygon in allPolygons)
                        {
                            //do this so long as it is not a poi
                            if (itemPolygon.FeatureType != "poi")
                            {
                                //add the featureType
                                if (itemPolygon.FeatureType == "")
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonFeatureType[" + totalAddedPolygonIndex + "] = \"main\";");
                                else
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonFeatureType[" + totalAddedPolygonIndex + "] = \"" + itemPolygon.FeatureType + "\";");

                                //add the polygonType
                                if (itemPolygon.PolygonType == "")
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPolygonType[" + totalAddedPolygonIndex + "] = \"rectangle\";");
                                else
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPolygonType[" + totalAddedPolygonIndex + "] = \"" + itemPolygon.PolygonType + "\";");

                                //add the label
                                if (Convert_String_To_XML_Safe(itemPolygon.Label) != "")
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"" + Convert_String_To_XML_Safe(itemPolygon.Label) + "\";");
                                else
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"Page " + (totalAddedPolygonIndex + 1) + "\";"); //2do localize this text???

                                //create the bounds string
                                string bounds = "new google.maps.LatLngBounds( ";
                                string bounds1 = "new google.maps.LatLng";
                                string bounds2 = "new google.maps.LatLng";
                                int localtotalAddedPolygonIndex = 0;
                                //determine how to handle bounds (2 edgepoints vs 4)
                                foreach (Coordinate_Point thisPoint in itemPolygon.Edge_Points)
                                {
                                    if (itemPolygon.Edge_Points_Count == 2)
                                    {
                                        if (localtotalAddedPolygonIndex == 0)
                                            bounds2 += "(" + Convert.ToString(thisPoint.Latitude) + "," + Convert.ToString(thisPoint.Longitude) + ")";
                                        if (localtotalAddedPolygonIndex == 1)
                                            bounds1 += "(" + Convert.ToString(thisPoint.Latitude) + "," + Convert.ToString(thisPoint.Longitude) + ")";
                                        localtotalAddedPolygonIndex++;
                                    }
                                    else
                                    {
                                        if (itemPolygon.Edge_Points_Count == 4)
                                        {
                                            if (localtotalAddedPolygonIndex == 0)
                                                bounds1 += "(" + Convert.ToString(thisPoint.Latitude) + "," + Convert.ToString(thisPoint.Longitude) + ")";
                                            if (localtotalAddedPolygonIndex == 2)
                                                bounds2 += "(" + Convert.ToString(thisPoint.Latitude) + "," + Convert.ToString(thisPoint.Longitude) + ")";
                                            localtotalAddedPolygonIndex++;
                                        }
                                    }
                                }
                                //finish bounds formatting
                                bounds += bounds2 + ", " + bounds1;
                                bounds += ")";
                                //add the bounds
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPath[" + totalAddedPolygonIndex + "] = " + bounds + ";"); //changed from bounds

                                //add image url
                                try
                                {
                                    //your way
                                    List<SobekCM_File_Info> first_page_files = currentItem.Web.Pages_By_Sequence[totalAddedPolygonIndex].Files;
                                    string first_page_jpeg = String.Empty;
                                    foreach (SobekCM_File_Info thisFile in first_page_files)
                                    {
                                        if ((thisFile.System_Name.ToLower().IndexOf(".jpg") > 0) && (thisFile.System_Name.ToLower().IndexOf("thm.jpg") < 0))
                                        {
                                            first_page_jpeg = thisFile.System_Name;
                                            break;
                                        }
                                    }
                                    string first_page_complete_url = "\"" + currentItem.Web.Source_URL + "/" + first_page_jpeg + "\"";
                                    ////polygonURL[totalAddedPolygonIndex] = first_page_complete_url;
                                    //polygonURL.Add(first_page_complete_url);
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonSourceURL[" + totalAddedPolygonIndex + "] = " + first_page_complete_url + ";");
                                }
                                catch (Exception)
                                {
                                    //there is no image
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonSourceURL[" + totalAddedPolygonIndex + "] = null;");
                                }

                                //add rotation
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonRotation[" + totalAddedPolygonIndex + "] = " + itemPolygon.Rotation + ";");

                                //add page sequence
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPageId[" + totalAddedPolygonIndex + "] = " + itemPolygon.Page_Sequence + ";");

                                //increment the totalAddedPolygonCount (the actual number of polys added)
                                totalAddedPolygonCount++;

                                //iterate index
                                totalAddedPolygonIndex++;
                            }
                        }
                        #endregion

                        #region Add the page info so we can convert to overlays in the app
                        foreach (var page in pages)
                        {
                            if (totalAddedPolygonIndex < pages.Count)
                            {
                                //increment the totalAddedPolygonCount (the actual number of polys added)
                                totalAddedPolygonCount++;
                                //add featuretype
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonFeatureType[" + totalAddedPolygonIndex + "] = \"hidden\";");
                                //add polygontype
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPolygonType[" + totalAddedPolygonIndex + "] = \"hidden\";");
                                //add the label
                                if (Convert_String_To_XML_Safe(pages[totalAddedPolygonIndex].Label) != "")
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"" + Convert_String_To_XML_Safe(pages[totalAddedPolygonIndex].Label) + "\";");
                                else
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"Page " + (totalAddedPolygonIndex + 1) + "\";"); //2do localize this text???
                                //add page sequence
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPageId[" + totalAddedPolygonIndex + "] = " + totalAddedPolygonCount + ";");
                                //add image url
                                try
                                {
                                    //your way
                                    List<SobekCM_File_Info> first_page_files = currentItem.Web.Pages_By_Sequence[totalAddedPolygonIndex].Files;

                                    string first_page_jpeg = String.Empty;
                                    foreach (SobekCM_File_Info thisFile in first_page_files)
                                    {
                                        if ((thisFile.System_Name.ToLower().IndexOf(".jpg") > 0) &&
                                            (thisFile.System_Name.ToLower().IndexOf("thm.jpg") < 0))
                                        {
                                            first_page_jpeg = thisFile.System_Name;
                                            break;
                                        }
                                    }
                                    string first_page_complete_url = "\"" + currentItem.Web.Source_URL + "/" + first_page_jpeg + "\"";
                                    ////polygonURL[totalAddedPolygonIndex] = first_page_complete_url;
                                    //polygonURL.Add(first_page_complete_url);
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonSourceURL[" + totalAddedPolygonIndex + "] = " + first_page_complete_url + ";");
                                }
                                catch (Exception)
                                {
                                    //my way
                                    string current_image_file = currentItem.Web.Source_URL + "/" + currentItem.VID + ".jpg";
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonSourceURL[" + totalAddedPolygonIndex + "] = \"" + current_image_file + "\"; ");
                                    //throw;
                                }
                                //increment index
                                totalAddedPolygonIndex++;
                            }
                        }
                        #endregion

                        #region Add pois (order of adding is important)
                        foreach (Coordinate_Polygon itemPolygon in allPolygons)
                        {
                            if (itemPolygon.FeatureType == "poi")
                            {
                                //add the featureType
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonFeatureType[" + totalAddedPolygonIndex + "] = \"" + itemPolygon.FeatureType + "\";");
                                //add the polygonType
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPolygonType[" + totalAddedPolygonIndex + "] = \"" + itemPolygon.PolygonType + "\";");
                                //add the label
                                if (Convert_String_To_XML_Safe(itemPolygon.Label) != "")
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"" + Convert_String_To_XML_Safe(itemPolygon.Label) + "\";");
                                else
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"Page " + (totalAddedPolygonIndex + 1) + "\";"); //2do localize this text???
                                //add the polygon path
                                Output.Write("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPath[" + totalAddedPolygonIndex + "] = [ ");
                                int edgePointCurrentCount = 0;
                                foreach (Coordinate_Point thisPoint in itemPolygon.Edge_Points)
                                {
                                    edgePointCurrentCount++;
                                    //determine if this is the last edge point (fixes the js issue where the trailing , could cause older browsers to crash)
                                    if (edgePointCurrentCount == itemPolygon.Edge_Points_Count)
                                        Output.Write("new google.maps.LatLng(" + thisPoint.Latitude + "," + thisPoint.Longitude + ") ");
                                    else
                                        Output.Write("new google.maps.LatLng(" + thisPoint.Latitude + "," + thisPoint.Longitude + "), ");
                                }
                                Output.WriteLine("];");
                                //iterate
                                totalAddedPolygonIndex++;
                            }
                        }
                        #endregion

                        Output.WriteLine(" ");
                        Output.WriteLine("      MAPEDITOR.GLOBAL.displayIncomingPolygons(); ");
                        Output.WriteLine(" ");
                    }
                    else
                    {
                        #region Add the page info so we can convert to overlays in the app
                        foreach (var page in pages)
                        {
                            if (totalAddedPolygonIndex < pages.Count)
                            {
                                //add featuretype
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonFeatureType[" + totalAddedPolygonIndex + "] = \"hidden\";");
                                //add polygontype
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPolygonType[" + totalAddedPolygonIndex + "] = \"hidden\";");
                                //add the label
                                if (Convert_String_To_XML_Safe(page.Label) != "")
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"" + Convert_String_To_XML_Safe(page.Label) + "\";");
                                else
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonLabel[" + totalAddedPolygonIndex + "] = \"Page " + (totalAddedPolygonIndex + 1) + "\";"); //2do localize this text???
                                //add page sequence
                                Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonPageId[" + totalAddedPolygonIndex + "] = " + (totalAddedPolygonIndex + 1) + ";");
                                //add image url
                                try
                                {
                                    //your way
                                    List<SobekCM_File_Info> first_page_files = currentItem.Web.Pages_By_Sequence[totalAddedPolygonIndex].Files;

                                    string first_page_jpeg = String.Empty;
                                    foreach (SobekCM_File_Info thisFile in first_page_files)
                                    {
                                        if ((thisFile.System_Name.ToLower().IndexOf(".jpg") > 0) &&
                                            (thisFile.System_Name.ToLower().IndexOf("thm.jpg") < 0))
                                        {
                                            first_page_jpeg = thisFile.System_Name;
                                            break;
                                        }
                                    }
                                    string first_page_complete_url = "\"" + currentItem.Web.Source_URL + "/" + first_page_jpeg + "\"";
                                    ////polygonURL[totalAddedPolygonIndex] = first_page_complete_url;
                                    //polygonURL.Add(first_page_complete_url);
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonSourceURL[" + totalAddedPolygonIndex + "] = " + first_page_complete_url + ";");
                                }
                                catch (Exception)
                                {
                                    //my way
                                    string current_image_file = currentItem.Web.Source_URL + "/" + currentItem.VID + ".jpg";
                                    Output.WriteLine("      MAPEDITOR.GLOBAL.DEFINES.incomingPolygonSourceURL[" + totalAddedPolygonIndex + "] = \"" + current_image_file + "\"; ");
                                    //throw;
                                }
                                totalAddedPolygonIndex++;
                            }
                        }
                        #endregion
                        Output.WriteLine(" ");
                        Output.WriteLine("      MAPEDITOR.GLOBAL.displayIncomingPolygons(); ");
                        Output.WriteLine(" ");
                    }
                    #endregion
                }
                Output.WriteLine("    MAPEDITOR.TRACER.addTracer(\"[INFO]: initGeoObjects completed...\"); ");
                Output.WriteLine("  }catch (err){ ");
                Output.WriteLine("    MAPEDITOR.TRACER.addTracer(\"[ERROR]: initGeoObjects failed...\"); ");
                Output.WriteLine("  } ");
                Output.WriteLine(" }");
                Output.WriteLine(" ");
                Output.WriteLine(" </script> ");
                Output.WriteLine(" ");

                #endregion

                //html page literal
                #region html page literat

                Output.WriteLine(" <div id=\"mapedit_container_message\"> ");
                Output.WriteLine("     <div id=\"content_message\"></div> ");
                Output.WriteLine(" </div> ");
                Output.WriteLine("  ");
                Output.WriteLine(" <div id=\"mapedit_container_pane_0\"> ");
                Output.WriteLine("     <ul class=\"sf-menu\"> ");
                Output.WriteLine("         <li> ");
                Output.WriteLine("             <a id=\"content_menubar_header1\"></a> ");
                Output.WriteLine("             <ul> ");
                Output.WriteLine("                 <li><a id=\"content_menubar_save\"></a></li> ");
                Output.WriteLine("                 <li><a id=\"content_menubar_cancel\"></a></li> ");
                Output.WriteLine("                 <li><a id=\"content_menubar_reset\"></a></li> ");
                Output.WriteLine("             </ul> ");
                Output.WriteLine("         </li> ");
                Output.WriteLine("         <li> ");
                Output.WriteLine("             <a id=\"content_menubar_header2\"></a> ");
                Output.WriteLine("             <ul> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_header2Sub1\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_toggleMapControls\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_toggleToolbox\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_toggleToolbar\"></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_header2Sub2\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_layerRoadmap\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_layerSatellite\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_layerHybrid\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_layerTerrain\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_layerCustom\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_layerReset\"></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_header2Sub3\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_zoomIn\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_zoomOut\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_zoomReset\"></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_header2Sub4\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_panUp\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_panRight\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_panDown\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_panLeft\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_panReset\"></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("             </ul> ");
                Output.WriteLine("         </li> ");
                Output.WriteLine("         <li> ");
                Output.WriteLine("             <a id=\"content_menubar_header3\"></a> ");
                Output.WriteLine("             <ul>                ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_manageSearch\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li> ");
                Output.WriteLine("                             <div class=\"mapedit_container_search\"> ");
                Output.WriteLine("                                 <input id=\"content_menubar_searchField\" class=\"search\" type=\"text\" placeholder=\"\" onClick=\"this.select();\" /> ");
                Output.WriteLine("                                 <div id=\"content_menubar_searchButton\" class=\"searchActionHandle\"></div> ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                         </li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_manageItem\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_itemGetUserLocation\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_itemPlace\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_useSearchAsLocation\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_convertToOverlay\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_itemReset\"></a></li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_itemDelete\"></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_manageOverlay\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_overlayGetUserLocation\"></a></li> ");
                Output.WriteLine("                         <!--<li><a id=\"content_menubar_overlayEdit\"></a></li>--> ");
                Output.WriteLine("                         <!--<li><a id=\"content_menubar_overlayPlace\"></a></li>--> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_overlayToggle\"></a></li> ");
                Output.WriteLine("                         <li> ");
                Output.WriteLine("                             <a id=\"content_menubar_header3Sub3Sub1\"></a> ");
                Output.WriteLine("                             <ul> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_rotationClockwise\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_rotationCounterClockwise\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_rotationReset\"></a></li> ");
                Output.WriteLine("                             </ul> ");
                Output.WriteLine("                         </li> ");
                Output.WriteLine("                         <li> ");
                Output.WriteLine("                             <a id=\"content_menubar_header3Sub3Sub2\"></a> ");
                Output.WriteLine("                             <ul> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_transparencyDarker\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_transparencyLighter\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_transparencyReset\"></a></li> ");
                Output.WriteLine("                             </ul> ");
                Output.WriteLine("                         </li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_overlayReset\" ></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("                 <li> ");
                Output.WriteLine("                     <a id=\"content_menubar_managePOI\"></a> ");
                Output.WriteLine("                     <ul> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_poiGetUserLocation\"></a></li> ");
                Output.WriteLine("                         <!--<li><a id=\"content_menubar_poiPlace\"></a></li>--> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_poiToggle\"></a></li> ");
                Output.WriteLine("                         <li> ");
                Output.WriteLine("                             <a id=\"content_menubar_header3Sub4Sub1\"></a> ");
                Output.WriteLine("                             <ul> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_poiMarker\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_poiCircle\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_poiRectangle\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_poiPolygon\"></a></li> ");
                Output.WriteLine("                                 <li><a id=\"content_menubar_poiLine\"></a></li> ");
                Output.WriteLine("                             </ul> ");
                Output.WriteLine("                         </li> ");
                Output.WriteLine("                         <li><a id=\"content_menubar_poiReset\"></a></li> ");
                Output.WriteLine("                     </ul> ");
                Output.WriteLine("                 </li> ");
                Output.WriteLine("             </ul> ");
                Output.WriteLine("         </li> ");
                Output.WriteLine("         <li> ");
                Output.WriteLine("             <a id=\"content_menubar_header4\"></a> ");
                Output.WriteLine("             <ul> ");
                Output.WriteLine("                 <li><a id=\"content_menubar_documentation\"></a></li> ");
                Output.WriteLine("                 <li><a id=\"content_menubar_reportAProblem\"></a></li> ");
                Output.WriteLine("             </ul> ");
                Output.WriteLine("         </li> ");
                Output.WriteLine("     </ul>     ");
                Output.WriteLine(" </div> ");
                Output.WriteLine("  ");
                Output.WriteLine(" <div id=\"mapedit_container_pane_1\"> ");
                Output.WriteLine("     <div id=\"mapedit_container_toolbar\">        ");
                Output.WriteLine("         <div class=\"toolbar_grouping\"> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_reset\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_toggleMapControls\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_toggleToolbox\" class=\"button\"></div> ");
                Output.WriteLine("         </div> ");
                Output.WriteLine("         <div class=\"toolbar_grouping\"> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_layerRoadmap\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_layerTerrain\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_layerSatellite\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_layerHybrid\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_layerCustom\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_layerReset\" class=\"button\"></div> ");
                Output.WriteLine("         </div> ");
                Output.WriteLine("         <div class=\"toolbar_grouping\"> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_panUp\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_panLeft\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_panReset\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_panRight\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_panDown\" class=\"button\"></div> ");
                Output.WriteLine("         </div> ");
                Output.WriteLine("         <div class=\"toolbar_grouping\"> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_zoomIn\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_zoomReset\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_zoomOut\" class=\"button\"></div> ");
                Output.WriteLine("         </div> ");
                Output.WriteLine("         <div class=\"toolbar_grouping\"> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_manageItem\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_manageOverlay\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_managePOI\" class=\"button\"></div> ");
                Output.WriteLine("             <div id=\"content_toolbar_button_manageSearch\" class=\"button\"></div> ");
                Output.WriteLine("         </div> ");
                Output.WriteLine("         <div class=\"toolbar_grouping\"> ");
                Output.WriteLine("             <div class=\"mapedit_container_search\"> ");
                Output.WriteLine("                 <input id=\"content_toolbar_searchField\" class=\"search\" type=\"text\" placeholder=\"\" onClick=\"this.select();\" /> ");
                Output.WriteLine("                 <div id=\"content_toolbar_searchButton\" class=\"searchActionHandle\"></div> ");
                Output.WriteLine("             </div> ");
                Output.WriteLine("         </div> ");
                Output.WriteLine("     </div> ");
                Output.WriteLine(" </div> ");
                Output.WriteLine("  ");
                Output.WriteLine(" <div id=\"mapedit_container\"> ");
                Output.WriteLine("      ");
                Output.WriteLine("     <div id=\"mapedit_container_toolbarGrabber\"> ");
                Output.WriteLine("         <div id=\"content_toolbarGrabber\"></div> ");
                Output.WriteLine("     </div>     ");
                Output.WriteLine("  ");
                Output.WriteLine("     <div id=\"mapedit_container_pane_2\"> ");
                Output.WriteLine("          ");
                Output.WriteLine("         <!--<div id=\"mapedit_container_message\"> ");
                Output.WriteLine("                 <div id=\"content_message\"></div> ");
                Output.WriteLine("             </div>--> ");
                Output.WriteLine("          ");
                Output.WriteLine("         <div id=\"mapedit_container_toolbox\" class=\"ui-widget-content\"> ");
                Output.WriteLine("             <div id=\"mapedit_container_toolboxMinibar\"> ");
                Output.WriteLine("                 <div id=\"content_minibar_icon\"></div>  ");
                Output.WriteLine("                 <div id=\"content_minibar_header\"></div>  ");
                Output.WriteLine("                 <div id=\"content_minibar_button_close\"></div>  ");
                Output.WriteLine("                 <div id=\"content_minibar_button_maximize\"></div>  ");
                Output.WriteLine("                 <div id=\"content_minibar_button_minimize\"></div>  ");
                Output.WriteLine("             </div> ");
                Output.WriteLine("             <div id=\"mapedit_container_toolboxTabs\"> ");
                Output.WriteLine("                 <div id=\"content_toolbox_tab1_header\" class=\"tab-title\"></div> ");
                Output.WriteLine("                 <div class=\"tab\"> ");
                Output.WriteLine("                     <div class=\"toolbox_tab-content\"> ");
                Output.WriteLine("                         <div id=\"mapedit_container_toolbox_tab1\"> ");
                Output.WriteLine("                             <div id=\"mapedit_container_grid\"> ");
                Output.WriteLine("                              ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_layerRoadmap\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_layerTerrain\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div class=\"x half\"></div> ");
                Output.WriteLine("                                 <div class=\"x\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_panUp\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div class=\"x\"></div> ");
                Output.WriteLine("  ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_layerSatellite\" class=\"x y button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_layerHybrid\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div class=\"x half\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_panLeft\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_panReset\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_panRight\" class=\"x button\"></div> ");
                Output.WriteLine("                              ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_layerCustom\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_layerReset\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div class=\"x half\"></div> ");
                Output.WriteLine("                                 <div class=\"x\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_panDown\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div class=\"x\"></div> ");
                Output.WriteLine("  ");
                Output.WriteLine("                                 <div class=\"x y half\"></div> ");
                Output.WriteLine("  ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_reset\" class=\"x y button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_toggleMapControls\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div class=\"x half\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_zoomIn\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_zoomReset\" class=\"x button\"></div> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_button_zoomOut\" class=\"x button\"></div> ");
                Output.WriteLine("                              ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                             <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         </div> ");
                Output.WriteLine("                     </div> ");
                Output.WriteLine("                 </div> ");
                Output.WriteLine("                 <div id=\"content_toolbox_tab2_header\" class=\"tab-title\"></div> ");
                Output.WriteLine("                 <div class=\"tab\"> ");
                Output.WriteLine("                     <div class=\"toolbox_tab-content\"> ");
                Output.WriteLine("                         <div id=\"mapedit_container_toolbox_tab2\"> ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_manageItem\" class=\"button\"></div> ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_manageOverlay\" class=\"button\"></div> ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_managePOI\" class=\"button\"></div> ");
                Output.WriteLine("                             <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                             <div class=\"mapedit_container_search\"> ");
                Output.WriteLine("                                 <input id=\"content_toolbox_searchField\" class=\"search\" type=\"text\" placeholder=\"\" onClick=\"this.select();\" /> ");
                Output.WriteLine("                                 <div id=\"content_toolbox_searchButton\" class=\"searchActionHandle\"></div> ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                             <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                             <div id=\"searchResults_container\"> ");
                Output.WriteLine("                                 <div id=\"searchResults_scoll_container\"> ");
                Output.WriteLine("                                     <div id=\"searchResults_list\"></div> ");
                Output.WriteLine("                                 </div> ");
                Output.WriteLine("                             </div>  ");
                Output.WriteLine("                         </div> ");
                Output.WriteLine("                     </div> ");
                Output.WriteLine("                 </div> ");
                Output.WriteLine("                 <div id=\"content_toolbox_tab3_header\" class=\"tab-title\"></div> ");
                Output.WriteLine("                 <div id=\"itemACL\" class=\"tab\"> ");
                Output.WriteLine("                     <div class=\"toolbox_tab-content\"> ");
                Output.WriteLine("                         <div id=\"mapedit_container_toolbox_tab3\"> ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_itemPlace\" class=\"button\"></div> ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_itemGetUserLocation\" class=\"button\"></div>   ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_useSearchAsLocation\" class=\"button\"></div> ");
                Output.WriteLine("                             <div id=\"content_toolbox_button_convertToOverlay\" class=\"button\"></div> ");
                Output.WriteLine("                             <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                             <textarea id=\"content_toolbox_posItem\" class=\"tab-field\" rows=\"2\" cols=\"24\" placeholder=\"Selected Lat/Long\"></textarea> ");
                Output.WriteLine("                             <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                             <textarea id=\"content_toolbox_rgItem\" class=\"tab-field\" rows=\"3\" cols=\"24\" placeholder=\"Nearest Address\"></textarea> ");
                Output.WriteLine("                             <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                             <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_saveItem\" > </div> ");
                Output.WriteLine("                             <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_clearItem\" > </div> ");
                Output.WriteLine("                             <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_deleteItem\" > </div> ");
                Output.WriteLine("                         </div> ");
                Output.WriteLine("                     </div> ");
                Output.WriteLine("                 </div> ");
                Output.WriteLine("                 <div id=\"content_toolbox_tab4_header\" class=\"tab-title\"></div> ");
                Output.WriteLine("                 <div id=\"overlayACL\" class=\"tab\"> ");
                Output.WriteLine("                     <div class=\"toolbox_tab-content\"> ");
                Output.WriteLine("                         <!--<div id=\"content_toolbox_button_overlayEdit\" class=\"button\"></div>--> ");
                Output.WriteLine("                         <!--<div id=\"content_toolbox_button_overlayPlace\" class=\"button\"></div>--> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_overlayGetUserLocation\" class=\"button\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_overlayToggle\" class=\"button\"></div> ");
                Output.WriteLine("                         <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         <div id=\"mapedit_container_toolbox_overlayTools\"> ");
                Output.WriteLine("                             <div id=\"rotation\"> ");
                Output.WriteLine("                                 <div id=\"rotationKnob\"> ");
                Output.WriteLine("                                     <input class=\"knob\" data-displayInput=\"false\" data-width=\"68\" data-step=\"1\" data-min=\"0\" data-max=\"360\" data-cursor=true data-bgColor=\"#B2B2B2\" data-fgColor=\"#111111\" data-thickness=\"0.3\" value=\"0\"> ");
                Output.WriteLine("                                 </div> ");
                Output.WriteLine("                                 <div id=\"mapedit_container_toolbox_rotationButtons\"> ");
                Output.WriteLine("                                     <div id=\"content_toolbox_rotationCounterClockwise\" class=\"button3\"></div> ");
                Output.WriteLine("                                     <div id=\"content_toolbox_rotationReset\" class=\"button3\"></div> ");
                Output.WriteLine("                                     <div id=\"content_toolbox_rotationClockwise\" class=\"button3\"></div> ");
                Output.WriteLine("                                 </div> ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                             <div id=\"transparency\"> ");
                Output.WriteLine("                                 <div id=\"overlayTransparencySlider\"></div> ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                         </div> ");
                Output.WriteLine("                         <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         <div id=\"overlayList_container\"> ");
                Output.WriteLine("                             <div id=\"overlayList_scoll_container\"> ");
                Output.WriteLine("                                 <div id=\"overlayList\"></div> ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                         </div>   ");
                Output.WriteLine("                         <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_saveOverlay\" > </div> ");
                Output.WriteLine("                         <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_clearOverlay\" > </div> ");
                Output.WriteLine("                     </div> ");
                Output.WriteLine("                 </div> ");
                Output.WriteLine("                 <div id=\"content_toolbox_tab5_header\" class=\"tab-title\"></div> ");
                Output.WriteLine("                 <div class=\"tab\"> ");
                Output.WriteLine("                     <div class=\"toolbox_tab-content\"> ");
                Output.WriteLine("                         <!--<div id=\"content_toolbox_button_placePOI\" class=\"button\"></div>--> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiGetUserLocation\" class=\"button\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiToggle\" class=\"button\"></div> ");
                Output.WriteLine("                         <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiMarker\" class=\"button\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiCircle\" class=\"button\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiRectangle\" class=\"button\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiPolygon\" class=\"button\"></div> ");
                Output.WriteLine("                         <div id=\"content_toolbox_button_poiLine\" class=\"button\"></div> ");
                Output.WriteLine("                         <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         <div id=\"poiList_container\"> ");
                Output.WriteLine("                             <div id=\"poiList_scoll_container\"> ");
                Output.WriteLine("                                 <div id=\"poiList\"></div> ");
                Output.WriteLine("                             </div> ");
                Output.WriteLine("                         </div>   ");
                Output.WriteLine("                         <div class=\"lineBreak\"></div> ");
                Output.WriteLine("                         <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_savePOI\" > </div> ");
                Output.WriteLine("                         <div class=\"button2\"> <input type=\"button\" id=\"content_toolbox_button_clearPOI\" > </div> ");
                Output.WriteLine("                     </div> ");
                Output.WriteLine("                 </div> ");
                Output.WriteLine("             </div> ");
                Output.WriteLine("         </div>     ");
                Output.WriteLine("         <div id=\"googleMap\"></div> ");
                Output.WriteLine("     </div> ");
                Output.WriteLine(" </div> ");
                Output.WriteLine(" <div id=\"debugs\"></div> ");


                #endregion

                //custom js files (load order does matter)
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Sobekcm_Map_Editor_Js + "\"></script> ");
                Output.WriteLine(" <script type=\"text/javascript\" src=\"" + Static_Resources_Gateway.Gmaps_MarkerwithLabel_Js + "\"></script> "); //must load after custom

                //end of custom content
                Output.WriteLine("</td>");

            }
            catch (Exception ee)
            {
                throw new SobekCM_Traced_Exception("Could Not Create MapEdit Page", ee, Tracer);
            }
        }

        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        /// <param name="Body_Attributes"> List of body attributes to be included </param>
        public override void Add_ViewerSpecific_Body_Attributes(List<Tuple<string, string>> Body_Attributes)
        {
            Body_Attributes.Clear();
            Body_Attributes.Add(new Tuple<string, string>("onload", "initMapEditor();"));
            Body_Attributes.Add(new Tuple<string, string>("onresize", "MAPEDITOR.UTILITIES.resizeView();"));
        }

        /// <summary> Gets the collection of special behaviors which this item viewer
        /// requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> ItemViewer_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum> 
					{
						HtmlSubwriter_Behaviors_Enum.Item_Subwriter_NonWindowed_Mode,
						HtmlSubwriter_Behaviors_Enum.Suppress_Footer,
						HtmlSubwriter_Behaviors_Enum.Suppress_Internal_Header,
						HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Item_Menu,
						HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Left_Navigation_Bar,
						HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Full_JQuery_UI
					};
            }
        }

        /// <summary> Allows controls to be added directory to a place holder, rather than just writing to the output HTML stream </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the bulk of the item viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method does nothing, since nothing is added to the place holder as a control for this item viewer </remarks>
        public override void Add_Main_Viewer_Section(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            // Do nothing
        }

        /// <summary> Converts a basic string into an XML-safe string </summary>
        /// <param name="Element"> Element data to convert </param>
        /// <returns> Data converted into an XML-safe string</returns>
        private static string Convert_String_To_XML_Safe(string Element)
        {
            if (Element == null)
                return string.Empty;

            string xml_safe = Element;
            int i = xml_safe.IndexOf("&");
            while (i >= 0)
            {
                if ((i != xml_safe.IndexOf("&amp;", i)) && (i != xml_safe.IndexOf("&quot;", i)) &&
                    (i != xml_safe.IndexOf("&gt;", i)) && (i != xml_safe.IndexOf("&lt;", i)))
                {
                    xml_safe = xml_safe.Substring(0, i + 1) + "amp;" + xml_safe.Substring(i + 1);
                }

                i = xml_safe.IndexOf("&", i + 1);
            }
            return xml_safe.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }
}
