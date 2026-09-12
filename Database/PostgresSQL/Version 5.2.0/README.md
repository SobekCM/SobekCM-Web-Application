# Version 5.2.0 PostgresSQL Database Scripts

## Upgrade_to_Ver520_PostgreSQL.sql
Takes an existing Version 5.1.0 PostgreSQL database and brings it up to Version 5.2.0. PostgreSQL port of `Upgrade_to_Ver520.sql` (the SQL Server upgrade script), matching it change for change:
- Adds `GCS Page Load URL Expiration Minutes` (default 10) and `GCS Download URL Expiration Minutes` (default 60), so signed GCS URLs can be shorter-lived for page images, thumbnails and download links than for files the page keeps requesting. Updates the help text of `GCS Signed URL Expiration Minutes` (default 240), which now covers only those long-running files (PDF, audio, video, page turner).
- Adds `GCS Restricted Streaming URL Expiration Minutes` (default 60), a separate cap for files a restricted item's page keeps requesting (PDF, audio, video). Previously every signed URL on a restricted item was capped at `GCS Restricted URL Expiration Minutes` (15), which broke playback and reading past 15 minutes for authorized users. Updates that setting's help text to say it now caps only page images, thumbnails and download links.
- Updates `SobekCM_Database_Version` to 5.2.0.

## Ver520_DB_Complete_PostgreSQL.sql
Not created yet: 5.2.0 is still in development. At release it will be built from the Version 5.1.0 complete script with this upgrade folded in. Until then, build a new database from `Version 5.1.0/Ver510_DB_Complete_PostgreSQL.sql` and run `Upgrade_to_Ver520_PostgreSQL.sql` against it.

## Notes on Upgrading
If you are running 5.1.0, run `Upgrade_to_Ver520_PostgreSQL.sql` against your existing database. If you are running an older version, apply the earlier PostgreSQL upgrade scripts first (see `Version 5.1.0/Upgrade_to_Ver510_PostgreSQL.sql`).
