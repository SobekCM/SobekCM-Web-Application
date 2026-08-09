# SolrReindexer

A standalone console tool that reindexes every item in a SobekCM instance's collection into Solr, using the same builder modules the Builder itself uses during normal ingestion (`ReloadMetsAndBasicDbInfoModule` + `SaveToSolrLuceneModule_v5`).

This isn't a polished utility — it was pulled directly out of the Builder codebase as a one-off tool, and it's offered here as-is as a starting point for developers who need to do something similar, not as a finished/supported tool. Expect leftover fields, hardcoded values, and rough edges from its Builder origins.

We used it to reindex our collections into Solr 10.

## How it works

`Solr_Reindexer_Controller` reads `config\sobekcm.config` next to the executable (same format/loader as the Builder — `MultiInstance_Builder_Settings_Reader`), and requires exactly one active instance in that file. `Worker_Reindexer.Perform_Reindexing` then pulls the full item list from the database (`Engine_Database.Item_List`) and, for every BibID/VID, loads the item's METS/DB info and runs it through `SaveToSolrLuceneModule_v5`, which pushes it into Solr — the same as what happens at the end of a normal Builder ingest pass.

## Pointing it at a different Solr instance

`Worker_Reindexer.Perform_Reindexing` (around line 110) contains hardcoded string replacements against `settings.Servers.Page_Solr_Index_URL` / `Document_Solr_Index_URL`:

```csharp
settings.Servers.Page_Solr_Index_URL = settings.Servers.Page_Solr_Index_URL.Replace("http://10.100.0.3:", "https://10.100.0.10:");
settings.Servers.Document_Solr_Index_URL = settings.Servers.Document_Solr_Index_URL.Replace("http://10.100.0.3:", "https://10.100.0.10:");

settings.Servers.Page_Solr_Index_URL = settings.Servers.Page_Solr_Index_URL.Replace("curacao", "uoc");
settings.Servers.Document_Solr_Index_URL = settings.Servers.Document_Solr_Index_URL.Replace("curacao", "uoc");
```

These were specific to our own old-host → new-host Solr 10 cutover and will need to be changed (or removed) for any other use. The general idea, though, is the useful part: the DB-configured Solr URLs get read as normal, then rewritten in code before the reindex module ever uses them. That means you can point a full reindex at a brand-new Solr instance without touching the live `sobekcm.config` a running site is using — just replace these lines with whatever substitution (or full override) gets you from the current URL to the target one.

## Running it

Build/publish the project, then run it next to a `config\sobekcm.config` (and writable `logs` folder) for the instance you want to reindex — same layout the Builder expects. It always runs once, in the foreground:

```
"Solr Reindexer.exe"
```

There's no command-line filtering — it walks every item returned by `Engine_Database.Item_List`. If you need to scope it to a subset, that'd be the first thing worth adding.
