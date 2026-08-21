<#
.SYNOPSIS
    Builds every .NET microservice in the LanguageWise solution.
.EXAMPLE
    .\scripts\build.ps1
    .\scripts\build.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Building LanguageWise.sln ($Configuration)..." -ForegroundColor Cyan
dotnet build (Join-Path $repoRoot 'LanguageWise.sln') --configuration $Configuration --nologo

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

Write-Host 'Build succeeded.' -ForegroundColor Green
