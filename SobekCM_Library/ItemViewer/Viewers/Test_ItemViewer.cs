﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using SobekCM.Library.HTML;

namespace SobekCM.Library.ItemViewer.Viewers
{
    public class Test_ItemViewer : abstractItemViewer
    {
        public override ItemViewer_Type_Enum ItemViewer_Type
        {
            get { return ItemViewer_Type_Enum.Test; }
        }


        /// <summary> Stream to which to write the HTML for this subwriter  </summary>
        /// <param name="Output"> Response stream for the item viewer to write directly to </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public override void Write_Main_Viewer_Section(TextWriter Output, Custom_Tracer Tracer)
        {
            // Start to build the response
            StringBuilder mapperBuilder = new StringBuilder();

            //page content
            Output.WriteLine("<td>");

            //used to force doctype html5 and css3
            //Output.WriteLine("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">");

            //standard css
            Output.WriteLine("<link rel=\"stylesheet\" href=\"" + CurrentMode.Base_URL + "default/jquery-ui.css\"/>");
            Output.WriteLine("<link rel=\"stylesheet\" href=\"" + CurrentMode.Base_URL + "default/jquery-searchbox.css\"/>");

            //custom css
            Output.WriteLine("<link rel=\"stylesheet\" href=\"" + CurrentMode.Base_URL + "default/SobekCM_Mapper_Theme_Default.css\"/>");
            Output.WriteLine("<link rel=\"stylesheet\" href=\"" + CurrentMode.Base_URL + "default/SobekCM_Mapper_Layout_Default.css\"/>");
            Output.WriteLine("<link rel=\"stylesheet\" href=\"" + CurrentMode.Base_URL + "default/SobekCM_Mapper_Other.css\"/>");


           Output.WriteLine(" <div id=\"container\" style=\"background:yellow;\"> ");
      //      Output.WriteLine("     <div id=\"container_pane_1\"> ");

      //      Output.WriteLine("     </div> ");
      //      Output.WriteLine("  ");
      //      Output.WriteLine("     <div id=\"container_toolbarGrabber\"> ");
      //      Output.WriteLine("         <div id=\"content_toolbarGrabber\"></div> ");
      //      Output.WriteLine("     </div>     ");
      //      Output.WriteLine("  ");
      //      Output.WriteLine("     <div id=\"container_pane_2\"> ");
      //      Output.WriteLine("          ");
      ////      Output.WriteLine("         <div id=\"googleMap\"></div> ");
      //      Output.WriteLine("     </div> ");
            Output.WriteLine(" </div> ");
            

            //end of custom content
            Output.WriteLine("</td>");


        }

        /// <summary> Gets the collection of body attributes to be included 
        /// within the HTML body tag (usually to add events to the body) </summary>
        /// <param name="Body_Attributes"> List of body attributes to be included </param>
        public override void Add_ViewerSpecific_Body_Attributes(List<Tuple<string, string>> Body_Attributes)
        {
            //Body_Attributes.Add(new Tuple<string, string>("onload", "load();"));
        }

        /// <summary> Gets the collection of special behaviors which this item viewer
        /// requests from the main HTML subwriter. </summary>
        public override List<HtmlSubwriter_Behaviors_Enum> ItemViewer_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum>
                    {
                        HtmlSubwriter_Behaviors_Enum.Item_Subwriter_NonWindowed_Mode,
                        HtmlSubwriter_Behaviors_Enum.Suppress_Footer,
                        HtmlSubwriter_Behaviors_Enum.Suppress_Internal_Header,
                        HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Item_Menu,
                        HtmlSubwriter_Behaviors_Enum.Item_Subwriter_Suppress_Left_Navigation_Bar
                    };
            }
        }

        /// <summary> Gets the number of pages for this viewer </summary>
        /// <value> This is a single page viewer, so this property always returns the value 1</value>
        public override int PageCount
        {
            get
            {
                return 1;
            }
        }

        public override ItemViewer_PageSelector_Type_Enum Page_Selector
        {
            get
            {
                return ItemViewer_PageSelector_Type_Enum.NONE;
            }
        }
    }
}