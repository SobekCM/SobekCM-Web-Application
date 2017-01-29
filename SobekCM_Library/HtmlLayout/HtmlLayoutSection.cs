using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Core.BriefItem;
using SobekCM.Library.HTML;
using SobekCM.Library.ItemViewer.Viewers;

namespace SobekCM.Library.HtmlLayout
{
    /// <summary> Enumerates the types of HTML layout sections used 
    /// within the layout objects created when the html template 
    /// is parsed </summary>
    public enum HtmlLayoutSectionTypeEnum : byte
    {
        /// <summary> This is a dynamic (non-special) section which may have writers
        /// configured to write within it </summary>
        Dynamic_Section,

        /// <summary> Static section of HTML which is directly from the HTML layout template </summary>
        Static_HTML,

        /// <summary> Special section contains the viewer, and often also allows control to be
        /// added to the page directly </summary>
        Viewer_Section
    }

    /// <summary> HTML layout section used when a HTML layout template is read </summary>
    public class HtmlLayoutSection
    {
        /// <summary> Type of this HTML layout section </summary>
        public HtmlLayoutSectionTypeEnum Type { get; set; }

        /// <summary> Name of this section, either from the HTML layout template directly
        /// or assigned while parsing </summary>
        public string Name { get; set; }

        /// <summary> Portion of static HTML, if this is a static HTML layout portion </summary>
        public string HTML { get; set; }
    }
}
