param([switch]$Force)

$ErrorActionPreference = 'Stop'
$keyPath = Join-Path (Split-Path $PSScriptRoot -Parent) '.mcp-api-key'

if ((Test-Path $keyPath) -and -not $Force) {
	Write-Host "MCP API key already exists at $keyPath (use -Force to rotate)."
	return
}

$bytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
$key = [Convert]::ToHexString($bytes).ToLowerInvariant()
[System.IO.File]::WriteAllText($keyPath, $key)
Write-Host "Wrote MCP API key to $keyPath"
