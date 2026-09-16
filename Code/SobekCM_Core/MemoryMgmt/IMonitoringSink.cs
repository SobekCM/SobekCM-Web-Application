namespace SobekCM.Core.MemoryMgmt
{
    /// <summary> Destination for exceptions and rate-limiting events that should reach the central monitoring
    /// database rather than (only) this instance's temp/ folder </summary>
    /// <remarks> Lives in SobekCM_Core, which has no SQL client, so ExceptionLog_Gateway and RateLimitLog_Gateway can
    /// hand records off without knowing where they go. The real implementation is SqlMonitoringSink in
    /// SobekCM_Engine_Library, set once at startup through <see cref="Monitoring_Gateway.Sink"/>.
    /// <para>Both methods must return immediately and never throw. Returning false tells the caller the record was
    /// not accepted (queue full, database recently unreachable, shutting down), so it writes the file entry itself.
    /// A record that is accepted but later fails to reach the database is written to the file by the sink, through
    /// the record's File_Fallback.</para> </remarks>
    public interface IMonitoringSink
    {
        /// <summary> Queues one exception for the monitoring database </summary>
        /// <param name="Record"> Exception details, including the fingerprint </param>
        /// <returns> TRUE if accepted, FALSE if the caller should fall back to the file </returns>
        bool TryEnqueue(Monitoring_Exception_Record Record);

        /// <summary> Queues one rate-limiting event for the monitoring database </summary>
        /// <param name="Record"> Rate-limiting event details </param>
        /// <returns> TRUE if accepted, FALSE if the caller should fall back to the file </returns>
        bool TryEnqueue(Monitoring_RateLimit_Record Record);
    }
}
