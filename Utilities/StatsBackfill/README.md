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
| `--through 2026-07` | Last month to consider, for calculating **and** zipping (default 2026-07: later logs use the new addresses, which the reader can't count) |
| `--threads N` | Daily logs read at the same time (default: the server's cores, at most 4; `1` reads them one at a time). Each day is read on its own, so the results are the same either way; more threads means more memory, since each log being read holds its requests |
| `--normal-priority` | Run at normal priority. By default the tool runs below normal, so Windows gives the web sites the CPU whenever they need it |
| `--read-only` | Read the logs into the daily and monthly datasets in the working folder and stop: nothing is saved to the database and nothing is zipped. Takes any month with logs, even one already in the database. For checking that `--threads` changes nothing: run a month twice with different `--threads` and `--work` folders and compare the two folders' files |
| `--allow-partial` | Also calculate months whose logs don't cover every day |
| `--scan` | Dry run: also count page requests (reads every log) |
| `--site PATH` | Web site folder (default: the "Application Server Network" setting) |
| `--work PATH` | Working folder (default `%TEMP%\SobekCM_Stats_Backfill\<time>`) |
| `--keep-logs` | Keep the unzipped / copied logs in the working folder |
| `--no-archive` | Don't zip the logs by month afterwards |
| `--archive-only` | Only zip the logs by month: no `--connection` needed, no statistics (still a dry run without `--execute`) |
| `--recalculate 2025-04,2025-05` | Delete these months from every statistics table and calculate them again (only the months named; works with `--logs` and `--from-datasets`). Safe to do: `SobekCM_Statistics_Aggregate` changes only that month's rows and works out each item's totals again from all its months, so nothing is counted twice. The dry run marks them "REPLACES the month in the database" |
| `--from-datasets PATH` | In place of `--logs`: save the monthly datasets of a `--read-only` working folder, reading no logs (see below) |

## Filtering the logs before moving them ( `--filter-robots PATH` )

On a busy site nearly all of a log is robots. `--filter-robots PATH` (with `--logs`, no `--connection`, a dry run without `--execute`) writes each month as `IIS_Logs_YYYY-MM_filtered.zip` in PATH, keeping only the lines the reader counts as views: robot requests (by the reader's own robot check), static files, POSTs and unreadable lines are dropped, using the reader's own line parsing. Each log ends with a remark, `#SobekCM-Robot-Hits: N`, which the reader adds to the month's robot hits. Reading the filtered zips gives exactly the same datasets as reading the full logs (checked byte for byte on a 6 GB month, which became a 15 MB zip). `--months`, `--through` and `--threads` work as usual; a month whose zip is already there is left alone. Then point `--logs` at a folder holding just those zips.

## Reading on one machine, saving on another

Saving is the slow part: one stored procedure call per item, collection and page row. Over a tunnel to the database a busy month can take 15 minutes; next to the database, a fraction of that. So the logs can be read wherever they are fastest to read, and the results saved from the web server:

1. Where the logs are: `--read-only --work D:\StatsWork` (with `--connection`, `--logs`, `--site`, and `--months` to read only the gap). Each month's datasets go into `D:\StatsWork\{YYYYMM}\data`.
2. Copy that working folder to the web server.
3. There: `--connection "..." --from-datasets D:\StatsWork` for a dry run, then add `--execute`. It makes the same checks as a normal run (a month already in the database is skipped, one with leftover rows refused, one missing days needs `--allow-partial`, counting the daily datasets), saves each month the Builder's way, verifies it, and deletes the site's `temp\overall_usage.xml`. `--months` and `--through` work as usual. The logs aren't needed there, and nothing is zipped.

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
