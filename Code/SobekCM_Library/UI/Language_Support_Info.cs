#region Using directives

using SobekCM.Library.Localization;
using SobekCM.Tools;
using System.Collections.Generic;

#endregion

namespace SobekCM.Library.UI
{
    /// <summary> Thin, stateless facade over the translation dictionary used for common UI terms
    /// ( citation field labels, results-viewer column headers, etc. ) </summary>
    /// <remarks> Each call lazily pulls the requested language's entire translation set through
    /// <see cref="SobekCM.Library.SobekCM_Assistant.Get_Translation_Set"/>, which caches it for a few
    /// minutes ( see <see cref="SobekCM.Core.MemoryMgmt.CachedDataManager.Localization"/> ) rather than
    /// this object holding any translations itself. This lives in <c>SobekCM_Library</c> rather than
    /// <c>SobekCM_Core</c> specifically so it can reach that caching/database layer. </remarks>
    public class Language_Support_Info
    {
        /// <summary> Generic method requests translation from the appropriate translation dictionary </summary>
        /// <param name="English"> Term in english </param>
        /// <param name="Language"> Current language code of the web interface </param>
        /// <returns> Translation of term, if it exists, otherwise the original term </returns>
        public string Get_Translation(string English, string Language)
        {
            if (string.IsNullOrEmpty(English))
                return string.Empty;

            return Localization_Gateway.General.Get(English, Language);
        }
    }
}
