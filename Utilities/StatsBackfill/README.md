# SobekCM Stats Backfill

A temporary tool for SobekCM 5.2 (one library per database). It fills in months that are missing from a library's usage statistics, using old IIS logs.

The Builder's `CalculateUsageStatisticsModule` only calculates months **after** the latest month already in the database. A month that was skipped is never filled in later. This tool finds those gaps and runs the Builder's own reader (`SobekCM_Stats_Reader_Processor`) for each one. Parsing and saving are therefore exactly what the Builder does.

## Running it

Run it on the web server, so the site folder and the logs can be reached:

```
SobekCM_Stats_Backfill --connection "Server=...;Database=...;..." --logs "D:\IIS Logs\W3SVC3"
```

It's a **dry run** unless you add `--execute`. The dry run lists each month the logs cover, how many days of logs that month has, and what will happen to it. Add `--scan` to also read the logs and count the SobekCM page requests in each month that would be calculated.

| Option | |
|---|---|
| `--execute` | Calculate and save the missing months |
| `--months 2023-01,2023-02` | Only these months |
| `--allow-partial` | Also calculate months whose logs don't cover every day |
| `--scan` | Dry run: also count page requests (reads every log) |
| `--site PATH` | Web site folder (default: the "Application Server Network" setting) |
| `--work PATH` | Working folder (default `%TEMP%\SobekCM_Stats_Backfill\<time>`) |
| `--keep-logs` | Keep the unzipped / copied logs in the working folder |
| `--no-archive` | Don't zip the logs by month afterwards |
| `--archive-only` | Only zip the logs by month: no `--connection` needed, no statistics (still a dry run without `--execute`) |

## What it does

- Finds `u_exYYMMDD.log` files in the log folder and its subfolders, both loose files and files inside `.zip` files. When the same day turns up more than once, it keeps the biggest copy. The current month is ignored.
- Skips a month that already has a row in `SobekCM_Statistics`.
- **Refuses** a month that has no `SobekCM_Statistics` row but does have rows in another statistics table. That means an earlier run failed partway, and needs to be looked at by hand.
- Skips a month whose logs don't cover every day, unless `--allow-partial` is given. `SobekCM_Statistics_Aggregate` runs only once per month, so whatever a month is calculated from is permanent.
- Refuses hourly logs (`u_exYYMMDDHH.log`). The reader handles only one log per day.
- For each month to calculate, it unzips or copies that month's logs into a working folder and counts the requests. If the month has **no** `/sobekcm.aspx` requests, it skips the month rather than lock it in as zero (see below). Otherwise it runs the reader for that month, then checks that the month was saved and aggregated.
- **Zips the logs back up by month.** It does this for every month the logs cover, not only the ones it calculated, and skips the current month. Each month goes into `IIS_Logs_YYYY-MM.zip` at the top of the log folder, holding one copy of each day's log. The zip is written to a `.tmp` file and checked (every log present at its full size) before it replaces anything. Only then are that month's loose logs deleted. An older zip is deleted only when every log in it is now in a monthly zip and it holds nothing else. Run the tool again and a month already zipped this way is left alone. The dry run shows what would be zipped and deleted.
- Deletes the site's `temp\overall_usage.xml`. Reset the site's cache, or recycle its app pool, afterwards.
- Sends no usage emails.

## Which logs it can read

The reader counts only requests for `/sobekcm.aspx`, `/sobekcm_data.aspx` and `/sobekcm_oai.aspx`, which is how IIS logged the old WebForms site. Logs written after the site moved to ASP.NET Core record the readable URLs (`/AA00001234/00001`) instead, so those months come out empty. That's why a month with no such requests is skipped.

Point `--logs` at **this site's** IIS log folder (`W3SVC{n}`). The reader doesn't check the host name, so logs from another site on the same server would be counted for this library.
