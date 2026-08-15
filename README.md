# SobekCM

SobekCM is an open-source digital library management system: a repository platform for ingesting, describing, storing, and presenting digitized items (books, images, newspapers, maps, audio/video, etc.) with full-text search, METS-based metadata, and a public/administrative web front end. It has been in production use at the University of Florida and partner institutions for over two decades.

## Latest Release Version

Version 5.0.0 was released on 8/15/2026.  Any subsequent fixes will be released in patches.

Highlights of this release:

- Migrated from .NET Framework 4.7.2 to .NET 10 / ASP.NET Core
- ~10-15x faster item loads via cached protobuf metadata; ~20x faster initial aggregation queries
- Single Sign On: native Sobek login plus OpenID Connect and SAML single sign-on
- Full localization in English, French, Spanish, German, Italian, Dutch, and Portuguese
- PostgreSQL/RDS database support alongside SQL Server, via the engine-agnostic data access layer
- Integrated archival system (cold storage, e.g. GCS/Glacier) with checksum verification and reporting
- IIIF manifest and Content Search 2.0 support
- User-group-based item access restrictions and expanded open educational resource (OER) support
- Much, much more...

For the release notes on the latest version, see [sobekrepository.org/sobekcm/currentversion](https://sobekrepository.org/sobekcm/currentversion).

## Architecture

SobekCM is really two applications sharing a common core:

- **SobekCM Web** (`SobekCM Web.sln`) — the public-facing and administrative web application. One instance is run per hosting institution. It serves item/collection pages, search, and admin/mySobek tooling, and talks to a per-institution SQL Server (soon also PostgreSQL) database via stored procedures and a Solr index for search.
- **SobekCM Builder** (`SobekCM Builder.sln`) — a background ingest/processing service. A single shared Builder instance polls the databases of all configured institutions and performs conversion, OCR, JP2 derivative generation, and search re-indexing for incoming material.

Both solutions share several class libraries (`SobekCM_Core`, `SobekCM_Resource_Object`, `SobekCM_Tools`, `SobekCM_Engine_Library`, `EngineAgnosticLayerDbAccess`, `SobekCM_Resource_Database`), so a model or database change generally needs to build cleanly against both.

Within the web app, the REST-style `/engine/*` microservice endpoints (`SobekCM_Engine_Library`) run **in-process** — they are invoked as plain method calls, not over HTTP, despite the "microservice" naming. A single in-process cache (`SobekCM_Core/MemoryMgmt/SharedCache.cs`) backs content caching, application-wide state, and per-session object storage, distinguished by key prefix rather than by separate cache instances. See `CLAUDE.md` for the full detail on session storage and caching design.

Database access for resource objects and engine services goes through an engine-agnostic access layer (`EngineAgnosticLayerDbAccess`, the "EAL") so the same call sites can eventually target SQL Server or PostgreSQL. Stored procedures are being kept as the primary interface into the database rather than moved to an ORM.

## Repository layout

```
Code/
  SobekCM/                        Main ASP.NET Core web application (entry point, Program.cs)
  SobekCM_Library/                Presentation/business layer: HTML subwriters, item/aggregation/
                                   admin/mySobek viewers, authentication, citation export, email,
                                   localization, TEI handling
  SobekCM_Core/                   Shared domain models and contracts (Users, Configuration,
                                   Aggregations, Items, Navigation, Search, Skins, MemoryMgmt, ...)
  SobekCM_Engine_Library/         In-process /engine/* microservice endpoints
  SobekCM_Resource_Object/        Digital object / METS model (bibliographic + structural metadata)
  SobekCM_Resource_Database/      Database access for resource objects
  EngineAgnosticLayerDbAccess/    DB-engine-agnostic data access layer ("EAL"): SQL Server / PostgreSQL
  SobekCM_Tools/                  Shared low-level utilities (tracing, hashing, path/IP helpers, ...)
  SobekCM Builder/                Builder console application (entry point)
  SobekCM_Builder_Library/        Builder ingest modules: folder scanning, item processing, FDA, statistics
  SobekCM_Builder_Service/        Windows Service host for the Builder
  TempStoreArchiveData/           One-off utility to import cold-storage archive manifests
  SobekCM_URL_Rewriter/           Legacy IHttpModule; dead code under .NET 10 (see CLAUDE.md), superseded
                                   by the PrettyUrl_Rewrite middleware in SobekCM/Program.cs

Database/SQL/                     Versioned schema/upgrade scripts and admin task scripts
Plugins/                          Custom METS metadata module plugins (Census, NSLA, and a sample library)
Solr/                             Solr core configuration (item-level and full-text page indexes)
Utilities/                        Standalone tools (archiving, Solr service wrapper, resource-read testing)
Batch Files/                      Legacy .NET Framework deployment scripts (aspnet_compiler-based);
                                   predate the .NET 10 migration and are not yet updated for it
```


## Running the web application

The web application expects institution-specific **design files** (skins, templates, static assets) to be copied in from a running SobekCM instance's `design` folder — these are not checked into this repository.

## More information

Historical documentation and help pages are linked from [sobekrepository.org](https://sobekrepository.org). 

## License

MIT — see `LICENSE`.
