using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Core.Settings;

namespace SobekCM.Builder_Library.Modules.Schedulable
{
    /// <summary> Schedulable builder module looks in the aggregation design folders for tiles and pulls the metadata for each
    /// tile to enable the hover-over in the aggregation tile display </summary>
    /// <remarks> This class implements the <see cref="abstractSchedulableModule" /> abstract class and implements the <see cref="iSchedulableModule" /> interface. </remarks>
    public class CacheAggregationTileMetadataModule : abstractSchedulableModule
    {
        /// <summary> Looks in the aggregation design folders for tiles and pulls the metadata for each
        /// tile to enable the hover-over in the aggregation tile display </summary>
        /// <param name="Settings"> Instance-wide settings which may be required for this process </param>
        public override void DoWork(InstanceWide_Settings Settings)
        {
            
        }
    }
}
