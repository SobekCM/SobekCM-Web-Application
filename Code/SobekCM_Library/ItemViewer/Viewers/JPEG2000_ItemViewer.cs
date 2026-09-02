using Microsoft.AspNetCore.Http;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Configuration.Localization;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Settings;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Configuration;
using SobekCM.Library.HTML;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Library.Localization;
using SobekCM.Library.UI;
using SobekCM.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> JPEG2000 viewer prototyper, which is used to check to see if a JPEG2000 file exists, 
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class JPEG2000_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the JPEG2000_ItemViewer_Prototyper class </summary>
        public JPEG2000_ItemViewer_Prototyper()
        {
            ViewerType = "JPEG2000";
            ViewerCode = "#x";
            FileExtensions = new string[] { "JP2" };
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
            // Circuit breaker: an instance that's opted into the image-server path (JP2ServerType set) but
            // has no URL configured for it (JP2ServerUrl empty) -- whether from an incomplete setup or the
            // image server being pulled out of service -- is guaranteed-broken for this viewer, with no
            // legacy fallback (see JPEG2000_ItemViewer.Write_Main_Viewer_Section's identical pairing check).
            // Don't offer the viewer at all in that state, same as if there were no JP2 files -- clearing
            // JP2ServerUrl is enough to take the viewer down cleanly if the image server needs to come out.
            if (String.Equals(UI_ApplicationCache_Gateway.Settings.Servers.JP2ServerType, JPEG2000_ItemViewer.TEST_JP2_SERVER_TYPE, StringComparison.OrdinalIgnoreCase)
                && String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.JP2ServerUrl))
            {
                return false;
            }

            // Check to see if there are any PDF files attached, but allow the configuration
            // to actually rule which files are necessary to be shown ( i.e., maybe 'PDFA' will be an extension
            // in the future )
            if (FileExtensions != null)
            {
                return FileExtensions.Any(Extension => CurrentItem.Web.Contains_File_Extension(Extension));
            }

            return CurrentItem.Web.Contains_File_Extension("JP2");
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
            // Don't offer the zoomable viewer to robots -- same gating StandardItemMenuProvider already
            // applies to other menu items, and worth it here specifically since crawling every page of every
            // JP2 would mean a lot of wasted GCS downloads/staging for a viewer no crawler can meaningfully use
            if (CurrentRequest.Is_Robot)
                return;

            // Get the URL for this
            string previous_code = CurrentRequest.ViewerCode.Replace("x", "").Replace("j", "");
            int current_page;
            if (!int.TryParse(previous_code, out current_page))
                current_page = 1;

            CurrentRequest.ViewerCode = ViewerCode.Replace("#", current_page.ToString());
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Add the item menu information
            var menuItem = new Item_MenuItem("Page Images", "Zoomable", null, url, ViewerCode);
            MenuItems.Add(menuItem);
        }

        /// <summary> Creates and returns the an instance of the <see cref="JPEG2000_ItemViewer"/> class for showing a zoomable 
        /// JPEG2000 image from a page within a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="CurrentFlags"> Calculated flags for this particular requests, to avoid recalculation in different viewers </param>
        /// <returns> Fully built and initialized <see cref="JPEG2000_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, RequestCache_RequestFlags CurrentFlags, HttpContext Context)
        {
            return new JPEG2000_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer, ViewerCode, FileExtensions, Context);
        }
    }

    /// <summary> Item page viewer displays the a zoomable JPEG2000 from the page images within a digital resource. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractPageFilesItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class JPEG2000_ItemViewer : abstractPageFilesItemViewer
    {
        // ***** TEMPORARY TEST CODE *****
        // Spike: when Server_Settings.JP2ServerType (DB setting "JPEG2000 Server Type" -- pre-existing,
        // never previously read anywhere) is set to this value, and JP2ServerUrl is configured, route
        // through the separate SobekCM_ImageServer site instead of the normal Image_Server_Root /
        // Image_Server_Network local/UNC path -- see JPEG2000_ImageServer_TestClient for the client side
        // of that protocol, and the SobekCM_ImageServer project for the server side. Every instance that
        // hasn't opted in (including the default, empty JP2ServerType) falls straight through to the
        // existing behavior further down, untouched.
        // internal, not private: JPEG2000_ItemViewer_Prototyper.Include_Viewer (below) checks this same
        // value to avoid ever offering a viewer that's guaranteed broken (JP2ServerType set but JP2ServerUrl
        // left empty), so both classes need it -- keeping it here (rather than duplicating the literal)
        // means there's exactly one place that defines what "opted in" means.
        internal const string TEST_JP2_SERVER_TYPE = "GCS Scratch";

        // Falls back to this only when ImageServerSharedKey.Path (set from appsettings.json
        // "ImageServer:SharedKeyPath" -- see RequestContextMiddleware) hasn't been configured explicitly --
        // lets this work out of the box with the conventional path rather than failing outright, while
        // still being fully overridable via that appsettings.json value.
        private const string TEST_DEFAULT_IMAGE_SERVER_SHARED_KEY_PATH = @"C:\SobekCM\Keys\image-server-shared-key.txt";

        // Shown for all three ways this design can fail once staging is involved: the initial DZI open
        // 404ing (OpenSeadragon's own Errors.OpenFailed, reworded), a tile 404ing mid-session because the
        // scratch file's cache window elapsed while someone was still viewing it (tile-load-failed, which
        // OpenSeadragon does not show any message for by default), and the deferred <script src> itself
        // failing to load (image server unreachable, or the render token already expired). No apostrophes --
        // this gets embedded inside a single-quoted JS string literal.
        private const string TEST_EXPIRED_MESSAGE = "This image view has expired. Please refresh the page and try again.";
        // ***** END TEMPORARY TEST CODE *****

        private readonly bool suppressNavigator;

        // information about the file to display
        private readonly int page;
        private string filename;

        /// <summary> Constructor for a new instance of the JPEG2000_ItemViewer class, used to display JPEG2000s linked to
        /// pages in a digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Context"> Current HTTP context -- only used to redirect robots to the plain JPEG viewer
        /// for this same page (see below); a real browser never touches it here </param>
        public JPEG2000_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, string ViewerCode, string[] FileExtensions, HttpContext Context)
        {
            // Add the trace
            Tracer?.Add_Trace("JPEG2000_ItemViewer.Constructor");

            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Determine if the navigator ( in the left nav bar ) should be suppressed
            suppressNavigator = false;
            if (UI_ApplicationCache_Gateway.Settings.Contains_Additional_Setting("JPEG2000 ItemViewer.Suppress Navigator"))
            {
                if (UI_ApplicationCache_Gateway.Settings.Get_Additional_Setting("JPEG2000 ItemViewer.Suppress Navigator").ToLower().Trim() != "false")
                    suppressNavigator = true;
            }

            // Determine the page
            page = 1;
            if (!String.IsNullOrEmpty(CurrentRequest.ViewerCode))
            {
                int tempPageParse;
                if (Int32.TryParse(CurrentRequest.ViewerCode.Replace(ViewerCode.Replace("#", ""), ""), out tempPageParse))
                    page = tempPageParse;
            }

            // Just a quick range check
            if (page > BriefItem.Images.Count)
                page = 1;

            // Robots may already have this exact URL indexed from before it stopped being linked in the menu
            // (see JPEG2000_ItemViewer_Prototyper.Add_Menu_Items), so keep redirecting it going forward --
            // to the plain JPEG viewer for this same page, not the item's front page, so the crawler still
            // gets real, indexable, JS-free content for this specific page rather than losing the association
            // entirely. "j" is JPEG_ItemViewer_Prototyper's own ViewerCode ("#j") -- matches the "x"/"j"
            // literal convention already used elsewhere in this class (see Add_Menu_Items above).
            if (CurrentRequest.Is_Robot)
            {
                CurrentRequest.ViewerCode = page + "j";
                UrlWriterHelper.Redirect(CurrentRequest, Context);
                return;
            }

            // Try to set the file information here
            if ((!set_file_information(FileExtensions)) && (page != 1))
            {
                // If there was an error, just set to the first page
                page = 1;
                set_file_information(FileExtensions);
            }

            // Since this is a paging viewer, set the viewer code
            if (String.IsNullOrEmpty(CurrentRequest.ViewerCode))
                CurrentRequest.ViewerCode = ViewerCode.Replace("#", page.ToString());
        }

        private bool set_file_information(string[] FileExtensions)
        {
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
                            // Get the JPEG information
                            filename = thisFile.Name;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary> Gets the collection of special behaviors which this item viewer
        /// requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> ItemViewer_Behaviors
        {
            get
            {
                // If the navigator will be  shown, we need a left nav bar, so return different behaviors
                if (!suppressNavigator)
                {
                    return new List<HtmlSubwriter_Behaviors_Enum> { HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Bottom_Pagination, HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Requires_Left_Navigation_Bar };
                }
                return new List<HtmlSubwriter_Behaviors_Enum> { HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Bottom_Pagination };

            }
        }

        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("<script src=\"" + Static_Resources_Gateway.OpenSeaDragon_Js + "\"></script>");
        }

        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        /// <param name="Body_Attributes"> List of body attributes to be included </param>
        public override void Add_ViewerSpecific_Body_Attributes(List<Tuple<string, string>> Body_Attributes)
        {
            Body_Attributes.Add(new Tuple<string, string>("onload", "jp2_set_fullscreen();"));
            Body_Attributes.Add(new Tuple<string, string>("onresize", "jp2_set_fullscreen();"));
        }


        /// <summary> Adds any viewer_specific information to the left Navigation Bar Menu Section  </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Left_Nav_Menu_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("JPEG2000_ItemViewer.Write_Nav_Bar_Menu_Section", "Adds small thumbnail for image navigation");

            if (suppressNavigator)
                return;

            string thumnbnail_text = Localization_Gateway.JPEG2000.Thumbnail(CurrentRequest.Language);

            Output.WriteLine("        <ul class=\"sbkIsw_NavBarMenu\">");
            Output.WriteLine("          <li class=\"sbkIsw_NavBarHeader\"> " + thumnbnail_text + " </li>");
            Output.WriteLine("          <li class=\"sbkIsw_NavBarMenuNonLink\">");
            Output.WriteLine("            <div id=\"sbkJp2_Navigator\"></div>");
            Output.WriteLine("            <br />");
            Output.WriteLine("          </li>");
            Output.WriteLine("        </ul>");
        }

        // ***** TEMPORARY TEST CODE *****
        /// <summary> Builds the URL for a &lt;script src&gt; tag that hands the whole staging job off to the
        /// separate image server -- no network call happens here; the browser is the one that actually
        /// requests this URL (see <see cref="JPEG2000_ImageServer_TestClient"/> and the image server's own
        /// GET /render for the rest of the contract). </summary>
        private string test_build_image_server_script_url()
        {
            // NOT SobekFileSystem.AssociFilePath -- despite the name, that's always the LOCAL/pairtree-split
            // path (e.g. "DR/00/00/00/16/00001/"), by design, even under GCS Hybrid mode (see
            // Hybrid_FileSystem's own doc remarks: every read-path method except the per-file Resource_Web_Uri
            // overload "always delegate[s] to the local file system unconditionally"). The GCS object key
            // shape is flat -- "{SystemCode}/{BibID}/{VID}/" -- built here to match GCS_FileSystem's own
            // private object_key_prefix exactly, since nothing in iFileSystem exposes that shape directly.
            string systemCode = UI_ApplicationCache_Gateway.Settings.System?.System_Code;
            if (String.IsNullOrEmpty(systemCode))
                systemCode = "SOBEK";

            string tag = systemCode + "/" + BriefItem.BibID + "/" + BriefItem.VID + "/";

            // Newspapers/books tend to hold a reader's attention longer than a single-page item -- ask the
            // image server to keep those around a bit longer than its default
            int? cacheMinutes = null;
            if ((BriefItem.Type == "Newspaper") || (BriefItem.Type == "Book"))
                cacheMinutes = 10;

            string sharedKeyPath = ImageServerSharedKey.Path;
            if (String.IsNullOrEmpty(sharedKeyPath))
                sharedKeyPath = TEST_DEFAULT_IMAGE_SERVER_SHARED_KEY_PATH;

            return JPEG2000_ImageServer_TestClient.BuildRenderScriptUrl(
                UI_ApplicationCache_Gateway.Settings.Servers.JP2ServerUrl,
                sharedKeyPath,
                UI_ApplicationCache_Gateway.Settings.Servers.GCS_Bucket_Name,
                tag, filename, cacheMinutes);
        }
        // ***** END TEMPORARY TEST CODE *****

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("JPEG2000_ItemViewer.Write_Main_Viewer_Section", "Adds the container for the zoomable image");

            // ***** TEMPORARY TEST CODE *****
            // Only takes effect when an instance has actually opted in via JP2ServerType/JP2ServerUrl (see
            // the const/helper declared near the top of this class) -- unset (the default) or any other
            // value leaves useImageServer FALSE, and everything below behaves exactly as it did before this
            // spike existed.
            bool useImageServer = String.Equals(UI_ApplicationCache_Gateway.Settings.Servers.JP2ServerType, TEST_JP2_SERVER_TYPE, StringComparison.OrdinalIgnoreCase)
                && !String.IsNullOrEmpty(UI_ApplicationCache_Gateway.Settings.Servers.JP2ServerUrl);
            // ***** END TEMPORARY TEST CODE *****

            Output.WriteLine("<td>");
            Output.WriteLine("<div id=\"sbkJp2_Container\" ></div>");
            Output.WriteLine();
            Output.WriteLine("<script type=\"text/javascript\">");

            // ***** TEMPORARY TEST CODE *****
            // Reword OpenSeadragon's own built-in "Unable to open ...: HTTP 404 attempting to load
            // TileSource" message (openseadragon.js Errors.OpenFailed, shown via its default 'open-failed'
            // handler) -- this is what fires if the DZI descriptor itself 404s, e.g. staging failed or the
            // render token was already stale by the time it was used.
            if (useImageServer)
                Output.WriteLine("   OpenSeadragon.setString('Errors.OpenFailed', '" + TEST_EXPIRED_MESSAGE + "');");
            // ***** END TEMPORARY TEST CODE *****

            Output.WriteLine("   viewer = OpenSeadragon({");
            Output.WriteLine("      id: \"sbkJp2_Container\",");
            Output.WriteLine("      prefixUrl : \"" + Static_Resources_Gateway.OpenSeaDragon_Image_Prefix + "\",");

            // ***** TEMPORARY TEST CODE *****
            // Only the image-server path serves tiles from a genuinely different origin than the page
            // itself. The navigator's small overview specifically needs canvas-drawn compositing, which the
            // browser taints/blocks for cross-origin images unless the <img> request carries
            // crossorigin="anonymous" -- OpenSeadragon's own 'drawer-error' handler (openseadragon.js) points
            // at exactly this fix. Requires the tile server to actually send CORS headers (see the iipimage
            // folder's web.config) -- "Anonymous" sends no credentials, matching a wildcard Access-Control-
            // Allow-Origin, which can't be combined with credentialed requests per the CORS spec anyway.
            if (useImageServer)
                Output.WriteLine("      crossOriginPolicy: \"Anonymous\",");
            // ***** END TEMPORARY TEST CODE *****

            if (suppressNavigator)
            {

                Output.WriteLine("      showNavigator:  false");
            }
            else
            {
                Output.WriteLine("      showNavigator:  true,");
                Output.WriteLine("      navigatorId:  \"sbkJp2_Navigator\",");

                // Doesn't actually set the navigator size (the CSS does), but setting this means
                // OpenSeaDragon won't try to set the width/height as a ratio of the main image.
                Output.WriteLine("      navigatorWidth:  \"195px\",");
                Output.WriteLine("      navigatorHeight:  \"195px\"");
            }

            Output.WriteLine("   });");
            Output.WriteLine();

            // ***** TEMPORARY TEST CODE *****
            // OpenSeadragon does not show any message for a failed tile by default (only a failed initial
            // open) -- this is the case that matters most for the scratch-cache design: the DZI opens fine,
            // then a tile 404s later because the scratch file's cache window elapsed while the page was
            // still open. Without this handler that failure is completely silent (blank/gray tiles).
            if (useImageServer)
                Output.WriteLine("   viewer.addHandler('tile-load-failed', function () { viewer._showMessage('" + TEST_EXPIRED_MESSAGE + "'); });");
            // ***** END TEMPORARY TEST CODE *****

            if (!useImageServer)
            {
                string dziSource;
                if (UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Root != null)
                {
                    //add by Keven for FIU dPanther's separate image server
                    dziSource = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Root.Replace("\\", "/") + SobekFileSystem.AssociFilePath(BriefItem).Replace("\\", "/") + filename;
                }
                else
                {
                    dziSource = UI_ApplicationCache_Gateway.Settings.Servers.Image_Server_Network.Replace("\\", "/") + SobekFileSystem.AssociFilePath(BriefItem).Replace("\\", "/") + filename;
                }

                Output.WriteLine("   viewer.open(\"" + CurrentRequest.Base_URL + "iipimage/iipsrv.fcgi?DeepZoom=" + dziSource + ".dzi\");");
            }

            Output.WriteLine("</script>");

            // ***** TEMPORARY TEST CODE *****
            // The image server does its own staging (from GCS, cached) entirely off this app's request
            // thread, then responds to the browser's request for this script with a single "viewer.open(...)"
            // statement -- see JPEG2000_ImageServer_TestClient and the image server's GET /render. onerror
            // covers the case where this script tag itself never loads at all (image server unreachable,
            // or the render token already expired by the time the browser got to it) -- viewer.open() never
            // gets called in that case, so OpenSeadragon's own open-failed event never fires either.
            if (useImageServer)
                Output.WriteLine("<script src=\"" + test_build_image_server_script_url() + "\" onerror=\"viewer._showMessage('" + TEST_EXPIRED_MESSAGE + "');\"></script>");
            // ***** END TEMPORARY TEST CODE *****

            Output.WriteLine("</td>");
        }

        /// <summary> Any additional inline style for this viewer that affects the main box around this</summary>
        /// <remarks> This makes the main viewport NOT centered, since this will be made full page by javascript anyway </remarks>
        public override string ViewerBox_InlineStyle
        {
            get
            {
                return "margin-left:10px; margin-right:10px; ";
            }
        }
    }
}
