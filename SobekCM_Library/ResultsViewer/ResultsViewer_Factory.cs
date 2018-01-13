using System;
using System.Collections.Generic;
using System.Reflection;
using SobekCM.Core.Navigation;
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

                    case "SobekCM.Library.ResultsViewer.Export_ResultsViewer":
                        returnValue = new Export_ResultsViewer();
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
                        returnValue = (iResultsViewer) Activator.CreateInstance(prototyperType);
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
