<#
.SYNOPSIS
    Runs the NUnit test suite for every microservice.
.EXAMPLE
    .\scripts\test.ps1
    .\scripts\test.ps1 -Service student-3
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('all', 'shared', 'student-1', 'student-2', 'student-3', 'student-4', 'student-5')]
    [string]$Service = 'all'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

if ($Service -eq 'all') {
    $target = Join-Path $repoRoot 'LanguageWise.sln'
}
else {
    $pascal = if ($Service -eq 'shared') { 'Shared' } else { 'Student' + $Service.Split('-')[1] }
    $target = Join-Path $repoRoot "$Service\tests\LanguageWise.$pascal.Api.Tests\LanguageWise.$pascal.Api.Tests.csproj"
}

Write-Host "Testing $Service ($Configuration)..." -ForegroundColor Cyan
dotnet test $target --configuration $Configuration --nologo

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

Write-Host 'All tests passed.' -ForegroundColor Green
