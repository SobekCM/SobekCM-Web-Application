using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Caching;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.HTML;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Library.UI;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Manage menu item viewer prototyper, which is used to check to see if a user has access to 
    /// this viewer and create the viewer itself if requested </summary>
    public class SearchEngineIndexing_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the SearchEngineIndexing_ItemViewer_Prototyper class </summary>
        public SearchEngineIndexing_ItemViewer_Prototyper()
        {
            ViewerType = "SEO";
            ViewerCode = "robot";
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
        public bool Include_Viewer(BriefItemInfo CurrentItem)
        {
            // This should always be included
            return true;
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since PDFs should never be shown if an item is checked out </returns>
        public bool Override_On_Checkout(BriefItemInfo CurrentItem)
        {
            return false;
        }

        /// <summary> Flag indicates if the current user has access to this viewer for the item </summary>
        /// <param name="CurrentItem"> Digital resource to see if the current user has correct permissions to use this viewer </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="IpRestricted"> Flag indicates if this item is IP restricted AND if the current user is outside the ranges </param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public bool Has_Access(BriefItemInfo CurrentItem, User_Object CurrentUser, bool IpRestricted)
        {
            return true;
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        /// <param name="IpRestricted"> Flag indicates if this item is IP restricted AND if the current user is outside the ranges </param> 
        public void Add_Menu_items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IpRestricted)
        {
            // Do nothing
        }

        /// <summary> Creates and returns the an instance of the <see cref="SearchEngineIndexing_ItemViewer"/> class for 
        /// exposing the metadata and full text for search engine indexing </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="SearchEngineIndexing_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer)
        {
            return new SearchEngineIndexing_ItemViewer(CurrentItem, CurrentUser, CurrentRequest);
        }
    }


    /// <summary> Item viewer displays lots of information about an item to facilitate 
    /// search engine indexing.  The Builder uses this viewer to create the static pages used to
    /// provide the data to be indexed</summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class SearchEngineIndexing_ItemViewer : abstractNoPaginationItemViewer
    {
        /// <summary> Constructor for a new instance of the SearchEngineIndexing_ItemViewer class, used to provide the 
        /// information to be indexed by search enegines</summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        public SearchEngineIndexing_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = new List<HTML.HtmlSubwriter_Behaviors_Enum>();
            Behaviors.Add(HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_TOC_Links);
            Behaviors.Add(HtmlSubwriter_Behaviors_Enum.Suppress_Internal_Header);
            Behaviors.Add(HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Item_Menu_Links);
        }

        /// <summary> CSS ID for the viewer viewport for this particular viewer </summary>
        /// <value> This always returns the value 'sbkMliv_Viewer' </value>
        public override string ViewerBox_CssId
        {
            get { return "sbkSeiiv_Viewer"; }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("SearchEngineIndexing_ItemViewer.Write_Main_Viewer_Section", "Write the citation information directly to the output stream");
            }

            // Add the HTML for the citation
            Output.WriteLine("        <!-- SEARCH ENGINE INDEXING ITEM VIEWER OUTPUT -->");
            Output.WriteLine("        ");

            // If this is DARK and the user cannot edit and the flag is not set to show citation, show nothing here
            if ((BriefItem.Behaviors.Dark_Flag) || (BriefItem.Behaviors.IP_Restriction_Membership != 0))
            {
                Output.WriteLine("          <td><div id=\"darkItemSuppressCitationMsg\">This item is DARK and cannot be viewed at this time</div>" + Environment.NewLine + "</td>" + Environment.NewLine + "  <!-- END SEARCH ENGINE VIEWER OUTPUT -->");
                return;
            }

            string viewer_code = CurrentRequest.ViewerCode;

            // Add the CITATION 
            Output.WriteLine("        <td align=\"left\"><span class=\"SobekViewerTitle\">Citation</span></td>");
            Output.WriteLine("      </tr>");
            Output.WriteLine("      <tr>");
            Output.WriteLine("        <td>");
            // Add the main wrapper division
            // Determine the material type
            string microdata_type = "CreativeWork";
            switch (BriefItem.Type)
            {
                case "BOOK":
                case "SERIAL":
                case "NEWSPAPER":
                    microdata_type = "Book";
                    break;

                case "MAP":
                    microdata_type = "Map";
                    break;

                case "PHOTOGRAPH":
                case "AERIAL":
                    microdata_type = "Photograph";
                    break;
            }

            // Set the width
            int width = 180;
            if ((CurrentRequest.Language == Web_Language_Enum.French) || (CurrentRequest.Language == Web_Language_Enum.Spanish))
                width = 230;

            // Add the main wrapper division, with microdata information
            Output.WriteLine("          <div id=\"sbkCiv_Citation\" itemprop=\"about\" itemscope itemtype=\"http://schema.org/" + microdata_type + "\">");
            Output.WriteLine();
            Output.WriteLine(Citation_Standard_ItemViewer.Standard_Citation_String(BriefItem, CurrentRequest, null, width, false, Tracer));
            CurrentRequest.ViewerCode = viewer_code;

            Output.WriteLine("        </td>");
            Output.WriteLine("      </tr>");
            Output.WriteLine("      <tr>");

            // Add the downloads
            if ((BriefItem.Downloads != null) && (BriefItem.Downloads.Count > 0))
            {
                Output.WriteLine("        <td align=\"left\"><span class=\"SobekViewerTitle\">Downloads</span></td>");
                Output.WriteLine("      </tr>");
                Output.WriteLine("      <tr>");
                Output.WriteLine("        <td id=\"sbkDiv_MainArea\">");
                Downloads_ItemViewer.Add_Download_Links(Output, BriefItem, CurrentRequest, null, Tracer);
                Output.WriteLine("        </td>");
            }
            Output.WriteLine("      </tr>");

            string textLocation = SobekFileSystem.Resource_Network_Uri(BriefItem);
            Add_Full_Text(Output, textLocation);
        
        
        }

        private void Add_Full_Text(TextWriter Output, string TextFileLocation)
        {
            // Is the text file location included, in which case any full text should be appended to the end?
            if ((TextFileLocation.Length > 0) && (Directory.Exists(TextFileLocation)))
            {
                // Get the list of all TXT files in this division
                string[] text_files = Directory.GetFiles(TextFileLocation, "*.txt");
                Dictionary<string, string> text_files_existing = new Dictionary<string, string>();
                foreach (string thisTextFile in text_files)
                {
                    string text_filename = (new FileInfo(thisTextFile)).Name.ToUpper();
                    text_files_existing[text_filename] = text_filename;
                }

                // Are there ANY text files?
                if (text_files.Length > 0)
                {
                    // If this has page images, check for related text files 
                    List<string> text_files_included = new List<string>();
                    bool started = false;
                    if ((BriefItem.Images != null ) && ( BriefItem.Images.Count > 0 ))
                    {
                        // Go through the first 100 text pages
                        int page_count = 0;
                        foreach (BriefItem_FileGrouping thisFileGroup in BriefItem.Images)
                        {
                            // Keep track of the page count
                            page_count++;

                            // Look for files in this page
                            if (thisFileGroup.Files.Count > 0)
                            {
                                bool found_non_thumb_file = false;
                                foreach (BriefItem_File thisFile in thisFileGroup.Files)
                                {
                                    // Make sure this is not a thumb
                                    if (thisFile.Name.ToLower().IndexOf("thm.jpg") < 0)
                                    {
                                        found_non_thumb_file = true;
                                        string root = Path.GetFileNameWithoutExtension(thisFile.Name);
                                        if (text_files_existing.ContainsKey(root.ToUpper() + ".TXT"))
                                        {
                                            string text_file = TextFileLocation + "\\" + root.ToUpper() + ".txt";

                                            // SInce this is marked to be included, save this name
                                            text_files_included.Add(root.ToUpper() + ".TXT");

                                            // For size reasons, we only include the text from the first 100 pages
                                            if (page_count <= 100)
                                            {
                                                if (!started)
                                                {
                                                    Output.WriteLine("       <tr>");
                                                    Output.WriteLine("         <td align=\"left\"><span class=\"SobekViewerTitle\">Full Text</span></td>");
                                                    Output.WriteLine("       </tr>");
                                                    Output.WriteLine("       <tr>");
                                                    Output.WriteLine("          <td>");
                                                    Output.WriteLine("            <div style=\"padding: 10px;background-color:white; color:#222; font-size: 0.9em; text-align: left;\">");

                                                    started = true;
                                                }

                                                try
                                                {
                                                    StreamReader reader = new StreamReader(text_file);
                                                    string text_line = reader.ReadLine();
                                                    while (text_line != null)
                                                    {
                                                        Output.WriteLine(text_line + "<br />");
                                                        text_line = reader.ReadLine();
                                                    }
                                                    reader.Close();
                                                }
                                                catch
                                                {
                                                    Output.WriteLine("Unable to read file: " + text_file);
                                                }

                                                Output.WriteLine("<br /><br />");
                                            }
                                        }

                                    }

                                    // If a suitable file was found, break here
                                    if (found_non_thumb_file)
                                        break;
                                }
                            }
                        }

                        // End this if it was ever started
                        if (started)
                        {
                            Output.WriteLine("            </div>");
                            Output.WriteLine("          </td>");
                            Output.WriteLine("       </tr>");
                        }
                    }

                    // Now, check for any other valid text files 
                    List<string> additional_text_files = text_files_existing.Keys.Where(ThisTextFile => (!text_files_included.Contains(ThisTextFile.ToUpper())) && (ThisTextFile.ToUpper() != "AGREEMENT.TXT") && (ThisTextFile.ToUpper().IndexOf("REQUEST") != 0)).ToList();

                    // Now, include any additional text files, which would not be page text files, possiblye 
                    // full text for included PDFs, Powerpoint, Word Doc, etc..
                    started = false;
                    foreach (string thisTextFile in additional_text_files)
                    {
                        if (!started)
                        {
                            Output.WriteLine("       <tr>");
                            Output.WriteLine("         <td align=\"left\"><span class=\"SobekViewerTitle\">Full Text</span></td>");
                            Output.WriteLine("       </tr>");
                            Output.WriteLine("       <tr>");
                            Output.WriteLine("          <td>");
                            Output.WriteLine("            <div style=\"padding: 10px;background-color:white; color:#222; font-size: 0.9em; text-align: left;\">");

                            started = true;
                        }

                        string text_file = TextFileLocation + "\\" + thisTextFile;

                        try
                        {


                            StreamReader reader = new StreamReader(text_file);
                            string text_line = reader.ReadLine();
                            while (text_line != null)
                            {
                                Output.WriteLine(text_line + "<br />");
                                text_line = reader.ReadLine();
                            }
                            reader.Close();
                        }
                        catch
                        {
                            Output.WriteLine("Unable to read file: " + text_file);
                        }

                        Output.WriteLine("<br /><br />");
                    }

                    // End this if it was ever started
                    if (started)
                    {
                        Output.WriteLine("            </div>");
                        Output.WriteLine("          </td>");
                        Output.WriteLine("       </tr>");
                    }
                }
            }
        }

        /// <summary> Any additional inline style for this viewer that affects the main box around this</summary>
        /// <remarks> This returns the width of the image for the width of the viewer port </remarks>
        public override string ViewerBox_InlineStyle
        {
            get
            {
                return "width:900px;";
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
    }
}
