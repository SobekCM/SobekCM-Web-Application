<#
    Minify-Assets.ps1

    Recursively minifies every .css and .js file under:
        Code\SobekCM\default\css
        Code\SobekCM\default\js

    Uses esbuild (https://esbuild.github.io/) via npx. Ensures esbuild is installed
    globally first.

    For each source file "name.ext" this writes/overwrites a sibling "name.min.ext"
    in the same folder. Files already named "*.min.js" / "*.min.css" are skipped as
    input (they're treated as output, not source), so re-running this script is safe.

    A source file is only re-minified if its ".min" sibling is missing or older than
    the source (LastWriteTime). If the ".min" file is already as new as, or newer
    than, the source, it's left alone -- this keeps re-runs fast on an unchanged tree.
#>

$ErrorActionPreference = "Stop"

# Folders to scan, relative to this script's location (Batch Files\)
$targets = @(
    (Join-Path $PSScriptRoot "..\Code\SobekCM\default\css"),
    (Join-Path $PSScriptRoot "..\Code\SobekCM\default\js")
)

# --- Ensure esbuild is installed ---

$npm = Get-Command npm -ErrorAction SilentlyContinue
if (-not $npm) {
    Write-Error "npm was not found on PATH. Install Node.js (which includes npm) first: https://nodejs.org/"
    exit 1
}

Write-Host "Ensuring esbuild is installed (npm install -g esbuild)..."
npm install -g esbuild
if ($LASTEXITCODE -ne 0) {
    Write-Error "npm install -g esbuild failed (exit code $LASTEXITCODE)."
    exit 1
}

# --- Minify ---

$processed = 0
$failed = 0
$skipped = 0

foreach ($target in $targets) {
    $resolvedTarget = Resolve-Path -Path $target -ErrorAction SilentlyContinue
    if (-not $resolvedTarget) {
        Write-Warning "Path not found, skipping: $target"
        continue
    }

    Write-Host "`nScanning $resolvedTarget ..."

    $files = Get-ChildItem -Path "$resolvedTarget\*" -Recurse -File -Include "*.js", "*.css" |
        Where-Object { $_.Name -notmatch '\.min\.(js|css)$' }

    foreach ($file in $files) {
        $ext = $file.Extension.TrimStart(".")
        $outFile = Join-Path $file.DirectoryName ($file.BaseName + ".min." + $ext)

        if (Test-Path $outFile) {
            $outFileItem = Get-Item $outFile
            if ($outFileItem.LastWriteTimeUtc -ge $file.LastWriteTimeUtc) {
                Write-Host "Up to date, skipping: $($file.FullName)"
                $skipped++
                continue
            }
        }

        Write-Host "Minifying: $($file.FullName)"
        esbuild $file.FullName --minify --outfile=$outFile

        if ($LASTEXITCODE -eq 0) {
            $processed++
        }
        else {
            Write-Warning "  FAILED: $($file.FullName)"
            $failed++
        }
    }
}

Write-Host "`nDone. Minified $processed file(s), skipped $skipped up-to-date file(s), $failed failure(s)."
if ($failed -gt 0) {
    exit 1
}
