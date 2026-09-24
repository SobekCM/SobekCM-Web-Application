using System;
using System.Threading;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Holds the correlation id for the request currently being handled, so everything one request
    /// causes -- including the engine calls it makes back into this same application -- can be tied together </summary>
    /// <remarks> The id is set by the first middleware in the pipeline (see Program.cs), which reuses an incoming
    /// <see cref="HeaderName"/> header when one is present and otherwise starts a new id. MicroservicesClientBase sends
    /// the header on every engine call, so an engine request made on behalf of a page shares the page's id, and
    /// ExceptionLog_Gateway.Record stamps the id on every exception it records. In the monitoring database that's what
    /// links, say, an admin viewer's "(500) Internal Server Error" to the engine exception that actually caused it.
    /// <para>It's an AsyncLocal rather than an HttpContext item because MicroservicesClientBase and
    /// ExceptionLog_Gateway have no HttpContext to read. Outside a request (the Builder, startup, background work)
    /// it's null, and nothing is sent or stamped.</para>
    /// <para>An incoming header is trusted from anyone -- it only groups monitoring records, it grants nothing -- but
    /// it has to look like an id this class would have made, so arbitrary text never reaches the database.</para> </remarks>
    public static class Correlation_Gateway
    {
        /// <summary> HTTP header carrying the correlation id between this application and its engine endpoints </summary>
        public const string HeaderName = "X-Sobek-Correlation-Id";

        /// <summary> Longest incoming id accepted, matching the monitoring database's CorrelationId column </summary>
        private const int MAX_LENGTH = 64;

        private static readonly AsyncLocal<string> current = new AsyncLocal<string>();

        /// <summary> Correlation id of the request currently being handled, or null outside a request </summary>
        public static string Current
        {
            get { return current.Value; }
        }

        /// <summary> Sets the correlation id for the rest of the current request, reusing the incoming header's value
        /// when it's a valid id, otherwise starting a new one </summary>
        /// <param name="IncomingHeader"> Value of the incoming <see cref="HeaderName"/> header, if any </param>
        /// <returns> The id now in effect </returns>
        /// <remarks> Must be called from middleware that awaits the rest of the pipeline, so the value flows down
        /// into it. An AsyncLocal set further down never flows back up, which is why this runs before the global
        /// exception handler rather than inside it. </remarks>
        public static string Begin_Request(string IncomingHeader)
        {
            string id = is_valid(IncomingHeader) ? IncomingHeader : Guid.NewGuid().ToString("N");
            current.Value = id;
            return id;
        }

        /// <summary> Letters, digits and dashes only, and not too long -- anything a caller might reasonably use as
        /// an id, but nothing that could carry markup or free text </summary>
        private static bool is_valid(string Value)
        {
            if (String.IsNullOrEmpty(Value) || (Value.Length > MAX_LENGTH))
                return false;

            foreach (char thisChar in Value)
            {
                if (!(((thisChar >= 'a') && (thisChar <= 'z')) || ((thisChar >= 'A') && (thisChar <= 'Z')) ||
                      ((thisChar >= '0') && (thisChar <= '9')) || (thisChar == '-')))
                    return false;
            }

            return true;
        }
    }
}
