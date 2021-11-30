﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using SobekCM.Core.BriefItem;
using SobekCM.Core.Navigation;
using SobekCM.Core.Users;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Library.ItemViewer.Menu;
using SobekCM.Tools;

namespace SobekCM.Library.ItemViewer.Viewers
{
    /// <summary> Restricted item viewer prototyper, which creates the special restricted viewer itself if the user selects that option
    /// which is shown when an item can not be displayed due to IP restrictions </summary>
    public class Restricted_ItemViewer_Prototyper : iItemViewerPrototyper
    {
        /// <summary> Constructor for a new instance of the Restricted_ItemViewer_Prototyper class </summary>
        public Restricted_ItemViewer_Prototyper()
        {
            ViewerType = "RESTRICTED";
            ViewerCode = "restricted";
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
            // This can only be shown when the item is DARK or IP restricted from this user
            return (( CurrentItem.Behaviors.Dark_Flag) || ( IsRestricted ));
        }

        /// <summary> Gets the menu items related to this viewer that should be included on the main item (digital resource) menu </summary>
        /// <param name="CurrentItem"> Digital resource object, which can be used to ensure if and how this viewer should appear 
        /// in the main item (digital resource) menu </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="MenuItems"> List of menu items, to which this method may add one or more menu items </param>
        /// <param name="IsRestricted"> Flag indicates if this item is restricted AND the current user is outside the ranges or not in the proper groups</param>
        public virtual void Add_Menu_Items(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, List<Item_MenuItem> MenuItems, bool IsRestricted  )
        {
            // If not IP restricted, then do nothing
            if (!IsRestricted) return;

            // Get the URL for this
            string previous_code = CurrentRequest.ViewerCode;
            CurrentRequest.ViewerCode = ViewerCode;
            string url = UrlWriterHelper.Redirect_URL(CurrentRequest);
            CurrentRequest.ViewerCode = previous_code;

            // Add the item menu information
            Item_MenuItem menuItem = new Item_MenuItem("Restricted", null, null, url, ViewerCode);
            MenuItems.Add(menuItem);
        }

        /// <summary> Creates and returns the an instance of the <see cref="Restricted_ItemViewer"/> class which is used to 
        /// display the restricted message for materials that are IP restricted </summary>
        /// <param name="CurrentItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Fully built and initialized <see cref="Restricted_ItemViewer"/> object </returns>
        /// <remarks> This method is called whenever a request requires the actual viewer to be created to render the HTML for
        /// the digital resource requested.  The created viewer is then destroyed at the end of the request </remarks>
        public virtual iItemViewer Create_Viewer(BriefItemInfo CurrentItem, User_Object CurrentUser, Navigation_Object CurrentRequest, Custom_Tracer Tracer)
        {
            return new Restricted_ItemViewer(CurrentItem, CurrentUser, CurrentRequest);
        }
    }

    /// <summary> Item viewer displays the applicable restricted message for a digital resource which is currently
    /// restricted due to IP restrictions </summary>
    /// <remarks> This class extends the abstract class <see cref="abstractNoPaginationItemViewer"/> and implements the 
    /// <see cref="iItemViewer" /> interface. </remarks>
    public class Restricted_ItemViewer : abstractNoPaginationItemViewer
    {
        /// <summary> Constructor for a new instance of the Restricted_ItemViewer class, used display
        ///  the applicable restricted message for a digital resource which is currently restricted due to IP restrictions </summary>
        /// <param name="BriefItem"> Digital resource object </param>
        /// <param name="CurrentUser"> Current user, who may or may not be logged on </param>
        /// <param name="CurrentRequest"> Information about the current request </param>
        public Restricted_ItemViewer(BriefItemInfo BriefItem, User_Object CurrentUser, Navigation_Object CurrentRequest)
        {
            // Save the arguments for use later
            this.BriefItem = BriefItem;
            this.CurrentUser = CurrentUser;
            this.CurrentRequest = CurrentRequest;

            // Set the behavior properties to the empy behaviors ( in the base class )
            Behaviors = EmptyBehaviors;
        }

        /// <summary> CSS ID for the viewer viewport for this particular viewer </summary>
        /// <value> This always returns the value 'sbkRiv_Viewer' </value>
        public override string ViewerBox_CssId
        {
            get { return "sbkRiv_Viewer"; }
        }

        /// <summary> Write the item viewer main section as HTML directly to the HTTP output stream </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("Restricted_ItemViewer.Write_Main_Viewer_Section", "");
            }

            // Check to see if this is IP restricted
            string restriction_message = "This item is not restricted";
            if (BriefItem.Behaviors.Dark_Flag)
            {
                restriction_message = "<table style=\"width:100%;\"> <tr style=\"text-align:center\"><td style=\"font-size:1.8em; font-weight: bold; color:white\"><br />This item is set to be DARK and resource files cannot be displayed<br /><br /></td></tr></table>";
            }
            else if (BriefItem.Behaviors.IP_Restriction_Membership > 0)
            {
                if (HttpContext.Current != null)
                {
                    int user_mask = (int)HttpContext.Current.Session["IP_Range_Membership"];
                    int comparison = BriefItem.Behaviors.IP_Restriction_Membership & user_mask;
                    if (comparison == 0)
                    {
                        int restriction = BriefItem.Behaviors.IP_Restriction_Membership;
                        int restriction_counter = 1;
                        while (restriction % 2 != 1)
                        {
                            restriction = restriction >> 1;
                            restriction_counter++;
                        }
                        if (Engine_ApplicationCache_Gateway.IP_Restrictions[restriction_counter] != null)
                            restriction_message = Engine_ApplicationCache_Gateway.IP_Restrictions[restriction_counter].Item_Restricted_Statement;
                        else
                            restriction_message = "Restricted Item";
                    }
                }
            }

            // Replace item URL in the restricted message
            CurrentRequest.ViewerCode = string.Empty;
            string msg = restriction_message.Replace("<%ITEMURL%>", UrlWriterHelper.Redirect_URL(CurrentRequest));

            Output.WriteLine("<td style=\"text-align:left;\" id=\"sbkRes_MainArea\">" + msg + "</td>");

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
