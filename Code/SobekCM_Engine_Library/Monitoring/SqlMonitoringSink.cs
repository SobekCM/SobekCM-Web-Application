using Microsoft.Data.SqlClient;
using SobekCM.Core.MemoryMgmt;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SobekCM.Engine_Library.Monitoring
{
    /// <summary> Sends exceptions and rate-limiting events to the central monitoring database shared by every
    /// SobekCM instance (see Database/SQL/Monitoring) </summary>
    /// <remarks> Requests never wait on this database. TryEnqueue only adds to an in-memory queue, and one background
    /// flusher writes whatever has queued up every couple of seconds, so a burst of identical exceptions goes out
    /// over one connection.
    /// <para>If the database can't be reached, every record in that batch is written to its temp/ file through its
    /// File_Fallback, and TryEnqueue refuses new records for a minute so callers write their files directly instead
    /// of queueing work that will just fail. A record the database rejects on its own (bad data) falls back alone,
    /// without affecting the rest of the batch.</para> </remarks>
    public sealed class SqlMonitoringSink : IMonitoringSink
    {
        private const int QUEUE_CAPACITY = 1000;
        private const int MAX_BATCH = 100;
        private const int COMMAND_TIMEOUT_SECONDS = 15;
        private static readonly TimeSpan FlushDelay = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan CircuitOpenDuration = TimeSpan.FromSeconds(60);

        private readonly string connectionString;
        private readonly string instanceName;
        private readonly string serverName;
        private readonly Channel<object> queue;
        private readonly CancellationTokenSource stopping = new CancellationTokenSource();
        private Task flusher;
        private long circuitOpenUntilTicks;

        /// <summary> Constructor for a new instance of the SqlMonitoringSink class </summary>
        /// <param name="ConnectionString"> Connection string for the monitoring database </param>
        /// <param name="InstanceName"> Name of this SobekCM instance, stored with every record </param>
        public SqlMonitoringSink(string ConnectionString, string InstanceName)
        {
            connectionString = ConnectionString;
            instanceName = InstanceName;
            serverName = Environment.MachineName;

            // Wait (rather than a drop mode) makes TryWrite return false when full, so the caller falls back to
            // its file instead of the record silently disappearing
            queue = Channel.CreateBounded<object>(new BoundedChannelOptions(QUEUE_CAPACITY)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true
            });
        }

        /// <summary> Starts the background flusher </summary>
        public void Start()
        {
            flusher = Task.Run(run_flusher);
        }

        /// <summary> Stops accepting records and flushes what's already queued, waiting at most the timeout </summary>
        /// <param name="Timeout"> Longest to wait for the final flush </param>
        public void Stop(TimeSpan Timeout)
        {
            queue.Writer.TryComplete();
            stopping.Cancel();

            try
            {
                flusher?.Wait(Timeout);
            }
            catch (Exception)
            {
                // Shutting down anyway
            }
        }

        /// <inheritdoc />
        public bool TryEnqueue(Monitoring_Exception_Record Record)
        {
            return enqueue(Record);
        }

        /// <inheritdoc />
        public bool TryEnqueue(Monitoring_RateLimit_Record Record)
        {
            return enqueue(Record);
        }

        private bool enqueue(object Record)
        {
            if (DateTime.UtcNow.Ticks < Interlocked.Read(ref circuitOpenUntilTicks))
                return false;

            return queue.Writer.TryWrite(Record);
        }

        private async Task run_flusher()
        {
            List<object> batch = new List<object>(MAX_BATCH);

            while (await queue.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                // Let a burst accumulate into one batch, except when shutting down
                try
                {
                    await Task.Delay(FlushDelay, stopping.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }

                while ((batch.Count < MAX_BATCH) && (queue.Reader.TryRead(out object item)))
                    batch.Add(item);

                await flush(batch).ConfigureAwait(false);
                batch.Clear();
            }
        }

        private async Task flush(List<object> Batch)
        {
            if (Batch.Count == 0)
                return;

            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                connection.Dispose();
                Interlocked.Exchange(ref circuitOpenUntilTicks, DateTime.UtcNow.Add(CircuitOpenDuration).Ticks);
                foreach (object item in Batch)
                    fallback(item);
                return;
            }

            using (connection)
            {
                List<Monitoring_RateLimit_Record> rateLimitEvents = new List<Monitoring_RateLimit_Record>();

                foreach (object item in Batch)
                {
                    if (item is Monitoring_Exception_Record exceptionRecord)
                    {
                        try
                        {
                            await log_exception(connection, exceptionRecord).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            fallback(exceptionRecord);
                        }
                    }
                    else if (item is Monitoring_RateLimit_Record rateLimitRecord)
                    {
                        rateLimitEvents.Add(rateLimitRecord);
                    }
                }

                if (rateLimitEvents.Count > 0)
                {
                    try
                    {
                        await log_rate_limit_events(connection, rateLimitEvents).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        foreach (Monitoring_RateLimit_Record rateLimitRecord in rateLimitEvents)
                            fallback(rateLimitRecord);
                    }
                }
            }
        }

        private async Task log_exception(SqlConnection Connection, Monitoring_Exception_Record Record)
        {
            using (SqlCommand command = new SqlCommand("dbo.Monitoring_Log_Exception", Connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                command.Parameters.Add("@InstanceName", SqlDbType.NVarChar, 100).Value = db_string(instanceName, 100);
                command.Parameters.Add("@ServerName", SqlDbType.NVarChar, 100).Value = db_string(serverName, 100);
                command.Parameters.Add("@Hash", SqlDbType.Char, 64).Value = db_string(Record.Hash, 64);
                command.Parameters.Add("@ExceptionType", SqlDbType.NVarChar, 500).Value = db_string(Record.ExceptionType, 500);
                command.Parameters.Add("@TopFrame", SqlDbType.NVarChar, 1000).Value = db_string(Record.TopFrame, 1000);
                command.Parameters.Add("@OccurredUtc", SqlDbType.DateTime2).Value = Record.OccurredUtc;
                command.Parameters.Add("@Source", SqlDbType.VarChar, 50).Value = db_string(Record.Source, 50);
                command.Parameters.Add("@Message", SqlDbType.NVarChar, 2000).Value = db_string(Record.Message, 2000);
                command.Parameters.Add("@InnerMessage", SqlDbType.NVarChar, 2000).Value = db_string(Record.InnerMessage, 2000);
                command.Parameters.Add("@StackTrace", SqlDbType.NVarChar, -1).Value = db_string(Record.StackTrace, -1);
                command.Parameters.Add("@Url", SqlDbType.NVarChar, 2000).Value = db_string(Record.Url, 2000);
                command.Parameters.Add("@ClientIp", SqlDbType.VarChar, 45).Value = db_string(Record.ClientIp, 45);
                command.Parameters.Add("@TraceText", SqlDbType.NVarChar, -1).Value = db_string(Record.TraceText, -1);

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        private async Task log_rate_limit_events(SqlConnection Connection, List<Monitoring_RateLimit_Record> Records)
        {
            DataTable events = new DataTable();
            events.Columns.Add("OccurredUtc", typeof(DateTime));
            events.Columns.Add("EventName", typeof(string));
            events.Columns.Add("LoggedOn", typeof(bool));
            events.Columns.Add("Address", typeof(string));
            events.Columns.Add("Details", typeof(string));
            events.Columns.Add("UserAgent", typeof(string));

            // Column order must match dbo.Monitoring_RateLimit_Event_Table
            foreach (Monitoring_RateLimit_Record record in Records)
            {
                events.Rows.Add(record.OccurredUtc, db_string(record.EventName, 50), record.LoggedOn,
                    db_string(record.Address, 100), db_string(record.Details, 2000), db_string(record.UserAgent, 500));
            }

            using (SqlCommand command = new SqlCommand("dbo.Monitoring_Log_RateLimit_Events", Connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = COMMAND_TIMEOUT_SECONDS;

                command.Parameters.Add("@InstanceName", SqlDbType.NVarChar, 100).Value = db_string(instanceName, 100);
                command.Parameters.Add("@ServerName", SqlDbType.NVarChar, 100).Value = db_string(serverName, 100);
                SqlParameter eventsParameter = command.Parameters.Add("@Events", SqlDbType.Structured);
                eventsParameter.TypeName = "dbo.Monitoring_RateLimit_Event_Table";
                eventsParameter.Value = events;

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        /// <summary> Null becomes DBNull, and anything longer than the column is cut to fit -- an over-length value
        /// would otherwise make SQL Server reject the whole record </summary>
        /// <param name="Value"> String value </param>
        /// <param name="MaxLength"> Column length, or -1 for (max) </param>
        private static object db_string(string Value, int MaxLength)
        {
            if (Value == null)
                return DBNull.Value;

            return ((MaxLength > 0) && (Value.Length > MaxLength)) ? Value.Substring(0, MaxLength) : Value;
        }

        private static void fallback(object Record)
        {
            try
            {
                if (Record is Monitoring_Exception_Record exceptionRecord)
                    exceptionRecord.File_Fallback?.Invoke();
                else if (Record is Monitoring_RateLimit_Record rateLimitRecord)
                    rateLimitRecord.File_Fallback?.Invoke();
            }
            catch (Exception)
            {
                // Best-effort logging -- nothing else to do if this itself fails.
            }
        }
    }
}
