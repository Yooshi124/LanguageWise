# Setup

Everything you need to get the Agentic Loop running against this repository.

---

## 1. Prerequisites

| Requirement | Notes |
| --- | --- |
| Python 3.10 or newer | `python --version`. Developed and verified on 3.14. |
| pip | Ships with Python. `python -m pip --version` |
| An OpenRouter API key | Same provider as the mini-games AI mode. See step 3. |
| [Ollama](https://ollama.com), running locally | Hosts the mandatory Review Agent. See step 3b. |
| GitHub personal access token (optional) | Only needed for the CI-failures mode; see step 3c. |
| Git (optional) | Only used to record the branch and commit in the evidence log. |

The tool is pure Python. There is nothing to build and no Docker involved — it is
a development-time utility that happens to live inside a repository that will
later hold containerised services.

---

## 2. Install

From the repository root:

```powershell
cd Tools\AgenticLoop
pip install -r requirements.txt
```

Using a virtual environment is recommended so the tool's dependencies stay
separate from your services:

```powershell
cd Tools\AgenticLoop
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
```

Dependencies installed:

- `python-dotenv` — loads `.env`
- `pydantic` — validates the model's structured JSON replies
- `rich` — console rendering
- `requests` — talks to OpenRouter, the GitHub API, and the local Ollama review agent

---

## 3. Get an OpenRouter API key

1. Go to <https://openrouter.ai/keys>.
2. Click **Create key**.
3. Copy the key (it starts with `sk-or-`).

OpenRouter routes the request to whichever provider hosts the configured model —
the same pattern the mini-games service uses for its AI game mode. Check the
model's pricing on <https://openrouter.ai/models> and make sure the account has
credit; free-tier models have low rate limits. If you hit `429` errors during
large reviews, lower `MAX_FILES_IN_CONTEXT` or switch `OPENROUTER_MODEL` to a
model with more headroom.

---

## 3b. Set up the Review Agent (Ollama + Gemma, local, mandatory)

The Review Agent is not optional — every finding from the implementation agent
(OpenRouter) is independently checked by a second, locally-hosted model before it
reaches you. It never leaves your machine and needs no API key.

1. Install Ollama: <https://ollama.com> (Windows, macOS, Linux).
2. Make sure the Ollama service is running (it starts automatically after
   install, or run `ollama serve`).
3. Pull the review model:

   ```powershell
   ollama pull gemma4:e2b
   ```

   `gemma4:e2b` is a small, fast Gemma 4 build ("effective 2B" parameters) —
   enough to sanity-check findings without slowing the loop down. A larger
   variant (e.g. `gemma4:12b`) can be used instead by setting
   `OLLAMA_REVIEW_MODEL` if you have the hardware and want sharper critiques.
4. Verify it responds:

   ```powershell
   ollama run gemma4:e2b "reply with the word ready"
   ```

If Ollama is not running or the model is not pulled, the loop still starts, but
any review round will stop with a clear error explaining what to install/pull —
it will never silently show you unreviewed findings.

---

## 3c. GitHub token for the CI-failures mode (optional)

The loop can pull the latest failed GitHub Actions run — including the failing
test and build log excerpts — and run a review round that diagnoses the root
cause against the actual code (`python main.py --ci-failures`, or `/ci-failures`
in the REPL).

- The repository is auto-detected from `git remote get-url origin`; set
  `GITHUB_REPO=owner/name` to override it.
- For public repositories a token is optional but recommended (the anonymous
  rate limit is tight). For private repositories it is required.
- Create a classic token with `repo` scope (or a fine-grained token with
  **Actions: read**) at <https://github.com/settings/tokens>, then set
  `GITHUB_TOKEN` in `.env`.
- Set `GITHUB_WORKFLOW` (e.g. `student-3.yml`) to always inspect one workflow;
  leave it blank to use the most recent failed run of any workflow.

---

## 3d. RAG server for the RAG-validate mode (optional)

The loop can retrieve documentation passages from the local RAG server
(`rag-server/`) and run a review round that flags code which contradicts or
drifts from documented behaviour (`python main.py --rag-validate`, or
`/rag-validate`/`/rag` in the REPL).

- The RAG server runs locally and is not containerised. Start it separately
  from the `rag-server` directory: `python server.py`.
- Set `RAG_BASE_URL` if it is not running on the default `http://localhost:8100`.
- This mode is independent of the CI-failures/GitHub setup above; neither
  requires the other.

---

## 3e. MCP server for the MCP-validate mode (optional)

The loop can exercise the shared local MCP server (`mcp-server/`) —
`initialize`, `tools/list` and one real `tools/call` probe — and run a review
round that validates the server code against the observed behaviour
(`python main.py --mcp-validate`, or `/mcp-validate`/`/mcp` in the REPL).

- The MCP server runs locally and is not containerised. Start it separately:
  `dotnet run` from `mcp-server/src/LanguageWise.McpServer`.
- The shared API key is read from `mcp-server/.mcp-api-key` (generated by
  `pwsh mcp-server/scripts/New-McpApiKey.ps1`); set `MCP_API_KEY` or
  `MCP_API_KEY_PATH` in `.env` to override.
- `MCP_TOOL_SCOPE` picks the tool scope to validate (`courses` by default;
  `games`, `chat`, `quests`, `leaderboard` exist as scopes). Only tools named
  `<scope>_*` are listed under a scope.
- `MCP_USER_TOKEN` (optional) forwards a user JWT so per-user tools can run;
  without it, those tools return their "signed in" error, which is still a
  valid validation outcome.

---

## 4. Create your `.env`

```powershell
cd Tools\AgenticLoop
copy .env.example .env
```

Open `.env` and paste your key:

```ini
OPENROUTER_API_KEY=sk-or-...your-key-here
OPENROUTER_MODEL=google/gemma-4-26b-a4b-it
```

That is the minimum. `.env` is listed in the repository's `.gitignore`, so your
key is never committed.

---

## 5. Full `.env` reference

### Required

| Variable | Default | Meaning |
| --- | --- | --- |
| `OPENROUTER_API_KEY` | *(none)* | Your OpenRouter key. Startup fails with a clear message if it is missing. |

### Models

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `OPENROUTER_MODEL` | `google/gemma-4-26b-a4b-it` | Model used for analysis and planning. Any OpenRouter model id. | `anthropic/claude-sonnet-4` |
| `OPENROUTER_SELECTION_MODEL` | falls back to `OPENROUTER_MODEL` | Model used to pick which files to read. A cheaper model is fine here. | `google/gemma-4-26b-a4b-it` |
| `OPENROUTER_BASE_URL` | `https://openrouter.ai/api/v1` | API base URL. Only change for a compatible proxy. | `https://openrouter.ai/api/v1` |

### GitHub Actions integration

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `GITHUB_TOKEN` | *(blank)* | Personal access token used by the CI-failures mode. Optional for public repos, required for private ones. Never committed or sent anywhere except `api.github.com`. | `ghp_...` |
| `GITHUB_REPO` | auto-detected from the `origin` remote | Repository as `owner/name`. | `my-org/LanguageWise` |
| `GITHUB_WORKFLOW` | *(blank)* | Workflow file or name to inspect. Blank means the most recent failed run of any workflow. | `student-3.yml` |

### RAG integration

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `RAG_BASE_URL` | `http://localhost:8100` | Base URL of the local, non-containerised RAG server (`rag-server/`). | `http://localhost:8100` |
| `RAG_REQUEST_TIMEOUT_SECONDS` | `10` | Per-request timeout against the RAG server. | `20` |
| `RAG_DEFAULT_N_RESULTS` | `5` | Number of passages requested per query when not specified explicitly. | `8` |

### MCP integration

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `MCP_BASE_URL` | `http://localhost:8200` | Base URL of the shared local, non-containerised MCP server (`mcp-server/`). The JSON-RPC endpoint is `{base}/mcp`. | `http://localhost:8200` |
| `MCP_API_KEY` | falls back to the key file | Shared MCP API key. Blank reads `MCP_API_KEY_PATH` (default `mcp-server/.mcp-api-key`, generated by `scripts/New-McpApiKey.ps1`). Redacted from all output. | *(leave blank)* |
| `MCP_API_KEY_PATH` | `mcp-server/.mcp-api-key` under the repo root | Where the shared key file lives. | `C:\secrets\mcp.key` |
| `MCP_TOOL_SCOPE` | `courses` | Tool scope sent as `X-LanguageWise-Tool-Scope`; only `<scope>_*` tools are listed/called. | `games` |
| `MCP_USER_TOKEN` | *(blank)* | Optional user JWT forwarded as `X-LanguageWise-User-Token` so per-user tools can run. | *(leave blank)* |
| `MCP_REQUEST_TIMEOUT_SECONDS` | `30` | Per-request timeout against the MCP server. | `60` |

### Review Agent (local, mandatory)

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `OLLAMA_HOST` | `http://localhost:11434` | Where the local Ollama daemon is listening. | `http://localhost:11434` |
| `OLLAMA_REVIEW_MODEL` | `gemma4:e2b` | Ollama model tag used by the Review Agent. Must be pulled first (`ollama pull <tag>`). A bigger tag gives sharper critiques at the cost of speed. | `gemma4:12b` |
| `OLLAMA_REQUEST_TIMEOUT_SECONDS` | `600` | Per-request timeout against Ollama. Local models can be slow, especially loading into memory on first use — 10 minutes gives it plenty of room. | `900` |
| `OLLAMA_KEEP_ALIVE` | `30s` | Ollama's own `keep_alive` duration: how long it keeps the review model loaded in RAM after a call before unloading it. Short values stop the model staying resident in memory between rounds, which matters when rag-server/mcp-server are also running locally. `0` unloads immediately, `-1` never unloads. | `0` |

There is no setting to disable the Review Agent — it always runs. If it cannot
be reached, the round aborts with an explanation instead of silently showing
unreviewed findings.

### Review scope

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `REPO_ROOT` | auto-detected | The repository to review. Auto-detection walks up from `Tools/AgenticLoop` looking for a `.git` folder. | `C:\Users\justi\source\repos\LanguageWise` |
| `TARGETED_DIRECTORY` | *(blank)* | Review only this directory and its children. Blank means the whole repository. Must live inside `REPO_ROOT`. | `C:\Users\justi\source\repos\LanguageWise\DatabaseService` |
| `IGNORE_DIRS` | `.git,node_modules,.venv,venv,env,__pycache__,bin,obj,dist,build,out,target,.vs,.idea,.pytest_cache,.mypy_cache,coverage,htmlcov,Sessions,Plans` | Directory names skipped anywhere in the tree. | `.git,node_modules,migrations` |
| `INCLUDE_EXTENSIONS` | a broad built-in set (`.py`, `.cs`, `.ts`, `.sql`, `.yml`, `.md`, ...) | Restrict which file types are scanned. Leading dots are optional. | `py,cs,sql` |

> The misspelling `TARGETTED_DIRECTORY` is also accepted, so a typo never
> silently reviews the entire repository.

### Context budget

| Variable | Default | Meaning |
| --- | --- | --- |
| `MAX_FILE_BYTES` | `200000` | Per-file read cap. Larger files are truncated and explicitly marked as truncated in the prompt. |
| `MAX_FILES_IN_CONTEXT` | `40` | Maximum files sent to the model in one round. |
| `MAX_TOTAL_CONTEXT_BYTES` | `1500000` | Total code payload budget per round. Must be at least `MAX_FILE_BYTES`. |

### Output

| Variable | Default | Meaning |
| --- | --- | --- |
| `SESSIONS_DIR` | `Sessions` | Where evidence logs are written. Relative paths resolve inside `Tools/AgenticLoop`. |
| `PLANS_DIR` | `Plans` | Where implementation plans are written. |

### Resilience and logging

| Variable | Default | Meaning |
| --- | --- | --- |
| `REQUEST_TIMEOUT_SECONDS` | `180` | Per-request timeout. |
| `MAX_OUTPUT_TOKENS` | `32000` | Cap on each reply, **including the model's internal thinking**. Too low and the model runs out of budget mid-answer, returning no findings. |
| `THINKING_LEVEL` | `low` | `minimal`, `low`, `medium`, `high`, or `default` to leave it to the model. Mapped to OpenRouter's unified `reasoning.effort` parameter; providers that do not support reasoning ignore it. |
| `MAX_RETRIES` | `3` | Attempts per call. Only transient errors (429, 5xx, timeouts) are retried. Rate limits honour the `Retry-After` header (capped at 8s), everything else uses exponential backoff. |
| `LOG_LEVEL` | `INFO` | One of `DEBUG`, `INFO`, `WARNING`, `ERROR`, `CRITICAL`. HTTP client chatter from the SDK is suppressed unless you set `DEBUG`. |

---

## 6. Choosing what gets reviewed

### Review everything (default)

Leave `TARGETED_DIRECTORY` blank. Every sibling folder of `Tools/` is reviewed:

```
LanguageWise/
├── Tools/AgenticLoop/     <- the tool
├── DatabaseService/       <- reviewed
├── EnrolmentService/      <- reviewed
└── FrontendService/       <- reviewed
```

### Review one service

```ini
TARGETED_DIRECTORY=C:\Users\justi\source\repos\LanguageWise\DatabaseService
```

Now only `DatabaseService` and its children are scanned.

### Change scope temporarily

You do not have to edit `.env` for a one-off:

```powershell
python main.py --scope ..\..\DatabaseService
```

or, inside the REPL:

```
agentic-loop > /scope DatabaseService
agentic-loop > /scope reset
```

Relative paths given to `--scope` and `/scope` resolve from `REPO_ROOT`, and must
stay inside it. `/scope reset` returns to the scope the session started with
(`TARGETED_DIRECTORY`, or `--scope` if you passed one).

---

## 7. First run

```powershell
cd Tools\AgenticLoop
python main.py
```

You should see a startup panel showing the model, scope and the path of this
session's evidence log, followed by a prompt:

```
agentic-loop >
```

Type `/status` to confirm the configuration, then try a real prompt such as
`review error handling in the database service`. See [USAGE.md](USAGE.md) for the
full walkthrough.

To verify without entering the REPL:

```powershell
python main.py --prompt "summarise the biggest risks in this codebase"
```

### Verifying the install cheaply

Point the first run at one small folder so it costs almost nothing:

```powershell
python main.py --scope collectors --prompt "review the error handling in these file collectors"
```

A healthy run shows all six stage banners, a numbered findings list, the
acceptance question, and a saved plan path. Then confirm the artefacts:

```powershell
Get-ChildItem Sessions, Plans
```

The session log should contain `### 1. PLAN` through `### 6. ADAPT` for the
round, your prompt verbatim, and an ACCEPTED/REJECTED table.

---

## 8. Privacy and safety

- The contents of the files selected for a round are **sent to OpenRouter**,
  which routes them to the provider hosting the configured model, for the
  implementation, planning and file-selection steps. Do not point the tool at a
  repository you are not permitted to share.
- In CI-failures mode, excerpts from failed GitHub Actions job logs are fetched
  from `api.github.com` and included in that same context. Your `GITHUB_TOKEN`
  is only ever sent to `api.github.com` and is redacted from all output.
- The Review Agent runs entirely on your machine via Ollama — the same source
  code and findings the implementation agent already saw are sent to it too,
  but only ever to the local Ollama daemon; nothing leaves your machine for
  that pass and no API key is involved.
- Files that look like secrets are never read or uploaded:
  `.env`, `*.env`, `.env.*`, `*.pem`, `*.key`, `*.pfx`, `*.p12`, `id_rsa*`,
  `secrets.*`, `credentials*`, `*.keystore`, `*.jks`.
- Binary files are detected and skipped.
- Your API key is redacted in every console message, log line and error.
- The tool opens no source file for writing. It writes only to `Sessions/` and `Plans/`.

---

## 9. Troubleshooting

| Symptom | Cause and fix |
| --- | --- |
| `OPENROUTER_API_KEY is not set.` | No `.env`, or the key line is blank. Copy `.env.example` to `.env` and paste your key. |
| `OpenRouter request failed with HTTP 401` | The key is wrong, truncated, or has stray whitespace. Regenerate it at <https://openrouter.ai/keys>. The round stops cleanly and the failure is written to the evidence log. |
| `OpenRouter request failed with HTTP 404` | The model id does not exist. Check `OPENROUTER_MODEL` against <https://openrouter.ai/models>. |
| `TARGETED_DIRECTORY does not exist` | Path typo, or the folder has not been created yet. Use an absolute path. |
| `TARGETED_DIRECTORY (...) must live inside REPO_ROOT (...)` | The target is outside the repository. Either move it inside, or set `REPO_ROOT` to a parent that contains both. |
| `no reviewable files found in scope` | The scope is empty, everything in it is ignored by `IGNORE_DIRS`, or its file types are missing from `INCLUDE_EXTENSIONS`. Run `/status` and `/config` to inspect. |
| `OpenRouter returned an empty response` | Usually an invalid `OPENROUTER_MODEL`. Check the model id against <https://openrouter.ai/models>. |
| `ran out of output budget and returned a partial reply` | The reply was cut off. Raise `MAX_OUTPUT_TOKENS`, lower `THINKING_LEVEL`, or narrow the scope. |
| Zero findings on a prompt that should find something | Same cause: the model spent its budget thinking. Lower `THINKING_LEVEL`, raise `MAX_OUTPUT_TOKENS`, or scope the review to one service or folder so there is less code to reason about. |
| `OpenRouter request failed with HTTP 429` | Rate limited. The client already retries honouring `Retry-After`; if it persists, lower `MAX_FILES_IN_CONTEXT` or add credit at <https://openrouter.ai/credits>. |
| `Could not determine the GitHub repository` | No `origin` remote and no `GITHUB_REPO` set. Add the remote or set `GITHUB_REPO=owner/name` in `.env`. |
| `GitHub API returned HTTP 401/403/404` in CI-failures mode | The repo is private or the rate limit is exhausted. Set `GITHUB_TOKEN` in `.env`. |
| `No failed GitHub Actions runs found` | There is no failed run for the chosen workflow. Run the workflow, or clear `GITHUB_WORKFLOW` to search all workflows. |
| `Could not reach the MCP server at http://localhost:8200/mcp` | The MCP server is not running. Start it with `dotnet run` from `mcp-server/src/LanguageWise.McpServer`. |
| `No MCP API key is configured` | Neither `MCP_API_KEY` nor the key file (`mcp-server/.mcp-api-key`) exists. Run `pwsh mcp-server/scripts/New-McpApiKey.ps1` once per machine. |
| `The MCP server rejected the shared API key (HTTP 401)` | The key does not match the server's. Re-check `MCP_API_KEY` / `MCP_API_KEY_PATH` against the file the server loads. |
| `Tool '...' is not listed for scope '...'` | The probe tool belongs to another scope, or no tools exist yet for `MCP_TOOL_SCOPE`. Try `/mcp` to list what the scope exposes. |
| `Could not reach the local review agent at http://localhost:11434 ...` | Ollama is not running. Install it from <https://ollama.com>, start it (`ollama serve`), and pull the model: `ollama pull gemma4:e2b`. The review agent cannot be disabled, so this stops the round until Ollama is reachable. |
| `Ollama model '...' was not found` | The tag in `OLLAMA_REVIEW_MODEL` has not been pulled yet. Run `ollama pull <tag>`. |
| `The local review agent (...) returned JSON that does not match CritiqueResult` | The local model produced malformed structured output — rare, but small models occasionally do this. Retry, or switch `OLLAMA_REVIEW_MODEL` to a slightly larger tag (e.g. `gemma4:12b`). |
| The review agent drops findings that look valid | The critic prompts are deliberately keep-first and a wholesale rejection triggers a mandatory re-check pass. If a small local model still over-drops, switch `OLLAMA_REVIEW_MODEL` to a larger tag (e.g. `gemma4:12b`). |
| `MAX_TOTAL_CONTEXT_BYTES ... must be at least MAX_FILE_BYTES` | The two budgets contradict each other. Raise the total or lower the per-file cap. |
| `Missing prompt template '...'` | A file was deleted from `prompts/`. Restore it from git. |
| `Prompt '...' expects values for: X` | You added a `{{X}}` placeholder the code does not supply. See [HOW_IT_WORKS.md](HOW_IT_WORKS.md#customising-prompts). |
| Garbled characters in the console | Use Windows Terminal or PowerShell 7. The tool forces UTF-8 output where the terminal allows it. |
