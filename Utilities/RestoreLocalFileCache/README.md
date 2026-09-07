# RestoreLocalFileCache

The inverse of `MigrateSobekFileSystem`: instead of pushing local files up to GCS, this rebuilds the *local* half of GCS Hybrid mode — thumbnails, METS, and `marc.xml` — for every item already sitting in a SobekCM instance's GCS bucket. Two intended uses:

- **Disaster recovery.** If an instance's local disk is ever lost, nothing is actually gone under GCS Hybrid — the bucket already holds the complete data (master/derivative images included). This tool rebuilds just the local half needed to serve normally again, without re-downloading everything.
- **Hydrating a brand new instance.** A freshly restored database pointed at an existing bucket (e.g. `testing.sobeklibrary.com`, or a throwaway e2e test environment) has never had any local files at all. Running this once gives it the same local half a long-running Hybrid instance would already have.

Defaults to a dry run. Nothing is downloaded unless `--execute` is passed.

## How it works

Like `MigrateSobekFileSystem`, this reuses `SobekFileSystem`/`Hybrid_FileSystem` from `SobekCM_Core` directly rather than any separate classification logic, so a file is treated as needing a local copy exactly the same way the running web application would treat it. It always targets **GCS Hybrid** classification (`SobekFileSystem.Initialize(settings, ForceGcsHybrid: true)`) regardless of the instance's live `File System Mode` setting — that's the point: it needs to work against a brand new instance that hasn't been switched to `"GCS Hybrid"` yet.

For each item, it lists every file known either locally or in GCS (`SobekFileSystem.GetFiles`), works out whether the item needs its *entire* folder kept local (a registered website/HTML/OpenTextbook viewer that resolves same-origin relative paths — same check `MigrateSobekFileSystem` makes), and then downloads whichever files belong locally under that classification and aren't already present on disk. Unlike `MigrateSobekFileSystem`'s equivalent check, the item's own METS file may not exist locally yet at all — that's exactly the kind of file this tool might be restoring — so it downloads the METS first if needed before reading it to check for a folder-relative viewer.

Pass `--full` to skip classification entirely and download **every** file for every item — master/derivative images and OCR text included. That's the true disaster-recovery case: the local disk is completely gone, and you want everything back, not just the usual Hybrid local half.

This never deletes or modifies anything in GCS, and never overwrites an existing local file unless `--force` is passed.

## Before running

Requires **GCS Bucket Name** already configured for the target instance — if it's not set, the tool exits immediately with an explanation. Does **not** require `File System Mode` to already be `"GCS Hybrid"` (see above).

## Usage

```
RestoreLocalFileCache --instance-path <path> [options]
```

### Arguments

| Argument | Description |
|---|---|
| `--instance-path <path>` | Root folder of the target SobekCM deployment — the folder containing `config\sobekcm.config`. |

### Options

| Flag | Description |
|---|---|
| `--execute` | Actually download files. Without it, the tool always runs as a dry run. Note: a dry run can't detect an item needing a full folder bundle if its METS isn't already local, since that check itself requires downloading the METS first — a dry run will under-report what `--execute` would actually do for such items. |
| `--full` | Download every file for every item, not just the usual thumbnail/METS/marc.xml local half. For a true full disaster-recovery restore. |
| `--force` | Re-download even if a same-named local file already exists. Without it, any existing local file is left alone regardless of size. |
| `--quiet` | Per-item/summary totals only, suppresses per-file console output. |
| `--bibid <BibID> --vid <VID>` | Target just this one item instead of every item in the database. Must be used together. Skips the database item-list lookup entirely, so this works even without DB connectivity, as long as the bucket has the item's files. |
| `--threads N` | Number of files to download concurrently within a single item (default 8). Especially helpful under `--full` on items with many small page images. Items themselves are still processed one at a time. |
| `--help` | Prints usage and exits. |

## Examples

Dry run first, to see what a restore would do:

```
RestoreLocalFileCache --instance-path "C:\inetpub\wwwroot\sobekcm"
```

Then actually restore the local half:

```
RestoreLocalFileCache --instance-path "C:\inetpub\wwwroot\sobekcm" --execute
```

Full disaster-recovery restore (every file, not just the local half):

```
RestoreLocalFileCache --instance-path "C:\inetpub\wwwroot\sobekcm" --execute --full
```

Restore just one item (e.g. hydrating a single known-good item for a quick check):

```
RestoreLocalFileCache --instance-path "C:\inetpub\wwwroot\sobekcm" --bibid UP00000339 --vid 00001 --execute
```
