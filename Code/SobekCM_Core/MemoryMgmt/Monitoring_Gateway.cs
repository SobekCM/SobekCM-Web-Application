using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Holds the central monitoring sink (if configured) and builds the exception records sent to it </summary>
    public static class Monitoring_Gateway
    {
        /// <summary> How many SobekCM stack frames make up a fingerprint </summary>
        private const int FINGERPRINT_FRAME_COUNT = 3;

        /// <summary> Compiler-generated names carry an index that shifts whenever a nearby lambda or async method is
        /// added (e.g. "&lt;Invoke&gt;d__5", "&lt;&gt;c__DisplayClass0_0"), which would split one defect into several
        /// fingerprints after an unrelated edit, so the index is dropped </summary>
        private static readonly Regex compilerGeneratedIndex = new Regex(@"(d__|b__|g__|DisplayClass)\d+(_\d+)?", RegexOptions.Compiled);

        /// <summary> Sink that sends exceptions and rate-limiting events to the central monitoring database. Set once
        /// at startup (see Program.cs) when appsettings.json's "Monitoring:ConnectionString" is set; left null
        /// otherwise, in which case everything is written to temp/ files exactly as before. </summary>
        public static IMonitoringSink Sink { get; set; }

        /// <summary> Builds the monitoring record for one exception, including its fingerprint </summary>
        /// <param name="Source"> Which logging call site caught it, e.g. "global-handler" </param>
        /// <param name="Ex"> The exception (may be a wrapper such as SobekCM_Traced_Exception) </param>
        /// <param name="Url"> Requested URL </param>
        /// <param name="ClientIp"> Client IP address </param>
        /// <param name="TraceText"> Custom_Tracer route text, if any </param>
        /// <remarks> The fingerprint is the innermost exception type plus the first three SobekCM stack frames, with
        /// no line numbers, parameters, or message. Line numbers are left out so an unrelated edit to the same file
        /// doesn't create a new fingerprint -- which means the same fingerprint reappearing after its fix was merged
        /// really does mean the fix didn't work. The message is left out because it usually carries request-specific
        /// values. Diagnostics with no SobekCM frames at all (e.g. the null-skin check, which never throws) fall back
        /// to the source and message instead. </remarks>
        internal static Monitoring_Exception_Record Build_Exception_Record(string Source, Exception Ex, string Url, string ClientIp, string TraceText)
        {
            Exception innermost = Ex;
            while (innermost?.InnerException != null)
                innermost = innermost.InnerException;

            string exceptionType = innermost?.GetType().FullName ?? "(none)";
            List<string> frames = sobek_frames(Ex);

            string fingerprintInput = exceptionType + "|" + String.Join("|", frames);
            if (frames.Count == 0)
                fingerprintInput += "|" + Source + "|" + innermost?.Message;

            return new Monitoring_Exception_Record
            {
                Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintInput))),
                ExceptionType = exceptionType,
                TopFrame = frames.Count > 0 ? frames[0] : null,
                OccurredUtc = DateTime.UtcNow,
                Source = Source,
                Message = Ex?.Message,
                InnerMessage = ((innermost != null) && (innermost != Ex)) ? innermost.Message : null,
                StackTrace = Ex?.ToString(),
                Url = Url,
                ClientIp = ClientIp,
                TraceText = TraceText
            };
        }

        /// <summary> First SobekCM stack frames, innermost exception first, as "Namespace.Type.Method" </summary>
        private static List<string> sobek_frames(Exception Ex)
        {
            List<Exception> chain = new List<Exception>();
            for (Exception current = Ex; current != null; current = current.InnerException)
                chain.Add(current);
            chain.Reverse();

            List<string> frames = new List<string>();
            foreach (Exception current in chain)
            {
                if (String.IsNullOrEmpty(current.StackTrace))
                    continue;

                foreach (string rawLine in current.StackTrace.Split('\n'))
                {
                    string line = rawLine.Trim();
                    if (!line.StartsWith("at SobekCM.", StringComparison.Ordinal))
                        continue;

                    string frame = line.Substring(3);
                    int parameterStart = frame.IndexOf('(');
                    if (parameterStart > 0)
                        frame = frame.Substring(0, parameterStart);
                    frame = compilerGeneratedIndex.Replace(frame, "$1");

                    if ((frames.Count == 0) || (frames[frames.Count - 1] != frame))
                        frames.Add(frame);

                    if (frames.Count == FINGERPRINT_FRAME_COUNT)
                        return frames;
                }
            }

            return frames;
        }
    }
}
