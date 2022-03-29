using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.HTML;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> HTML website item viewer prototyper, which is used to check to see if there is a HTML site to display, 
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class HTML_WebSite_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        private string fileFromViewerAttribute;

        /// <summary> Constructor for a new instance of the HTML_WebSite_ItemViewer_Prototyper class </summary>
        public HTML_WebSite_ItemViewer_Prototyper() 
        {
            ViewerType = "WEBSITE";
            ViewerCode = "WEBSITE";
            FileExtensions = new string[] { "HTML", "HTM" };
            fileFromViewerAttribute = String.Empty;
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
            return true;
            //return FileExtensions.Any(Extension => CurrentItem.Web.Contains_File_Extension(Extension));
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
            // Start with an empty label
            string first_label = String.Empty;

            // First, look at the viewer information from the database
            BriefItem_BehaviorViewer thisViewer = CurrentItem.Behaviors.Get_Viewer("WEBSITE");
            if (!String.IsNullOrWhiteSpace(thisViewer.Label))
                first_label = thisViewer.Label;

            // Finally, just default to HTML otherwise
            if (String.IsNullOrEmpty(first_label))
                first_label = "HTML";

            // Get the URL for this
            string previous_code = CurrentRequest.ViewerCode;
            CurrentRequest.ViewerCode = ViewerCode;
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Allow the label to be implemented for this viewer from the database as well
            BriefItem_BehaviorViewer thisViewerInfo = CurrentItem.Behaviors.Get_Viewer(ViewerCode);

            // If this is found, and has a custom label, use that 
            if ((thisViewerInfo != null) && (!String.IsNullOrWhiteSpace(thisViewerInfo.Label)))
                first_label = thisViewerInfo.Label;

            // Save the attribute as the file name
            fileFromViewerAttribute = thisViewer.Attributes;

            // Add the item menu information
            Item_MenuItem menuItem = new Item_MenuItem(first_label, null, null, url, ViewerCode);
            MenuItems.Add(menuItem);
        }

        /// <summary> Creates and returns the an instance of the <see cref="HTML_WebSite_ItemViewer"/> class for showing an
        /// HTML web site that is loaded locally with the digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="HTML_WebSite_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer)
        {
            return new HTML_WebSite_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, fileFromViewerAttribute);
        }
    }


    /// <summary> Item viewer displays the a HTML website in a frame within the SobekCM window for viewing. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class HTML_WebSite_ItemViewer : abstractNoPaginationItemViewer
    {
        private readonly string htmlFile;

        /// <summary> Constructor for a new instance of the HTML_WebSite_ItemViewer class, used to display a HTML file from a digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        public HTML_WebSite_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, string FileName)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;

            // Determine the html file name
            string itemLink = SobekFileSystem.Resource_Web_Uri(BriefItem);
            htmlFile = itemLink + FileName;
        }

        /// <summary> Gets the collection of special behaviors which this item viewer
        /// requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> ItemViewer_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum> { HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Bottom_Pagination };
            }
        }


        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        /// <param name="Body_Attributes"> List of body attributes to be included </param>
        public override void Add_ViewerSpecific_Body_Attributes(List<Tuple<string, string>> Body_Attributes)
        {
            Body_Attributes.Add(new Tuple<string, string>("onload", "jp2_set_fullscreen();"));
            Body_Attributes.Add(new Tuple<string, string>("onresize", "jp2_set_fullscreen();"));
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("HTML_WebSite_ItemViewer.Write_Main_Viewer_Section", "");
            }

            // Save the current viewer code
            string current_view_code = CurrentRequest.ViewerCode;

            // Start the citation table
            Output.WriteLine("\t\t<!-- HTML WEBSITE VIEWER OUTPUT -->");
            Output.WriteLine("\t\t<td style=\"align:left;width=\"100%\"\">");

            Output.WriteLine("<iframe id=\"sbkJp2_Container\" style=\"width:800px; height:800px\" src=\"" + htmlFile + "\"></iframe>");

            // Finish the table
            Output.WriteLine("\t\t</td>");
            Output.WriteLine("\t\t<!-- END HTML WEBSITE VIEWER OUTPUT -->");

            // Restore the mode
            CurrentRequest.ViewerCode = current_view_code;
        }

        /// <summary> Allows controls to be added directory to a place holder, rather than just writing to the output HTML stream </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the bulk of the item viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method does nothing, since nothing is added to the place holder as a control for this item viewer </remarks>
        public override void Add_Main_Viewer_Section(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer)
        {
            // Do nothing
        }

        /// <summary> Any additional inline style for this viewer that affects the main box around this</summary>
        /// <remarks> This makes the main viewport NOT centered, since this will be made full page by javascript anyway </remarks>
        public override string ViewerBox_InlineStyle
        {
            get
            {
                return "margin-left:10px; margin-right:10px; width: 1200px;";
            }
        }
    }
}
