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

        /// <summary> Perform all the work of adding the full body content to the response stream back to the web user </summary>
        /// <param name="Output"> Stream to which to write the text for this main writer </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        public abstract void Write_Body(TextWriter Output, Custom_Tracer Tracer);
    }
}
