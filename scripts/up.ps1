<#
.SYNOPSIS
    Builds and starts the whole integrated application with Docker Compose.
.EXAMPLE
    .\scripts\up.ps1
    .\scripts\up.ps1 -NoBuild
#>
[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    $arguments = @('compose', 'up', '--detach')
    if (-not $NoBuild) { $arguments += '--build' }

    Write-Host 'Starting LanguageWise...' -ForegroundColor Cyan
    & docker @arguments
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed with exit code $LASTEXITCODE." }

    Write-Host "`nWaiting for services to report healthy..." -ForegroundColor Cyan
    Start-Sleep -Seconds 10
    & docker compose ps

    Write-Host "`nOpen the application:" -ForegroundColor Green
    @(
        'Home (shared)                     http://localhost:3000',
        'Student 1  Mini Games             http://localhost:3001',
        'Student 2  Discussion Forum       http://localhost:3002',
        'Student 3  Quizzes and Courses    http://localhost:3003',
        'Student 4  Quests and Achievements http://localhost:3004',
        'Student 5  Leaderboard            http://localhost:3005'
    ) | ForEach-Object { Write-Host "  $_" }
}
finally {
    Pop-Location
}
