using Microsoft.Extensions.Primitives;
using System;

namespace SobekCM.Library.UI
{
    /// <summary> Extension methods for <see cref="StringValues"/>, which is returned by
    /// ASP.NET Core's IFormCollection and IQueryCollection indexers </summary>
    public static class StringValuesExtensions
    {
        /// <summary> Returns the first value in the StringValues trimmed of whitespace,
        /// or <see cref="String.Empty"/> if there are no values </summary>
        public static string TrimFirst(this StringValues values)
        {
            return values.Count > 0 ? (values[0] ?? string.Empty).Trim() : string.Empty;
        }
    }
}
