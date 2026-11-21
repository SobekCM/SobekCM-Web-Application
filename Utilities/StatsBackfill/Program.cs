#region Using directives

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EngineAgnosticLayerDbAccess;
using SobekCM.Builder_Library.Statistics;
using SobekCM.Core.Configuration;
using SobekCM.Core.MemoryMgmt;
using SobekCM.Core.Navigation;
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
            // Run behind the web sites: Windows gives IIS the CPU whenever it wants it
            if (!options.NormalPriority)
            {
                try
                {
                    System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
                }
                catch (Exception ee)
                {
                    Warn("Could not lower this tool's priority: " + ee.Message);
                }
            }

            if (!String.IsNullOrWhiteSpace(options.FromDatasets))
                return Run_From_Datasets(options);

            Console.WriteLine();
            Console.WriteLine("SobekCM usage statistics backfill" + (options.ArchiveOnly ? " ( zipping logs by month only )" : "")
                + (!String.IsNullOrWhiteSpace(options.FilterRobots) ? " ( writing logs without robot requests to " + options.FilterRobots + " )" : "")
                + (options.ReadOnly ? "  ( READ ONLY: logs read into the working folder, nothing saved or zipped )" : (options.Execute ? "" : "  ( DRY RUN: nothing is changed; add --execute )")));
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

            // Filtering may take this month's logs so far too ( kept to be read once the month is over )
            if (options.IncludeCurrentMonth)
                currentYearMonth = (now.Year * 100 + now.Month) + 1;
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

            // Nothing after the last month asked for with --through ( no limit by default )
            int skippedLater = logs.Count(L => (L.YearMonth > options.Through) && (L.YearMonth < currentYearMonth));
            if (skippedLater > 0)
                Console.WriteLine("  Ignoring " + skippedLater + " log(s) after " + (options.Through / 100) + "-" + (options.Through % 100).ToString("00") + " ( --through ).");
            months.RemoveAll(M => M.YearMonth > options.Through);
            if (months.Count == 0)
            {
                Error("No logs from earlier months" + (options.Months.Count > 0 ? " matching --months" : "") + ".");
                return 1;
            }

            // Just writing copies of the logs without their robot requests, to move elsewhere
            if (!String.IsNullOrWhiteSpace(options.FilterRobots))
                return Filter_Robots(options, months);

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
            {
                // Name what failed: the parts the reader uses stop the run, the rest only warn
                var needed = new List<(string Name, Func<bool> Refresh)>
                {
                    ("configuration", () => Engine_ApplicationCache_Gateway.RefreshConfiguration(dbInstance)),
                    ("collections", Engine_ApplicationCache_Gateway.RefreshCodes),
                    ("collection aliases", Engine_ApplicationCache_Gateway.RefreshCollectionAliases),
                    ("portals", Engine_ApplicationCache_Gateway.RefreshUrlPortals),
                    ("web content pages", Engine_ApplicationCache_Gateway.RefreshWebContentHierarchy)
                };
                var other = new List<(string Name, Func<bool> Refresh)>
                {
                    ("statistics dates", Engine_ApplicationCache_Gateway.RefreshStatsDateRange),
                    ("web skins", Engine_ApplicationCache_Gateway.RefreshWebSkins),
                    ("IP restrictions", Engine_ApplicationCache_Gateway.RefreshIP_Restrictions),
                    ("thematic headings", Engine_ApplicationCache_Gateway.RefreshThematicHeadings),
                    ("user groups", Engine_ApplicationCache_Gateway.RefreshUserGroups),
                    ("mime types", Engine_ApplicationCache_Gateway.RefreshMimeTypes),
                    ("icons", Engine_ApplicationCache_Gateway.RefreshIcons),
                    ("default metadata templates", Engine_ApplicationCache_Gateway.RefreshDefaultMetadataTemplates),
                    ("titles", Engine_ApplicationCache_Gateway.RefreshTitles)
                };

                var neededFailed = needed.Where(N => !N.Refresh()).Select(N => N.Name).ToList();
                var otherFailed = other.Where(N => !N.Refresh()).Select(N => N.Name).ToList();
                if (otherFailed.Count > 0)
                    Warn("  Did not load ( not used for statistics ): " + String.Join(", ", otherFailed));
                if (neededFailed.Count > 0)
                {
                    Error("Could not load what the statistics reader needs: " + String.Join(", ", neededFailed) + "  ( " + Engine_Database.Last_Exception?.Message + " )");
                    return 1;
                }
            }
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
                bool recalculate = options.Recalculate.Contains(month.YearMonth);
                if (monthsInDatabase.Contains(month.YearMonth) && !recalculate)
                {
                    month.Action = Month_Action.Has_Statistics;
                    continue;
                }

                List<string> leftovers = Tables_With_Rows(month.Year, month.Month);
                if (recalculate && (leftovers.Count > 0))
                    month.Replace = true;
                if ((leftovers.Count > 0) && !recalculate)
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
            foreach (Month_Info month in months.Where(M => M.Replace && (M.Action == Month_Action.Calculate)))
                month.Note = (String.IsNullOrEmpty(month.Note) ? "" : month.Note + "; ") + "REPLACES the month in the database";
            if (!Recalculate_Months_Found(options, months.Select(M => M.YearMonth)))
                return 1;

            // Reading only ( to check the results ): any month with daily logs, whatever the database holds
            if (options.ReadOnly)
            {
                foreach (Month_Info month in months.Where(M => !M.Hourly))
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
                long converted = 0;
                foreach (Log_Source log in month.Logs)
                {
                    // The new site's page requests are rewritten into the old form as they are staged
                    string target = Path.Combine(logFolder, log.FileName);
                    using (Stream source = log.Open())
                    using (FileStream output = File.Create(target))
                        converted += Copy_Converted(source, output);
                    using (FileStream staged = File.OpenRead(target))
                        Count_Lines(staged, month);
                }
                Console.WriteLine("  Staged " + month.Logs.Count + " log(s): " + month.TotalLines.ToString("N0") + " requests, " + month.PageLines.ToString("N0") + " SobekCM page requests"
                    + (converted > 0 ? " ( " + converted.ToString("N0") + " of them from the new site's addresses )" : ""));

                if (month.PageLines == 0)
                {
                    Warn("  No SobekCM page requests ( /sobekcm.aspx ) in these logs.  Skipped, so the month isn't locked in as zero.");
                    month.Action = Month_Action.Skipped_No_Page_Requests;
                    Delete_Quietly(monthFolder);
                    Console.WriteLine();
                    continue;
                }

                // Recalculating: the month's old rows go first ( the aggregate step works out the
                // items' totals again from every month's rows, so nothing is counted twice )
                if (month.Replace && !options.ReadOnly)
                {
                    string deleteProblem = Delete_Month(month.Year, month.Month);
                    if (deleteProblem != null)
                    {
                        Error("  Could not delete the month's existing statistics: " + deleteProblem);
                        month.Action = Month_Action.Failed;
                        failed++;
                        Delete_Quietly(monthFolder);
                        Console.WriteLine();
                        continue;
                    }
                    Console.WriteLine("  Deleted the month's existing statistics");
                }

                // Run the Builder's own reader for just this month
                bool processorError = false;
                var processor = new SobekCM_Stats_Reader_Processor(logFolder, dataFolder, siteDirectory, new List<string> { month.YearMonth.ToString() })
                {
                    Max_Parallel_Logs = options.Threads,
                    Read_Only = options.ReadOnly
                };
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
                var watch = System.Diagnostics.Stopwatch.StartNew();
                processor.Process_IIS_Logs();
                Console.WriteLine("  Took " + watch.Elapsed.ToString(@"h\:mm\:ss") + " ( " + options.Threads + " thread" + (options.Threads == 1 ? "" : "s") + " )");

                if (options.ReadOnly)
                {
                    if (processorError)
                    {
                        month.Action = Month_Action.Failed;
                        failed++;
                    }
                    else
                    {
                        month.Action = Month_Action.Read_Only;
                        calculated++;
                    }
                    if (!options.KeepLogs)
                        Delete_Quietly(logFolder);
                    Console.WriteLine();
                    continue;
                }

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
            if ((calculated > 0) && !options.ReadOnly)
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
            Console.WriteLine(calculated + " month(s) " + (options.ReadOnly ? "read ( nothing saved )" : "calculated") + ", " + failed + " failed.");
            if ((calculated > 0) && !options.ReadOnly)
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

        #region Reading the ASP.NET Core ( 6.0 ) logs

        /// <summary> Rewrites the page requests of the ASP.NET Core site ( "/AA00001234/00001" ) into the form the old
        /// WebForms site logged ( "/sobekcm.aspx?urlrelative=AA00001234/00001" ), so the statistics reader counts them.
        /// Old lines, and new lines the reader must not count, are returned unchanged ( the reader ignores them ). </summary>
        /// <remarks> One instance per log: it remembers the log's #Fields line. Safe for mixed days around each site's switch
        /// ( mid-August 2026 ), since every line is judged on its own. The old site logged only the rewritten form, never the
        /// pretty address, so nothing is counted twice. </remarks>
        private class Log_Line_Converter
        {
            private int stemField = -1, queryField = -1, methodField = -1, statusField = -1;

            /// <summary> Lines rewritten so far </summary>
            public long Converted;

            private static readonly Regex Not_A_Page_Regex = new Regex(
                @"(?ix) \.(css|js|mjs|map|png|jpe?g|gif|svg|ico|webp|avif|bmp|tiff?|jp2|woff2?|ttf|eot|otf|txt|xml|json|pdf|zip|msi|mp3|mp4|m4a|wav|webm|ogg|php\d?|asp|aspx|ashx|axd|svc|cgi|pl|py|sh|env|ini|bak|sql|log|yml|yaml|config|htaccess)$
                    | ^/[._~]
                    | /\.
                    | ^/(wp|wordpress|wp-[^/]*|xmlrpc|phpmyadmin|cgi-bin|vendor|node_modules|\.well-known)(/|$)
                    | ^/(content|design|default|mysobek|data|engine|cache|notifications|browserpass|csp-report|version|item|rss|files|iiif|media|signin|adminsignin|datatables|languagecss|helpai|usersearch|designupload|submissionimage|processattachment|bannersample|itemtypeicon|extensionicon|platformicon|metadatacardsummary|metadatablockpreview|facetsuggest|authoritysearch|api|robots\.txt|favicon\.ico|sitemap[^/]*|apple-touch-icon[^/]*)(/|$)",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

            /// <summary> Returns the line to give the reader: rewritten for a page the new site served, else unchanged </summary>
            public string Convert(string Line)
            {
                if ((Line.Length == 0) || (Line[0] == '#'))
                {
                    if (Line.StartsWith("#Fields:", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] fields = Line.Substring(8).Trim().Split(' ');
                        stemField = Array.IndexOf(fields, "cs-uri-stem");
                        queryField = Array.IndexOf(fields, "cs-uri-query");
                        methodField = Array.IndexOf(fields, "cs-method");
                        statusField = Array.IndexOf(fields, "sc-status");
                    }
                    return Line;
                }
                if ((stemField < 0) || (queryField < 0) || (methodField < 0) || (statusField < 0))
                    return Line;

                string[] parts = Line.Split(' ');
                if (parts.Length <= Math.Max(Math.Max(stemField, queryField), Math.Max(methodField, statusField)))
                    return Line;

                string stem = parts[stemField];
                if ((stem.Length == 0) || (stem[0] != '/') || (parts[methodField] != "GET"))
                    return Line;

                // Only pages actually served: the new site answers bots with redirects, refusals and missing pages
                string status = parts[statusField];
                if ((status != "200") && (status != "304"))
                    return Line;

                // Old-format lines, files, the site's own endpoints and probes stay as they are
                if (Not_A_Page_Regex.IsMatch(stem))
                    return Line;

                string query = parts[queryField];
                string relative = stem.Trim('/');
                string newQuery = (relative.Length == 0)
                    ? ((query == "-") ? "-" : query)
                    : "urlrelative=" + relative + ((query == "-") ? "" : "&" + query);
                parts[stemField] = "/sobekcm.aspx";
                parts[queryField] = newQuery;
                Converted++;
                return String.Join(" ", parts);
            }
        }

        /// <summary> Copies a log, rewriting the new site's page requests for the reader </summary>
        /// <returns> How many lines were rewritten </returns>
        private static long Copy_Converted(Stream Input, Stream Output)
        {
            var converter = new Log_Line_Converter();
            using (var reader = new StreamReader(Input))
            using (var writer = new StreamWriter(Output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    writer.WriteLine(converter.Convert(line));
            }
            return converter.Converted;
        }

        #endregion

        #region Writing logs without their robot requests ( --filter-robots )

        /// <summary> Writes each month's logs, keeping only the lines the statistics reader would count as views, into
        /// IIS_Logs_YYYY-MM_filtered.zip in the output folder: every robot request ( by the reader's own robot check ),
        /// static file, POST and unreadable line is dropped, and each log ends with a remark saying how many robot
        /// requests it dropped, which the reader adds to the month's robot hits.  Needs no database. </summary>
        private static int Filter_Robots(Options options, List<Month_Info> Months)
        {
            string output = options.FilterRobots;
            Console.WriteLine((options.Execute ? "Filtering" : "Would filter") + " " + Months.Count + " month(s) into " + output);
            Console.WriteLine();

            if (options.Execute)
                Directory.CreateDirectory(output);

            int failed = 0;
            long totalIn = 0, totalOut = 0;
            foreach (Month_Info month in Months)
            {
                string zipPath = Path.Combine(output, "IIS_Logs_" + month.Label + "_filtered.zip");
                long sizeIn = month.Logs.Sum(L => L.Length);
                if (File.Exists(zipPath))
                {
                    Console.WriteLine(month.Label + ": " + Path.GetFileName(zipPath) + " is already there, so left alone");
                    continue;
                }
                if (!options.Execute)
                {
                    Console.WriteLine(month.Label + ": " + month.Logs.Count + " log(s), " + (sizeIn / 1048576.0).ToString("N1", CultureInfo.InvariantCulture) + " MB"
                        + (month.DaysCovered < month.DaysInMonth ? "  ( missing days " + month.Missing_Days() + " )" : ""));
                    continue;
                }

                var watch = System.Diagnostics.Stopwatch.StartNew();
                string tempFolder = Path.Combine(output, "_filtering_" + month.YearMonth);
                Delete_Quietly(tempFolder);
                Directory.CreateDirectory(tempFolder);
                long kept = 0, robots = 0;
                var problems = new System.Collections.Concurrent.ConcurrentBag<string>();
                Parallel.ForEach(month.Logs, new ParallelOptions { MaxDegreeOfParallelism = options.Threads }, log =>
                {
                    try
                    {
                        using (Stream input = log.Open())
                        {
                            (long logKept, long logRobots) = Filter_Log(input, Path.Combine(tempFolder, log.FileName), options.ExcludeIps, options.Strict);
                            System.Threading.Interlocked.Add(ref kept, logKept);
                            System.Threading.Interlocked.Add(ref robots, logRobots);
                        }
                    }
                    catch (Exception ee)
                    {
                        problems.Add(log.FileName + ": " + ee.Message);
                    }
                });

                if (!problems.IsEmpty)
                {
                    foreach (string problem in problems)
                        Error("  " + problem);
                    Error(month.Label + ": FAILED, nothing written for this month");
                    Delete_Quietly(tempFolder);
                    failed++;
                    continue;
                }

                // One zip per month, written aside and put in place only when complete
                string tempZip = zipPath + ".tmp";
                if (File.Exists(tempZip))
                    File.Delete(tempZip);
                using (ZipArchive zip = ZipFile.Open(tempZip, ZipArchiveMode.Create))
                {
                    foreach (Log_Source log in month.Logs)
                        zip.CreateEntryFromFile(Path.Combine(tempFolder, log.FileName), log.FileName, CompressionLevel.Optimal);
                }
                File.Move(tempZip, zipPath);
                Delete_Quietly(tempFolder);

                long sizeOut = new FileInfo(zipPath).Length;
                totalIn += sizeIn;
                totalOut += sizeOut;
                Console.WriteLine(month.Label + ": " + month.Logs.Count + " log(s), " + (sizeIn / 1048576.0).ToString("N1", CultureInfo.InvariantCulture) + " MB -> "
                    + (sizeOut / 1048576.0).ToString("N1", CultureInfo.InvariantCulture) + " MB zipped;  kept " + kept.ToString("N0") + " request(s), dropped "
                    + robots.ToString("N0") + " robot request(s)  ( " + watch.Elapsed.ToString(@"h\:mm\:ss") + " )"
                    + (month.DaysCovered < month.DaysInMonth ? "  ( missing days " + month.Missing_Days() + " )" : ""));
            }

            Console.WriteLine();
            if (options.Execute)
            {
                Console.WriteLine("Done: " + (totalIn / 1048576.0).ToString("N0", CultureInfo.InvariantCulture) + " MB of logs -> "
                    + (totalOut / 1048576.0).ToString("N0", CultureInfo.InvariantCulture) + " MB of zips, " + failed + " month(s) failed.");
                Console.WriteLine("Read them elsewhere with --logs pointed at the folder holding just these zips.");
            }
            else
            {
                Console.WriteLine("Run again with --execute to do this.");
            }
            return failed > 0 ? 2 : 0;
        }

        /// <summary> Copies one log, keeping its remarks and the lines the reader would count as views, and ends it with
        /// the number of robot requests dropped </summary>
        private static (long Kept, long Robots) Filter_Log(Stream Input, string Output_File, List<string> Excluded_Ips, bool Strict)
        {
            long kept = 0, robots = 0;
            int statusField = -1;
            using (var reader = new StreamReader(Input))
            using (var writer = new StreamWriter(Output_File))
            {
                string line;
                var converter = new Log_Line_Converter();
                while ((line = reader.ReadLine()) != null)
                {
                    // The new site's page requests, rewritten into the old form the reader counts
                    line = converter.Convert(line);

                    if ((line.Length > 0) && (line[0] == '#'))
                    {
                        // Where the status is, for --strict
                        if (line.StartsWith("#Fields:", StringComparison.OrdinalIgnoreCase))
                            statusField = Array.IndexOf(line.Substring(8).Trim().Split(' '), "sc-status");

                        // A log filtered before: carry its count on rather than copying the remark
                        if (line.StartsWith(SobekCM_Log_Reader.ROBOT_HITS_REMARK, StringComparison.OrdinalIgnoreCase))
                        {
                            if (Int64.TryParse(line.Substring(SobekCM_Log_Reader.ROBOT_HITS_REMARK.Length).Trim(), out long earlier))
                                robots += earlier;
                        }
                        else
                        {
                            writer.WriteLine(line);
                        }
                        continue;
                    }

                    // Lines the reader never counts at all ( static files, POSTs, unreadable lines )
                    if (!SobekCM_Log_Reader.Parse_Line(line, out _, out string ip, out _, out string url, out string userAgent))
                        continue;

                    // Addresses named with --exclude-ips ( scrapers posing as browsers ), and the server asking
                    // itself ( keep-alive scripts, which the reader also counts as robots ), count as robots
                    if (Is_Excluded_Ip(ip, Excluded_Ips) || (ip == "127.0.0.1") || (ip == "::1"))
                    {
                        robots++;
                        continue;
                    }

                    // The reader's own robot check ( IIS writes the user agent's spaces as '+' ), which it only
                    // makes for page requests: a download ( .zip, .msi ) always counts as a view
                    bool isPage = url.IndexOf(".ASPX", StringComparison.OrdinalIgnoreCase) >= 0;
                    string agent = userAgent?.Replace('+', ' ').Trim();
                    if (isPage && Navigation_Object.Is_UserAgent_Robot(agent))
                    {
                        robots++;
                        continue;
                    }

                    if (Strict)
                    {
                        // Browser strings no real browser sends, and link-preview fetchers, are robots too
                        if (isPage && (Is_Impossible_Browser(agent) || Is_Link_Preview(agent) || Is_Unlisted_Crawler(agent)))
                        {
                            robots++;
                            continue;
                        }

                        // Only a page actually served is a view: no redirects, refusals or missing pages
                        // ( a download may also be served in parts, 206 )
                        if (statusField >= 0)
                        {
                            string[] parts = line.Split(' ');
                            string status = (parts.Length > statusField) ? parts[statusField] : String.Empty;
                            bool served = (status == "200") || (status == "304") || (!isPage && (status == "206"));
                            if (!served)
                                continue;
                        }
                    }

                    writer.WriteLine(line);
                    kept++;
                }
                writer.WriteLine(SobekCM_Log_Reader.ROBOT_HITS_REMARK + " " + robots);
            }
            return (kept, robots);
        }

        /// <summary> Chrome on iOS with a reduced "CriOS/147.0.0.0" version, which real Chrome on iOS never sends </summary>
        /// <remarks> Copied from 6.0's UserAgentBlocklist_Gateway, where DCDP's October 2026 logs showed these strings
        /// coming from a botnet of residential addresses </remarks>
        private static readonly Regex Fake_CriOS_Regex = new Regex(@"CriOS/\d+\.0\.0\.0 ", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary> Stock Chrome on Android naming a phone model with a reduced version: real Chrome on Android always
        /// says "Android 10; K" since Chrome 110 ( copied from 6.0's UserAgentBlocklist_Gateway ) </summary>
        private static readonly Regex Fake_Android_Chrome_Regex = new Regex(
            @"\(Linux; Android [\d.]+; (?!K\))[^)]+\) AppleWebKit/537\.36 \(KHTML, like Gecko\) Chrome/\d+\.0\.0\.0 Mobile Safari/537\.36$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static bool Is_Impossible_Browser(string Agent)
        {
            if (String.IsNullOrEmpty(Agent))
                return false;
            if (Agent.Contains("CriOS/", StringComparison.Ordinal) && Fake_CriOS_Regex.IsMatch(Agent))
                return true;
            return Agent.Contains("Android", StringComparison.Ordinal) && Fake_Android_Chrome_Regex.IsMatch(Agent);
        }

        /// <summary> Social link-preview fetchers: served normal pages so shared links get their card, but never a reader </summary>
        private static readonly string[] Link_Preview_Tokens =
        {
            "facebookexternalhit", "facebookcatalog", "Twitterbot", "LinkedInBot", "Slackbot", "Discordbot", "WhatsApp", "TelegramBot", "Pinterestbot", "redditbot", "Embedly", "SkypeUriPreview", "vkShare"
        };

        private static bool Is_Link_Preview(string Agent)
        {
            if (String.IsNullOrEmpty(Agent))
                return false;
            foreach (string token in Link_Preview_Tokens)
            {
                if (Agent.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary> Crawlers and scripts that say what they are, in a form the robot list misses ( "SeekportBot;" has
        /// no "BOT/", a script names its library ) </summary>
        private static readonly string[] Unlisted_Crawler_Tokens =
        {
            "SeekportBot", "Scrapy", "python-requests", "python-urllib", "aiohttp", "httpx", "curl/", "Wget/",
            "Go-http-client", "HeadlessChrome", "Lightpanda", "okhttp", "node-fetch", "axios/", "libwww-perl", "Apache-HttpClient",
            "StatusCake", "Site24x7", "Better Uptime", "BetterStack", "Datadog", "UptimeRobot", "Uptime-Kuma", "Pingdom", "GoogleStackdriverMonitoring", "CloudWatchSynthetics"
        };

        private static bool Is_Unlisted_Crawler(string Agent)
        {
            // No user agent at all is a script or a monitor ( IIS logs it as "-" ), never a browser
            if (String.IsNullOrEmpty(Agent) || (Agent == "-"))
                return true;

            // Every browser's user agent starts "Mozilla/" ( old Opera "Opera/" ): anything else is a program
            if (!Agent.StartsWith("Mozilla/", StringComparison.OrdinalIgnoreCase) && !Agent.StartsWith("Opera/", StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (string token in Unlisted_Crawler_Tokens)
            {
                if (Agent.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary> Is this address one named with --exclude-ips ( exactly, or under a "1.2.*" range )? </summary>
        private static bool Is_Excluded_Ip(string Ip, List<string> Excluded)
        {
            if ((Excluded == null) || (Excluded.Count == 0) || String.IsNullOrEmpty(Ip))
                return false;
            foreach (string address in Excluded)
            {
                if (address.EndsWith("*"))
                {
                    if (Ip.StartsWith(address.Substring(0, address.Length - 1), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else if (String.Equals(Ip, address, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Saving datasets read elsewhere ( --from-datasets )

        private static readonly Regex DailyDatasetRegex = new Regex(@"^(\d{4})(\d{2})(\d{2})\.xml$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary> Saves the monthly datasets of a --read-only working folder ( {YYYYMM}\data\{YYYYMM}.xml ),
        /// with the same checks as a normal run, but reading no logs </summary>
        private static int Run_From_Datasets(Options options)
        {
            Console.WriteLine();
            Console.WriteLine("SobekCM usage statistics backfill  ( saving datasets from " + options.FromDatasets + " )"
                + (options.Execute ? "" : "  ( DRY RUN: nothing is changed; add --execute )"));
            Console.WriteLine();

            if (!Directory.Exists(options.FromDatasets))
            {
                Error("Dataset folder not found: " + options.FromDatasets);
                return 1;
            }

            // ---- Find each month's dataset ----
            DateTime now = DateTime.Now;
            int currentYearMonth = now.Year * 100 + now.Month;
            var months = new List<Dataset_Month>();
            foreach (string folder in Directory.GetDirectories(options.FromDatasets).OrderBy(F => F, StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(folder);
                if ((name.Length != 6) || !Int32.TryParse(name, out int yearMonth) || (yearMonth % 100 < 1) || (yearMonth % 100 > 12))
                    continue;
                if ((yearMonth >= currentYearMonth) || (yearMonth > options.Through))
                    continue;
                if ((options.Months.Count > 0) && !options.Months.Contains(yearMonth))
                    continue;

                string dataFolder = Path.Combine(folder, "data");
                string monthly = Path.Combine(dataFolder, name + ".xml");
                var month = new Dataset_Month(yearMonth, dataFolder);
                if (!File.Exists(monthly))
                {
                    month.Action = Month_Action.Failed;
                    month.Note = "no " + name + ".xml in " + dataFolder;
                }
                else
                {
                    month.Size = new FileInfo(monthly).Length;
                    foreach (string daily in Directory.GetFiles(dataFolder, "*.xml"))
                    {
                        Match match = DailyDatasetRegex.Match(Path.GetFileName(daily));
                        if (match.Success && (Convert.ToInt32(match.Groups[1].Value + match.Groups[2].Value) == yearMonth))
                            month.Days.Add(Convert.ToInt32(match.Groups[3].Value));
                    }
                }
                months.Add(month);
            }
            if (months.Count == 0)
            {
                Error("No month folders ( YYYYMM\\data\\YYYYMM.xml ) found in " + options.FromDatasets + (options.Months.Count > 0 ? " matching --months" : "") + ".");
                return 1;
            }

            // ---- Connect ( only the database is needed: the datasets are already read ) ----
            Console.WriteLine("Connecting to the database ...");
            var dbInstance = new Database_Instance_Configuration { Connection_String = options.ConnectionString };
            Engine_Database.Connection_String = options.ConnectionString;
            Engine_Database.DatabaseType = EalDbTypeEnum.MSSQL;
            InstanceWide_Settings dbSettings = null;
            try
            {
                dbSettings = InstanceWide_Settings_Builder.Build_Settings(dbInstance);
            }
            catch (Exception ee)
            {
                Engine_Database.Last_Exception = ee;
            }
            if (dbSettings == null)
            {
                Error("Unable to read the settings from the database: " + Engine_Database.Last_Exception?.Message);
                return 1;
            }
            Console.WriteLine("  Library: " + dbSettings.System.System_Name);
            Console.WriteLine();

            // ---- The same checks as a normal run ----
            HashSet<int> monthsInDatabase = Months_In_Database();
            foreach (Dataset_Month month in months.Where(M => M.Action != Month_Action.Failed))
            {
                bool recalculate = options.Recalculate.Contains(month.YearMonth);
                if (monthsInDatabase.Contains(month.YearMonth) && !recalculate)
                {
                    month.Action = Month_Action.Has_Statistics;
                    continue;
                }

                List<string> leftovers = Tables_With_Rows(month.Year, month.Month);
                if (recalculate && (leftovers.Count > 0))
                    month.Replace = true;
                if ((leftovers.Count > 0) && !recalculate)
                {
                    month.Action = Month_Action.Refused_Partial_Data;
                    month.Note = "rows already in " + String.Join(", ", leftovers);
                    continue;
                }

                if (month.Days.Count < month.DaysInMonth)
                {
                    month.Note = "missing days " + month.Missing_Days();
                    month.Action = options.AllowPartial ? Month_Action.Calculate : Month_Action.Skipped_Incomplete;
                    continue;
                }

                month.Action = Month_Action.Calculate;
            }
            foreach (Dataset_Month month in months.Where(M => M.Replace && (M.Action == Month_Action.Calculate)))
                month.Note = (String.IsNullOrEmpty(month.Note) ? "" : month.Note + "; ") + "REPLACES the month in the database";
            if (!Recalculate_Months_Found(options, months.Select(M => M.YearMonth)))
                return 1;

            Write_Dataset_Report(months);

            List<Dataset_Month> toSave = months.Where(M => M.Action == Month_Action.Calculate).ToList();
            if (!options.Execute)
            {
                Console.WriteLine(toSave.Count == 0 ? "Nothing to save." : toSave.Count + " month(s) would be saved.");
                Console.WriteLine("Run again with --execute to do this.");
                return 0;
            }
            if (toSave.Count == 0)
            {
                Console.WriteLine("Nothing to save.");
                return 0;
            }

            // ---- Save, one month at a time ----
            int saved = 0, failed = 0;
            foreach (Dataset_Month month in toSave)
            {
                Console.WriteLine("==== " + month.Label + " ====");
                bool processorError = false;
                var processor = new SobekCM_Stats_Reader_Processor(month.DataFolder, month.DataFolder, String.Empty, new List<string> { month.YearMonth.ToString() });
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
                if (month.Replace)
                {
                    string deleteProblem = Delete_Month(month.Year, month.Month);
                    if (deleteProblem != null)
                    {
                        Error("  Could not delete the month's existing statistics: " + deleteProblem);
                        month.Action = Month_Action.Failed;
                        failed++;
                        Console.WriteLine();
                        continue;
                    }
                    Console.WriteLine("  Deleted the month's existing statistics");
                }

                var watch = System.Diagnostics.Stopwatch.StartNew();
                processor.Save_Monthly_Datasets();
                Console.WriteLine("  Took " + watch.Elapsed.ToString(@"h\:mm\:ss"));

                string verify = Verify_Month(month.Year, month.Month);
                if ((verify == null) && !processorError)
                {
                    Console.WriteLine("  Saved and aggregated.");
                    month.Action = Month_Action.Calculated;
                    saved++;
                }
                else
                {
                    Error("  " + (verify ?? "The reader reported errors (see above); check this month in the database."));
                    month.Action = Month_Action.Failed;
                    failed++;
                }
                Console.WriteLine();
            }

            // The public site caches the overall usage
            if (saved > 0)
            {
                string siteDirectory = String.IsNullOrWhiteSpace(options.SiteDirectory) ? dbSettings.Servers.Application_Server_Network : options.SiteDirectory;
                try
                {
                    if (!String.IsNullOrWhiteSpace(siteDirectory))
                    {
                        string cacheFile = Path.Combine(siteDirectory, "temp", "overall_usage.xml");
                        if (File.Exists(cacheFile))
                        {
                            File.Delete(cacheFile);
                            Console.WriteLine("Deleted the cached " + cacheFile);
                        }
                    }
                }
                catch (Exception ee)
                {
                    Warn("Could not delete temp\\overall_usage.xml: " + ee.Message);
                }
            }

            Console.WriteLine();
            Write_Dataset_Report(months);
            Console.WriteLine(saved + " month(s) saved, " + failed + " failed.");
            if (saved > 0)
                Console.WriteLine("Reset the web site's cache ( or recycle its app pool ) so it sees the new months.");
            return failed > 0 ? 2 : 0;
        }

        private static void Write_Dataset_Report(List<Dataset_Month> Months)
        {
            Console.WriteLine("Month      Days  Dataset(MB)  Status");
            Console.WriteLine(new string('-', 78));
            foreach (Dataset_Month month in Months)
            {
                string line = month.Label.PadRight(9)
                    + (month.Days.Count + "/" + month.DaysInMonth).PadLeft(6)
                    + (month.Size / 1048576.0).ToString("N1", CultureInfo.InvariantCulture).PadLeft(13) + "  "
                    + Status_Text(month.Action);
                if (!String.IsNullOrEmpty(month.Note))
                    line += "  ( " + month.Note + " )";
                Console.WriteLine(line);
            }
            Console.WriteLine();
        }

        private class Dataset_Month
        {
            public readonly int YearMonth;
            public readonly int Year;
            public readonly int Month;
            public readonly int DaysInMonth;
            public readonly string DataFolder;
            public readonly HashSet<int> Days = new HashSet<int>();
            public long Size;
            public bool Replace;
            public Month_Action Action = Month_Action.Calculate;
            public string Note;

            public Dataset_Month(int Year_Month, string Data_Folder)
            {
                YearMonth = Year_Month;
                Year = Year_Month / 100;
                Month = Year_Month % 100;
                DaysInMonth = DateTime.DaysInMonth(Year, Month);
                DataFolder = Data_Folder;
            }

            public string Label => Year + "-" + Month.ToString("00");

            public string Missing_Days()
            {
                var missing = Enumerable.Range(1, DaysInMonth).Where(D => !Days.Contains(D)).ToList();
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

        #endregion

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

        /// <summary> Deletes one month from every statistics table, in one transaction </summary>
        /// <returns> NULL if it worked, else the error </returns>
        private static string Delete_Month(int Year, int Month)
        {
            var sql = new System.Text.StringBuilder("set xact_abort on; begin transaction; ");
            foreach (string table in StatisticsTables)
                sql.Append("delete from " + table + " where [Year]=" + Year + " and [Month]=" + Month + "; ");
            sql.Append("commit transaction;");
            try
            {
                EalDbAccess.ExecuteNonQuery(Engine_Database.DatabaseType, Engine_Database.Connection_String, CommandType.Text, sql.ToString());
            }
            catch (Exception ee)
            {
                return ee.Message;
            }
            List<string> left = Tables_With_Rows(Year, Month);
            return left.Count == 0 ? null : "rows still in " + String.Join(", ", left);
        }

        /// <summary> Every --recalculate month must have logs ( or datasets ) to recalculate it from </summary>
        private static bool Recalculate_Months_Found(Options options, IEnumerable<int> Months_Found)
        {
            var found = new HashSet<int>(Months_Found);
            var missing = options.Recalculate.Where(M => !found.Contains(M)).OrderBy(M => M).ToList();
            if (missing.Count == 0)
                return true;
            Error("--recalculate names month(s) with nothing to recalculate them from: " + String.Join(", ", missing.Select(M => (M / 100) + "-" + (M % 100).ToString("00"))));
            return false;
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
                case Month_Action.Read_Only: return "read ( nothing saved )";
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
            Read_Only,
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
            public bool Replace;
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

            /// <summary> Last month considered ( YYYYMM ); no limit by default ( the current month is always left out ).
            /// The new site's logs ( from mid-August 2026 ) are rewritten for the reader as they are staged </summary>
            public int Through = 999912;

            /// <summary> Run at normal priority, rather than below the web sites </summary>
            public bool NormalPriority;

            /// <summary> Only read the logs into the working folder's datasets ( to check results ), saving and zipping nothing </summary>
            public bool ReadOnly;

            /// <summary> Save the monthly datasets of a --read-only working folder, reading no logs ( run next to the database ) </summary>
            public string FromDatasets;

            /// <summary> Write the logs without their robot requests into this folder, as monthly zips, and stop </summary>
            public string FilterRobots;

            /// <summary> With --filter-robots: addresses also dropped as robots ( "1.2.3.4", or "184.154.*" for a range ) </summary>
            public readonly List<string> ExcludeIps = new List<string>();

            /// <summary> With --filter-robots: count only pages actually served ( 200/304 ), and treat impossible browser
            /// strings and link-preview fetchers as robots </summary>
            public bool Strict;

            /// <summary> With --filter-robots: also filter this month's logs so far ( never read or saved: a month is saved once ) </summary>
            public bool IncludeCurrentMonth;

            /// <summary> Daily logs read at the same time ( each holds its requests in memory, so capped at 4 by default ) </summary>
            public int Threads = Math.Max(1, Math.Min(Environment.ProcessorCount, 4));

            public readonly HashSet<int> Months = new HashSet<int>();

            /// <summary> Months already in the database to delete and calculate again </summary>
            public readonly HashSet<int> Recalculate = new HashSet<int>();

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
                            case "--normal-priority": options.NormalPriority = true; break;
                            case "--from-datasets": options.FromDatasets = Next(); break;
                            case "--filter-robots": options.FilterRobots = Next(); break;
                            case "--strict": options.Strict = true; break;
                            case "--include-current-month": options.IncludeCurrentMonth = true; break;
                            case "--exclude-ips":
                                foreach (string address in Next().Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    if (!Regex.IsMatch(address, @"^[0-9a-fA-F:.]+\*?$"))
                                        throw new ArgumentException("Bad --exclude-ips address '" + address + "' ( use 1.2.3.4 or 1.2.* )");
                                    options.ExcludeIps.Add(address);
                                }
                                break;
                            case "--read-only":
                                options.ReadOnly = true;
                                options.Execute = true;
                                options.NoArchive = true;
                                break;
                            case "--threads":
                                {
                                    string threads = Next();
                                    if (!Int32.TryParse(threads, out int threadCount) || (threadCount < 1) || (threadCount > 64))
                                        throw new ArgumentException("Bad --threads '" + threads + "' ( use 1 to 64 )");
                                    options.Threads = threadCount;
                                }
                                break;
                            case "--through":
                                {
                                    string through = Next();
                                    string digits = through.Replace("-", "").Replace("/", "");
                                    if ((digits.Length != 6) || !Int32.TryParse(digits, out int throughMonth) || (throughMonth % 100 < 1) || (throughMonth % 100 > 12))
                                        throw new ArgumentException("Bad --through '" + through + "' ( use YYYY-MM )");
                                    options.Through = throughMonth;
                                }
                                break;
                            case "--months":
                                foreach (string month in Next().Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    string digits = month.Replace("-", "").Replace("/", "");
                                    if ((digits.Length != 6) || !Int32.TryParse(digits, out int yearMonth))
                                        throw new ArgumentException("Bad month '" + month + "' ( use YYYY-MM )");
                                    options.Months.Add(yearMonth);
                                }
                                break;
                            case "--recalculate":
                                foreach (string month in Next().Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    string digits = month.Replace("-", "").Replace("/", "");
                                    if ((digits.Length != 6) || !Int32.TryParse(digits, out int yearMonth) || (yearMonth % 100 < 1) || (yearMonth % 100 > 12))
                                        throw new ArgumentException("Bad --recalculate month '" + month + "' ( use YYYY-MM )");
                                    options.Recalculate.Add(yearMonth);
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

                if (!String.IsNullOrWhiteSpace(options.FromDatasets))
                {
                    if (String.IsNullOrWhiteSpace(options.ConnectionString))
                        return null;
                    if (options.ReadOnly || options.ArchiveOnly || !String.IsNullOrWhiteSpace(options.LogDirectory))
                    {
                        Console.WriteLine("--from-datasets reads no logs: don't use it with --logs, --read-only or --archive-only");
                        return null;
                    }
                    options.NoArchive = true;
                    return options;
                }
                if (String.IsNullOrWhiteSpace(options.LogDirectory))
                    return null;
                if (!String.IsNullOrWhiteSpace(options.FilterRobots))
                {
                    if (options.ReadOnly || options.ArchiveOnly)
                    {
                        Console.WriteLine("--filter-robots can't be used with --read-only or --archive-only");
                        return null;
                    }
                    if (options.ExcludeIps.Count > 0)
                        Console.WriteLine("Also dropping as robots: " + String.Join(", ", options.ExcludeIps));
                    if (options.Strict)
                        Console.WriteLine("Strict: only pages served ( 200/304 ) count; impossible browsers and link previews are robots");
                    options.NoArchive = true;
                    return options;
                }
                if ((options.ExcludeIps.Count > 0) || options.Strict || options.IncludeCurrentMonth)
                {
                    Console.WriteLine("--exclude-ips, --strict and --include-current-month only work with --filter-robots ( filter the logs, then read the filtered zips )");
                    return null;
                }
                if (String.IsNullOrWhiteSpace(options.ConnectionString) && !options.ArchiveOnly)
                    return null;
                if (options.ReadOnly && options.ArchiveOnly)
                {
                    Console.WriteLine("--read-only and --archive-only can't be used together");
                    return null;
                }
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
                Console.WriteLine("  --through YYYY-MM Last month to consider ( default: every month before this one; the ASP.NET Core site's");
                Console.WriteLine("                    logs, from mid-August 2026, are read too )");
                Console.WriteLine("  --threads N       Daily logs read at the same time ( default: cores, at most 4; 1 = one at a time )");
                Console.WriteLine("  --normal-priority Don't run below the web sites' priority ( the default is below normal )");
                Console.WriteLine("  --read-only       Read the logs into datasets in the working folder and stop: nothing saved or zipped,");
                Console.WriteLine("                    any month with logs ( even one in the database ); to compare --threads settings");
                Console.WriteLine("  --allow-partial   Also calculate months whose logs don't cover every day ( locked in as they are )");
                Console.WriteLine("  --scan            Dry run: also read the logs and count SobekCM page requests per month");
                Console.WriteLine("  --site PATH       Web site folder ( default: the 'Application Server Network' setting )");
                Console.WriteLine("  --work PATH       Working folder ( default: %TEMP%\\SobekCM_Stats_Backfill\\<time> )");
                Console.WriteLine("  --keep-logs       Keep the unzipped / copied logs in the working folder");
                Console.WriteLine("  --no-archive      Don't zip the logs by month afterwards ( IIS_Logs_YYYY-MM.zip in the log folder,");
                Console.WriteLine("                    replacing the loose logs and older zips holding only those months' logs )");
                Console.WriteLine("  --archive-only    Only zip the logs by month ( no --connection needed, no statistics )");
                Console.WriteLine("  --recalculate LIST  Delete these months from the database and calculate them again, e.g. 2025-04,2025-05");
                Console.WriteLine("                    ( only the months named; with --read-only they are just read )");
                Console.WriteLine("  --filter-robots PATH  Only write the logs, without robot requests, static files or POSTs, into PATH as");
                Console.WriteLine("                    IIS_Logs_YYYY-MM_filtered.zip ( no --connection needed; much less to move; the robot");
                Console.WriteLine("                    requests dropped are still counted when the zips are read )");
                Console.WriteLine("  --strict          With --filter-robots: only pages served ( 200/304 ) count; impossible browsers, link");
                Console.WriteLine("                    previews, monitors and unlisted crawlers ( and no user agent ) are robots");
                Console.WriteLine("  --exclude-ips LIST  With --filter-robots: these addresses ( 1.2.3.4 or 1.2.* ) are robots too");
                Console.WriteLine("  --include-current-month  With --filter-robots: also this month's logs so far ( to keep, not to save )");
                Console.WriteLine("  --from-datasets PATH  In place of --logs: save the monthly datasets in PATH, a --read-only working");
                Console.WriteLine("                    folder, reading no logs ( run it next to the database: the saving is the slow part )");
                Console.WriteLine();
            }
        }

        #endregion
    }
}
