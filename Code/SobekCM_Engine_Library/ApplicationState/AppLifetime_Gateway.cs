#region Using directives

using Microsoft.Extensions.Hosting;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SobekCM.Engine_Library.ApplicationState
{
    /// <summary> Static gateway holding the application's <see cref="IHostApplicationLifetime"/>, captured
    /// once at startup, so code deep in the call stack (e.g. an extension-toggle admin endpoint) can request
    /// a graceful process restart without needing ASP.NET Core DI threaded through it </summary>
    /// <remarks> Used for extensions whose <see cref="Core.Configuration.Extensions.ExtensionInfo.RestartRequiredOnToggle"/>
    /// flag is set — e.g. OIDC/SAML auth providers, whose ASP.NET Core authentication schemes are only ever
    /// read once, at application startup (see <c>SobekCM/Program.cs</c>'s <c>AuthenticationBuilder</c> setup) </remarks>
    public static class AppLifetime_Gateway
    {
        /// <summary> Seconds to wait before actually stopping the application, so the request that triggered
        /// the restart (an engine microservice call nested inside an outer admin page render) has time to
        /// actually reach the browser first </summary>
        private const int RESTART_DELAY_SECONDS = 3;

        private static int restartPending;

        /// <summary> The application's host lifetime, captured once in <c>SobekCM/Program.cs</c> right after
        /// <c>app.Build()</c> </summary>
        public static IHostApplicationLifetime Lifetime { get; set; }

        /// <summary> Request a graceful application restart after a short delay </summary>
        /// <param name="Reason"> Short, human-readable reason for the restart, written to the exception/event log </param>
        /// <remarks> Safe to call more than once in quick succession - only the first call schedules a restart;
        /// later calls while one is already pending are no-ops </remarks>
        public static void RequestRestart(string Reason)
        {
            // Only the first caller actually schedules the restart; if one is already pending, do nothing
            if (Interlocked.CompareExchange(ref restartPending, 1, 0) != 0)
                return;

            ExceptionLog_Gateway.Append("Application restart requested: " + Reason + " ( restarting in " + RESTART_DELAY_SECONDS + " seconds )");

            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(RESTART_DELAY_SECONDS));
                Lifetime?.StopApplication();
            });
        }
    }
}
