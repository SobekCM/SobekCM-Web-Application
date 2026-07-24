using System;
using System.IO;

namespace SobekCM.Tools
{
    /// <summary> Helpers for neutralizing directory-traversal ('..') attempts in
    /// user-supplied strings before they are used to build file system paths </summary>
    public static class PathTraversalGuard
    {
        /// <summary> Reduces a possibly-tainted string down to a bare file name,
        /// discarding any directory segments (including '..' traversal). Use this
        /// where only a file name - never a subpath - is ever legitimate. </summary>
        /// <param name="UntrustedFileName"> File name value from user input (form post, query string, upload) </param>
        /// <returns> Bare file name with no path separators, or an empty string if none remains </returns>
        public static string SanitizeFileName(string UntrustedFileName)
        {
            if (string.IsNullOrEmpty(UntrustedFileName))
                return string.Empty;

            return Path.GetFileName(UntrustedFileName);
        }

        /// <summary> Resolves a possibly-tainted relative path against a base directory and
        /// confirms the result cannot escape that base directory via '..' segments or an
        /// injected rooted path. Use this where a subpath (not just a bare file name) is
        /// legitimately expected, e.g. a value sourced from a server-generated dropdown. </summary>
        /// <param name="BaseDirectory"> Trusted directory the resolved path must stay within </param>
        /// <param name="UntrustedRelativePath"> Relative path value from user input </param>
        /// <param name="ResolvedFullPath"> The resolved, verified-contained full path, or null if rejected </param>
        /// <returns> TRUE if the resolved path stays within <paramref name="BaseDirectory"/>, otherwise FALSE </returns>
        public static bool TryResolveContainedPath(string BaseDirectory, string UntrustedRelativePath, out string ResolvedFullPath)
        {
            ResolvedFullPath = null;
            if (string.IsNullOrEmpty(UntrustedRelativePath))
                return false;

            string baseFullPath = Path.GetFullPath(BaseDirectory);
            if (!baseFullPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                baseFullPath += Path.DirectorySeparatorChar;

            string candidateFullPath = Path.GetFullPath(Path.Combine(baseFullPath, UntrustedRelativePath));

            if (!candidateFullPath.StartsWith(baseFullPath, StringComparison.OrdinalIgnoreCase))
                return false;

            ResolvedFullPath = candidateFullPath;
            return true;
        }
    }
}
