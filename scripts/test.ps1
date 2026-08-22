<#
.SYNOPSIS
    Runs the NUnit tests for every microservice, or just one of them.

.PARAMETER Service
    Which microservice to test. Defaults to all of them.

.PARAMETER Configuration
    Debug or Release. Defaults to Debug.

.EXAMPLE
    .\scripts\test.ps1
    .\scripts\test.ps1 -Service quizzes-courses-service
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
    Write-Host "`nTesting $($solution.Directory.Name) ($Configuration)..." -ForegroundColor Cyan
    dotnet test $solution.FullName --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { $failed += $solution.Directory.Name }
}

if ($failed) {
    Write-Host "`nTests FAILED: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host "`nAll tests passed ($($solutions.Count) microservice(s))." -ForegroundColor Green
