#region Using directives

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library.HTML;
using SobekCM.Tools;
using System.Collections.Generic;
using System.IO;

#endregion

namespace SobekCM.Library.AdminViewer
{
    /// <summary> Abstract base class extended by all admin viewer objects </summary>
    public abstract class abstract_AdminViewer : iMySobek_Admin_Viewer
    {

        /// <summary> Empty list of behaviors, returned by default </summary>
        /// <remarks> This just prevents an empty set from having to be created over and over </remarks>
        protected static List<HtmlSubwriter_Behaviors_Enum> emptybehaviors = new List<HtmlSubwriter_Behaviors_Enum>();

        /// <summary> Protected field contains the information specific to the current request </summary>
        protected RequestCache RequestSpecificValues;

        /// <summary> HTTP context for the current request </summary>
        protected HttpContext Context;

        /// <summary> Constructor for a new instance of the abstract_AdminViewer class </summary>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        protected abstract_AdminViewer(RequestCache RequestSpecificValues, HttpContext Context)
        {
            this.RequestSpecificValues = RequestSpecificValues;
            this.Context = Context;
        }

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        /// <remarks> Abstract property must be implemented by all extending classes </remarks>
        public abstract string Web_Title { get; }

        /// <summary> Gets the URL for the icon related to this administrative task </summary>
        /// <remarks> Abstract property must be implemented by all extending classes </remarks>
        public abstract string Viewer_Icon { get; }

        /// <summary> Property indicates if this mySobek viewer can contain pop-up forms</summary>
        /// <remarks> If the mySobek viewer contains pop-up forms the overall page renders differently,
        /// allowing for the blanket division and the popup forms near the top of the rendered HTML </remarks>
        ///<value> This defaults to FALSE but is overwritten by the mySobek viewers which use pop-up forms </value>
        public virtual bool Contains_Popup_Forms
        {
            get { return false; }
        }

        /// <summary> Gets the collection of special behaviors which this admin or mySobek viewer
        /// requests from the main HTML subwriter. </summary>
        /// <remarks> By default, this returns an empty list </remarks>
        public virtual List<HtmlSubwriter_Behaviors_Enum> Viewer_Behaviors
        {
            get
            {
                if (Contains_Popup_Forms)
                {
                    return new List<HtmlSubwriter_Behaviors_Enum>
                    {
                        HtmlSubwriter_Behaviors_Enum.Suppress_Header,
                        HtmlSubwriter_Behaviors_Enum.Suppress_Footer
                    };
                }

                return emptybehaviors;
            }
        }

        /// <summary> Add the HTML to be displayed in the main SobekCM viewer area (outside of any form) </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer</param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <remarks> Abstract method must be implemented by all extending classes </remarks>
        public abstract void Write_HTML(TextWriter Output, Custom_Tracer Tracer);

        /// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized
        /// for the current request, or if it can be hidden/omitted. </summary>
        /// <value> By default, this returns FALSE.</value>
        /// <remarks> This can be overriden in base classes that extend this abstract class </remarks>
        public virtual bool Upload_File_Possible { get { return false; } }

        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        public virtual bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer)
        {
            return false;
        }

        /// <summary> Gets the CSS class of the container that the page is wrapped within </summary>
        /// <value> By default, this returns NULL, which would use the base, or default container </value>
        /// <remarks> This can be overriden in base classes that extend this abstract class </remarks>
        public virtual string Container_CssClass { get { return null; } }

        /// <summary> Navigation type to be displayed (mostly used by the mySobek viewers) </summary>
        public virtual MySobek_Admin_Included_Navigation_Enum Standard_Navigation_Type { get { return MySobek_Admin_Included_Navigation_Enum.Admin; } }

        /// <summary> Flag indicates if a user must be logged in to access this
        /// admin or mySobek view.  </summary>
        /// <value> This returns TRUE by default, but can be overriden by classes that extend this abstract class </value>
        public virtual bool Requires_Logged_In_User { get { return true; } }

        protected void Write_ItemNavForm_Opening(TextWriter Output)
        {
            string formAction = Context.Items[RequestCache_Keys.OriginalUrl]?.ToString() ?? Context.Request.GetDisplayUrl();
            string enctype = Upload_File_Possible ? " enctype=\"multipart/form-data\"" : "";
            Output.Write($"<form id=\"itemNavForm\" name=\"itemNavForm\" action=\"{formAction}\" method=\"post\"{enctype}>");
        }

        protected void Write_ItemNavForm_Closing(TextWriter Output)
        {
            Output.Write("</form>");
        }
    }
}
