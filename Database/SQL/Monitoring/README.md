# Central monitoring database

An optional SQL Server database that collects exceptions and rate-limiting events from every SobekCM instance in one place. Without it, each instance writes these to its own `temp\exceptions.txt`, `temp\trace_<guid>.txt` and `temp\ratelimiting.txt` files.

This database is separate from the per-instance SobekCM database and is not part of its version upgrade scripts.

## Setup

1. Create an empty SQL Server database, for example `monitoring`.
2. Run `Monitoring_Database.sql` against it. The script is safe to run again after changes.
3. Create a login for the web application, map it to a user in this database, and add that user to the `monitoring_writer` role. The script creates the role, and it can only log exceptions and rate-limiting events. (A process that reads exceptions back, such as an automated triage job, goes in the `monitoring_triage` role instead. A read-only viewer goes in `monitoring_reader`, which can only read the tables.)
4. Add the connection string to each instance's `appsettings.json`:

   ```json
   "Monitoring": {
     "ConnectionString": "Server=...;Database=monitoring;User ID=...;Password=...;Encrypt=True;TrustServerCertificate=False",
     "InstanceName": "opennj"
   }
   ```

   `InstanceName` identifies the instance in every row. If it's left empty, the name of the site's folder is used.

   This connection reaches across instances and carries a SQL login password, so the monitoring server needs a certificate the web server already trusts. `Encrypt=True` on its own only encrypts the connection — it's `TrustServerCertificate=False` that checks the server is the one it claims to be. If you set it to `True` to get going against a self-signed certificate, treat that as development-only and get a trusted certificate installed before the instance sends real traffic.

5. Restart the site.

## How it behaves

- Requests never wait on this database. Records are queued in memory and written in the background every couple of seconds.
- If the database can't be reached, records go to the `temp\` files as before. The instance tries the database again a minute later.
- Repeat occurrences of the same defect are grouped under one fingerprint. Only the first 5 occurrences per fingerprint, per instance, per hour are stored in full; after that only the count goes up.

## What's in it

| Table | Contents |
|---|---|
| `Monitoring_Exception_Fingerprint` | One row per distinct defect: exception type, top SobekCM stack frame, counts, first and last seen, and the autofix workflow status |
| `Monitoring_Exception_Occurrence` | Sample occurrences: instance, time, message, full stack trace, URL, client IP and trace route |
| `Monitoring_RateLimit_Event` | The same events written to `ratelimiting.txt`, including the user agent of the request that tripped each one (only that one request, so not necessarily typical of the traffic behind it) |

Run `dbo.Monitoring_Purge_Old` periodically (for example weekly, from a SQL Agent job or a scheduled Cloud Function) to remove occurrences and rate-limiting events older than 90 days. The login that runs it goes in the `monitoring_maintenance` role, which can do nothing else. Keep it out of `monitoring_triage`: the procedure takes a `@Days` parameter, so anyone who can run it can delete every sample.
