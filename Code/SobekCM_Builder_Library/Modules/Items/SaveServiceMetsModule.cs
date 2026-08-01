#region Using directives

using System;

using SobekCM.Tools;
#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module saves a service METS file within the digital resource folder </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface. </remarks>
    public class SaveServiceMetsModule : abstractSubmissionPackageModule
    {
        /// <summary> Saves a service METS file within the digital resource folder </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("SaveServiceMetsModule.DoWork");

            try
            {
                Resource.Metadata.Save_SobekCM_METS();
            }
            catch (Exception ee)
            {
                OnError("Exception caught while saving the SobekCM service METS : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("SaveServiceMetsModule.DoWork", "Exception caught while saving the SobekCM service METS: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return false;
            }

            return true;
        }
    }
}
