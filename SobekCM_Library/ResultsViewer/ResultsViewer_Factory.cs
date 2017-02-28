using System;
using System.Collections.Generic;
using System.Reflection;
using SobekCM.Core.Results;
using SobekCM.Core.UI_Configuration.Viewers;
using SobekCM.Library.UI;

namespace SobekCM.Library.ResultsViewer
{
    /// <summary> Static class is a factory that generates and returns the requested item viewer object 
    /// which implements the <see cref="SobekCM.Library.ResultsViewer.iResultsViewer"/> interface.</summary>
    public static class ResultsViewer_Factory
    {
        /// <summary> Gets the indicated results viewer, by results viewer code, usually from the URL </summary>
        /// <param name="ViewerCode"> Code which indicates which results viewer </param>
        /// <param name="RequestSpecificValues"> All the necessary, non-global data specific to the current request </param>
        /// <param name="ResultsStats"> Statistics about the results to display including the facets </param>
        /// <param name="PagedResults"> Actual pages of results </param>
        /// <returns> Either the results vieweer, or NULL </returns>
        public static iResultsViewer Get_Results_Viewer(string ViewerCode, RequestCache RequestSpecificValues, Search_Results_Statistics ResultsStats, List<iSearch_Title_Result> PagedResults)
        {
            // Get the match by viewer code
            ResultsSubViewerConfig config = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByCode(ViewerCode);

            // If no match, just try by viewer type then
            if (config == null)
            {
                config = UI_ApplicationCache_Gateway.Configuration.UI.WriterViewers.Results.GetViewerByType(ViewerCode);
            }

            // If still null, return NULL
            if (config == null)
                return null;

            // Was an assembly indicated
            if (String.IsNullOrEmpty(config.Assembly))
            {

                // Return a standard class
                switch (config.Class)
                {
                    case "SobekCM.Library.ResultsViewer.Bookshelf_ResultsViewer":
                        return new Bookshelf_View_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);

                    case "SobekCM.Library.ResultsViewer.Brief_ResultsViewer":
                        return new Brief_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);

                    case "SobekCM.Library.ResultsViewer.Export_ResultsViewer":
                        return new Export_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);

                    case "SobekCM.Library.ResultsViewer.Google_Map_ResultsViewer":
                        return new Google_Map_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);

                    case "SobekCM.Library.ResultsViewer.Table_ResultsViewer":
                        return new Table_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);

                    case "SobekCM.Library.ResultsViewer.Thumbnail_ResultsViewer":
                        return new Thumbnail_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);

                    case "SobekCM.Library.ResultsViewer.No_Results_ResultsViewer":
                        return new No_Results_ResultsViewer(RequestSpecificValues, ResultsStats, PagedResults);
                }

                // If it made it here, there is no assembly, but it is an unexpected type.  
                // Just create it from the same assembly then
                try
                {
                    Assembly dllAssembly = Assembly.GetCallingAssembly();
                    Type prototyperType = dllAssembly.GetType(config.Class);
                    iResultsViewer returnObj = (iResultsViewer)Activator.CreateInstance(prototyperType);
                    return returnObj;
                }
                catch (Exception)
                {
                    // Not sure exactly what to do here, honestly
                    return null;
                }
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
                return returnObj;
            }
            catch (Exception ee)
            {
                // Not sure exactly what to do here, honestly
                if (ee.Message.Length > 0)
                    return null;
                return null;
            }
        }
    }
}
