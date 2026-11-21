#region Using directives

using System;
using System.Threading;

#endregion

namespace SobekCM.Builder_Library.Statistics
{
    /// <summary> Information about a single session (or visitor) against the SobekCM system </summary>
    /// <remarks> This class is used by the SobekCM Stats Reader app </remarks>
    public class SobekCM_Session
    {
        // The last session id handed out ( the first is 1 ).  Log files may be read on several threads
        // at once, so ids come from Interlocked.Increment, never a plain ++, or two sessions could share one
        private static int static_sessionid;

        /// <summary> Constructor for a new instance of the SobekCM_Session class </summary>
        /// <param name="IP"> IP address which is the origination of this session, or visit </param>
        /// <param name="Last_Hit"> Time of the last hit from this session </param>
        public SobekCM_Session(string IP, DateTime Last_Hit)
        {
            this.IP = IP;
            this.Last_Hit = Last_Hit;

            SessionID = Interlocked.Increment(ref static_sessionid);
        }

        /// <summary> Unique, incrementing ID for this session </summary>
        public int SessionID { get; private set; }

        /// <summary> IP address which is the origination of this session, or visit </summary>
        public string IP { get; set; }

        /// <summary> Time of the last hit from this session </summary>
        public DateTime Last_Hit { get; set; }
    }
}