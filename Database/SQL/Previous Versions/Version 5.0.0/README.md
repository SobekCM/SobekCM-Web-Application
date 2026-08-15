# Version 5.0.0 MS SQL Database Scripts

## Ver5_DB_Complete.sql
Builds a complete Version 5.0.0 database from scratch. Creates the `sobek_builder`/`sobek_user` roles, all tables, views, and stored procedures, grants `EXECUTE` on every procedure to both roles, then loads all the reference/seed data every installation needs (settings, metadata field definitions, viewer types, builder modules, mime types, workflow types, etc.). Run this against a brand-new, empty database.

## Upgrade_to_Ver500.sql
Takes an existing 4.11.0 database and brings it up to Version 5.0.0: adds new tables and columns, widens/retypes a few existing columns, backfills data for the new columns where needed, and updates stored procedures to their current definitions. Run this against a production database that's still on an older version.  If your version is older than 4.11.0 you will need to find the original update scripts in the Previous Version, and run those to get your db version up to 4.11.0 first.

## compare_schema_to_ver5.sql
A read-only checkup script. Compares a database's actual table/column definitions (type, length, nullability) against what Version 5.0.0 expects, and prints out anything that doesn't match - missing tables, missing columns, extra columns, or columns with the wrong type/size/nullability. Run this after an upgrade to confirm it fully took effect. Doesn't touch anything or check indexes/foreign keys, just column definitions.

## compare_seed_data_to_ver5.sql
Same idea as above, but for data instead of schema: confirms every expected reference/seed row (settings, metadata types, viewer types, builder modules, etc.) is actually present in the database. Only reports rows that are missing, not extra ones, and deliberately ignores a handful of instance-specific values (like the actual value of each setting, or identity IDs that are never referenced elsewhere) that are expected to legitimately vary between installations.  Does not change any data in your database.

## Individual scripts\
The historical, incremental patch scripts this version's changes were originally written and applied as - mostly one small dated fix at a time, plus two larger consolidated upgrade scripts along the way. Kept for history/reference; their changes are already folded into `Ver5_DB_Complete.sql` and `Upgrade_to_Ver500.sql` above, so there's no need to run anything in this folder directly.
