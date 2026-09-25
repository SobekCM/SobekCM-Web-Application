using System;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> One exception (or exception-like diagnostic) headed for the central monitoring database </summary>
    /// <remarks> Built by <see cref="Monitoring_Gateway"/> from ExceptionLog_Gateway.Record. The instance and server
    /// names are added by the sink, since they're the same for every record from this process. </remarks>
    public class Monitoring_Exception_Record
    {
        /// <summary> SHA-256 (hex) fingerprint grouping repeat occurrences of the same defect </summary>
        public string Hash { get; set; }

        /// <summary> Full type name of the innermost exception </summary>
        public string ExceptionType { get; set; }

        /// <summary> First SobekCM stack frame (type and method, no line number), if any </summary>
        public string TopFrame { get; set; }

        /// <summary> When this occurred, in UTC </summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary> Which logging call site caught it, e.g. "global-handler" or "main-writer" </summary>
        public string Source { get; set; }

        /// <summary> Message of the outermost exception </summary>
        public string Message { get; set; }

        /// <summary> Message of the innermost exception, when different from the outermost </summary>
        public string InnerMessage { get; set; }

        /// <summary> Full exception text, including inner exceptions and stack traces </summary>
        public string StackTrace { get; set; }

        /// <summary> Requested URL </summary>
        public string Url { get; set; }

        /// <summary> Client IP address </summary>
        public string ClientIp { get; set; }

        /// <summary> Custom_Tracer route text for the request, if any </summary>
        public string TraceText { get; set; }

        /// <summary> Correlation id of the request this occurred in (see <see cref="Correlation_Gateway"/>), shared
        /// with any other record the same request caused -- including in the engine requests it made </summary>
        public string CorrelationId { get; set; }

        /// <summary> Writes this record to temp/exceptions.txt instead, used by the sink if the database write fails </summary>
        public Action File_Fallback { get; set; }
    }
}
