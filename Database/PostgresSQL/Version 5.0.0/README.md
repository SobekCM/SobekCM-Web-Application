# Version 5.0.0 PostgresSQL Database Scripts

## Ver5_DB_Complete_PostgresSQL.sql
Builds a complete Version 5.0.0 database from scratch. Creates the `sobek_builder`/`sobek_user` roles, all tables, views, and stored procedures, grants `EXECUTE` on every procedure to both roles, then loads all the reference/seed data every installation needs (settings, metadata field definitions, viewer types, builder modules, mime types, workflow types, etc.). Run this against a brand-new, empty database.

## Notes on Upgrading
This is the first release that supports PostgresSQL, so no upgrade scripts are needed.
