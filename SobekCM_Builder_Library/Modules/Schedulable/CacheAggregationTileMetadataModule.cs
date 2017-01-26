using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SobekCM.Core.Results;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.Database;
using SobekCM.Resource_Object;

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
            // Get the spot where the aggregations sit 
            string aggregation_base = Path.Combine(Settings.Servers.Base_Design_Location, "aggregations");

            // Ensure it exists before continuing
            if (!Directory.Exists(aggregation_base))
                return;

            // Step through each aggregation folder
            string[] aggregation_folders = Directory.GetDirectories(aggregation_base);
            foreach (string thisAggrFolder in aggregation_folders)
            {
                // Find the associated aggregation tiles folder
                string tile_folder = Path.Combine(thisAggrFolder, "images", "tiles");

                // Skip this aggregation if it doesn't exist
                if (!Directory.Exists(tile_folder))
                    continue;

                // Get JPEG images
                string[] tile_images = Directory.GetFiles(tile_folder, "*.jpg");

                // Skip this aggregation if no images exist
                if (tile_images.Length == 0)
                    continue;

                // Step through each image, collecting the bibs and vids
                List<string> bibs = new List<string>();
                List<string> vids = new List<string>();
                foreach (string thisTile in tile_images)
                {
                    string thisFileNameSansExtension = Path.GetFileNameWithoutExtension(thisTile);
                    if ((thisFileNameSansExtension.Length == 10) && (SobekCM_Item.is_bibid_format(thisFileNameSansExtension)))
                    {
                        bibs.Add(thisFileNameSansExtension);
                        vids.Add("00001");
                    }
                    else if ((thisFileNameSansExtension.Length == 15) && (SobekCM_Item.is_bibid_format(thisFileNameSansExtension.Substring(0, 10))) && (SobekCM_Item.is_vids_format(thisFileNameSansExtension.Substring(10))))
                    {
                        bibs.Add(thisFileNameSansExtension.Substring(0, 10));
                        vids.Add(thisFileNameSansExtension.Substring(10));
                    }
                    else if ((thisFileNameSansExtension.Length == 16) && (SobekCM_Item.is_bibid_format(thisFileNameSansExtension.Substring(0, 10))) && (SobekCM_Item.is_vids_format(thisFileNameSansExtension.Substring(11))))
                    {
                        bibs.Add(thisFileNameSansExtension.Substring(0, 10));
                        vids.Add(thisFileNameSansExtension.Substring(11));
                    }
                }

                // Skip if no valid bib/vid names were found
                if (bibs.Count == 0)
                    continue;

                // Now, look up each of these bib/vids

                // First, pad the lists correctly
                while (bibs.Count%10 != 0)
                {
                    bibs.Add(String.Empty);
                    vids.Add(String.Empty);
                }

                // Get the aggregation code
                string aggrCode = Path.GetFileName(thisAggrFolder);

                // FOR TESTING DON'T USE COLLECTION CODE
                aggrCode = "";

                // Start the results object
                Database_Results_Info allResults = new Database_Results_Info();

                // Now, get the results
                int offset = 0;
                while (offset < bibs.Count)
                {
                    //Get the results from the database
                    Database_Results_Info results = Engine_Library.Database.Engine_Database.Metadata_By_Bib_Vid(aggrCode, bibs[offset + 0], vids[offset + 0], bibs[offset + 1], vids[offset + 1],
                        bibs[offset + 2], vids[offset + 2], bibs[offset + 3], vids[offset + 3],
                        bibs[offset + 4], vids[offset + 4], bibs[offset + 5], vids[offset + 5],
                        bibs[offset + 6], vids[offset + 6], bibs[offset + 7], vids[offset + 7],
                        bibs[offset + 8], vids[offset + 8], bibs[offset + 9], vids[offset + 9], null);

                    // Combine into full results
                    if ((results != null) && (results.Metadata_Labels != null) && (results.Metadata_Labels.Count > 0))
                        allResults.Metadata_Labels = results.Metadata_Labels;

                    if ((results != null) && ( results.Results != null ) && ( results.Results.Count > 0 ))
                        allResults.Results.AddRange(results.Results);

                    offset += 10;
                }


                // Save these results
                string cached_results_file = Path.Combine(tile_folder, "tile_metadata.xml");
                StreamWriter writer = new StreamWriter(cached_results_file, false);

                XmlSerializer x = new XmlSerializer(allResults.GetType());
                x.Serialize(writer, allResults);
            }
        }
    }
}
