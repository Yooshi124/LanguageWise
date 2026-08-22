<#
.SYNOPSIS
    Stops the application.

.PARAMETER Clean
    Also delete the named volumes, wiping every SQLite database. The next start
    re-seeds from scratch.

.EXAMPLE
    .\scripts\down.ps1
    .\scripts\down.ps1 -Clean
#>
[CmdletBinding()]
param(
    [switch] $Clean
)

$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)

try {
    $composeArgs = @('compose', 'down')
    if ($Clean) {
        $composeArgs += '--volumes'
        Write-Host 'Stopping and wiping all databases...' -ForegroundColor Yellow
    }
    else {
        Write-Host 'Stopping (databases are kept)...' -ForegroundColor Cyan
    }

    docker @composeArgs
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
