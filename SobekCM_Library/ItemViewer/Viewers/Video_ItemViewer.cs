﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.FileSystems;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Video viewer prototyper, which is used to check to see if there is a locally loaded video file,
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class Video_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the Video_ItemViewer_Prototyper class </summary>
        public Video_ItemViewer_Prototyper()
        {
            ViewerType = "VIDEO";
            ViewerCode = "video";

            FileExtensions = new string[] {"WEBM", "OGG", "MP4"};
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
            // If the FileExtensions IS null, that is an error
            if ( FileExtensions == null ) 
                FileExtensions = new string[] {"WEBM", "OGG", "MP4"};

            // Check to see if there are any Video files attached, but allow the configuration 
            // to actually rule which files are necessary to be shown ( i.e., maybe 'PDFA' will be an extension
            // in the future )
            return FileExtensions.Any(Extension => CurrentItem.Web.Contains_File_Extension(Extension));
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
        /// <param name="IpRestricted"> Flag indicates if this item is IP restricted AND if the current user is outside the ranges </param>
        /// <returns> TRUE if the user has access to use this viewer, otherwise FALSE </returns>
        public virtual bool Has_Access(BriefItemInfo CurrentItem, User_Object CurrentUser, bool IpRestricted)
        {
            return !IpRestricted;
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        /// <param name="IpRestricted"> Flag indicates if this item is IP restricted AND if the current user is outside the ranges </param>
        public virtual void Add_Menu_Items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IpRestricted )
        {
            // Get the URL for this
            string previous_code = CurrentRequest.ViewerCode;
            CurrentRequest.ViewerCode = ViewerCode;
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Allow the label to be implemented for this viewer
            BriefItem_BehaviorViewer thisViewerInfo = CurrentItem.Behaviors.Get_Viewer(ViewerCode);

            // If this is null, or no label, use the default
            if ((thisViewerInfo == null) || (String.IsNullOrWhiteSpace(thisViewerInfo.Label)))
            {
                // Add the item menu information using the default label
                Item_MenuItem menuItem = new Item_MenuItem("Video", null, null, url, ViewerCode);
                MenuItems.Add(menuItem);
            }
            else
            {
                // Add the item menu information using the custom level
                Item_MenuItem menuItem = new Item_MenuItem(thisViewerInfo.Label, null, null, url, ViewerCode);
                MenuItems.Add(menuItem);
            }
        }

        /// <summary> Creates and returns the an instance of the <see cref="Video_ItemViewer"/> class for showing a locally
        /// loaded video for a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="Video_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, RequestCache_RequestFlags CurrentFlags)
        {
            return new Video_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer, FileExtensions);
        }
    }


    /// <summary> Item viewer displays a video loaded locally with the digital resource embedded into the SobekCM window for viewing. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Video_ItemViewer : abstractNoPaginationItemViewer
    {
        private readonly int video;
        private readonly List<string> videoFileNames;
        private readonly List<string> videoLabels;

        /// <summary> Constructor for a new instance of the Video_ItemViewer class, used to display a video loaded
        /// locally with the digital resource files </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="FileExtensions"> List of file extensions this video viewer should show </param>
        public Video_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, string[] FileExtensions )
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;

            // Determine if a particular video was selected 
            video = 1;
            if (!String.IsNullOrEmpty(HttpContext.Current.Request.QueryString["video"]))
            {
                int tryVideo;
                if (Int32.TryParse(HttpContext.Current.Request.QueryString["video"], out tryVideo))
                {
                    if (tryVideo < 1)
                        tryVideo = 1;
                    video = tryVideo;
                }
            }

            // Collect the list of videos by stepping through each download page
            videoFileNames = new List<string>();
            videoLabels = new List<string>();
            foreach (BriefItem_FileGrouping downloadPage in BriefItem.Downloads)
            {
                foreach (BriefItem_File thisFileInfo in downloadPage.Files)
                {
                    string extension = thisFileInfo.File_Extension.Replace(".","");
                    foreach (string thisPossibleFileExtension in FileExtensions)
                    {
                        if (String.Compare(extension, thisPossibleFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            videoFileNames.Add(thisFileInfo.Name);
                            videoLabels.Add(downloadPage.Label);
                        }
                    }
                }
            }

            // Ensure the video count wasn't too large
            if (video > videoFileNames.Count)
                video = 1;
        }

        /// <summary> CSS ID for the viewer viewport for this particular viewer </summary>
        /// <value> This always returns the value 'sbkViiv_Viewer' </value>
        public override string ViewerBox_CssId
        {
            get { return "sbkViiv_Viewer"; }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Video_ItemViewer.Add_Main_Viewer_Section", "");
            }

            // Add the HTML for the image
            Output.WriteLine("        <!-- VIDEO VIEWER OUTPUT -->");
            Output.WriteLine("          <td><div id=\"sbkFiv_ViewerTitle\">" + videoLabels[video - 1] + "</div></td>");
            Output.WriteLine("        </tr>");

            if (videoFileNames.Count > 1)
            {
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td style=\"text-align:center;\">");
                string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                Output.WriteLine("            <select id=\"sbkViv_VideoSelect\" name=\"sbkViv_VideoSelect\" onchange=\"item_jump_video('" + url + "');\">");

                for (int i = 0; i < videoFileNames.Count; i++)
                {
                    if (video == i + 1)
                        Output.WriteLine("              <option value=\"" + (i + 1) + "\" selected=\"selected\">" + videoFileNames[i] + "</option>");
                    else
                        Output.WriteLine("              <option value=\"" + (i + 1) + "\">" + videoFileNames[i] + "</option>");
                }

                Output.WriteLine("            </select>");
                Output.WriteLine("          </td>");
                Output.WriteLine("        </tr>");
            }


            string video_url = SobekFileSystem.Resource_Web_Uri(BriefItem, videoFileNames[video - 1]);

            // MAKE THIS USE THE FILES.ASPX WEB PAGE if this is restricted (or dark)
            if ((BriefItem.Behaviors.Dark_Flag) || (BriefItem.Behaviors.IP_Restriction_Membership > 0))
                video_url = CurrentRequest.Base_URL + "files/" + BriefItem.BibID + "/" + BriefItem.VID + "/" + videoFileNames[video - 1];

            Output.WriteLine("        <tr>");
            Output.WriteLine("          <td id=\"sbkFiv_MainArea\">");
            Output.WriteLine("            <video id=\"sbkViv_Movie\" src=\"" + video_url + "\" controls autoplay></video>");
            Output.WriteLine("          </td>");
            Output.WriteLine("        <!-- END VIDEO VIEWER OUTPUT -->");
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
