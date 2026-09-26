using SobekCM.Core.Navigation;
using SobekCM.Core.Navigation;
using SobekCM.Core.Results;
using SobekCM.Core.UI_Configuration.Viewers;
using SobekCM.Library.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Static class is a factory that generates and returns the requested item viewer object 
    /// which implements the <see cref="SobekCM.Library.ResultsViewer.iResultsViewer"/> interface.</summary>
    public static class ResultsViewer_Factory
    {
        /// <summary> Gets whether the map view has anything to draw for this page of results: this is a coordinate
        /// search, and at least one result on the page has coordinates </summary>
        /// <param name="CurrentMode"> Current navigation, which says whether this is a coordinate search </param>
        /// <param name="PagedResults"> The current page of results </param>
        /// <remarks> The one test behind every map decision on a results page -- whether the map view is offered in
        /// the menu tabs and view icons (<see cref="Get_Offered_Result_Views"/>), and whether a request for the map
        /// view falls back to the brief view (PagedResults_HtmlHelper) -- so they can never disagree, e.g. offer a
        /// MAP VIEW tab that only leads back to the brief view. It looks at the current page only, since that's all
        /// a results page has; another page of the same search can decide differently. </remarks>
        public static bool Map_View_Available(Navigation_Object CurrentMode, List<iSearch_Title_Result> PagedResults)
        {
            if ((CurrentMode == null) || (String.IsNullOrEmpty(CurrentMode.Coordinates)))
                return false;

            return Has_Mappable_Results(PagedResults);
        }

        /// <summary> Gets whether at least one result on this page has coordinates the map view can draw </summary>
        /// <param name="PagedResults"> The current page of results </param>
        /// <remarks> The page-data half of <see cref="Map_View_Available"/>, without the coordinate-search condition --
        /// used by the brief-view fallback, which also covers a collection whose default result view is the map. </remarks>
        public static bool Has_Mappable_Results(List<iSearch_Title_Result> PagedResults)
        {
            return (PagedResults != null) && (PagedResults.Exists(Result => !String.IsNullOrEmpty(Result.Spatial_Coordinates)));
        }

        /// <summary> Gets the result view types to offer on a results page -- the collection's own list, with the map
        /// view added or removed according to <see cref="Map_View_Available"/> </summary>
        /// <param name="CollectionViews"> Result view types the collection (or other hierarchy object) offers </param>
        /// <param name="CurrentMode"> Current navigation, which says whether this is a coordinate search </param>
        /// <param name="PagedResults"> The current page of results </param>
        /// <returns> View types, in the collection's order, with the map view last when it was added here </returns>
        /// <remarks> A coordinate search opens in the map view whatever the collection's list says, so when there's
        /// something to map it's offered even by a collection that never listed it. When there isn't, it's left out
        /// even by one that did, since the page falls back to the brief view anyway. Used by both the results menu
        /// (MainMenus_HtmlHelper) and the view icons (PagedResults_HtmlHelper). </remarks>
        public static List<string> Get_Offered_Result_Views(List<string> CollectionViews, Navigation_Object CurrentMode, List<iSearch_Title_Result> PagedResults)
        {
            List<string> views = (CollectionViews != null) ? new List<string>(CollectionViews) : new List<string>();

            ResultsSubViewerConfig mapConfig = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByCode("map");
            if (mapConfig == null)
                return views;

            bool listed = views.Exists(View => String.Equals(View, mapConfig.ViewerType, StringComparison.OrdinalIgnoreCase));
            if (Map_View_Available(CurrentMode, PagedResults))
            {
                if ((mapConfig.Enabled) && (!listed))
                    views.Add(mapConfig.ViewerType);
            }
            else if (listed)
            {
                views.RemoveAll(View => String.Equals(View, mapConfig.ViewerType, StringComparison.OrdinalIgnoreCase));
            }

            return views;
        }

        /// <summary> Gets the indicated results viewer, by results viewer code, usually from the URL </summary>
        /// <param name="ViewerCode"> Code which indicates which results viewer </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ResultsStats"> Statistics about the results to display including the facets </param>
        /// <param name="PagedResults"> Actual pages of results </param>
        /// <returns> Either the results vieweer, or NULL </returns>
        public static iResultsViewer Get_Results_Viewer(string ViewerCode, RequestCache RequestSpecificValues, Search_Results_Statistics ResultsStats, List<iSearch_Title_Result> PagedResults)
        {
            // Determine the actual viewercode
            string viewerCode = ViewerCode;
            if (RequestSpecificValues.Current_Mode.Mode == Display_Mode_Enum.My_Sobek)
            {
                if (!String.IsNullOrEmpty(RequestSpecificValues.Current_Mode.ViewerCode))
                    viewerCode = RequestSpecificValues.Current_Mode.ViewerCode;
                else
                    viewerCode = "brief";
            }

            // Get the match by viewer code
            ResultsSubViewerConfig config = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByCode(viewerCode);

            // If no match, just try by viewer type then
            if (config == null)
            {
                config = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByType(viewerCode);
            }

            // If this is still NULL, just try to get the brief view
            if (config == null)
            {
                config = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByCode("brief");
            }

            // If still null, return NULL
            if (config == null)
                return null;

            // Was an assembly indicated
            if (String.IsNullOrEmpty(config.Assembly))
            {
                iResultsViewer returnValue = null;

                // Return a standard class
                switch (config.Class)
                {
                    case "SobekCM.Library.ResultsViewer.Bookshelf_ResultsViewer":
                        returnValue = new Bookshelf_View_ResultsViewer();
                        break;

                    case "SobekCM.Library.ResultsViewer.Brief_ResultsViewer":
                        returnValue = new Brief_ResultsViewer();
                        break;

                    case "SobekCM.Library.ResultsViewer.Google_Map_ResultsViewer":
                        returnValue = new Google_Map_ResultsViewer();
                        break;

                    case "SobekCM.Library.ResultsViewer.Table_ResultsViewer":
                        returnValue = new Table_ResultsViewer();
                        break;

                    case "SobekCM.Library.ResultsViewer.Thumbnail_ResultsViewer":
                        returnValue = new Thumbnail_ResultsViewer();
                        break;

                    case "SobekCM.Library.ResultsViewer.No_Results_ResultsViewer":
                        returnValue = new No_Results_ResultsViewer();
                        break;
                }

                if (returnValue == null)
                {
                    // If it made it here, there is no assembly, but it is an unexpected type.  
                    // Just create it from the same assembly then
                    try
                    {
                        Assembly dllAssembly = Assembly.GetCallingAssembly();
                        Type prototyperType = dllAssembly.GetType(config.Class);
                        returnValue = (iResultsViewer)Activator.CreateInstance(prototyperType);
                    }
                    catch (Exception ee)
                    {
                        RequestSpecificValues.Tracer.Add_Trace("ResultsViewer_Factory", "Exception when creating a results viewer from the current assembly via reflection");
                        RequestSpecificValues.Tracer.Add_Trace("ResultsViewer_Factory", ee.Message);

                        // Not sure exactly what to do here, honestly
                        return null;
                    }
                }

                // If a results viewer was created, finish the construction and return it
                if (returnValue != null)
                {
                    returnValue.RequestSpecificValues = RequestSpecificValues;
                    returnValue.ResultsStats = ResultsStats;
                    returnValue.PagedResults = PagedResults;
                    return returnValue;
                }

                // Return value must be NULL
                RequestSpecificValues.Tracer.Add_Trace("ResultsViewer_Factory", "NULL value when creating a results viewer from the current assembly (via reflection)");
                return null;
            }


            // An assembly was indicated
            try
            {
                // Try to find the file/path for this assembly then
                Assembly dllAssembly = null;
                string assemblyFilePath = UI_ApplicationCache_Gateway.Configuration.Extensions.Get_Assembly(config.Assembly);
                if (assemblyFilePath != null)
                {
                    dllAssembly = Assembly.LoadFrom(assemblyFilePath);
                }
                Type prototyperType = dllAssembly.GetType(config.Class);
                iResultsViewer returnObj = (iResultsViewer)Activator.CreateInstance(prototyperType);

                // If a results viewer was created, finish the construction and return it
                if (returnObj != null)
                {
                    returnObj.RequestSpecificValues = RequestSpecificValues;
                    returnObj.ResultsStats = ResultsStats;
                    returnObj.PagedResults = PagedResults;
                    return returnObj;
                }

                // Return value must be NULL
                RequestSpecificValues.Tracer.Add_Trace("ResultsViewer_Factory", "NULL value when creating a results viewer from a separate assembly via reflection");
                return null;
            }
            catch (Exception ee)
            {
                RequestSpecificValues.Tracer.Add_Trace("ResultsViewer_Factory", "Exception when creating a results viewer from a separate assembly via reflection");
                RequestSpecificValues.Tracer.Add_Trace("ResultsViewer_Factory", ee.Message);

                return null;
            }
        }
    }
}
