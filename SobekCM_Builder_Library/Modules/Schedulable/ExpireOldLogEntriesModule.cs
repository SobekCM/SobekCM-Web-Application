#region Using directives

using System;
using SobekCM.Core.Settings;

#endregion

namespace SobekCM.Builder_Library.Modules.Schedulable
{
    /// <summary> Schedulable builder module expires (and deletes) expired log files, per instance-wide settings </summary>
    /// <remarks> This class implements the <see cref="abstractSchedulableModule" /> abstract class and implements the <see cref="iSchedulableModule" /> interface. </remarks>
    public class ExpireOldLogEntriesModule : abstractSchedulableModule
    {
        /// <summary> Clears the old logs that are ready to be expired, per instance-wide settings </summary>
        /// <param name="Settings"> Instance-wide settings which may be required for this process </param>
        public override void DoWork(InstanceWide_Settings Settings)
        {
            // Clear the old logs
            OnProcess("ExpireOldLogEntriesModule : Expiring old log entries", "Standard", null, null, -1);

            // Clear the logs
            Library.Database.SobekCM_Database.Builder_Expire_Log_Entries(Settings.Builder.Log_Expiration_Days);
        }
    }
}
