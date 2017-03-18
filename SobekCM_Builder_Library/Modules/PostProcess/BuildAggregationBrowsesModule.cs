#region Using directives

using System;
using System.Collections.Generic;
using SobekCM.Builder_Library.Tools;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;

#endregion

namespace SobekCM.Builder_Library.Modules.PostProcess
{
    /// <summary> Post-process module builds the static aggregation-level (and instance-level) browse and RSS feed files </summary>
    /// <remarks> This class implements the <see cref="abstractPostProcessModule" /> abstract class and implements the <see cref="iPostProcessModule" /> interface. </remarks>
    public class BuildAggregationBrowsesModule : abstractPostProcessModule
    {
        /// <summary> Builds the static aggregation-level (and instance-level) browse and RSS feed files </summary>
        /// <param name="AggregationsAffected"> List of aggregations affected during the last process of incoming digital resources </param>
        /// <param name="ProcessedItems"> List of all items just processed (or reprocessed) </param>
        /// <param name="DeletedItems"> List of all delete requests just processed </param>
        /// <param name="Settings"> Instance-wide settings which may be required for this process </param>
        public override void DoWork(List<string> AggregationsAffected, List<BibVidStruct> ProcessedItems, List<BibVidStruct> DeletedItems, InstanceWide_Settings Settings)
        {
            // If no aggregations were affected, skip out
            if (AggregationsAffected.Count == 0)
                return;
            
            // Add the primary log entry
            long updatedId = OnProcess("....Performing some aggregation update functions", "Aggregation Updates", String.Empty, String.Empty, -1);

            // Create the (new) helper class
            Aggregation_Static_Page_Writer staticWriter = new Aggregation_Static_Page_Writer();
            staticWriter.Process += staticWriter_Process;
            staticWriter.Error += staticWriter_Error;

            // IN THIS CASE, WE DO NEED TO SET THE SINGLETON, SINCE THIS CALLS THE LIBRARIES
            //Engine_ApplicationCache_Gateway.Settings = Settings;

            // Build the aggregation browses
            staticWriter.ReCreate_Aggregation_Level_Pages(AggregationsAffected, Settings, updatedId);
        }

        void staticWriter_Error(string Message, long UpdateID)
        {
            OnError(Message, null, null, UpdateID);
        }

        void staticWriter_Process(string Message, long UpdateID)
        {
            OnProcess(Message, "Aggregation Updates", null, null, UpdateID);
        }
    }
}
