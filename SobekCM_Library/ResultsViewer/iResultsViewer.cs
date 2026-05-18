#region Using directives

using System.Collections.Generic;
using System.IO;
using SobekCM.Core.Results;
using SobekCM.Tools;

#endregion

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Interface which all results viewer objects must implement </summary>
    public interface iResultsViewer
    {
        /// <summary> All the necessary, non-global data specific to the current request </summary>
        RequestCache RequestSpecificValues { get; set; }

        /// <summary> Statistics about the results to display including the facets </summary>
        Search_Results_Statistics ResultsStats { get; set; }

        /// <summary> Actual pages of results  </summary>
        List<iSearch_Title_Result> PagedResults { get; set; }

        /// <summary> Flag indicates if this result view is sortable </summary>
        bool Sortable { get; }

        /// <summary> Gets the total number of results to display </summary>
        int Total_Results { get; }

        /// <summary> Adds the controls for this result viewer to the place holder on the main form </summary>
        /// <param name="Output"> TextWriter to write HTML output </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        void Add_HTML(TextWriter Output, Custom_Tracer Tracer);
        
        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer);
    }
}
