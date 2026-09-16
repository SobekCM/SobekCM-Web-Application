using System;

namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> One rate-limiting event headed for the central monitoring database </summary>
    /// <remarks> Same fields as a temp/ratelimiting.txt line (see RateLimitLog_Gateway.Append). The instance and
    /// server names are added by the sink. </remarks>
    public class Monitoring_RateLimit_Record
    {
        /// <summary> When this occurred, in UTC </summary>
        public DateTime OccurredUtc { get; set; }

        /// <summary> Which limiter tripped, one of RateLimitLog_Gateway's Event_ constants </summary>
        public string EventName { get; set; }

        /// <summary> Whether the request that tripped it was logged on </summary>
        public bool LoggedOn { get; set; }

        /// <summary> Exact IP for the burst ban, subnet key for everything else </summary>
        public string Address { get; set; }

        /// <summary> What was reached and what happens now </summary>
        public string Details { get; set; }

        /// <summary> User agent of the request that tripped the event (only that one request, so not necessarily
        /// representative of the traffic behind it) </summary>
        public string UserAgent { get; set; }

        /// <summary> Writes this event to temp/ratelimiting.txt instead, used by the sink if the database write fails </summary>
        public Action File_Fallback { get; set; }
    }
}
