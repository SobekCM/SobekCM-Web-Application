#region Using directives

using Microsoft.Extensions.Caching.Memory;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Library.Citation.Template;
using SobekCM.Tools;
using System;

#endregion

namespace SobekCM.Library.Citation
{
    /// <summary> Memory management utility for storing and retrieving metadata templates </summary>
    /// <remarks> Since the complete template object sits entirely in the UI library portion, this cannot be combined
    /// with the memory management portion that sits in the Core library, without moving the template stuff into the Core library.</remarks>
    public static class Template_MemoryMgmt_Utility
    {
        #region Static methods relating to storing and retrieving templates (for online submission and editing)

        /// <summary> Retrieves the template ( for online submission and editing ) from the cache or caching server </summary>
        /// <param name="Template_Code"> Code which specifies the template to retrieve </param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        /// <returns> Requested template object for online submissions and editing</returns>
        public static CompleteTemplate Retrieve_Template(string Template_Code, Custom_Tracer Tracer)
        {
            Tracer?.Add_Trace("Template_MemoryMgmt_Utility.Retrieve_Template", "");

            string key = "TEMPLATE_" + Template_Code;
            return SharedCache.Instance.Get(key) as CompleteTemplate;
        }

        /// <summary> Stores the template ( for online submission and editing ) to the cache or caching server </summary>
        /// <param name="Template_Code"> Code for the template to store </param>
        /// <param name="StoreObject"> CompleteTemplate object for online submissions and editing to store</param>
        /// <param name="Tracer"> Trace object keeps a list of each method executed and important milestones in rendering</param>
        public static void Store_Template(string Template_Code, CompleteTemplate StoreObject, Custom_Tracer Tracer)
        {
            string key = "TEMPLATE_" + Template_Code;

            Tracer?.Add_Trace("Template_MemoryMgmt_Utility.Store_Template", "Adding object '" + key + "' to the cache with expiration of thirty minutes");

            if (SharedCache.Instance[key] == null)
            {
                SharedCache.Instance.Set(key, StoreObject, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(30) });
            }
        }

        #endregion
    }
}
