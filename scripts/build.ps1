<#
.SYNOPSIS
    Builds every microservice, or just one of them.

.DESCRIPTION
    Each microservice owns a .slnx solution and has no dependency on the others, so
    this simply builds each solution in turn. A failure names the service that broke.

.PARAMETER Service
    Which microservice to build. Defaults to all of them.

.PARAMETER Configuration
    Debug or Release. Defaults to Debug.

.EXAMPLE
    .\scripts\build.ps1
    .\scripts\build.ps1 -Service quizzes-courses-service -Configuration Release
#>
[CmdletBinding()]
param(
    [ValidateSet('all', 'shared', 'mini-games-service', 'chat-discussion-service',
                 'quizzes-courses-service', 'quests-achievements-notifications-service',
                 'leaderboard-analytics-service')]
    [string] $Service = 'all',

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$solutions = Get-ChildItem -Path $repoRoot -Recurse -Filter '*.slnx' -File |
    Where-Object { $Service -eq 'all' -or $_.Directory.Name -eq $Service } |
    Sort-Object FullName

if (-not $solutions) {
    Write-Error "No solution found for '$Service'."
}

$failed = @()
foreach ($solution in $solutions) {
    Write-Host "`nBuilding $($solution.Directory.Name) ($Configuration)..." -ForegroundColor Cyan
    dotnet build $solution.FullName --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { $failed += $solution.Directory.Name }
}

if ($failed) {
    Write-Host "`nBuild FAILED: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host "`nBuild succeeded ($($solutions.Count) microservice(s))." -ForegroundColor Green
