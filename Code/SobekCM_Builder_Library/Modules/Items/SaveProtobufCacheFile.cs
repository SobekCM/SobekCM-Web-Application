#region Using directives

using SobekCM.Core.BriefItem;
using SobekCM.Engine_Library.Items.BriefItems;
using SobekCM.Tools;

#endregion

namespace SobekCM.Builder_Library.Modules.Items
{
    /// <summary> Item-level submission package module writes (or overwrites) the cached BriefItemInfo
    /// protobuf file for the digital resource, directly in its resource folder </summary>
    /// <remarks> This class implements the <see cref="abstractSubmissionPackageModule" /> abstract class and implements the <see cref="iSubmissionPackageModule" /> interface.<br /><br />
    /// Runs just after <see cref="SaveToDatabaseModule" />, by which point all incoming files have already
    /// been copied to the web server and <see cref="Incoming_Digital_Resource.Resource_Folder"/> points at
    /// the correct final location -- so the cache file written here always overwrites any existing one and
    /// always reflects this run's METS/behaviors. </remarks>
    public class SaveProtobufCacheFile : abstractSubmissionPackageModule
    {
        /// <summary> Writes (or overwrites) the cached BriefItemInfo protobuf file for the digital resource </summary>
        /// <param name="Resource"> Incoming digital resource object </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering </param>
        /// <returns> TRUE if processing can continue, FALSE if a critical error occurred which should stop all processing </returns>
        public override bool DoWork(Incoming_Digital_Resource Resource, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("SaveProtobufCacheFile.DoWork");

            BriefItemInfo briefItem = BriefItem_Factory.Create(Resource.Metadata, Tracer);
            BriefItem_Cache.WriteCache(Resource.BibID, Resource.VID, briefItem, Tracer);

            return true;
        }
    }
}
