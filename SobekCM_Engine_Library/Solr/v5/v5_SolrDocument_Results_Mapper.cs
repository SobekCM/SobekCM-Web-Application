using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SobekCM.Engine_Library.Solr.Legacy;

namespace SobekCM.Engine_Library.Solr.v5
{
    public class v5_SolrDocument_Results_Mapper
    {
        public Legacy_Solr_Document_Result Map_To_Result(v5_SolrDocument thisResult)
        {
            // Create the results
            Legacy_Solr_Document_Result resultConverted = new Legacy_Solr_Document_Result();
            resultConverted.DID = thisResult.DID;
            resultConverted.Title = thisResult.Title ?? "NO TITLE";
            resultConverted.HoldingLocation = thisResult.Holding;
            resultConverted.SourceInstitution = thisResult.Source;
            resultConverted.MaterialType = thisResult.Type;
            resultConverted.MainThumbnail = thisResult.MainThumbnail;

            resultConverted.Metadata_Display_Values = new string[]
            {
                        collection_to_string(thisResult.Creator_Display),
                        collection_to_string(thisResult.Publisher_Display),
                        (thisResult.Type ?? String.Empty ),
                        (thisResult.Format ?? String.Empty),
                        (thisResult.Edition ?? String.Empty),
                        (thisResult.Source ?? String.Empty),
                        (thisResult.Holding ?? String.Empty),
                        (thisResult.Donor ?? String.Empty),
                        collection_to_string(thisResult.Genre),
                        collection_to_string(thisResult.Subject)
            };

            //// Add the highlight snipper
            //if ((results.Highlights.ContainsKey(thisResult.DID)) && (results.Highlights[thisResult.DID].Count > 0) && (results.Highlights[thisResult.DID].ElementAt(0).Value.Count > 0))
            //{
            //    thisResult.Snippet = results.Highlights[thisResult.DID].ElementAt(0).Value.ElementAt(0);
            //}

            return resultConverted;
        }

        private static string collection_to_string(List<string> Values)
        {
            if ((Values == null) || (Values.Count == 0)) return String.Empty;

            if (Values.Count == 1) return Values[0];

            if (Values.Count == 2) return Values[0] + " | " + Values[1];

            if (Values.Count == 3) return Values[0] + " | " + Values[1] + " | " + Values[2];

            if (Values.Count == 4) return Values[0] + " | " + Values[1] + " | " + Values[2] + " | " + Values[3];

            return Values[0] + " | " + Values[1] + " | " + Values[2] + " | " + Values[3] + " | " + Values[4];
        }
    }
}
