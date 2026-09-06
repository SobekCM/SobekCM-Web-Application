# MigrateSobekFileSystem

A one-time/occasional bulk utility for a SobekCM instance moving to GCS-backed file storage — either GCS Hybrid (master/derivative images in GCS, METS/marc.xml/thumbnails stay local too) or GCS Full (everything in GCS, no permanent local copy of anything but `cache.protobuf`). It walks every existing item in that instance's database and either pushes their files up to the configured GCS bucket while the live site still serves files locally (`--mode migrate`), or — run separately, later, once you've confirmed a migration is correct and switched the site's File System Mode over to `"GCS Hybrid"`/`"GCS Full"` — deletes the now-redundant local copies of files that only need to live in GCS (`--mode cleanup`).

Both modes default to a dry run. Nothing is uploaded or deleted unless `--execute` is passed.

## How it works

This deliberately does not implement its own upload/classification logic — it reuses `SobekFileSystem`/`Hybrid_FileSystem`/`GCS_Full_FileSystem` from `SobekCM_Core` directly, so a file gets routed to GCS-only, dual-write (local + GCS), or local-only exactly the same way it would if the running web application or Builder had written it. That also means the same changed-file skip check (`GCS_FileSystem.ObjectMatchesLocalFile`, by size) applies here, so re-running a migration is cheap — files already uploaded and unchanged are skipped, not re-transferred.

`--mode migrate` classifies files as GCS Hybrid would by default; pass `--full` to classify as GCS Full would instead (also picked up automatically if the instance's File System Mode is already `"GCS Full"`). The difference: under Hybrid, METS/marc.xml/thumbnails are always dual-write (pushed to GCS *and* kept local); under Full, they're GCS-only too, same as master images, unless the item needs its whole folder kept local (see below). `--mode cleanup` always classifies according to whichever mode the instance's File System Mode is currently set to — there's no separate `--full` for cleanup.

GCS-only is the default classification for most files — but a handful of items (those with a registered website/HTML/OpenTextbook viewer, e.g. an embedded Unity WebGL build) need their *entire* folder kept local instead, since the browser resolves their internal files via same-origin relative paths, not signed URLs. To catch these, this tool reads each item's own `.mets` file (once per item, only if present) before classifying its files — a small added cost per item, but necessary for correctness; without it, that kind of item's files would be wrongly pushed GCS-only and (in `--mode cleanup`) have their local copies deleted, breaking the item.

Settings are bootstrapped the same lightweight way the app's own lazy `Engine_ApplicationCache_Gateway.Settings` property does: `AppRoot_Gateway.AppRootPath` is pointed at `--instance-path`, and touching `Settings` loads `{instance-path}\config\sobekcm.config` (and, from there, the database connection string) exactly as the real app would. The item list comes from `SobekCM_Item_Database.Get_All_BibID_VID_Pairs()`, which includes dark and IP-restricted items — where a file physically lives doesn't depend on who's allowed to view it.

## Before running

- **`--mode migrate`** is meant to run *before* cutover, with the live site still serving files locally — it never deletes or otherwise touches local files, so it only requires **GCS Bucket Name** to be configured and a service account key file in place. By default that key file is expected at `{instance-path}\config\user\gcs-service-account.json`, but this tool's own `appsettings.json` (next to the exe) can override that location — same convention the web application uses for its own `appsettings.json`:
  ```json
  {
    "GCS": {
      "ServiceAccountJsonPath": "C:/SobekCM/Keys/gcs-service-account.json"
    }
  }
  ```
  Leave `ServiceAccountJsonPath` blank (or the whole file absent) to fall back to the `{instance-path}\config\user\gcs-service-account.json` default. It does **not** require **File System Mode** to already be `"GCS Hybrid"`/`"GCS Full"` — it forces that construction for its own run regardless of the live setting (Hybrid by default, Full if `--full` is passed), so you can migrate files up to the bucket well ahead of flipping the site over. If the bucket name isn't configured, the tool exits immediately with an explanation.
- **`--mode cleanup`** deletes local copies, so it's gated on the site having actually cut over: **File System Mode** must already be `"GCS Hybrid"` or `"GCS Full"` for the target instance, or the tool exits immediately without doing anything. Only run it after a `--mode migrate` run against the same instance has been independently confirmed correct (spot-check the bucket, spot-check that item viewers still work) *and* the live site has been switched over. It only deletes a local file after re-verifying GCS has a matching-size copy of it — if that verification fails for any file, it's left in place and logged as a warning rather than deleted.

## Usage

```
MigrateSobekFileSystem --instance-path <path> --mode migrate|cleanup [options]
```

### Arguments

| Argument | Description |
|---|---|
| `--instance-path <path>` | Root folder of the target SobekCM deployment — the folder containing `config\sobekcm.config`. |
| `--mode migrate` | Upload/verify every item's files in GCS. Never deletes anything locally. |
| `--mode cleanup` | Delete local copies of GCS-only master files, only after verifying GCS already has a matching copy. |

### Options

| Flag | Description |
|---|---|
| `--execute` | Actually perform uploads/deletions. Without it, the tool always runs as a dry run — it reports what it would do and touches nothing. |
| `--full` | `--mode migrate` only. Classifies files as `"GCS Full"` would — METS/marc.xml/thumbnails get pushed to GCS too, not just master/derivative images. Without it, migrate classifies as `"GCS Hybrid"` would. Automatically implied if the instance's File System Mode is already `"GCS Full"`. |
| `--force` | `--mode migrate` only. Re-uploads even when GCS already has a same-size object. Without it, the changed-file-skip optimization (`GCS_FileSystem.ObjectMatchesLocalFile`, by size) treats a matching size as "already migrated" and skips the upload — which means it will **not** re-upload a file whose bytes are unchanged but whose GCS object metadata (e.g. content type) is wrong. Use `--force` to repair objects uploaded before a metadata-affecting fix, or any time you want a guaranteed clean re-push regardless of what's already there. |
| `--quiet` | Per-item totals only, suppresses the (now default) per-file console output. |
| `--bibid <BibID> --vid <VID>` | Target just this one item instead of every item in the database — e.g. to re-run `--mode migrate` for a single file that `--mode cleanup` left in place with a "no verified GCS copy" warning (it was never successfully uploaded, or uploaded with a size mismatch, in an earlier bulk run). Must be used together. Skips the database item-list lookup entirely, so this works even without DB connectivity. |
| `--file <FileName>` | Narrows a `--bibid`/`--vid` run to just this one file within that item, instead of every file in its folder. Requires `--bibid` and `--vid`. |
| `--threads N` | Number of files to upload/delete concurrently within a single item (default 8). Small files are mostly network-latency-bound rather than bandwidth-bound, so this can speed up a run substantially — especially on items with many small files, like page images. Items themselves are still processed one at a time; only the files within one item run in parallel. |
| `--help` | Prints usage and exits. |

Per-file output is on by default now. `--verbose` is still accepted for compatibility but is a no-op — use `--quiet` to get the old terse behavior back.

## Examples

Dry run first, to see what a migration would do:

```
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode migrate
```

Then actually migrate:

```
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode migrate --execute
```

Once that's confirmed correct — separately, later — reclaim local disk space:

```
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode cleanup --execute
```

Re-push everything regardless of what's already in the bucket (e.g. after a fix to how objects get uploaded, like a corrected content-type):

```
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode migrate --execute --force
```

Fix a single file a `--mode cleanup` run left in place with a "no verified GCS copy" warning — re-upload just that file, then clean it up:

```
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode migrate --bibid CBS0000402 --vid 00005 --file CBS0000402_00005_00052.jpg --execute --force
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode cleanup --bibid CBS0000402 --vid 00005 --file CBS0000402_00005_00052.jpg --execute
```
