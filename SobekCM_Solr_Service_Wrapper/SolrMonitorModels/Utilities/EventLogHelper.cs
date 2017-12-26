using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolrServiceMonitor.Settings;

namespace SolrServiceMonitor.Utilities
{
    public static class EventLogHelper
    {
        private const string VERBOSE_MESSAGE = "\n\n-----------------------\n\nThis is a VERBOSE message and is being added to your event log due to your application settings.\n\nTo stop having these added to the logs, set the Logging.VerboseEventID setting to -1 in the SolrServiceMonitor.exe.config in the service application directory";


        public static void Write_Event(int EventID, string Message)
        {
            if (EventID > 0)
            {
                EventLog.WriteEntry(ServiceSettings.Name, Message, EventLogEntryType.Information, EventID);
            }
        }

        public static void Write_Verbose(string Message)
        {
            if (ServiceSettings.Logging.VerboseEventId > 0)
            {
                EventLog.WriteEntry(ServiceSettings.Name, Message + VERBOSE_MESSAGE, EventLogEntryType.Information, ServiceSettings.Logging.VerboseEventId);
            }
        }

        public static void Write_Warning(int EventID, string Message)
        {
            if (EventID > 0 )
                EventLog.WriteEntry(ServiceSettings.Name, Message, EventLogEntryType.Information, EventID);
        }

        public static void Write_Error(int EventID, string Message)
        {
            if (EventID > 0)
                EventLog.WriteEntry(ServiceSettings.Name, Message, EventLogEntryType.Error, EventID);
        }

        public static void Write_Error(int EventID, string Message, Exception ex)
        {
            if (EventID > 0)
                EventLog.WriteEntry(ServiceSettings.Name, Message + "\n\n-----------------------\n\nEXCEPTION: " + ex.Message + " ( " + ex.GetType() + " )\n\n-----------------------\n\nSTACK TRACE:\n\n" + ex.StackTrace, EventLogEntryType.Error, EventID);
        }

        


    }
}
