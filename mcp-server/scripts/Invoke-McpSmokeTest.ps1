param(
	[string]$Endpoint = 'http://localhost:8200/mcp',
	[string]$UserToken,
	[string]$CourseCode = 'it',
	[string]$LessonSlug = 'greetings'
)

$ErrorActionPreference = 'Stop'
$keyPath = Join-Path (Split-Path $PSScriptRoot -Parent) '.mcp-api-key'
$apiKey = (Get-Content $keyPath -Raw).Trim()
$script:id = 0

function Invoke-Mcp([string]$Method, [hashtable]$Params = @{}, [string]$Scope = 'courses', [string]$Key = $apiKey) {
	$script:id++
	$headers = @{ Accept = 'application/json, text/event-stream' }
	if ($Key) { $headers['X-LanguageWise-Mcp-Key'] = $Key }
	if ($Scope) { $headers['X-LanguageWise-Tool-Scope'] = $Scope }
	if ($UserToken) { $headers['X-LanguageWise-User-Token'] = $UserToken }
	$body = @{ jsonrpc = '2.0'; id = $script:id; method = $Method; params = $Params } | ConvertTo-Json -Depth 10 -Compress
	$response = Invoke-WebRequest -Uri $Endpoint -Method Post -Headers $headers -ContentType 'application/json' -Body $body -SkipHttpErrorCheck
	if ($response.StatusCode -ne 200) { return [pscustomobject]@{ status = $response.StatusCode } }
	$content = [string]$response.Content
	$json = if ($content.TrimStart().StartsWith('{')) { $content } else { ($content -split "`n" | Where-Object { $_ -like 'data:*' } | Select-Object -Last 1).Substring(5) }
	return $json | ConvertFrom-Json
}

function Show([string]$Title, $Value) {
	Write-Host "`n=== $Title ===" -ForegroundColor Cyan
	$Value | ConvertTo-Json -Depth 10 | Write-Host
}

$init = Invoke-Mcp 'initialize' @{ protocolVersion = '2025-06-18'; capabilities = @{}; clientInfo = @{ name = 'smoke-test'; version = '1.0' } }
Show 'initialize → serverInfo' $init.result.serverInfo

$list = Invoke-Mcp 'tools/list'
Show 'tools/list (scope=courses)' ($list.result.tools | Select-Object name, description)

Show 'tools/call courses_list_courses' (Invoke-Mcp 'tools/call' @{ name = 'courses_list_courses'; arguments = @{} }).result
Show 'tools/call courses_get_lesson_vocabulary' (Invoke-Mcp 'tools/call' @{ name = 'courses_get_lesson_vocabulary'; arguments = @{ courseCode = $CourseCode; lessonSlug = $LessonSlug } }).result
Show 'tools/call courses_get_my_progress' (Invoke-Mcp 'tools/call' @{ name = 'courses_get_my_progress'; arguments = @{ courseCode = $CourseCode } }).result
Show 'tools/call courses_list_lessons' (Invoke-Mcp 'tools/call' @{ name = 'courses_list_lessons'; arguments = @{ courseCode = $CourseCode } }).result
Show 'tools/call courses_list_quizzes' (Invoke-Mcp 'tools/call' @{ name = 'courses_list_quizzes'; arguments = @{ courseCode = $CourseCode } }).result
Show 'tools/call courses_get_flashcards' (Invoke-Mcp 'tools/call' @{ name = 'courses_get_flashcards'; arguments = @{ courseCode = $CourseCode; lessonSlug = $LessonSlug } }).result
Show 'tools/call courses_get_my_vocabulary' (Invoke-Mcp 'tools/call' @{ name = 'courses_get_my_vocabulary'; arguments = @{ courseCode = $CourseCode } }).result
Show 'tools/call courses_get_my_milestones' (Invoke-Mcp 'tools/call' @{ name = 'courses_get_my_milestones'; arguments = @{ limit = 5 } }).result

$noKey = Invoke-Mcp 'tools/list' -Key ''
Show 'Missing API key (expect 401)' $noKey

$otherScope = Invoke-Mcp 'tools/list' -Scope 'games'
Show 'tools/list (scope=games, expect no courses tools)' ($otherScope.result.tools | Select-Object name)

$outOfScope = Invoke-Mcp 'tools/call' @{ name = 'courses_list_courses'; arguments = @{} } -Scope 'games'
Show 'tools/call courses_* with scope=games (expect error)' $outOfScope.error
