#region Using directives

using Microsoft.AspNetCore.Http;
using SobekCM.Core.Navigation;
using SobekCM.Tools;
using System.IO;

#endregion

namespace SobekCM.Library.MainWriters
{
    /// <summary> Abstract class which all main writer classes must extend </summary>
    public abstract class abstractMainWriter
    {
        /// <summary> Protected field contains the information specific to the current request </summary>
        protected RequestCache RequestSpecificValues;

        /// <summary> Context for this individual HTTP request </summary>
        protected HttpContext Context;

        /// <summary> Constructor for a new instance of the abstractMainWriter abstract class </summary>
        /// <param name="Context"> Context for this individual HTTP request </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        protected abstractMainWriter(HttpContext Context, RequestCache RequestSpecificValues)
        {
            this.Context = Context;
            this.RequestSpecificValues = RequestSpecificValues;
        }

        /// <summary> Gets the enumeration of the type of main writer </summary>
        public abstract Writer_Type_Enum Writer_Type { get; }


        /// <summary> Returns a flag indicating whether the navigation form should be included in the page </summary>
        /// <value> This value can be override by child classes, but by default this returns FALSE </value>
        public virtual bool Include_Navigation_Form
        {
            get
            {
                return false;
            }
        }


        /// <summary> Returns a flag indicating whether the additional place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form will be utilized 
        /// for the current request, or if it can be hidden. </summary>
        /// <value> This value can be override by child classes, but by default this returns FALSE </value>
        public virtual bool Include_Main_Place_Holder
        {
            get
            {
                return false;
            }
        }

        /// <summary> Returns a flag indicating whether the file upload specific holder in the itemNavForm form will be utilized 
        /// for the current request, or if it can be hidden. </summary>
        /// <value> This value can be override by child classes, but by default this returns FALSE </value>
        public virtual bool File_Upload_Possible
        {
            get
            {
                return false;
            }
        }


        /// <summary> Perform all the work of adding text directly to the response stream back to the web user </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public abstract void Write_Html(TextWriter Output, Custom_Tracer Tracer);

        /// <summary> Write any additional HTML into the main form area of the page </summary>
        /// <param name="Output"> Stream to which to write additional HTML </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        public virtual void Add_Controls(TextWriter Output, Custom_Tracer Tracer)
        {
            // Do nothing
        }
    }
}
