# PullGcsFiles

A small command-line utility that downloads every file present in a list of items' GCS folders down to a local output folder — one subfolder per item, named `BibID_VID`.

## How it works

Reads a plain text list file of items, one per line, as either a bare 10-digit BibID (e.g. `AA00000001`) or a `BibID:VID` pair (e.g. `AA00000001:00002`). A line with no `:VID` suffix defaults to VID `00001`. Blank lines are skipped.

For each item, it lists every object in the configured GCS bucket under the key prefix `{InstanceCode}/{BibID}/{VID}/` — matching the same object key convention `GCS_FileSystem` uses in the running web application — and downloads them into `{OutputFolder}/{BibID}_{VID}/`. Only files directly under that prefix are pulled (mirroring `GCS_FileSystem.GetFiles`/`DownloadAll`'s flat, one-level listing); nothing deeper is expected to exist there.

This is a standalone tool — it talks to Google Cloud Storage directly via `Google.Cloud.Storage.V1` rather than referencing `SobekCM_Core`, since it has no need for signed URLs, item metadata, or database access.

## Configuration

The bucket name, service account credential, and instance code don't change from run to run, so they live in `appsettings.json` next to the executable rather than being passed as arguments every time:

```json
{
  "GCS_Bucket_Name": "sobekdigital-sobek-repo",
  "GCS_Service_Account_Json_Key_Path": "C:\\path\\to\\service-account.json",
  "Instance_Code": "DEMO"
}
```

Each can be overridden on the command line (`--bucket`, `--key-path`, `--instance-code`) for a one-off run against a different bucket/instance without editing the file.

## Usage

```
PullGcsFiles --list <path> --output <path> [options]
```

### Arguments

| Argument | Description |
|---|---|
| `--list <path>` | Text file with one BibID (or `BibID:VID`) per line. A BibID with no VID defaults to `00001`. |
| `--output <path>` | Root folder to download into. Each item is written to `<output>\<BibID>_<VID>\`. Created if it doesn't exist. |

### Options

| Flag | Description |
|---|---|
| `--bucket <name>` | Overrides `GCS_Bucket_Name` from appsettings.json. |
| `--key-path <path>` | Overrides `GCS_Service_Account_Json_Key_Path` from appsettings.json. |
| `--instance-code <code>` | Overrides `Instance_Code` from appsettings.json. |
| `--help` | Prints usage and exits. |

## Example

```
PullGcsFiles --list from_demo.txt --output C:\temp\pulled_items
```

Given a list containing `AA00000001` and `UF00074173:00003`, and `Instance_Code` set to `DEMO`, this downloads:

- `DEMO/AA00000001/00001/*` into `C:\temp\pulled_items\AA00000001_00001\`
- `DEMO/UF00074173/00003/*` into `C:\temp\pulled_items\UF00074173_00003\`
