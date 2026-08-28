# MigrateSobekFileSystem

A one-time/occasional bulk utility for a SobekCM instance moving to GCS Hybrid file storage. It walks every existing item in that instance's database and either pushes their files up to the configured GCS bucket while the live site still serves files locally (`--mode migrate`), or — run separately, later, once you've confirmed a migration is correct and switched the site's File System Mode over to `"GCS Hybrid"` — deletes the now-redundant local copies of files that only need to live in GCS (`--mode cleanup`).

Both modes default to a dry run. Nothing is uploaded or deleted unless `--execute` is passed.

## How it works

This deliberately does not implement its own upload/classification logic — it reuses `SobekFileSystem`/`Hybrid_FileSystem` from `SobekCM_Core` directly, so a file gets routed to GCS-only, dual-write (local + GCS), or local-only exactly the same way it would if the running web application or Builder had written it. That also means the same changed-file skip check (`GCS_FileSystem.ObjectMatchesLocalFile`, by size) applies here, so re-running a migration is cheap — files already uploaded and unchanged are skipped, not re-transferred.

Settings are bootstrapped the same lightweight way the app's own lazy `Engine_ApplicationCache_Gateway.Settings` property does: `AppRoot_Gateway.AppRootPath` is pointed at `--instance-path`, and touching `Settings` loads `{instance-path}\config\sobekcm.config` (and, from there, the database connection string) exactly as the real app would. The item list comes from `SobekCM_Item_Database.Get_All_BibID_VID_Pairs()`, which includes dark and IP-restricted items — where a file physically lives doesn't depend on who's allowed to view it.

## Before running

- **`--mode migrate`** is meant to run *before* cutover, with the live site still serving files locally — it never deletes or otherwise touches local files, so it only requires **GCS Bucket Name** to be configured and a service account key file in place at `{instance-path}\config\user\gcs-service-account.json`. It does **not** require **File System Mode** to already be `"GCS Hybrid"` — it forces GCS Hybrid construction for its own run regardless of the live setting, so you can migrate files up to the bucket well ahead of flipping the site over. If the bucket name isn't configured, the tool exits immediately with an explanation.
- **`--mode cleanup`** deletes local copies, so it's gated on the site having actually cut over: **File System Mode** must already be `"GCS Hybrid"` for the target instance, or the tool exits immediately without doing anything. Only run it after a `--mode migrate` run against the same instance has been independently confirmed correct (spot-check the bucket, spot-check that item viewers still work) *and* the live site has been switched to `"GCS Hybrid"`. It only deletes a local file after re-verifying GCS has a matching-size copy of it — if that verification fails for any file, it's left in place and logged as a warning rather than deleted.

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
| `--force` | `--mode migrate` only. Re-uploads even when GCS already has a same-size object. Without it, the changed-file-skip optimization (`GCS_FileSystem.ObjectMatchesLocalFile`, by size) treats a matching size as "already migrated" and skips the upload — which means it will **not** re-upload a file whose bytes are unchanged but whose GCS object metadata (e.g. content type) is wrong. Use `--force` to repair objects uploaded before a metadata-affecting fix, or any time you want a guaranteed clean re-push regardless of what's already there. |
| `--verbose` | Per-file console output, not just per-item totals. |
| `--help` | Prints usage and exits. |

## Examples

Dry run first, to see what a migration would do:

```
MigrateSobekFileSystem --instance-path "C:\inetpub\wwwroot\sobekcm" --mode migrate --verbose
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
