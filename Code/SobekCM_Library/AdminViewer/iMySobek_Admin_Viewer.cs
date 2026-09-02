#region Using directives

using SobekCM.Library.HTML;
using SobekCM.Tools;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Enumeration indicates which type of main menu navigation
    /// to include </summary>
    public enum MySobek_Admin_Included_Navigation_Enum : byte
    {
        /// <summary> Suppress the standard mySobek navigational elements.  This viewer will
        /// utilize its own navigational elements at the top of the page </summary>
        NONE = 1,

        /// <summary> Standard mySobek navigation menu </summary>
        Standard,

        /// <summary> Special navigation menu for the logon screen </summary>
        LogOn,

        /// <summary> Shows the administrative menu at the top </summary>
        Admin
    }

    /// <summary> Interface defines the required behavior for the mySobek and administrative viewers  </summary>
    public interface iMySobek_Admin_Viewer
    {

        /// <summary> Gets the collection of special behaviors which this admin or mySobek viewer
        /// requests from the main HTML subwriter. </summary>
        List<HtmlSubwriter_Behaviors_Enum> Viewer_Behaviors { get; }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <remarks> Abstract property must be implemented by all extending classes </remarks>
        string Web_Title { get; }

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        /// <remarks> Abstract property must be implemented by all extending classes </remarks>
        string Viewer_Icon { get; }

        /// <summary> Property indicates if this mySobek viewer can contain pop-up forms</summary>
        /// <remarks> If the mySobek viewer contains pop-up forms the overall page renders differently, 
        /// allowing for the blanket division and the popup forms near the top of the rendered HTML </remarks>
        ///<value> This defaults to FALSE but is overwritten by the mySobek viewers which use pop-up forms </value>
        bool Contains_Popup_Forms { get; }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of any form) </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> Abstract method must be implemented by all extending classes </remarks>
        void Write_HTML(TextWriter Output, Custom_Tracer Tracer);

        /// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized
        /// for the current request, or if it can be hidden/omitted. </summary>
        bool Upload_File_Possible { get; }

        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer);

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        string Container_CssClass { get; }

        /// <summary> Navigation type to be displayed (mostly used by the mySobek viewers) </summary>
        MySobek_Admin_Included_Navigation_Enum Standard_Navigation_Type { get; }

        /// <summary> Flag indicates if a user must be logged in to access this 
        /// admin or mySobek view.  </summary>
        bool Requires_Logged_In_User { get; }
    }
}