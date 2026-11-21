#region Using directives

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using SobekCM.Builder_Library.Tools;
using SobekCM.Engine_Library.Database;

#endregion

namespace SobekCM.Builder_Library.Statistics
{

    #region Definitions of delegates

    /// <summary> Delegate for the custom event which is fired when the status
    /// string on the main form needs to change </summary>
    public delegate void SobekCM_Stats_Reader_Processor_New_Status_Delegate(string new_message, bool ErrorFlag);

    #endregion

    /// <summary> Class processes all web IIS logs to SQL insert commands in a worker thread </summary>
    /// <remarks> This class is used by the SobekCM Stats Reader app </remarks>
    public class SobekCM_Stats_Reader_Processor
    {
        private readonly string dataset_location;
        private readonly string sobekcm_log_location;
        private readonly string sobekcm_web_location;
        private readonly List<string> year_months;

        /// <summary> Constructor for a new instance of the SobekCM_Stats_Reader_Processor worker class </summary>
        /// <param name="SobekCM_Log_Location">  </param>
        /// <param name="Temporary_Workspace"></param>
        /// <param name="SobekCM_Web_App_Directory"> </param>
        /// <param name="Year_Months"></param>
        public SobekCM_Stats_Reader_Processor(string SobekCM_Log_Location, string Temporary_Workspace, string SobekCM_Web_App_Directory, List<string> Year_Months)
        {
            sobekcm_log_location = SobekCM_Log_Location;
            dataset_location = Temporary_Workspace;
            sobekcm_web_location = SobekCM_Web_App_Directory;
            year_months = Year_Months;
        }

        /// <summary> Event is fired when the statistics reader has progress to report during the overall process </summary>
        /// <remarks> Never raised by two threads at once, even while logs are read in parallel </remarks>
        public event SobekCM_Stats_Reader_Processor_New_Status_Delegate New_Status;

        /// <summary> How many daily logs may be read at the same time ( default 1, one after another ) </summary>
        /// <remarks> Each day is read on its own, with its own sessions, so the results don't depend on this.
        /// Every log being read holds its requests in memory, so more threads means more memory. </remarks>
        public int Max_Parallel_Logs { get; set; } = 1;

        /// <summary> Only read the logs and write the daily and monthly datasets, saving nothing to the database </summary>
        public bool Read_Only { get; set; }

        private readonly object statusLock = new object();

        private void On_New_Status(string New_Message, bool ErrorFlag)
        {
            lock (statusLock)
            {
                if (New_Status != null)
                    New_Status(New_Message, ErrorFlag);
            }
        }

        /// <summary> Reads one day's log and writes its XML dataset, unless that dataset already exists </summary>
        private void read_one_log(string Log_File, SobekCM_Log_Reader Reader)
        {
            try
            {
                var fileInfo = new FileInfo(Log_File);
                On_New_Status("Processing " + fileInfo.Name, false);

                string name = fileInfo.Name.Replace(fileInfo.Extension, "");
                var logDate = new DateTime(Convert.ToInt32("20" + name.Substring(4, 2)),
                    Convert.ToInt32(name.Substring(6, 2)), Convert.ToInt32(name.Substring(8, 2)));

                string resultant_file = Path.Combine(dataset_location, logDate.Year.ToString() + logDate.Month.ToString().PadLeft(2, '0') + logDate.Day.ToString().PadLeft(2, '0') + ".xml");
                if (File.Exists(resultant_file))
                    return;

                SobekCM_Stats_DataSet daily = Reader.Read_Log(Log_File);
                if (daily == null)
                {
                    On_New_Status("Error reading " + fileInfo.Name + ": the log could not be analyzed", true);
                    return;
                }
                daily.Write_XML(dataset_location);
            }
            catch (Exception ee)
            {
                On_New_Status("Error reading a log file and writing as XML dataset. " + ee.Message, true);
            }
        }

        /// <summary> Process the IIS web logs to SQL insert commands </summary>
        public void Process_IIS_Logs()
        {
            // **** READ THE LOOKUP TABLES FROM THE DATABASE **** //
            var lookupTables = new DataSet();
            //lookupTables.ReadXml(@"C:\Users\mark.v.sullivan\Documents\Visual Studio 2012\Projects\SobekDbInfoSaver\SobekDbInfoSaver\bin\Release\uf\lookup.xml");

            try
            {
                lookupTables = Engine_Database.Get_Statistics_Lookup_Tables();
                On_New_Status("Retrieved statistics lookup tables from the database", false);
            }
            catch (Exception ee)
            {
                On_New_Status("Error getting statistics lookup tables from the database.  " + ee.Message, true);
                return;
            }

            // Ensure the lookup tables were not null
            if (lookupTables == null)
            {
                On_New_Status("Error getting statistics lookup tables from the database.  Table is NULL", true);
                return;
            }

            // Determine, from the year_month, which logs to read
            var logs_start = new List<string>();
            foreach (string thisYearMonth in year_months)
            {
                logs_start.Add("u_ex" + thisYearMonth.Substring(2, 2) + thisYearMonth.Substring(4, 2));
            }

            // ***** CODE BELOW READS ALL THE LOG FILES AND THEN WRITES THEM AS XML DATASETS *****//
            On_New_Status("Read all needed log files and write them as XML datasets", false);
            string[] files = File_System_Tools.GetFiles(sobekcm_log_location, "u_ex*.log");
            var logs_to_read = new List<string>();
            foreach (string thisFile in files)
            {
                string fileName = Path.GetFileName(thisFile);
                if ((fileName.Length >= 8) && (logs_start.Contains(fileName.ToLower().Substring(0, 8))))
                    logs_to_read.Add(thisFile);
            }

            // Each thread gets its own reader ( a reader keeps one day's hits and sessions ), all built
            // here first, one at a time, since building one reads the shared item list
            int threads = Math.Max(1, Math.Min(Max_Parallel_Logs, logs_to_read.Count));
            var readers = new ConcurrentBag<SobekCM_Log_Reader>();
            for (int i = 0; i < threads; i++)
                readers.Add(new SobekCM_Log_Reader(lookupTables.Tables[0], sobekcm_web_location));

            if (threads == 1)
            {
                readers.TryTake(out SobekCM_Log_Reader onlyReader);
                foreach (string thisFile in logs_to_read)
                    read_one_log(thisFile, onlyReader);
            }
            else
            {
                On_New_Status("Reading up to " + threads + " logs at a time", false);
                Parallel.ForEach(logs_to_read, new ParallelOptions { MaxDegreeOfParallelism = threads }, thisFile =>
                {
                    // There are as many readers as threads, so one is always free
                    readers.TryTake(out SobekCM_Log_Reader reader);
                    try
                    {
                        read_one_log(thisFile, reader);
                    }
                    finally
                    {
                        readers.Add(reader);
                    }
                });
            }



            // ***** CODE BELOW READS ALL THE DAILY XML DATASETS AND COMBINES THEM INTO MONTHLY *****//
            // ***** DATASETS WHICH ARE SUBSEQUENTLY WRITTEN AS XML DATASETS AS WELL            *****//    
            On_New_Status("Combining daily datasets into monthly datasets", false);
            try
            {
                foreach (string year_month in year_months)
                {
                    On_New_Status("Combining " + year_month + " daily datasets into one month", false);

                    string[] year_month_files = File_System_Tools.GetFiles(dataset_location, year_month + "*.xml");
                    if (year_month_files.Length > 0)
                    {

                        var combined = new SobekCM_Stats_DataSet();
                        foreach (string file in year_month_files)
                        {
                            if ((new FileInfo(file)).Name.IndexOf(year_month + ".xml") < 0)
                            {
                                var daily = new SobekCM_Stats_DataSet();
                                daily.Read_XML(file);

                                combined.Merge(daily);
                            }
                        }

                        // Write the complete data set
                        combined.Write_XML(dataset_location, year_month + ".xml");

                        // Just write the highest users in a seperate, more readable, file
                        combined.Write_Highest_Users(dataset_location, "users_" + year_month + ".xml");
                    }
                }
            }
            catch (Exception ee)
            {
                On_New_Status("Error combining daily datasets into monthly datasets. " + ee.Message, true);
                return;
            }

            // Reading only ( to check the results ): the daily and monthly datasets are written, nothing is saved
            if (Read_Only)
            {
                On_New_Status("Read only: the datasets are in " + dataset_location + ", nothing was saved", false);
                return;
            }

            save_monthly_datasets(lookupTables);
        }

        /// <summary> Saves the monthly datasets ( YYYYMM.xml ) already in the temporary workspace into the database,
        /// without reading any logs: for datasets read elsewhere with <see cref="Read_Only"/> </summary>
        public void Save_Monthly_Datasets()
        {
            DataSet lookupTables;
            try
            {
                lookupTables = Engine_Database.Get_Statistics_Lookup_Tables();
                On_New_Status("Retrieved statistics lookup tables from the database", false);
            }
            catch (Exception ee)
            {
                On_New_Status("Error getting statistics lookup tables from the database.  " + ee.Message, true);
                return;
            }
            if (lookupTables == null)
            {
                On_New_Status("Error getting statistics lookup tables from the database.  Table is NULL", true);
                return;
            }

            save_monthly_datasets(lookupTables);
        }

        private void save_monthly_datasets(DataSet lookupTables)
        {
            //// ***** CODE BELOW READS THE MONTHLY DATASETS AND THEN WRITES THE SQL INSERTION SCRIPTS ***** //
            // Read all the data lists first for id lookups
            Dictionary<string, int> aggregationHash = Table_To_Hash(lookupTables.Tables[2]);
            Dictionary<string, int> bibHash = Table_To_Hash(lookupTables.Tables[1]);
            var portalHash = new Dictionary<string, int>();
            foreach (DataRow thisRow in lookupTables.Tables[3].Rows)
            {
                if (!portalHash.ContainsKey(thisRow[2].ToString().ToUpper()))
                {
                    portalHash[thisRow[2].ToString().ToUpper()] = Convert.ToInt32(thisRow[0]);
                }
            }

            On_New_Status("Insert new statistics into database", false );
            try
            {
                foreach (string yearmonth in year_months)
                {
                    On_New_Status("Writing statistics for " + yearmonth, false);

                    SobekCM_Stats_DataSet monthly;
                    string thisFile = Path.Combine(dataset_location, yearmonth + ".xml");
                    if (!File.Exists(thisFile))
                    {
                        On_New_Status("No monthly dataset " + yearmonth + ".xml in " + dataset_location + ", so nothing to save", false);
                    }
                    else
                    {
                        monthly = new SobekCM_Stats_DataSet();
                        monthly.Read_XML(thisFile);
                        int year = Convert.ToInt32(yearmonth.Substring(0, 4));
                        int month = Convert.ToInt32(yearmonth.Substring(4));
                        monthly.Perform_SQL_Inserts(year, month, aggregationHash, bibHash, portalHash);
                    }
                }
            }
            catch (Exception ee)
            {
                On_New_Status("Error saving new usage statistics into the database. " + ee.Message, true);
                On_New_Status("Trace from previous error: " + ee.StackTrace, true);
                return;
            }

            On_New_Status("COMPLETE!", false);
        }

        private static Dictionary<string, int> Table_To_Hash(DataTable table)
        {
            var returnValue = new Dictionary<string, int>();
            foreach (DataRow dataRow in table.Rows)
            {
                returnValue[dataRow[1].ToString().ToUpper()] = Convert.ToInt32(dataRow[0]);
            }
            return returnValue;
        }
    }
}