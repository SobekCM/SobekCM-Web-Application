using Microsoft.AspNetCore.Http;
using SobekCM.Core.BriefItem;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Library.Localization;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> JPEG viewer prototyper, which is used to check to see if a (non-thumbnail) JPEG file exists, 
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class JPEG_ItemViewer_Prototyper : abstractItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the JPEG_ItemViewer_Prototyper class </summary>
        public JPEG_ItemViewer_Prototyper()
        {
            ViewerType = "JPEG";
            ViewerCode = "#j";
            FileExtensions = new string[] { "JPG" };
        }

        /// <summary> Indicates if the specified item matches the basic requirements for this viewer, or
        /// if this viewer should be ignored for this item </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer really should be included </param>
        /// <returns> TRUE if this viewer should generally be included with this item, otherwise FALSE </returns>
        public override bool Include_Viewer(BriefItemInfo CurrentItem)
        {
            // Check to see if there are any PDF files attached, but allow the configuration 
            // to actually rule which files are necessary to be shown ( i.e., maybe 'PDFA' will be an extension
            // in the future )
            if (FileExtensions != null)
            {
                return FileExtensions.Any(Extension => CurrentItem.Web.Contains_File_Extension(Extension));
            }

            return CurrentItem.Web.Contains_File_Extension("JPG");
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since PDFs should never be shown if an item is checked out </returns>
        public override bool Override_On_Checkout(BriefItemInfo CurrentItem)
        {
            return true;
        }

        /// <summary> Flag indicates if the current user has access to this viewer for the item </summary>
        /// <param name="CurrentItem"> Digital resource to see if the current user has correct permissions to use this viewer </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public override bool Has_Access(BriefItemInfo CurrentItem, RequestCache RequestSpecificValues)
        {
            bool IsRestricted = RequestSpecificValues.Flags.ItemRestrictedFromUser;

            return !IsRestricted;
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        public override void Add_Menu_Items(BriefItemInfo CurrentItem, RequestCache RequestSpecificValues, List<Item_MenuItem> MenuItems)
        {
            var CurrentRequest = RequestSpecificValues.Current_Mode;

            int current_page = 1;
            string previous_code = "1";

            // Get the URL for this
            if (CurrentRequest.ViewerCode != null)
            {
                previous_code = CurrentRequest.ViewerCode.Replace("x", "").Replace("j", "");
                if (!int.TryParse(previous_code, out current_page))
                    current_page = 1;
            }

            CurrentRequest.ViewerCode = ViewerCode.Replace("#", current_page.ToString());
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Add the item menu information
            var menuItem = new Item_MenuItem("Page Images", Localization_Gateway.JPEG.Menu_Standard(CurrentRequest.Language), null, url, ViewerCode);
            MenuItems.Add(menuItem);
        }

        /// <summary> Creates and returns the an instance of the <see cref="JPEG_ItemViewer"/> class for showing a  
        /// JPEG image from a page within a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="JPEG_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public override iItemViewer Create_Viewer(BriefItemInfo CurrentItem, RequestCache RequestSpecificValues, Custom_Tracer Tracer)
        {
            var CurrentUser = RequestSpecificValues.Current_User;
            var CurrentRequest = RequestSpecificValues.Current_Mode;

            // Only link the page image to the zoomable viewer when that viewer would actually open for this request.
            // A robot, or a subnet over its JP2 budget (or a tripped circuit breaker), gets redirected straight back
            // to this viewer by JPEG2000_ItemViewer, so the link and its "switch to zoomable" prompt would be a dead end.
            // Passing the reason rather than a yes/no lets the viewer explain it instead (robots get nothing).
            JP2_Zoom_Withheld_Enum zoomWithheld = (CurrentRequest.Is_Robot)
                ? JP2_Zoom_Withheld_Enum.Robot
                : JPEG2000_ItemViewer_Prototyper.Zoom_Withheld_Reason(CurrentUser, RequestSpecificValues.Context, out _);

            return new JPEG_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer, ViewerCode.ToLower(), FileExtensions, zoomWithheld);
        }
    }

    /// <summary> Item page viewer displays the a JPEG from the page images within a digital resource. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractPageFilesItemViewer"/> and implements the
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class JPEG_ItemViewer : abstractPageFilesItemViewer
    {
        // information about the page to display
        private readonly int page;
        private int width;
        private int height;
        private string filename;

        // properties about linking to the zoomable file
        private bool includeLinkToZoomable;
        private readonly string zoomableViewerCode;

        // Why this page's zoomable version is being withheld, when it has one and the JP2 rate limits are withholding
        // it -- drives the notice above the page image (see write_zoom_withheld_notice)
        private JP2_Zoom_Withheld_Enum zoomWithheldNotice;

        /// <summary> Constructor for a new instance of the JPEG_ItemViewer class, used to display JPEGs linked to
        /// pages in a digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="JPEG_ViewerCode"> JPEG viewer code, as determined by configuration files </param>
        /// <param name="FileExtensions"> File extensions that this viewer allows, as determined by configuration files </param>
        /// <param name="ZoomWithheld"> Whether, and why, the zoomable viewer is withheld from this request -- for a robot,
        /// or when the JP2 budget or circuit breaker is withholding it, the page image isn't linked to it </param>
        public JPEG_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, string JPEG_ViewerCode, string[] FileExtensions, JP2_Zoom_Withheld_Enum ZoomWithheld)
        {
            // Add the trace
            Tracer?.Add_Trace("JPEG_ItemViewer.Constructor");

            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties
            Behaviors = EmptyBehaviors;

            // Is the JPEG2000 viewer included in this item? Whether it would actually open for this request is applied
            // once the page's files are known, below, so a withheld zoomable version can still be explained.
            bool zoomableViewerIncluded = BriefItem.UI.Includes_Viewer_Type("JPEG2000");
            string[] jpeg2000_extensions = null;
            if (zoomableViewerIncluded)
            {
                iItemViewerPrototyper jp2Prototyper = ItemViewer_Factory.Get_Viewer_By_ViewType("JPEG2000");
                if (jp2Prototyper == null)
                    zoomableViewerIncluded = false;
                else
                {
                    zoomableViewerCode = jp2Prototyper.ViewerCode;
                    jpeg2000_extensions = jp2Prototyper.FileExtensions;
                }
            }

            // Set some default values
            width = 500;
            height = -1;
            includeLinkToZoomable = false;

            // Determine the page
            page = 1;
            if (!String.IsNullOrEmpty(CurrentRequest.ViewerCode))
            {
                int tempPageParse;
                if (Int32.TryParse(CurrentRequest.ViewerCode.Replace(JPEG_ViewerCode.Replace("#", ""), ""), out tempPageParse))
                    page = tempPageParse;
            }

            // Just a quick range check
            if (page > BriefItem.Images.Count)
                page = 1;

            // Try to set the file information here
            if ((!set_file_information(FileExtensions, zoomableViewerIncluded, jpeg2000_extensions)) && (page != 1))
            {
                // If there was an error, just set to the first page
                page = 1;
                set_file_information(FileExtensions, zoomableViewerIncluded, jpeg2000_extensions);
            }

            // includeLinkToZoomable now only says whether this page has a zoomable version at all. Link to it only when
            // the zoomable viewer would actually open; otherwise keep the reason, so a notice can say why.
            if ((includeLinkToZoomable) && (ZoomWithheld != JP2_Zoom_Withheld_Enum.Not_Withheld))
            {
                includeLinkToZoomable = false;
                zoomWithheldNotice = ZoomWithheld;
            }

            // Since this is a paging viewer, set the viewer code
            if (String.IsNullOrEmpty(CurrentRequest.ViewerCode))
                CurrentRequest.ViewerCode = JPEG_ViewerCode.Replace("#", page.ToString());

        }

        private bool set_file_information(string[] FileExtensions, bool zoomableViewerIncluded, string[] zoomableFileExtensions)
        {
            bool returnValue = false;
            includeLinkToZoomable = false;
            bool width_found = false;

            // Find the page information
            BriefItem_FileGrouping imagePage = BriefItem.Images[page - 1];
            if (imagePage.Files != null)
            {
                // Step through each file in this page
                foreach (BriefItem_File thisFile in imagePage.Files)
                {
                    // Get this file extension
                    string extension = thisFile.File_Extension.Replace(".", "");

                    // Step through all permissable file extensions
                    foreach (string thisPossibleFileExtension in FileExtensions)
                    {
                        if (String.Compare(extension, thisPossibleFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            // If a return value was already found, look to see if this one is bigger, in which case it will be used
                            // This is a convenient way to get around thumbnails issue without looking for "thm.jpg"
                            if (returnValue)
                            {
                                // Are their widths present?
                                if (width_found && thisFile.Width.HasValue)
                                {
                                    if (thisFile.Width.Value > width)
                                    {
                                        // THis file is bigger (wider)
                                        filename = thisFile.Name;
                                        width = thisFile.Width.Value;
                                        if (thisFile.Height.HasValue) height = thisFile.Height.Value;
                                    }
                                }
                                else
                                {
                                    // Since no width was found, just go for a shorter filename
                                    if (filename.Length > thisFile.Name.Length)
                                    {
                                        // This name is shorter... assuming it doesn't include thm.jpg then
                                        filename = thisFile.Name;
                                        if (thisFile.Width.HasValue)
                                        {
                                            width = thisFile.Width.Value;
                                            width_found = true;
                                        }
                                        else
                                        {
                                            width = 500;
                                        }
                                        if (thisFile.Height.HasValue) height = thisFile.Height.Value;
                                    }
                                }
                            }
                            else
                            {
                                // Get the JPEG information
                                filename = thisFile.Name;
                                if (thisFile.Width.HasValue)
                                {
                                    width = thisFile.Width.Value;
                                    width_found = true;
                                }
                                if (thisFile.Height.HasValue) height = thisFile.Height.Value;
                            }

                            // Found a value to return
                            returnValue = true;
                        }
                    }

                    // Also look for the JPEG2000 viewers
                    if (zoomableViewerIncluded)
                    {
                        // Step through all JPEG2000 extensions
                        foreach (string thisPossibleFileExtension in zoomableFileExtensions)
                        {
                            if (String.Compare(extension, thisPossibleFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                // Found a jpeg2000
                                includeLinkToZoomable = true;
                                break;
                            }
                        }
                    }
                }

                // Finished looking at all the page files and found the file to display, so return TRUE
                if (returnValue) return true;
            }

            return false;
        }

        /// <summary> Any additional inline style for this viewer that affects the main box around this</summary>
        /// <remarks> This returns the width of the image for the width of the viewer port </remarks>
        public override string ViewerBox_InlineStyle
        {
            get
            {
                if (width < 500)
                    return "width:500px;";
                return "width:" + width + "px;";
            }
        }

        /// <summary> Writes a short notice above the page image when this page has a zoomable version that the JP2 rate
        /// limits are withholding, saying why -- with a log on link when logging on would give zoom back </summary>
        /// <param name="Output"> Response stream to write to </param>
        /// <remarks> Writes nothing when zoom isn't withheld, or for a robot. The text goes through General.Get, so it
        /// can be translated later without a code change. </remarks>
        private void write_zoom_withheld_notice(TextWriter Output)
        {
            string language = CurrentRequest.Language;
            string message;
            switch (zoomWithheldNotice)
            {
                case JP2_Zoom_Withheld_Enum.Circuit_Breaker:
                case JP2_Zoom_Withheld_Enum.Anonymous_Budget:
                    message = Localization_Gateway.General.Get("The zoomable view is temporarily unavailable.", language);
                    break;

                case JP2_Zoom_Withheld_Enum.Logged_On_Budget:
                    message = Localization_Gateway.General.Get("The zoomable view is temporarily unavailable from your network. Please try again later.", language);
                    break;

                default:
                    return;
            }

            Output.Write("\t\t\t<div id=\"sbkJiv_ZoomUnavailable\" style=\"margin-bottom:8px;\">" + message);

            // Logging on only helps when it's the anonymous budget that ran out: the logged-on budget is counted
            // separately, and the circuit breaker applies to everyone
            if (zoomWithheldNotice == JP2_Zoom_Withheld_Enum.Anonymous_Budget)
            {
                // Build the log on URL by temporarily switching the live navigation object, then put back every
                // field that was changed (same approach as Item_HtmlSubwriter.write_rate_limit_message). The return
                // URL is this page's zoomable view, so a successful log on lands straight back in it.
                Navigation_Object navigation = CurrentRequest;
                Display_Mode_Enum originalMode = navigation.Mode;
                My_Sobek_Type_Enum originalMySobekType = navigation.My_Sobek_Type;
                string originalReturnUrl = navigation.Return_URL;
                string originalViewerCode = navigation.ViewerCode;
                string logOnUrl;
                try
                {
                    if (!String.IsNullOrEmpty(zoomableViewerCode))
                        navigation.ViewerCode = zoomableViewerCode.Replace("#", page.ToString());
                    string returnUrl = UrlWriterHelper.Redirect_URL(navigation);

                    navigation.ViewerCode = originalViewerCode;
                    navigation.Mode = Display_Mode_Enum.My_Sobek;
                    navigation.My_Sobek_Type = My_Sobek_Type_Enum.Logon;
                    navigation.Return_URL = returnUrl;
                    logOnUrl = UrlWriterHelper.Redirect_URL(navigation);
                }
                finally
                {
                    navigation.Mode = originalMode;
                    navigation.My_Sobek_Type = originalMySobekType;
                    navigation.Return_URL = originalReturnUrl;
                    navigation.ViewerCode = originalViewerCode;
                }

                string logOnPrompt = Localization_Gateway.General.Get("Log on to get the zoomable view back.", language);
                Output.Write(" <a href=\"" + System.Net.WebUtility.HtmlEncode(logOnUrl) + "\">" + logOnPrompt + "</a>");
            }

            Output.WriteLine("</div>");
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("JPEG_ItemViewer.Write_Main_Viewer_Section", "");

            // If no matching page image file was found for this page, show a message rather than crash
            if (String.IsNullOrEmpty(filename))
            {
                Output.WriteLine("\t\t<td align=\"center\" id=\"sbkJiv_Image\">" + Localization_Gateway.JPEG.Error_No_Image_Found(CurrentRequest.Language) + "</td>");
                return;
            }

            // No need to route this through the "files/" auth-checked endpoint for a restricted/dark item:
            // this viewer only renders at all once Has_Access has already confirmed the current user is
            // allowed to see it, so the direct (signed, for GCS) URL is already safe
            string displayFileName = SobekFileSystem.Resource_Web_Uri(BriefItem, filename, Lifetime: Signed_Url_Lifetime_Enum.Page_Load);


            string name_for_image = System.Net.WebUtility.HtmlEncode(BriefItem.Title);


            if ((BriefItem.Images != null) && (BriefItem.Images.Count > 1) && (Current_Page - 1 < BriefItem.Images.Count))
            {
                string name_of_page = BriefItem.Images[Current_Page - 1].Label;
                name_for_image = name_for_image + " - " + System.Net.WebUtility.HtmlEncode(name_of_page);
            }



            // Add the HTML for the image
            if (includeLinkToZoomable)
            {
                string currViewer = CurrentRequest.ViewerCode;
                CurrentRequest.ViewerCode = zoomableViewerCode.Replace("#", page.ToString());
                string toZoomable = UrlWriterHelper.Redirect_URL(CurrentRequest);
                CurrentRequest.ViewerCode = currViewer;
                Output.WriteLine("\t\t<td id=\"sbkJiv_ImageZoomable\">");
                Output.WriteLine(Localization_Gateway.JPEG.Zoomable_Switch_Prompt(CurrentRequest.Language) + "<br />");
                Output.WriteLine("<a href=\"" + toZoomable + "\" title=\"" + Localization_Gateway.JPEG.Zoomable_Switch_Title(CurrentRequest.Language) + "\">");

                Output.Write("\t\t\t<img itemprop=\"primaryImageOfPage\" ");
                if ((height > 0) && (width > 0))
                    Output.Write("style=\"height:" + height + "px;width:" + width + "px;\" ");
                Output.WriteLine("src=\"" + displayFileName + "\" alt=\"" + name_for_image + "\" />");

                Output.WriteLine("</a>");
            }
            else
            {
                Output.WriteLine("\t\t<td align=\"center\" id=\"sbkJiv_Image\">");
                write_zoom_withheld_notice(Output);

                Output.Write("\t\t\t<img itemprop=\"primaryImageOfPage\" ");
                if ((height > 0) && (width > 0))
                    Output.Write("style=\"height:" + height + "px;width:" + width + "px;\" ");
                Output.WriteLine("src=\"" + displayFileName + "\" alt=\"" + name_for_image + "\" />");
            }

            Output.WriteLine("\t\t</td>");
        }
    }
}
