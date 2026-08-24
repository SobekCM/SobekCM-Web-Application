# Version 5.0.1 PostgresSQL Database Scripts

## Upgrade_to_Ver501_PostgreSQL.sql
Takes an existing Version 5.0.0 PostgreSQL database and brings it up to Version 5.0.1 (Patch 1). Adds the `ClearEngineCacheModule` and `TesseractOcrModule` builder modules if they aren't already present, updates the `OcrTiffsModule` description, and updates `SobekCM_Database_Version` to 5.0.1.

## Notes on Upgrading
This is the first PostgreSQL upgrade script -- PostgreSQL support was introduced in 5.0.0, so there is no earlier PostgreSQL upgrade path to run first. If you are running 5.0.0, run `Upgrade_to_Ver501_PostgreSQL.sql` against your existing database.
