#region Using directives

using System;
using SobekCM.Resource_Object.Behaviors;
using SobekCM.Tools;

#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module generates a newspaper item's serial hierarchy from its
    /// date issued, when the incoming package did not already supply one </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.
    /// <br /><br />
    /// Runs immediately before <see cref="SaveServiceMetsModule" />, so any hierarchy it generates is captured in the
    /// service METS this package writes out. The actual generation logic lives in <see cref="Serial_Info.Synchronize_Newspaper_Hierarchy_With_Date_Issued" />,
    /// shared with the online submission and metadata-editing paths -- this module simply invokes it for whatever
    /// package is currently being processed by the builder. </remarks>
    public class NewspaperPopulateSerialHierarchyModule : abstractSubmissionPackageModule
    {
        /// <summary> Generates a newspaper item's serial hierarchy from its date issued, if it doesn't already have one </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("NewspaperPopulateSerialHierarchyModule.DoWork");

            try
            {
                Serial_Info.Synchronize_Newspaper_Hierarchy_With_Date_Issued(Resource.Metadata);
            }
            catch (Exception ee)
            {
                OnError("Exception caught while generating the newspaper serial hierarchy : " + ee.Message, Resource.BibID + ":" + Resource.VID, Resource.METS_Type_String, Resource.BuilderLogId);
                Tracer?.Add_Trace("NewspaperPopulateSerialHierarchyModule.DoWork", "Exception caught while generating the newspaper serial hierarchy: " + ee.Message, Custom_Trace_Type_Enum.Error);
                return false;
            }

            return true;
        }
    }
}
