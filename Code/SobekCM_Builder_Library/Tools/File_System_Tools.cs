#region Using directives

using System.IO;

#endregion

namespace SobekCM.Builder_Library.Tools
{
    /// <summary> Small helper for filesystem enumeration that behaves consistently across operating systems </summary>
    public static class File_System_Tools
    {
        /// <summary> .NET matches Directory.GetFiles search patterns case-insensitively on Windows but
        /// case-sensitively on Linux (MatchCasing.PlatformDefault) - since scanned/captured files arrive
        /// with inconsistent extension casing (e.g. "SCAN01.TIF"), pattern matching is forced case-insensitive
        /// here regardless of OS, so behavior stays identical to what the Windows Builder has always done. </summary>
        private static readonly EnumerationOptions CaseInsensitiveMatch = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive };

        /// <summary> Directory.GetFiles wrapper that matches the search pattern case-insensitively on every OS </summary>
        /// <param name="Path"> Directory to search </param>
        /// <param name="SearchPattern"> Search pattern (e.g. "*.tif") </param>
        /// <returns> Matching file names, same as Directory.GetFiles </returns>
        public static string[] GetFiles(string Path, string SearchPattern)
        {
            return Directory.GetFiles(Path, SearchPattern, CaseInsensitiveMatch);
        }
    }
}
