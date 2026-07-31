# IiifImporter

Imports items from a IIIF Presentation API manifest (currently the David Rumsey Map Collection's LUNA IIIF endpoint) into SobekCM-ingestible METS packages, written to local `{BibID}_{VID}` folders. Nothing is written to a database — the output folders are meant to be dropped into whichever SobekCM instance's builder incoming folder does the actual ingest.

## Usage

```
IiifImporter <csv-path> <output-folder> [options]
```

### Arguments

| Argument | Description |
|---|---|
| `<csv-path>` | Path to a text file with one LUNA object ID per line (e.g. `RUMSEY~8~1~377167~90143301`), no header. Duplicate IDs are skipped automatically (a message is logged for each skip). |
| `<output-folder>` | Folder to write item folders into. Each item gets its own subfolder named `{BibID}_{VID}` (e.g. `DR00000001_00001`). |

### Options

| Flag | Default | Description |
|---|---|---|
| `--start-bibid ID` | `DR00000001` | First BibID to assign. Must be a letter prefix followed by digits (e.g. `DR00000001`); the digit count is preserved as the padding width for every subsequent BibID. BibIDs increment by one per unique object processed. VID is always `00001`. |
| `--aggregation CODE` | *(none)* | Aggregation code to add to every item's `Behaviors`. Repeatable — pass the flag multiple times to add more than one aggregation. |
| `--download-images` | off | Also downloads each canvas's image(s) into the item folder, using the same filename(s) referenced in the generated METS fileSec: the JPEG derivative via the IIIF Image API, plus — for David Rumsey specifically — the archival JP2K master, when the canvas advertises one (see below). Without this flag, only `manifest.json` and the `.mets` file are written; the METS fileSec still references the eventual image filename(s), they just won't exist on disk yet. |
| `--max-image-size N` | *(no limit)* | Only affects the JPEG derivative downloaded via the IIIF Image API (not the JP2 master, which is always downloaded at its native size). Caps the requested image at `N` pixels on the longest side (via the IIIF Image API `!N,N` size request). If omitted, the JPEG is downloaded at full resolution (`full` size request) — for large scans this can be very large files. |
| `--limit N` | *(no limit)* | Only process the first `N` unique object IDs (applied after de-duplication). Useful for a quick test run, e.g. `--limit 5`. |
| `--delay-ms N` | `500` | Pause `N` milliseconds between items (before starting the next object's manifest fetch/downloads), so as not to hammer the remote IIIF server. Pass `--delay-ms 0` to disable. |
| `-h`, `--help` | | Prints usage and exits. |

## What gets written per item

Each `{BibID}_{VID}` output folder contains:

- `manifest.json` — the raw IIIF manifest as fetched, saved for reference/troubleshooting.
- `{BibID}_{VID}.mets` — the generated METS package (MODS bibliographic description + SobekCM custom metadata + fileSec/structMap for the page image(s)).
- The downloaded image file(s), only if `--download-images` was passed: `{BibID}_NNNN.jpg` per page always, plus `{BibID}_NNNN.jp2` per page when a JP2 master link was found (see below).

## David Rumsey JP2K master download

David Rumsey's IIIF canvases carry a raw HTML anchor in a `Download 1` (occasionally `Download 2`) metadata field pointing at the archival JP2000 master file, e.g.:

```json
{ "label": "Download 1", "value": "<a href=https://www.davidrumsey.com/rumsey/download.pl?image=/222/16908002.jp2 target=_blank>Full Image Download in JP2 Format</a>" }
```

That link isn't reachable through the IIIF Image API (which only ever serves JPEG derivatives), so `ItemBuilder` scans each canvas's `Download N` fields for one ending in `.jp2` and, when `--download-images` is passed, downloads it alongside the JPEG derivative. Both files get their own entry in the METS fileSec and are both pointed at from the page's structMap.

This is intentionally scoped to David Rumsey (the "David Rumsey specific" comments in `ItemBuilder.cs` mark it) rather than built as a generic feature — for a manifest from another IIIF source that doesn't carry these links, the lookup just finds nothing and only the JPEG derivative is downloaded, no flag or code change needed either way.

## Examples

```
IiifImporter DavidRumseyMaps.csv C:\ingest\rumsey-maps --aggregation DRUMSEY --download-images --max-image-size 4000
```

Quick test run against just the first 5 unique object IDs:

```
IiifImporter DavidRumseyMaps.csv C:\ingest\rumsey-maps-test --limit 5 --download-images
```
