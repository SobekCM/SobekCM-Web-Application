#region Using directives

using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
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
        /// <param name="MainPlaceHolder"> Main place holder ( &quot;mainPlaceHolder&quot; ) in the itemNavForm form into which the the bulk of the result viewer's output is displayed</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Sorted tree with the results in hierarchical structure with volumes and issues under the titles and sorted by serial hierarchy </returns>
        void Add_HTML(PlaceHolder MainPlaceHolder, Custom_Tracer Tracer);
        
        /// <summary> Write any additional values within the HTML Head of the final served page </summary>
        /// <param name="Output"> Output stream currently within the HTML head tags </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if this should completely override the default added by the admin or mySobek viewer </returns>
        /// <remarks> By default this does nothing, but can be overwritten by all the individual html subwriters </remarks>
        bool Write_Within_HTML_Head(TextWriter Output, Custom_Tracer Tracer);
    }
}
