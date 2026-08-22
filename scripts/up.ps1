<#
.SYNOPSIS
    Builds and starts the whole application with Docker Compose, then prints every URL.

.PARAMETER Detach
    Return to the prompt once the containers are healthy instead of streaming logs.

.EXAMPLE
    .\scripts\up.ps1 -Detach
#>
[CmdletBinding()]
param(
    [switch] $Detach
)

$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)

try {
    # Ollama also runs natively on some machines and would collide on 11434.
    if (-not $env:OLLAMA_PORT -and (Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue)) {
        $env:OLLAMA_PORT = '11435'
        Write-Host "Port 11434 is already in use, publishing Ollama on $env:OLLAMA_PORT instead." -ForegroundColor Yellow
    }

    $composeArgs = @('compose', 'up', '--build')
    if ($Detach) { $composeArgs += '--detach' }

    docker @composeArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if ($Detach) {
        Write-Host ''
        Write-Host 'LanguageWise is running:' -ForegroundColor Green
        @(
            'Home                                  http://localhost:3000',
            'Mini Games              (Kyan)        http://localhost:3001',
            'Discussion Forum        (Lachlan)     http://localhost:3002',
            'Quizzes and Courses     (Justin)      http://localhost:3003',
            'Quests and Achievements (Amber)       http://localhost:3004',
            'Leaderboard and Analytics (Roan)      http://localhost:3005'
        ) | ForEach-Object { Write-Host "  $_" }
        Write-Host ''
        Write-Host '  Backends 5000-5005 and database services 6000-6005 expose /health.'
    }
}
finally {
    Pop-Location
}
