<#
.SYNOPSIS
    Stops the integrated application.
.DESCRIPTION
    By default the SQLite named volumes are kept, so your data survives a restart.
    Pass -Clean to delete them and force a fresh re-seed on the next start.
.EXAMPLE
    .\scripts\down.ps1
    .\scripts\down.ps1 -Clean
#>
[CmdletBinding()]
param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $arguments = @('compose', 'down')
    if ($Clean) {
        Write-Warning 'Removing named volumes — every database will be re-seeded on the next start.'
        $arguments += '--volumes'
    }

    & docker @arguments
    if ($LASTEXITCODE -ne 0) { throw "docker compose down failed with exit code $LASTEXITCODE." }

    Write-Host 'LanguageWise stopped.' -ForegroundColor Green
}
finally {
    Pop-Location
}
