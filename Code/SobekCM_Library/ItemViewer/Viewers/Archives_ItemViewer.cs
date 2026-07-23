using Microsoft.AspNetCore.Http;
using SobekCM.Core.Archiving;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.Database;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Tools;
using System.Collections.Generic;
using System.IO;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Archives item viewer prototyper, which is used to check to see if a user has access to view the
    /// cold-storage archiving history for a digital resource, and to create the viewer itself if the user selects
    /// that option </summary>
    public class Archives_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the Archives_ItemViewer_Prototyper class </summary>
        public Archives_ItemViewer_Prototyper()
        {
            ViewerType = "ARCHIVES";
            ViewerCode = "archives";
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
        /// <returns> FALSE always, since this viewer doesn't touch the actual checked-out digital resource </returns>
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
            // If there is no user (or they aren't logged in) then obviously, they can't view this
            if ((CurrentUser == null) || (!CurrentUser.LoggedOn))
            {
                return false;
            }

            // Only system or host administrators can see the archiving history
            return (CurrentUser.Is_Host_Admin) || (CurrentUser.Is_System_Admin);
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
            // Do nothing since this is already handled and added to the menu by the MANAGE MENU item viewer and INTERNAL header
        }

        /// <summary> Creates and returns the an instance of the <see cref="Archives_ItemViewer"/> class for showing the
        /// cold-storage archiving history for a digital resource during execution of a single HTTP request. </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <param name="CurrentFlags"> Calculated flags for this particular requests, to avoid recalculation in different viewers </param>
        /// <returns> Fully built and initialized <see cref="Archives_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer, RequestCache_RequestFlags CurrentFlags, HttpContext Context)
        {
            return new Archives_ItemViewer(CurrentItem, CurrentUser, CurrentRequest, Tracer);
        }
    }

    /// <summary> Item viewer displays the cold-storage archiving history (files, snapshots, and stored copies)
    /// for a digital resource </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Archives_ItemViewer : abstractNoPaginationItemViewer
    {
        private readonly Archived_Files archivedFiles;

        /// <summary> Constructor for a new instance of the Archives_ItemViewer class, used to display
        /// the cold-storage archiving history for a digital resource </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public Archives_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;

            // Get the archives information
            Tracer.Add_Trace("Archives_ItemViewer.Constructor", "Try to pull the archiving history for this item");
            archivedFiles = Engine_Database.Get_Archived_Files(BriefItem.Web.ItemID, Tracer);
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
            Tracer.Add_Trace("Archives_ItemViewer.Write_Main_Viewer_Section", "");

            // Add the HTML for the archives table
            Output.WriteLine("<!-- ARCHIVES ITEM VIEWER OUTPUT -->");

            // Start the citation table
            Output.WriteLine("  <td align=\"left\"><span class=\"sbkTrk_ViewerTitle\">Archiving Information</span></td>");
            Output.WriteLine("</tr>");
            Output.WriteLine("<tr>");
            Output.WriteLine("  <td class=\"sbkTrk_MainArea\">");

            Tracer.Add_Trace("Archives_ItemViewer.Write_Main_Viewer_Section", "Displaying archiving history");
            if ((archivedFiles == null) || (archivedFiles.Files == null) || (archivedFiles.Files.Count == 0))
            {
                Output.WriteLine("<br /><br /><br /><center><strong>ITEM HAS NO ARCHIVE INFORMATION</strong></center><br /><br /><br />");
            }
            else
            {
                Output.WriteLine("<br />");
                Output.WriteLine("<table border=\"1px\" cellpadding=\"1px\" cellspacing=\"0px\" rules=\"cols\" frame=\"void\" bordercolor=\"#e7e7e7\" width=\"100%\">");
                Output.WriteLine("  <tr align=\"center\" bgcolor=\"#0022a7\" height=\"25px\"><td colspan=\"6\"><span style=\"color: White\"><b>ARCHIVED FILE INFORMATION</b></span></td></tr>");
                Output.WriteLine("  <tr align=\"left\" bgcolor=\"#7d90d5\" height=\"25px\">");
                Output.WriteLine("    <th><span style=\"color: White\">FILENAME</span></th>");
                Output.WriteLine("    <th><span style=\"color: White\">SIZE</span></th>");
                Output.WriteLine("    <th><span style=\"color: White\">ORIGINAL CREATION DATE</span></th>");
                Output.WriteLine("    <th><span style=\"color: White\">STORED DATE</span></th>");
                Output.WriteLine("    <th><span style=\"color: White\">STATUS</span></th>");
                Output.WriteLine("    <th><span style=\"color: White\">LOCATION</span></th>");
                Output.WriteLine("  </tr>");

                foreach (Archived_File thisFile in archivedFiles.Files)
                {
                    Output.WriteLine("  <tr height=\"25px\" >");
                    Output.WriteLine("    <td>" + thisFile.FileName + "</td>");
                    Output.WriteLine("    <td>" + friendly_File_Size(thisFile.FileSize) + "</td>");
                    Output.WriteLine("    <td>" + thisFile.OriginalCreationDate.ToShortDateString() + "</td>");
                    Output.WriteLine("    <td>" + thisFile.StoredDate.ToShortDateString() + "</td>");
                    Output.WriteLine("    <td>" + thisFile.Status + "</td>");
                    Output.WriteLine("    <td>" + thisFile.LocationName + "</td>");
                    Output.WriteLine("  </tr>");
                    Output.WriteLine("  <tr><td bgcolor=\"#e7e7e7\" colspan=\"6\"></td></tr>");
                }

                Output.WriteLine("</table>");
            }

            Output.WriteLine("<br /> <br />");

            Output.WriteLine("  </td>");
            Output.WriteLine("  <!-- END ARCHIVES VIEWER OUTPUT -->");
        }

        /// <summary> Formats a file size, in bytes, as a friendly string using the largest unit ( KB, MB, GB ) that keeps the value at least 1 </summary>
        /// <param name="SizeInBytes"> File size, in bytes </param>
        /// <returns> Friendly, human-readable file size string </returns>
        private static string friendly_File_Size(long SizeInBytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (SizeInBytes >= GB)
                return (SizeInBytes / (double)GB).ToString("0.##") + " GB";

            if (SizeInBytes >= MB)
                return (SizeInBytes / (double)MB).ToString("0.##") + " MB";

            if (SizeInBytes >= KB)
                return (SizeInBytes / (double)KB).ToString("0.##") + " KB";

            return SizeInBytes + " bytes";
        }
    }
}
