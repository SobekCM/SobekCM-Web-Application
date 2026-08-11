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
    /// <summary> Audio viewer prototyper, which is used to check to see if there is a locally loaded audio file,
    /// to create the link in the main menu, and to create the viewer itself if the user selects that option </summary>
    public class Audio_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the Audio_ItemViewer_Prototyper class </summary>
        public Audio_ItemViewer_Prototyper()
        {
            ViewerType = "AUDIO";
            ViewerCode = "audio";

            FileExtensions = new string[] { "MP3", "M4A" };
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
            if (FileExtensions == null)
                FileExtensions = new string[] { "MP3", "M4A" };

            // Check to see if there are any Audio files attached, but allow the configuration
            // to actually rule which files are necessary to be shown ( i.e., maybe 'WAV' will be an extension
            // in the future )
            return FileExtensions.Any(Extension => CurrentItem.Web.Contains_File_Extension(Extension));
        }

        /// <summary> Flag indicates if this viewer should be override on checkout </summary>
        /// <param name="CurrentItem"> Digital resource to examine to see if this viewer should really be overriden </param>
        /// <returns> TRUE always, since audio should never be shown if an item is checked out </returns>
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
        public virtual void Add_Menu_Items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IpRestricted)
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
                var menuItem = new Item_MenuItem(Localization_Gateway.Audio.Menu_Audio_Label(CurrentRequest.Language), null, null, url, ViewerCode);
                MenuItems.Add(menuItem);
            }
            else
            {
                // Add the item menu information using the custom level
                var menuItem = new Item_MenuItem(thisViewerInfo.Label, null, null, url, ViewerCode);
                MenuItems.Add(menuItem);
            }
        }

        /// <summary> Creates and returns the an instance of the <see cref="Audio_ItemViewer"/> class for showing a locally
        /// loaded audio file for a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="CurrentFlags"> Calculated flags for this particular requests, to avoid recalculation in different viewers </param>
        /// <returns> Fully built and initialized <see cref="Audio_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, RequestCache_RequestFlags CurrentFlags, HttpContext Context)
        {
            return new Audio_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer, FileExtensions, Context);
        }
    }


    /// <summary> Item viewer displays an audio file loaded locally with the digital resource embedded into the SobekCM window for listening. </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Audio_ItemViewer : abstractNoPaginationItemViewer
    {
        private readonly int audio;
        private readonly List<string> audioFileNames;
        private readonly List<string> audioLabels;

        /// <summary> Constructor for a new instance of the Audio_ItemViewer class, used to display an audio file loaded
        /// locally with the digital resource files </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="FileExtensions"> List of file extensions this audio viewer should show </param>
        public Audio_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, string[] FileExtensions, HttpContext context)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;
            this.Context = context;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;

            // Determine if a particular audio file was selected
            audio = 1;
            string audioParam = context?.Request.Query["audio"].ToString();
            if (!String.IsNullOrEmpty(audioParam))
            {
                int tryAudio;
                if (Int32.TryParse(audioParam, out tryAudio))
                {
                    if (tryAudio < 1)
                        tryAudio = 1;
                    audio = tryAudio;
                }
            }

            // Collect the list of audio files by stepping through each download page
            audioFileNames = new List<string>();
            audioLabels = new List<string>();
            foreach (BriefItem_FileGrouping downloadPage in BriefItem.Downloads)
            {
                foreach (BriefItem_File thisFileInfo in downloadPage.Files)
                {
                    string extension = thisFileInfo.File_Extension.Replace(".", "");
                    foreach (string thisPossibleFileExtension in FileExtensions)
                    {
                        if (String.Compare(extension, thisPossibleFileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            audioFileNames.Add(thisFileInfo.Name);
                            audioLabels.Add(downloadPage.Label);
                        }
                    }
                }
            }

            // Ensure the audio count wasn't too large
            if (audio > audioFileNames.Count)
                audio = 1;
        }

        /// <summary> CSS ID for the viewer viewport for this particular viewer </summary>
        /// <value> This always returns the value 'sbkAdiv_Viewer' </value>
        public override string ViewerBox_CssId
        {
            get { return "sbkAdiv_Viewer"; }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Audio_ItemViewer.Add_Main_Viewer_Section", "");

            // Add the HTML for the audio player
            Output.WriteLine("        <!-- AUDIO VIEWER OUTPUT -->");
            Output.WriteLine("          <td><div id=\"sbkFiv_ViewerTitle\">" + audioLabels[audio - 1] + "</div></td>");
            Output.WriteLine("        </tr>");

            if (audioFileNames.Count > 1)
            {
                Output.WriteLine("        <tr>");
                Output.WriteLine("          <td style=\"text-align:center;\">");
                string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
                string url_separator = url.IndexOf("?") > 0 ? "&audio=" : "?audio=";
                Output.WriteLine("            <select id=\"sbkAdiv_AudioSelect\" name=\"sbkAdiv_AudioSelect\" onchange=\"window.location.href='" + url + url_separator + "' + this.value;\">");

                for (int i = 0; i < audioFileNames.Count; i++)
                {
                    if (audio == i + 1)
                        Output.WriteLine("              <option value=\"" + (i + 1) + "\" selected=\"selected\">" + audioFileNames[i] + "</option>");
                    else
                        Output.WriteLine("              <option value=\"" + (i + 1) + "\">" + audioFileNames[i] + "</option>");
                }

                Output.WriteLine("            </select>");
                Output.WriteLine("          </td>");
                Output.WriteLine("        </tr>");
            }


            string audio_url = SobekFileSystem.Resource_Web_Uri(BriefItem, audioFileNames[audio - 1]);

            // MAKE THIS USE THE FILES.ASPX WEB PAGE if this is restricted (or dark)
            if ((BriefItem.Behaviors.Dark_Flag) || (BriefItem.Behaviors.IP_Restriction_Membership > 0))
                audio_url = CurrentRequest.Base_URL + "files/" + BriefItem.BibID + "/" + BriefItem.VID + "/" + audioFileNames[audio - 1];

            Output.WriteLine("        <tr>");
            Output.WriteLine("          <td id=\"sbkFiv_MainArea\">");
            Output.WriteLine("            <audio id=\"sbkAdiv_Audio\" src=\"" + audio_url + "\" controls autoplay></audio>");
            Output.WriteLine("          </td>");
            Output.WriteLine("        <!-- END AUDIO VIEWER OUTPUT -->");
        }

        /// <summary> Allows controls to be added directory to a place holder, rather than just writing to the output HTML stream </summary>
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the bulk of the item viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <remarks> This method does nothing, since nothing is added to the place holder as a control for this item viewer </remarks>
    }
}
