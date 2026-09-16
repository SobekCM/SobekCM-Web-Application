using System;
using System.Collections.Generic;
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
    /// no fallback, so they're swallowed here the same way every prior call site already did.
    /// <para>Those call sites now go through <see cref="Record"/>, which sends the exception to the central
    /// monitoring database when <see cref="Monitoring_Gateway.Sink"/> is configured, and only writes
    /// exceptions.txt (plus its trace file) when it isn't or that database can't be reached. <see cref="Append"/>
    /// is still used directly for notes that aren't failures, like AppLifetime_Gateway's restart message.</para> </remarks>
    public static class ExceptionLog_Gateway
    {
        private const string Separator = "------------------------------------------------------------------\n";

        private static readonly object writeLock = new object();

        /// <summary> Query-string parameters whose values are stripped by <see cref="Redact_Url"/> before a URL is
        /// logged, matched case-insensitively </summary>
        /// <remarks> These carry credentials rather than anything worth diagnosing. The OIDC and SAML names are the
        /// reason this exists: an exception thrown anywhere inside an authentication callback reaches the global
        /// handler with the authorization code still on the URL, which would then be readable by every login that
        /// can query the shared monitoring database. </remarks>
        private static readonly HashSet<string> redactedQueryParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "code", "state", "session_state", "id_token", "access_token", "refresh_token", "token",
            "client_secret", "password", "pwd", "api_key", "apikey",
            "SAMLResponse", "SAMLRequest", "RelayState", "Signature"
        };

        /// <summary> Replaces the value of any credential-bearing query-string parameter with [redacted], leaving the
        /// rest of the URL intact </summary>
        /// <param name="Url"> Requested URL, with or without a query string </param>
        /// <returns> The URL, safe to store in the monitoring database and exceptions.txt </returns>
        /// <remarks> The query string is kept rather than dropped because it's usually the most useful part of a
        /// logged URL -- the search terms, item id or viewer code that provoked the exception. Only the named
        /// parameters lose their values. </remarks>
        public static string Redact_Url(string Url)
        {
            if (String.IsNullOrEmpty(Url))
                return Url;

            int queryStart = Url.IndexOf('?');
            if ((queryStart < 0) || (queryStart == Url.Length - 1))
                return Url;

            try
            {
                string[] pairs = Url.Substring(queryStart + 1).Split('&');
                bool redacted = false;

                for (int i = 0; i < pairs.Length; i++)
                {
                    int equals = pairs[i].IndexOf('=');
                    if (equals <= 0)
                        continue;

                    string name = pairs[i].Substring(0, equals);
                    if (redactedQueryParameters.Contains(name))
                    {
                        pairs[i] = name + "=[redacted]";
                        redacted = true;
                    }
                }

                return redacted ? Url.Substring(0, queryStart + 1) + String.Join("&", pairs) : Url;
            }
            catch (Exception)
            {
                // A query string that can't be parsed can't be shown to be free of credentials either, so drop it
                return Url.Substring(0, queryStart);
            }
        }

        /// <summary> Whether per-occurrence temp/trace_&lt;guid&gt;.txt files should be written alongside
        /// exceptions.txt entries. Set once at startup from appsettings.json's "ErrorHandling:SuppressTraceFiles"
        /// (see Program.cs); defaults to false (trace files written normally). Flip it on to stop trace
        /// files accumulating for a known, already-diagnosed issue (e.g. a customer's SSL cert problem)
        /// without losing the lighter-weight exceptions.txt summary entries themselves. </summary>
        public static bool SuppressTraceFiles { get; set; }

        /// <summary> Static page to redirect users to when an unhandled exception occurs. Set once at
        /// startup from appsettings.json's "ErrorHandling:RemoteErrorPage" (see Program.cs) -- this is now
        /// the single source for the error redirect URL, replacing the old sobekcm.config "ErrorPage"
        /// element (removed; appsettings.json is easier to change per-deployment without touching the
        /// config file that also carries the database connection string). </summary>
        public static string RemoteErrorPage { get; set; }

        /// <summary> Records one exception -- to the central monitoring database if configured, otherwise (or if
        /// that fails) to temp/exceptions.txt. Never throws or blocks on I/O when the database is used. </summary>
        /// <param name="Source"> Which call site caught it, e.g. "global-handler" -- stored with the occurrence </param>
        /// <param name="Ex"> The exception. A diagnostic that never throws can pass a new, unthrown exception
        /// describing the problem (see Monitoring_Gateway.Build_Exception_Record for how that's fingerprinted). </param>
        /// <param name="Url"> Requested URL </param>
        /// <param name="ClientIp"> Client IP address </param>
        /// <param name="TraceText"> Custom_Tracer route text, if any. Stored in the database, or written to its own
        /// trace_&lt;guid&gt;.txt file when falling back (subject to SuppressTraceFiles). </param>
        /// <param name="FileText"> The exceptions.txt entry, used only when falling back. The trace note and the
        /// closing separator line are added here, so leave both out. </param>
        public static void Record(string Source, Exception Ex, string Url, string ClientIp, string TraceText, string FileText)
        {
            try
            {
                IMonitoringSink sink = Monitoring_Gateway.Sink;
                if (sink != null)
                {
                    Monitoring_Exception_Record record = Monitoring_Gateway.Build_Exception_Record(Source, Ex, Url, ClientIp, TraceText);
                    record.File_Fallback = () => append_with_trace(FileText, TraceText);
                    if (sink.TryEnqueue(record))
                        return;
                }
            }
            catch (Exception)
            {
                // Fall through to the file below
            }

            append_with_trace(FileText, TraceText);
        }

        /// <summary> Appends a block of diagnostic text to temp/exceptions.txt under the current
        /// content root, serialized against other concurrent callers. Never throws. </summary>
        /// <param name="Message"> Text to append (the caller is responsible for its own formatting/newlines) </param>
        public static void Append(string Message)
        {
            try
            {
                string logPath = Path.Combine(AppRoot_Gateway.AppRootPath, "temp", LogFile_Names.Exceptions);
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

        /// <summary> File fallback for <see cref="Record"/>: writes the trace file (if any), then the entry with its
        /// trace note and separator </summary>
        private static void append_with_trace(string FileText, string TraceText)
        {
            string traceNote = String.IsNullOrEmpty(TraceText) ? null : WriteTraceFileAndGetNote(TraceText);
            Append(FileText + (traceNote != null ? traceNote + "\n" : String.Empty) + Separator);
        }
    }
}
