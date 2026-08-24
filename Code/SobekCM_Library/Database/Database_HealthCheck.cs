#region Using directives

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SobekCM.Library.Database
{
    /// <summary> Health check that verifies the main SobekCM database is reachable </summary>
    public class Database_HealthCheck : IHealthCheck
    {
        private const int CONNECTION_TIMEOUT_SECONDS = 5;
        private const int RESULT_CACHE_SECONDS = 10;
        private const string CACHE_KEY = "HEALTHCHECK|DATABASE";

        /// <summary> Checks whether the main SobekCM database can currently be reached </summary>
        /// <remarks> The actual connection attempt is cached for <see cref="RESULT_CACHE_SECONDS"/> seconds
        /// (fixed, not sliding) so repeated hits against /health can't be used to hammer the database with
        /// connection attempts -- worst case is one real check per cache window, not one per request </remarks>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (SharedCache.Instance[CACHE_KEY] is HealthCheckResult cachedResult)
                return Task.FromResult(cachedResult);

            HealthCheckResult result = SobekCM_Database.Test_Connection(CONNECTION_TIMEOUT_SECONDS)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Unable to connect to the SobekCM database within " + CONNECTION_TIMEOUT_SECONDS + " seconds");

            SharedCache.Instance.Set(CACHE_KEY, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(RESULT_CACHE_SECONDS) });

            return Task.FromResult(result);
        }
    }
}
