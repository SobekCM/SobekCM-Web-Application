﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Downloads viewer prototyper, which is used to check to see if there are downloads to display, 
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class Downloads_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the Downloads_ItemViewer_Prototyper class </summary>
        public Downloads_ItemViewer_Prototyper()
        {
            ViewerType = "DOWNLOADS";
            ViewerCode = "downloads";
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
            // If the downloads is null, then return FALSE
            if (CurrentItem.Downloads == null)
                return false;

            // Any download files are really good enoughh
            if (CurrentItem.Downloads.Any(DownloadGroup => DownloadGroup.Files.Count > 0))
                return true;

            // Otherwise, if this is an aerial, and there are JPEG2000 files, add it (UF legacy code)
            if ((CurrentItem.Behaviors.Page_File_Extensions_For_Download != null) && (CurrentItem.Behaviors.Page_File_Extensions_For_Download.Length > 0) && (CurrentItem.Images != null))
            {
                // Just double check that some of those files exist though!
                foreach (string extensionToAdd in CurrentItem.Behaviors.Page_File_Extensions_For_Download)
                {
                    if (CurrentItem.Web.Contains_File_Extension(extensionToAdd))
                        return true;
                }
            }

            // Finally, return false
            return false;
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since PDFs should never be shown if an item is checked out </returns>
        public virtual bool Override_On_Checkout(BriefItemInfo CurrentItem)
        {
            return true;
        }

        /// <summary> Flag indicates if the current user has access to this viewer for the item </summary>
        /// <param name="CurrentItem"> Digital resource to see if the current user has correct permissions to use this viewer </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public virtual bool Has_Access(BriefItemInfo CurrentItem, User_Object CurrentUser, bool IsRestricted)
        {
            return !IsRestricted;
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
            // Get the URL for this
            string previous_code = CurrentRequest.ViewerCode;
            CurrentRequest.ViewerCode = ViewerCode;
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Start with the default label on the menu
            string label = "Downloads";

            // Allow the label to be implemented for this viewer
            BriefItem_BehaviorViewer thisViewerInfo = CurrentItem.Behaviors.Get_Viewer(ViewerCode);

            // If this is found, and has a custom label, use that 
            if ((thisViewerInfo != null) && (!String.IsNullOrWhiteSpace(thisViewerInfo.Label)))
                label = thisViewerInfo.Label;

            // Add the item menu information
            Item_MenuItem menuItem = new Item_MenuItem(label, null, null, url, ViewerCode);
            MenuItems.Add(menuItem);
        }

        /// <summary> Creates and returns the an instance of the <see cref="Downloads_ItemViewer"/> class for showing the list of
        /// downloads linked to a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="CurrentFlags"> Calculated flags for this particular requests, to avoid recalculation in different viewers </param>
        /// <returns> Fully built and initialized <see cref="Downloads_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, RequestCache_RequestFlags CurrentFlags)
        {
            return new Downloads_ItemViewer(CurrentItem, CurrentUser, CurrentRequest);
        }
    }


    /// <summary> Item viewer displays all downloads ( i.e., not the page images ) for download </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Downloads_ItemViewer : abstractNoPaginationItemViewer
    {
        /// <summary> Constructor for a new instance of the Downloads_ItemViewer class, used to display a list of files to download from a digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        public Downloads_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;
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
                Tracer.Add_Trace("Download_ItemViewer.Write_Main_Viewer_Section", "");
            }

            Output.WriteLine("          <td>");
            Output.WriteLine("            <div id=\"sbkDiv_MainArea\">");
            Add_Download_Links(Output, BriefItem, CurrentRequest, CurrentUser, Tracer );
            Output.WriteLine("            </div>");
            Output.WriteLine("          </td>");
        }

        /// <summary> Adds the download links directly to the output stream </summary>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="Output"> Response stream for the item viewer to write directly to  </param>
        /// <param name="BriefItem"> Current item to display </param>
        /// <param name="CurrentRequest"> Current request object, which has all the basic navigation, mode information for this request </param>
        /// <param name="CurrentUser"> Current user, if one is logged in, or NULL</param>
        public static void Add_Download_Links(TextWriter Output, BriefItemInfo BriefItem, Navigation_Object CurrentRequest, User_Object CurrentUser, Custom_Tracer Tracer)
        {
            // Start the citation table
            string explanation_text = "This item has the following downloads:";
            switch (CurrentRequest.Language)
            {
                case Web_Language_Enum.French:
                    explanation_text = "Ce document est la suivante téléchargements:";
                    break;

                case Web_Language_Enum.Spanish:
                    explanation_text = "Este objeto tiene las siguientes descargas:";
                    break;

                default:
                    if ((BriefItem.Images == null) || (BriefItem.Images.Count == 0))
                        explanation_text = "This item is only available as the following downloads:";
                    break;
            }


            Output.WriteLine("              <br />");

            if ((BriefItem.Downloads != null) && (BriefItem.Downloads.Count > 0))
            {
                Output.WriteLine("                <h2>" + explanation_text + "</h2>");
                Output.WriteLine("                  <div id=\"sbkDiv_Downloads\">");

                // Step through each download in this item
                string greenstoneLocation = BriefItem.Web.Source_URL + "/";
                foreach (BriefItem_FileGrouping downloadGroup in BriefItem.Downloads)
                {
                    // Step through each download in this download group/page
                    foreach (BriefItem_File download in downloadGroup.Files)
                    {
                        // Get the file extension
                        string file_extension = Path.GetExtension(download.Name);

                        // If the file extension is null, skip this
                        if (String.IsNullOrEmpty(file_extension))
                            continue;

                        // Is this an external link?
                        if (download.Name.IndexOf("http") >= 0)
                        {
                            // Is the extension already a part of the label?
                            string label_upper = downloadGroup.Label.ToUpper();
                            if (label_upper.IndexOf(file_extension.ToUpper()) > 0)
                            {
                                Output.WriteLine("                  <a href=\"" + download.Name + "\" target=\"_blank\">" + downloadGroup.Label + "</a><br /><br />");
                            }
                            else
                            {
                                Output.WriteLine("                  <a href=\"" + download.Name + "\" target=\"_blank\">" + downloadGroup.Label + " ( " + file_extension + " )</a><br /><br />");
                            }
                        }
                        else
                        {
                            string file_link = greenstoneLocation + download.Name;

                            // MAKE THIS USE THE FILES.ASPX WEB PAGE if this is restricted (or dark)
                            if ((BriefItem.Behaviors.Dark_Flag) || (BriefItem.Behaviors.IP_Restriction_Membership > 0))
                                file_link = CurrentRequest.Base_URL + "files/" + BriefItem.BibID + "/" + BriefItem.VID + "/" + download.Name;


                            // Is the extension already a part of the label?
                            string label = downloadGroup.Label;
                            if (label.Length == 0)
                            {
                                label = download.Name;
                            }
                            if (label.IndexOf(file_extension, StringComparison.OrdinalIgnoreCase) > 0)
                            {
                                Output.WriteLine("                  <a href=\"" + file_link + "\" target=\"_blank\">" + label + "</a><br /><br />");
                            }
                            else
                            {
                                Output.WriteLine("                  <a href=\"" + file_link + "\" target=\"_blank\">" + label + " ( " + file_extension + " )</a><br /><br />");
                            }
                        }
                    }
                }

                Output.WriteLine("                  </div>");
            }

            // If this was an aerial, allow each jpeg2000 page to be downloaded
            if ((BriefItem.Behaviors.Page_File_Extensions_For_Download != null) && (BriefItem.Behaviors.Page_File_Extensions_For_Download.Length > 0) && (BriefItem.Images != null))
            {
                List<string> pageDownloads = new List<string>();

                foreach (BriefItem_FileGrouping pageNode in BriefItem.Images)
                {
                    // If no file, continue
                    if ((pageNode.Files == null) || (pageNode.Files.Count == 0))
                        continue;

                    // Stp through each file
                    foreach (BriefItem_File thisFile in pageNode.Files)
                    {
                        string file_extension = Path.GetExtension(thisFile.Name);
                        if (!String.IsNullOrEmpty(file_extension))
                        {
                            bool forDownload = BriefItem.Behaviors.Page_File_Extensions_For_Download.Any(ThisDownload => String.Compare(ThisDownload, file_extension, StringComparison.OrdinalIgnoreCase) == 0);
                            if (forDownload)
                            {
                                pageDownloads.Add("<a href=\"" + (BriefItem.Web.Source_URL + "/" + thisFile.Name).Replace("\\", "/").Replace("//", "/").Replace("http:/", "http://") + "\">" + pageNode.Label + "</a>");
                            }
                        }
                        break;
                    }
                }

                if (pageDownloads.Count > 0)
                {

                    Output.WriteLine("                <h2>The following tiles are available for download:</h2>");
                    Output.WriteLine((!String.IsNullOrEmpty(CurrentRequest.Browser_Type)) && (CurrentRequest.Browser_Type.IndexOf("FIREFOX") >= 0)
                                           ? "                <p>To download, right click on the tile name below, select 'Save Link As...' and save the JPEG2000 to your local computer.</p>"
                                           : "                <p>To download, right click on the tile name below, select 'Save Target As...' and save the JPEG2000 to your local computer. </p>");
                    Output.WriteLine("                  <table id=\"sbkDiv_Aerials\">");

                    int rows = pageDownloads.Count / 3;
                    if ((pageDownloads.Count % 3) != 0)
                        rows++;

                    for (int i = 0; i < rows; i++)
                    {
                        Output.Write("                    <tr>");
                        if (pageDownloads.Count > i)
                        {
                            Output.Write("<td>" + pageDownloads[i] + "</td>");
                        }
                        if (pageDownloads.Count > (i + rows))
                        {
                            Output.Write("<td>" + pageDownloads[i + rows] + "</td>");
                        }
                        else
                        {
                            Output.Write("<td>&nbsp;</td>");
                        }
                        if (pageDownloads.Count > (i + rows + rows))
                        {
                            Output.Write("<td>" + pageDownloads[i + rows + rows] + "</td>");
                        }
                        else
                        {
                            Output.Write("<td>&nbsp;</td>");
                        }

                        Output.WriteLine("</tr>");
                    }

                    Output.WriteLine("                  </table>");
                }
            }

            Output.WriteLine("              <br />");
        }

        /// <summary> Allows controls to be added directory to a place holder, rather than just writing to the output HTML stream </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the bulk of the item viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method does nothing, since nothing is added to the place holder as a control for this item viewer </remarks>
        public override void Add_Main_Viewer_Section(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            // Do nothing
        }
    }
}
