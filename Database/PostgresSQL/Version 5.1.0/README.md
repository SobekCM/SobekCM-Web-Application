# Version 5.1.0 PostgresSQL Database Scripts

## Ver510_DB_Complete_PostgreSQL.sql
Builds a complete Version 5.1.0 database from scratch (the 5.0.1 baseline with `Upgrade_to_Ver510_PostgreSQL.sql`'s changes folded in). Run this against a brand-new, empty PostgreSQL database instead of running the 5.0.1 complete script followed by the 5.1.0 upgrade script.

## Upgrade_to_Ver510_PostgreSQL.sql
Takes an existing Version 5.0.1 PostgreSQL database and brings it up to Version 5.1.0. PostgreSQL port of `Upgrade_to_Ver510.sql` (the SQL Server upgrade script), matching it change for change:
- Adds the `StageResourceFilesLocallyModule`, `PushMasterFilesToGcsModule`, and `SaveStructMapModule` builder modules.
- Adds per-extension settings storage (`Extension_Code` column on `SobekCM_Settings`, plus the new `SobekCM_Get_Extension_Settings` / `SobekCM_Set_Extension_Setting_Value` functions) and updates `SobekCM_Get_Settings` to exclude extension-owned rows from the general settings loader.
- Adds the "GCS Hybrid" file-system-mode settings (`File System Mode`, `GCS Bucket Name`, `GCS Signed URL Expiration Minutes`, `GCS Restricted URL Expiration Minutes`).
- Re-adds `Can Submit Edit Online` as a flag distinct from `Can Submit Items Online`, and adds `Disabled Online Changes Link`.
- Adds the `AUDIO` item viewer type and a `GCS Scratch` option to `JPEG2000 Server Type`.
- Renames the `System Base Abbreviation` setting to `System Base Code`.
- Adds a second flag, `AdditionalWork_MetadataOnly`, alongside `SobekCM_Item.AdditionalWorkNeeded`, so the Builder can tell a metadata-only reprocessing pass apart from a full one. Updates `SobekCM_Update_Additional_Work_Needed_Flag`, `SobekCM_Get_Items_Needing_Aditional_Work`, `SobekCM_Set_Item_Visibility`, and `Admin_Unembargo_Items_Past_Embargo_Date` accordingly.
- Updates `SobekCM_Database_Version` to 5.1.0.

One deliberate correction from the SQL Server version: the `GCS Restricted URL Expiration Minutes` existence guard there checks a Setting_Key name ("GCS Restricted **Signed** URL Expiration Minutes") that is never the one actually inserted, which would throw a primary key violation on a second run. This PostgreSQL port checks the correct (actually-inserted, actually-read-by-C#) key name instead.

## Notes on Upgrading
If you are running 5.0.1, run `Upgrade_to_Ver510_PostgreSQL.sql` against your existing database. If you are running an older version, apply the earlier PostgreSQL upgrade scripts first (see `Previous Versions/Version 5.0.1/Upgrade_to_Ver501_PostgreSQL.sql`).
