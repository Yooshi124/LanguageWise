[CmdletBinding()]
param(
	[string]$ResultsDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) 'languagewise-e2e-results')
)

$ErrorActionPreference = 'Stop'
$repo = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$state = Join-Path ([System.IO.Path]::GetTempPath()) ("languagewise-e2e-" + [Guid]::NewGuid().ToString('N'))
$project = "lw-e2e-" + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$null = New-Item -ItemType Directory -Path $state
$null = New-Item -ItemType Directory -Path $ResultsDirectory -Force
$env:E2E_STATE_DIR = $state
$env:E2E_RESULTS_DIR = [System.IO.Path]::GetFullPath($ResultsDirectory)
$compose = @('compose', '--project-name', $project, '--project-directory', $repo,
	'--env-file', (Join-Path $state '.env'),
	'-f', (Join-Path $repo 'docker-compose.yml'), '-f', (Join-Path $PSScriptRoot 'compose.yml'))

function Invoke-Compose {
	param([string[]]$Arguments)
	& docker @compose @Arguments
	if ($LASTEXITCODE -ne 0) {
		throw "E2E docker compose command failed: $($Arguments -join ' ')"
	}
}

try {
	[System.IO.File]::WriteAllText((Join-Path $state '.env'), '')
	$rsa = [System.Security.Cryptography.RSA]::Create(2048)
	try {
		[System.IO.File]::WriteAllText((Join-Path $state 'signing_private_key.pem'), $rsa.ExportRSAPrivateKeyPem())
		[System.IO.File]::WriteAllText((Join-Path $state 'signing_public_key.pem'), $rsa.ExportSubjectPublicKeyInfoPem())
	}
	finally {
		$rsa.Dispose()
	}
	$key = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
	[System.IO.File]::WriteAllText((Join-Path $state 'mcp-api-key'), $key)
	Invoke-Compose -Arguments @('config', '--quiet')
	Invoke-Compose -Arguments @('build', 'e2e', 'generation-provider', 'rag-server', 'mcp-server',
		'shared-db', 'shared-db-api', 'shared-backend',
		'garry-ai-service-backend', 'mini-games-service-db', 'mini-games-service-backend',
		'quizzes-courses-service-db', 'quizzes-courses-service-backend',
		'chat-discussion-service-db', 'chat-discussion-service-backend',
		'quests-achievements-notifications-service-backend', 'leaderboard-analytics-service-backend')
	Invoke-Compose -Arguments @('up', '-d', '--wait', '--wait-timeout', '600',
		'shared-backend', 'mini-games-service-backend', 'quizzes-courses-service-backend',
		'chat-discussion-service-backend', 'quests-achievements-notifications-service-backend',
		'leaderboard-analytics-service-backend')
	foreach ($service in @('rag-server', 'mcp-server')) {
		$uid = & docker @compose exec -T $service id -u
		if ($LASTEXITCODE -ne 0 -or $uid.Trim() -eq '0') {
			throw "$service must run as a non-root user."
		}
	}
	Invoke-Compose -Arguments @('run', '--rm', '--no-deps', 'e2e')
	Invoke-Compose -Arguments @('stop', 'rag-server', 'mini-games-service-backend')
	Invoke-Compose -Arguments @('run', '--rm', '--no-deps', 'e2e', 'python', '-m', 'pytest', '-q',
		'--tb=short', 'outage_checks.py', '--junitxml=/results/outages.xml')
	# Reuse the same state/cache volumes to exercise startup reindexing.
	Invoke-Compose -Arguments @('up', '-d', '--wait', '--wait-timeout', '600', 'rag-server',
		'mini-games-service-backend')
	Invoke-Compose -Arguments @('run', '--rm', '--no-deps', 'e2e', 'python', '-m', 'pytest', '-q',
		'--tb=short', 'test_e2e.py', '-k', 'rag_transport_and_filtering', '--junitxml=/results/restart.xml')
}
finally {
	& docker @compose ps --all | Out-File (Join-Path $env:E2E_RESULTS_DIR 'compose-status.txt')
	& docker @compose logs --no-color --tail 150 | Out-File (Join-Path $env:E2E_RESULTS_DIR 'compose.log')
	& docker @compose down --volumes --remove-orphans --timeout 10
	if ($LASTEXITCODE -ne 0) {
		Write-Warning "E2E cleanup failed for project $project."
	}
	Remove-Item -LiteralPath $state -Recurse -Force
	Remove-Item Env:E2E_STATE_DIR, Env:E2E_RESULTS_DIR
}
