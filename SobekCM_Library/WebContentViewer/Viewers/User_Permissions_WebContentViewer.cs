﻿using System.IO;
using SobekCM.Core.Navigation;
using SobekCM.Library.Settings;
using SobekCM.Tools;

namespace SobekCM.Library.WebContentViewer.Viewers
{
    /// <summary> Web content viewer shows all of the user permissions, either assigned individually to a user or through a user group,
    /// affecting the ability of users to perform changes against this web content page </summary>
    /// <remarks> This viewer extends the <see cref="abstractWebContentViewer" /> abstract class and implements the <see cref="iWebContentViewer"/> interface. </remarks>
    public class User_Permissions_WebContentViewer : abstractWebContentViewer
    {
        /// <summary>  Constructor for a new instance of the User_Permissions_WebContentViewer class  </summary>
        /// <param name="RequestSpecificValues">  All the necessary, non-global data specific to the current request  </param>
        public User_Permissions_WebContentViewer(RequestCache RequestSpecificValues) : base ( RequestSpecificValues )
        {
            
        }


        /// <summary> Gets the type of specialized web content viewer </summary>
        public override WebContent_Type_Enum Type { get { return WebContent_Type_Enum.Permissions; }}

        /// <summary> Title for the page that displays this viewer, this is shown in the search box at the top of the page, just below the banner </summary>
        public override string Viewer_Title
        {
            get { return "Web Content User Permissions"; }
        }

        /// <summary> Gets the URL for the icon related to this web content viewer task </summary>
        public override string Viewer_Icon
        {
            get { return Static_Resources.User_Permission_Img; }
        }

        /// <summary> Add the HTML to be displayed </summary>
        /// <param name="Output"> Textwriter to write the HTML for this viewer </param>
        /// <param name="Tracer">Trace object keeps a list of each method executed and important milestones in rendering</param>
        public override void Add_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            if (Tracer != null)
            {
                Tracer.Add_Trace("User_Permissions_WebContentViewer.Add_HTML", "No html added");
            }

            Output.WriteLine("IN THE USER_PERMISSIONS_WEBCONTENTVIEWER");
        }
    }
}
