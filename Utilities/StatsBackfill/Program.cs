#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using EngineAgnosticLayerDbAccess;
using SobekCM.Builder_Library.Statistics;
using SobekCM.Core.Configuration;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Settings;
using SobekCM.Engine_Library.ApplicationState;
using SobekCM.Engine_Library.Database;
using SobekCM.Engine_Library.Settings;

#endregion

namespace SobekCM.Stats_Backfill
{
    /// <summary> One-off tool: fills in the months missing from a SobekCM 5.2 database's usage statistics
    /// from old IIS logs (plain or zipped), using the Builder's own statistics reader. </summary>
    /// <remarks> Dry run unless --execute is given.  A month is only calculated when the database has nothing
    /// at all for it, since SobekCM_Statistics_Aggregate can never be run twice for the same month. </remarks>
    internal static class Program
    {
        private static readonly Regex LogNameRegex = new Regex(@"^u_ex(\d{2})(\d{2})(\d{2})(\d{2})?\.log$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // The tables a month's statistics are written into
        private static readonly string[] StatisticsTables =
        {
            "SobekCM_Statistics", "SobekCM_Item_Statistics", "SobekCM_Item_Group_Statistics",
            "SobekCM_Item_Aggregation_Statistics", "SobekCM_Portal_URL_Statistics", "SobekCM_Webcontent_Statistics"
        };

        private static int Main(string[] args)
        {
            Options options = Options.Parse(args);
            if (options == null)
            {
                Options.Write_Usage();
                return 1;
            }

            try
            {
                return Run(options);
            }
            catch (Exception ee)
            {
                Error("Unexpected error: " + ee.Message);
                Console.WriteLine(ee.StackTrace);
                return 1;
            }
        }

        private static int Run(Options options)
        {
            Console.WriteLine();
            Console.WriteLine("SobekCM usage statistics backfill" + (options.ArchiveOnly ? " ( zipping logs by month only )" : "") + (options.Execute ? "" : "  ( DRY RUN: nothing is changed; add --execute )"));
            Console.WriteLine();

            if (!Directory.Exists(options.LogDirectory))
            {
                Error("Log directory not found: " + options.LogDirectory);
                return 1;
            }

            // ---- Find the logs, plain and zipped ----
            Console.WriteLine("Scanning " + options.LogDirectory + " ...");
            var allCopies = new List<Log_Source>();
            var zipsWithOtherFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<Log_Source> logs = Scan_Logs(options.LogDirectory, allCopies, zipsWithOtherFiles);
            if (logs.Count == 0)
            {
                Error("No IIS logs ( u_exYYMMDD.log, plain or inside .zip files ) found.");
                return 1;
            }

            DateTime now = DateTime.Now;
            int currentYearMonth = now.Year * 100 + now.Month;
            var months = logs
                .GroupBy(L => L.YearMonth)
                .Where(G => G.Key < currentYearMonth)
                .Where(G => (options.Months.Count == 0) || options.Months.Contains(G.Key))
                .OrderBy(G => G.Key)
                .Select(G => new Month_Info(G.Key, G.ToList()))
                .ToList();

            int skippedCurrent = logs.Count(L => L.YearMonth >= currentYearMonth);
            if (skippedCurrent > 0)
                Console.WriteLine("  Ignoring " + skippedCurrent + " log(s) from the current month.");
            if (months.Count == 0)
            {
                Error("No logs from earlier months" + (options.Months.Count > 0 ? " matching --months" : "") + ".");
                return 1;
            }

            // Just zipping the logs by month, no statistics
            if (options.ArchiveOnly)
            {
                bool zipped = Archive_By_Month(options.LogDirectory, months, allCopies, zipsWithOtherFiles, options.Execute);
                if (!options.Execute)
                    Console.WriteLine("Run again with --execute to do this.");
                return zipped ? 0 : 2;
            }

            // ---- Connect and load what the reader needs ----
            Console.WriteLine("Connecting to the database and loading settings, collections and portals ...");
            var dbInstance = new Database_Instance_Configuration { Connection_String = options.ConnectionString };
            Engine_Database.Connection_String = options.ConnectionString;
            Engine_Database.DatabaseType = EalDbTypeEnum.MSSQL;
            // Read the settings once here first: if this fails, the gateway's Settings property would
            // quietly fall back to the Builder's config file, which this tool doesn't use
            InstanceWide_Settings dbSettings = null;
            try
            {
                dbSettings = InstanceWide_Settings_Builder.Build_Settings(dbInstance);
            }
            catch (Exception ee)
            {
                Engine_Database.Last_Exception = ee;
            }
            if ((dbSettings == null) || !Engine_ApplicationCache_Gateway.RefreshSettings(dbInstance))
            {
                Error("Unable to read the settings from the database: " + Engine_Database.Last_Exception?.Message);
                return 1;
            }

            string siteDirectory = String.IsNullOrWhiteSpace(options.SiteDirectory) ? Engine_ApplicationCache_Gateway.Settings.Servers.Application_Server_Network : options.SiteDirectory;
            if (String.IsNullOrWhiteSpace(siteDirectory) || !Directory.Exists(siteDirectory))
            {
                Error("Web site folder not found ( '" + siteDirectory + "' ).  It comes from the 'Application Server Network' setting; pass --site to override.");
                return 1;
            }
            AppRoot_Gateway.AppRootPath = siteDirectory;

            if (!Engine_ApplicationCache_Gateway.RefreshAll(dbInstance, siteDirectory))
                Warn("Some cached data did not load cleanly ( continuing; the reader needs only settings, collections, aliases, portals and web content ).");
            if (Engine_ApplicationCache_Gateway.Codes == null)
            {
                Error("Unable to load the collection codes from the database.");
                return 1;
            }

            Console.WriteLine("  Library: " + Engine_ApplicationCache_Gateway.Settings.System.System_Name + "  ( site folder " + siteDirectory + " )");
            Console.WriteLine();

            // ---- What does the database already hold? ----
            HashSet<int> monthsInDatabase = Months_In_Database();
            foreach (Month_Info month in months)
            {
                if (monthsInDatabase.Contains(month.YearMonth))
                {
                    month.Action = Month_Action.Has_Statistics;
                    continue;
                }

                List<string> leftovers = Tables_With_Rows(month.Year, month.Month);
                if (leftovers.Count > 0)
                {
                    month.Action = Month_Action.Refused_Partial_Data;
                    month.Note = "rows already in " + String.Join(", ", leftovers);
                    continue;
                }

                if (month.Hourly)
                {
                    month.Action = Month_Action.Refused_Hourly;
                    month.Note = "hourly logs; the reader only handles one log per day";
                    continue;
                }

                if (month.DaysCovered < month.DaysInMonth)
                {
                    month.Note = "missing days " + month.Missing_Days();
                    month.Action = options.AllowPartial ? Month_Action.Calculate : Month_Action.Skipped_Incomplete;
                    continue;
                }

                month.Action = Month_Action.Calculate;
            }

            // Optionally count the lines the reader will actually use ( reads every log )
            if (options.Scan && !options.Execute)
            {
                Console.WriteLine("Counting SobekCM page requests in the logs to be calculated ...");
                foreach (Month_Info month in months.Where(M => M.Action == Month_Action.Calculate))
                {
                    foreach (Log_Source log in month.Logs)
                    {
                        using (Stream stream = log.Open())
                            Count_Lines(stream, month);
                    }
                }
                Console.WriteLine();
            }

            Write_Report(months, options.Scan || options.Execute);

            List<Month_Info> toCalculate = months.Where(M => M.Action == Month_Action.Calculate).ToList();
            if (!options.Execute)
            {
                if (toCalculate.Count == 0)
                    Console.WriteLine("Nothing to calculate.");
                else
                    Console.WriteLine(toCalculate.Count + " month(s) would be calculated.");
                if (!options.NoArchive)
                {
                    Console.WriteLine();
                    Archive_By_Month(options.LogDirectory, months, allCopies, zipsWithOtherFiles, false);
                }
                Console.WriteLine("Run again with --execute to do this.");
                return 0;
            }

            if (toCalculate.Count == 0)
            {
                Console.WriteLine("Nothing to calculate.");
                if (!options.NoArchive)
                    return Archive_By_Month(options.LogDirectory, months, allCopies, zipsWithOtherFiles, true) ? 0 : 2;
                return 0;
            }

            // ---- Calculate, one month at a time ----
            string workRoot = String.IsNullOrWhiteSpace(options.WorkDirectory)
                ? Path.Combine(Path.GetTempPath(), "SobekCM_Stats_Backfill", now.ToString("yyyyMMdd_HHmmss"))
                : options.WorkDirectory;
            Directory.CreateDirectory(workRoot);
            Console.WriteLine("Working folder: " + workRoot);
            Console.WriteLine();

            int calculated = 0, failed = 0;
            foreach (Month_Info month in toCalculate)
            {
                Console.WriteLine("==== " + month.Label + " ====");
                string monthFolder = Path.Combine(workRoot, month.YearMonth.ToString());
                string logFolder = Path.Combine(monthFolder, "logs");
                string dataFolder = Path.Combine(monthFolder, "data");
                if (Directory.Exists(monthFolder))
                    Directory.Delete(monthFolder, true);
                Directory.CreateDirectory(logFolder);
                Directory.CreateDirectory(dataFolder);

                // Unzip / copy this month's logs into one flat folder, counting the lines the reader uses
                month.TotalLines = 0;
                month.PageLines = 0;
                foreach (Log_Source log in month.Logs)
                {
                    string target = Path.Combine(logFolder, log.FileName);
                    using (Stream source = log.Open())
                    using (FileStream output = File.Create(target))
                        source.CopyTo(output);
                    using (FileStream staged = File.OpenRead(target))
                        Count_Lines(staged, month);
                }
                Console.WriteLine("  Staged " + month.Logs.Count + " log(s): " + month.TotalLines.ToString("N0") + " requests, " + month.PageLines.ToString("N0") + " SobekCM page requests");

                if (month.PageLines == 0)
                {
                    Warn("  No SobekCM page requests ( /sobekcm.aspx ) in these logs.  Skipped, so the month isn't locked in as zero.");
                    month.Action = Month_Action.Skipped_No_Page_Requests;
                    Delete_Quietly(monthFolder);
                    Console.WriteLine();
                    continue;
                }

                // Run the Builder's own reader for just this month
                bool processorError = false;
                var processor = new SobekCM_Stats_Reader_Processor(logFolder, dataFolder, siteDirectory, new List<string> { month.YearMonth.ToString() });
                processor.New_Status += (Message, IsError) =>
                {
                    if (IsError)
                    {
                        processorError = true;
                        Error("  " + Message);
                    }
                    else
                    {
                        Console.WriteLine("  " + Message);
                    }
                };
                processor.Process_IIS_Logs();

                // Did it land?
                string verify = Verify_Month(month.Year, month.Month);
                if ((verify == null) && !processorError)
                {
                    Console.WriteLine("  Saved and aggregated.");
                    month.Action = Month_Action.Calculated;
                    calculated++;
                }
                else
                {
                    Error("  " + (verify ?? "The reader reported errors (see above); check this month in the database."));
                    month.Action = Month_Action.Failed;
                    failed++;
                }

                // The staged logs can be large; the daily/monthly XML datasets are kept for reference
                if (!options.KeepLogs)
                    Delete_Quietly(logFolder);
                Console.WriteLine();
            }

            // The public site caches the overall usage
            if (calculated > 0)
            {
                try
                {
                    string cacheFile = Path.Combine(siteDirectory, "temp", "overall_usage.xml");
                    if (File.Exists(cacheFile))
                    {
                        File.Delete(cacheFile);
                        Console.WriteLine("Deleted the cached " + cacheFile);
                    }
                }
                catch (Exception ee)
                {
                    Warn("Could not delete temp\\overall_usage.xml: " + ee.Message);
                }
            }

            Console.WriteLine();
            Write_Report(months, true);
            Console.WriteLine(calculated + " month(s) calculated, " + failed + " failed.");
            if (calculated > 0)
                Console.WriteLine("Reset the web site's cache ( or recycle its app pool ) so it sees the new months.");
            Console.WriteLine("Daily and monthly datasets are in " + workRoot);

            bool archived = true;
            if (!options.NoArchive)
            {
                Console.WriteLine();
                archived = Archive_By_Month(options.LogDirectory, months, allCopies, zipsWithOtherFiles, true);
            }

            return ((failed > 0) || !archived) ? 2 : 0;
        }

        #region Zipping the logs back up by month

        /// <summary> Writes each month's logs into one zip in the log folder ( IIS_Logs_YYYY-MM.zip ), checks it,
        /// then deletes the loose logs and any older zip holding nothing but logs of months now archived </summary>
        /// <returns> FALSE if any month could not be archived </returns>
        private static bool Archive_By_Month(string Log_Directory, List<Month_Info> Months, List<Log_Source> All_Copies, HashSet<string> Zips_With_Other_Files, bool Execute)
        {
            Console.WriteLine(Execute ? "Zipping the logs by month ..." : "Logs that would be zipped by month:");
            var archivedMonths = new HashSet<int>();
            var monthlyZips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool allGood = true;

            foreach (Month_Info month in Months)
            {
                string target = Path.Combine(Log_Directory, "IIS_Logs_" + month.Label + ".zip");
                monthlyZips.Add(target);

                // Every copy of this month's logs, wherever it is
                List<Log_Source> copies = All_Copies.Where(L => L.YearMonth == month.YearMonth).ToList();
                bool alreadyZipped = month.Logs.All(L => (L.ZipEntry != null) && String.Equals(L.Path, target, StringComparison.OrdinalIgnoreCase));
                if (alreadyZipped && !copies.Any(L => L.ZipEntry == null))
                {
                    archivedMonths.Add(month.YearMonth);
                    continue;   // the monthly zip already holds the best copy of every day
                }

                int looseCount = copies.Count(L => L.ZipEntry == null);
                if (!Execute)
                {
                    Console.WriteLine("  " + Path.GetFileName(target) + ": " + month.Logs.Count + " log(s)" + (looseCount > 0 ? ", then delete " + looseCount + " loose log(s)" : ""));
                    archivedMonths.Add(month.YearMonth);
                    continue;
                }

                string temp = target + ".tmp";
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);

                    using (ZipArchive zip = ZipFile.Open(temp, ZipArchiveMode.Create))
                    {
                        foreach (Log_Source log in month.Logs)
                        {
                            ZipArchiveEntry entry = zip.CreateEntry(log.FileName, CompressionLevel.Optimal);
                            entry.LastWriteTime = log.Modified;
                            using (Stream source = log.Open())
                            using (Stream output = entry.Open())
                                source.CopyTo(output);
                        }
                    }

                    // Check every log went in whole before anything is deleted
                    using (ZipArchive check = ZipFile.OpenRead(temp))
                    {
                        foreach (Log_Source log in month.Logs)
                        {
                            ZipArchiveEntry entry = check.GetEntry(log.FileName);
                            if ((entry == null) || (entry.Length != log.Length))
                                throw new IOException(log.FileName + " is missing or the wrong size in the new zip");
                        }
                    }

                    File.Move(temp, target, true);
                }
                catch (Exception ee)
                {
                    Error("  " + month.Label + ": not zipped, nothing deleted.  " + ee.Message);
                    try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                    allGood = false;
                    continue;
                }

                // The zip is good: remove the loose copies
                int deleted = 0;
                foreach (Log_Source loose in copies.Where(L => L.ZipEntry == null))
                {
                    try
                    {
                        File.Delete(loose.Path);
                        deleted++;
                    }
                    catch (Exception ee)
                    {
                        Warn("  Could not delete " + loose.Path + ": " + ee.Message);
                    }
                }
                archivedMonths.Add(month.YearMonth);
                Console.WriteLine("  " + Path.GetFileName(target) + ": " + month.Logs.Count + " log(s)" + (deleted > 0 ? ", deleted " + deleted + " loose log(s)" : ""));
            }

            // Older zips go once every log in them is in a monthly zip
            var oldZips = All_Copies.Where(L => L.ZipEntry != null).GroupBy(L => L.Path, StringComparer.OrdinalIgnoreCase);
            foreach (var oldZip in oldZips)
            {
                if (monthlyZips.Contains(oldZip.Key))
                    continue;
                if (Zips_With_Other_Files.Contains(oldZip.Key))
                {
                    Console.WriteLine("  Keeping " + oldZip.Key + " ( it holds other files too )");
                    continue;
                }
                if (!oldZip.All(L => archivedMonths.Contains(L.YearMonth)))
                {
                    Console.WriteLine("  Keeping " + oldZip.Key + " ( it holds logs of months not zipped by month )");
                    continue;
                }

                if (!Execute)
                {
                    Console.WriteLine("  then delete " + oldZip.Key);
                    continue;
                }
                try
                {
                    File.Delete(oldZip.Key);
                    Console.WriteLine("  Deleted " + oldZip.Key);
                }
                catch (Exception ee)
                {
                    Warn("  Could not delete " + oldZip.Key + ": " + ee.Message);
                }
            }

            Console.WriteLine();
            return allGood;
        }

        #endregion

        #region Finding the logs

        private static List<Log_Source> Scan_Logs(string Directory_Path, List<Log_Source> All_Copies, HashSet<string> Zips_With_Other_Files)
        {
            var found = All_Copies;

            foreach (string file in Directory.EnumerateFiles(Directory_Path, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using (ZipArchive zip = ZipFile.OpenRead(file))
                        {
                            foreach (ZipArchiveEntry entry in zip.Entries)
                            {
                                Log_Source zipped = Log_Source.From_Name(entry.Name);
                                if (zipped == null)
                                {
                                    // Anything else in the zip ( folders aside ) means it is never deleted
                                    if (entry.Name.Length > 0)
                                        Zips_With_Other_Files.Add(file);
                                    continue;
                                }
                                zipped.Path = file;
                                zipped.ZipEntry = entry.FullName;
                                zipped.Length = entry.Length;
                                zipped.Modified = entry.LastWriteTime;
                                found.Add(zipped);
                            }
                        }
                    }
                    catch (Exception ee)
                    {
                        Warn("  Can't read zip file " + file + ": " + ee.Message);
                    }
                }
                else
                {
                    Log_Source plain = Log_Source.From_Name(name);
                    if (plain == null) continue;
                    var info = new FileInfo(file);
                    plain.Path = file;
                    plain.Length = info.Length;
                    plain.Modified = info.LastWriteTime;
                    found.Add(plain);
                }
            }

            // The same day's log may be both loose and zipped, or in two zips: keep the biggest copy
            var unique = new List<Log_Source>();
            int duplicates = 0;
            foreach (var group in found.GroupBy(L => L.FileName.ToLowerInvariant()))
            {
                unique.Add(group.OrderByDescending(L => L.Length).ThenBy(L => L.ZipEntry == null ? 0 : 1).First());
                duplicates += group.Count() - 1;
            }

            int zippedCount = unique.Count(L => L.ZipEntry != null);
            Console.WriteLine("  Found " + unique.Count + " log(s) ( " + zippedCount + " inside zip files )" + (duplicates > 0 ? ", ignoring " + duplicates + " duplicate copies" : ""));
            return unique;
        }

        private static void Count_Lines(Stream Log_Stream, Month_Info Month)
        {
            using (var reader = new StreamReader(Log_Stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if ((line.Length == 0) || (line[0] == '#'))
                        continue;
                    Month.TotalLines++;

                    // Same test as SobekCM_Log_Reader.parse_line ( which also takes .msi / .zip downloads )
                    if ((line.IndexOf("/sobekcm.aspx", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (line.IndexOf("/sobekcm_data.aspx", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (line.IndexOf("/sobekcm_oai.aspx", StringComparison.OrdinalIgnoreCase) >= 0))
                        Month.PageLines++;
                }
            }
        }

        #endregion

        #region Database checks

        private static HashSet<int> Months_In_Database()
        {
            var months = new HashSet<int>();
            DataSet result = EalDbAccess.ExecuteDataset(Engine_Database.DatabaseType, Engine_Database.Connection_String, CommandType.StoredProcedure, "SobekCM_Get_Statistics_Dates");
            foreach (DataRow row in result.Tables[0].Rows)
                months.Add(Convert.ToInt32(row["Year"]) * 100 + Convert.ToInt32(row["Month"]));
            return months;
        }

        private static List<string> Tables_With_Rows(int Year, int Month)
        {
            var tables = new List<string>();
            foreach (string table in StatisticsTables)
            {
                string sql = "select count(*) from " + table + " where [Year]=" + Year + " and [Month]=" + Month;
                DataSet result = EalDbAccess.ExecuteDataset(Engine_Database.DatabaseType, Engine_Database.Connection_String, CommandType.Text, sql);
                if (Convert.ToInt32(result.Tables[0].Rows[0][0]) > 0)
                    tables.Add(table);
            }
            return tables;
        }

        /// <summary> Returns NULL when the month's top-level row exists and was aggregated, else what is wrong </summary>
        private static string Verify_Month(int Year, int Month)
        {
            string sql = "select top 1 isnull(Aggregate_Statistics_Complete, 0) from SobekCM_Statistics where [Year]=" + Year + " and [Month]=" + Month;
            DataSet result = EalDbAccess.ExecuteDataset(Engine_Database.DatabaseType, Engine_Database.Connection_String, CommandType.Text, sql);
            if (result.Tables[0].Rows.Count == 0)
                return "No SobekCM_Statistics row was saved for this month.";
            if (!Convert.ToBoolean(result.Tables[0].Rows[0][0]))
                return "The month was saved but not aggregated ( Aggregate_Statistics_Complete is false ).";
            return null;
        }

        #endregion

        #region Output

        private static void Write_Report(List<Month_Info> Months, bool Show_Lines)
        {
            Console.WriteLine("Month      Logs  Days    Size(MB)  " + (Show_Lines ? "  Requests   SobekCM  " : "") + "Status");
            Console.WriteLine(new string('-', Show_Lines ? 100 : 78));
            foreach (Month_Info month in Months)
            {
                string line = month.Label.PadRight(9)
                    + month.Logs.Count.ToString().PadLeft(5)
                    + (month.DaysCovered + "/" + month.DaysInMonth).PadLeft(7)
                    + (month.Logs.Sum(L => L.Length) / 1048576.0).ToString("N1", CultureInfo.InvariantCulture).PadLeft(11) + "  ";
                if (Show_Lines)
                {
                    bool counted = month.TotalLines > 0;
                    line += (counted ? month.TotalLines.ToString("N0") : "").PadLeft(10) + (counted ? month.PageLines.ToString("N0") : "").PadLeft(10) + "  ";
                }
                line += Status_Text(month.Action);
                if (!String.IsNullOrEmpty(month.Note))
                    line += "  ( " + month.Note + " )";
                Console.WriteLine(line);
            }
            Console.WriteLine();
        }

        private static string Status_Text(Month_Action Action)
        {
            switch (Action)
            {
                case Month_Action.Has_Statistics: return "already in database";
                case Month_Action.Calculate: return "CALCULATE";
                case Month_Action.Skipped_Incomplete: return "skipped: incomplete ( --allow-partial to include )";
                case Month_Action.Refused_Partial_Data: return "REFUSED: database has partial data";
                case Month_Action.Refused_Hourly: return "REFUSED";
                case Month_Action.Skipped_No_Page_Requests: return "skipped: no SobekCM page requests";
                case Month_Action.Calculated: return "calculated";
                case Month_Action.Failed: return "FAILED";
                default: return Action.ToString();
            }
        }

        private static void Warn(string Message)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Message);
            Console.ForegroundColor = previous;
        }

        private static void Error(string Message)
        {
            ConsoleColor previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ForegroundColor = previous;
        }

        private static void Delete_Quietly(string Folder)
        {
            try
            {
                if (Directory.Exists(Folder))
                    Directory.Delete(Folder, true);
            }
            catch { }
        }

        #endregion

        #region Small classes

        private enum Month_Action
        {
            Has_Statistics,
            Calculate,
            Skipped_Incomplete,
            Refused_Partial_Data,
            Refused_Hourly,
            Skipped_No_Page_Requests,
            Calculated,
            Failed
        }

        /// <summary> One day's IIS log, either a file on disk or an entry in a zip file </summary>
        private class Log_Source
        {
            public string FileName;
            public string Path;
            public string ZipEntry;
            public long Length;
            public DateTimeOffset Modified;
            public int Year;
            public int Month;
            public int Day;
            public bool Hourly;

            public int YearMonth => Year * 100 + Month;

            public static Log_Source From_Name(string Name)
            {
                Match match = LogNameRegex.Match(Name ?? String.Empty);
                if (!match.Success) return null;

                int year = 2000 + Int32.Parse(match.Groups[1].Value);
                int month = Int32.Parse(match.Groups[2].Value);
                int day = Int32.Parse(match.Groups[3].Value);
                if ((month < 1) || (month > 12) || (day < 1) || (day > DateTime.DaysInMonth(year, month)))
                    return null;

                return new Log_Source
                {
                    FileName = Name,
                    Year = year,
                    Month = month,
                    Day = day,
                    Hourly = match.Groups[4].Success
                };
            }

            public Stream Open()
            {
                if (ZipEntry == null)
                    return File.OpenRead(Path);

                // Hand back a stream that closes the zip with it
                ZipArchive zip = ZipFile.OpenRead(Path);
                ZipArchiveEntry entry = zip.GetEntry(ZipEntry);
                return new Zip_Entry_Stream(zip, entry.Open());
            }
        }

        private class Zip_Entry_Stream : Stream
        {
            private readonly ZipArchive zip;
            private readonly Stream inner;

            public Zip_Entry_Stream(ZipArchive Zip, Stream Inner)
            {
                zip = Zip;
                inner = Inner;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] Buffer, int Offset, int Count) => inner.Read(Buffer, Offset, Count);
            public override long Seek(long Offset, SeekOrigin Origin) => throw new NotSupportedException();
            public override void SetLength(long Value) => throw new NotSupportedException();
            public override void Write(byte[] Buffer, int Offset, int Count) => throw new NotSupportedException();

            protected override void Dispose(bool Disposing)
            {
                if (Disposing)
                {
                    inner.Dispose();
                    zip.Dispose();
                }
                base.Dispose(Disposing);
            }
        }

        private class Month_Info
        {
            public readonly int YearMonth;
            public readonly int Year;
            public readonly int Month;
            public readonly List<Log_Source> Logs;
            public readonly int DaysCovered;
            public readonly int DaysInMonth;
            public readonly bool Hourly;
            public Month_Action Action;
            public string Note;
            public long TotalLines;
            public long PageLines;

            public Month_Info(int Year_Month, List<Log_Source> Month_Logs)
            {
                YearMonth = Year_Month;
                Year = Year_Month / 100;
                Month = Year_Month % 100;
                Logs = Month_Logs.OrderBy(L => L.FileName, StringComparer.OrdinalIgnoreCase).ToList();
                DaysCovered = Logs.Select(L => L.Day).Distinct().Count();
                DaysInMonth = DateTime.DaysInMonth(Year, Month);
                Hourly = Logs.Any(L => L.Hourly);
            }

            public string Label => Year + "-" + Month.ToString("00");

            public string Missing_Days()
            {
                var have = new HashSet<int>(Logs.Select(L => L.Day));
                var missing = Enumerable.Range(1, DaysInMonth).Where(D => !have.Contains(D)).ToList();

                // Collapse runs: 3-9, 14, 20-31
                var parts = new List<string>();
                for (int i = 0; i < missing.Count; i++)
                {
                    int start = missing[i];
                    while ((i + 1 < missing.Count) && (missing[i + 1] == missing[i] + 1))
                        i++;
                    parts.Add(start == missing[i] ? start.ToString() : start + "-" + missing[i]);
                }
                return String.Join(", ", parts);
            }
        }

        private class Options
        {
            public string ConnectionString;
            public string LogDirectory;
            public string SiteDirectory;
            public string WorkDirectory;
            public bool Execute;
            public bool AllowPartial;
            public bool Scan;
            public bool KeepLogs;
            public bool NoArchive;
            public bool ArchiveOnly;
            public readonly HashSet<int> Months = new HashSet<int>();

            public static Options Parse(string[] Args)
            {
                var options = new Options();
                for (int i = 0; i < Args.Length; i++)
                {
                    string arg = Args[i].ToLowerInvariant();
                    string Next()
                    {
                        if (i + 1 >= Args.Length) throw new ArgumentException(arg + " needs a value");
                        return Args[++i];
                    }

                    try
                    {
                        switch (arg)
                        {
                            case "--connection": options.ConnectionString = Next(); break;
                            case "--logs": options.LogDirectory = Next(); break;
                            case "--site": options.SiteDirectory = Next(); break;
                            case "--work": options.WorkDirectory = Next(); break;
                            case "--execute": options.Execute = true; break;
                            case "--allow-partial": options.AllowPartial = true; break;
                            case "--scan": options.Scan = true; break;
                            case "--keep-logs": options.KeepLogs = true; break;
                            case "--no-archive": options.NoArchive = true; break;
                            case "--archive-only": options.ArchiveOnly = true; break;
                            case "--months":
                                foreach (string month in Next().Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    string digits = month.Replace("-", "").Replace("/", "");
                                    if ((digits.Length != 6) || !Int32.TryParse(digits, out int yearMonth))
                                        throw new ArgumentException("Bad month '" + month + "' ( use YYYY-MM )");
                                    options.Months.Add(yearMonth);
                                }
                                break;
                            case "-h":
                            case "--help":
                            case "/?":
                                return null;
                            default:
                                Console.WriteLine("Unknown argument: " + Args[i]);
                                return null;
                        }
                    }
                    catch (ArgumentException ee)
                    {
                        Console.WriteLine(ee.Message);
                        return null;
                    }
                }

                if (String.IsNullOrWhiteSpace(options.LogDirectory))
                    return null;
                if (String.IsNullOrWhiteSpace(options.ConnectionString) && !options.ArchiveOnly)
                    return null;
                if (options.ArchiveOnly && options.NoArchive)
                {
                    Console.WriteLine("--archive-only and --no-archive can't be used together");
                    return null;
                }
                return options;
            }

            public static void Write_Usage()
            {
                Console.WriteLine();
                Console.WriteLine("SobekCM_Stats_Backfill --connection \"<SQL Server connection string>\" --logs \"<IIS log folder>\" [options]");
                Console.WriteLine();
                Console.WriteLine("Fills in months missing from the usage statistics from old IIS logs ( u_exYYMMDD.log, loose or zipped,");
                Console.WriteLine("searched in subfolders too ).  A dry run unless --execute is given.");
                Console.WriteLine();
                Console.WriteLine("  --execute         Calculate and save the missing months");
                Console.WriteLine("  --months LIST     Only these months, e.g. 2023-01,2023-02");
                Console.WriteLine("  --allow-partial   Also calculate months whose logs don't cover every day ( locked in as they are )");
                Console.WriteLine("  --scan            Dry run: also read the logs and count SobekCM page requests per month");
                Console.WriteLine("  --site PATH       Web site folder ( default: the 'Application Server Network' setting )");
                Console.WriteLine("  --work PATH       Working folder ( default: %TEMP%\\SobekCM_Stats_Backfill\\<time> )");
                Console.WriteLine("  --keep-logs       Keep the unzipped / copied logs in the working folder");
                Console.WriteLine("  --no-archive      Don't zip the logs by month afterwards ( IIS_Logs_YYYY-MM.zip in the log folder,");
                Console.WriteLine("                    replacing the loose logs and older zips holding only those months' logs )");
                Console.WriteLine("  --archive-only    Only zip the logs by month ( no --connection needed, no statistics )");
                Console.WriteLine();
            }
        }

        #endregion
    }
}
