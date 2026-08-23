#region Using directives

using System;
using SobekCM.Core.Client;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module tells the running engine/web application to clear
    /// its in-memory cache for this digital resource </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.<br /><br />
    /// This is a best-effort notification, not a critical step -- a failure here never stops the rest of
    /// the pipeline, since the in-memory cache would otherwise just self-correct once its sliding
    /// expiration lapses. Should run near the end of the per-item module chain, after the item has
    /// actually been saved to the database. </remarks>
    public class ClearEngineCacheModule : abstractSubmissionPackageModule
    {
        /// <summary> Tells the running engine/web application to clear its in-memory cache for this digital resource </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> Always TRUE, since a failure here should not stop the rest of the processing pipeline </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("ClearEngineCacheModule.DoWork");

            try
            {
                SobekEngineClient.Items.Clear_Item_Cache(Resource.BibID, Resource.VID, Tracer);
            }
            catch (Exception ee)
            {
                OnError("Exception caught while clearing the engine's in-memory cache : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("ClearEngineCacheModule.DoWork", "Exception caught while clearing the engine's in-memory cache: " + ee.Message, Custom_Trace_Type_Enum.Error);
            }

            return true;
        }
    }
}
