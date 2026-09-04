<#
.SYNOPSIS
Finds, and optionally deletes, web.config files under a folder tree.

.DESCRIPTION
Recursively scans a folder tree (typically a Sobek resource root) for leftover
web.config files -- the per-item ipSecurity restriction files the app and
Builder used to write for dark/IP-restricted items back when the resource
folder was itself served by IIS. Now that access is gated by GCS signed URLs
instead, those files are obsolete and can be removed.

Streams results as it walks the tree rather than collecting everything into
memory first, since a resource root can hold a very large number of small
files/folders.

.PARAMETER Path
Root folder to scan. Defaults to the current directory.

.PARAMETER Execute
Actually delete each web.config file found. Without this switch, the script
only reports what it would delete -- this is also the default when neither
-WhatIf nor -Execute is passed.

.PARAMETER WhatIf
Explicitly request dry-run reporting (same as the default). Mutually
exclusive with -Execute.

.EXAMPLE
.\Remove-WebConfigFiles.ps1 -Path D:\sobekcm\resources -WhatIf

.EXAMPLE
.\Remove-WebConfigFiles.ps1 -Path D:\sobekcm\resources -Execute
#>
[CmdletBinding()]
param(
    [string]$Path = (Get-Location).Path,
    [switch]$WhatIf,
    [switch]$Execute
)

if ($WhatIf -and $Execute)
{
    throw "Specify either -WhatIf or -Execute, not both."
}

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path

Write-Host "Scanning '$resolvedPath' for web.config files..."
if (-not $Execute)
{
    Write-Host "(dry run -- pass -Execute to actually delete)"
}

$found = 0
$deleted = 0

Get-ChildItem -LiteralPath $resolvedPath -Recurse -Force -File -Filter 'web.config' -ErrorAction SilentlyContinue |
    ForEach-Object {
        $found++
        if ($Execute)
        {
            try
            {
                Remove-Item -LiteralPath $_.FullName -Force -ErrorAction Stop
                $deleted++
                Write-Host "Deleted: $($_.FullName)"
            }
            catch
            {
                Write-Warning "Failed to delete $($_.FullName): $($_.Exception.Message)"
            }
        }
        else
        {
            Write-Host "Would delete: $($_.FullName)"
        }
    }

if ($Execute)
{
    Write-Host "Done. Deleted $deleted of $found web.config file(s) found."
}
else
{
    Write-Host "Done. $found web.config file(s) found."
}
