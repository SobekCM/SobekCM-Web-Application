using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Tools;

namespace SobekCM.Library.HTML
{
    public class Empty_HtmlSubwriter : abstractHtmlSubwriter
    {


        /// <summary> Constructor for a new instance of the <see cref="Empty_HtmlSubwriter"/> class </summary>
        /// <param name="RequestSpecificValues">All the necessary, non-global data specific to the current request</param>
        public Empty_HtmlSubwriter(RequestCache RequestSpecificValues) : base(RequestSpecificValues)
        {
            // Do nothing
        }

        public override bool Write_HTML(TextWriter Output, Custom_Tracer Tracer)
        {
            Output.WriteLine("<div id=\"empty\" />");
            return true;
        }

        public override List<HtmlSubwriter_Behaviors_Enum> Subwriter_Behaviors
        {
            get
            {
                return new List<HtmlSubwriter_Behaviors_Enum> {HtmlSubwriter_Behaviors_Enum.Omit_Main_Navigation_Form, HtmlSubwriter_Behaviors_Enum.Omit_Main_PlaceHolder };
            }
        }
    }
}
