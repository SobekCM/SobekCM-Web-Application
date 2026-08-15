using System;
using System.IO;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Centralizes appends to the shared temp/exceptions.txt diagnostic log. </summary>
    /// <remarks> Several independent call sites (OIDC sign-in failure, the global exception-handler
    /// middleware, Html_MainWriter's error page, HeaderFooter_Helper's null-skin diagnostic) each used
    /// to open temp/exceptions.txt directly. Under concurrent requests, two of them opening the file
    /// at the same instant throws ("being used by another process" -- actually another thread in this
    /// same process, since the file is opened without FileShare). Routing every writer through this
    /// single in-process lock serializes those appends instead. Never throws -- logging failures have
    /// no fallback, so they're swallowed here the same way every prior call site already did. </remarks>
    public static class ExceptionLog_Gateway
    {
        private static readonly object writeLock = new object();

        /// <summary> Whether per-occurrence temp/trace_&lt;guid&gt;.txt files should be written alongside
        /// exceptions.txt entries. Set once at startup from appsettings.json's "ErrorHandling:SuppressTraceFiles"
        /// (see Program.cs); defaults to false (trace files written normally). Flip it on to stop trace
        /// files accumulating for a known, already-diagnosed issue (e.g. a customer's SSL cert problem)
        /// without losing the lighter-weight exceptions.txt summary entries themselves. </summary>
        public static bool SuppressTraceFiles { get; set; }

        /// <summary> Appends a block of diagnostic text to temp/exceptions.txt under the current
        /// content root, serialized against other concurrent callers. Never throws. </summary>
        /// <param name="Message"> Text to append (the caller is responsible for its own formatting/newlines) </param>
        public static void Append(string Message)
        {
            try
            {
                string logPath = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", "exceptions.txt");
                lock (writeLock)
                {
                    File.AppendAllText(logPath, Message);
                }
            }
            catch (Exception)
            {
                // Best-effort logging -- nothing else to do if this itself fails.
            }
        }

        /// <summary> Writes a per-occurrence temp/trace_&lt;guid&gt;.txt file (a unique filename per call,
        /// so no cross-call lock is needed, unlike Append above) and returns a short note referencing it
        /// for inclusion in the matching Append(...) call -- unless SuppressTraceFiles is on, in which case
        /// no file is written and a note saying so is returned instead. Never throws. </summary>
        /// <param name="TraceText"> Full trace route text to write to the file </param>
        /// <returns> "Trace GUID: &lt;guid&gt;" if a file was written, otherwise an explanatory note </returns>
        public static string WriteTraceFileAndGetNote(string TraceText)
        {
            if (SuppressTraceFiles)
                return "(trace file suppressed -- see ErrorHandling:SuppressTraceFiles in appsettings.json)";

            try
            {
                var guid = Guid.NewGuid();
                string traceFile = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", "trace_" + guid + ".txt");
                File.AppendAllText(traceFile, TraceText);
                return "Trace GUID: " + guid;
            }
            catch (Exception)
            {
                return "(failed to write trace file)";
            }
        }
    }
}
