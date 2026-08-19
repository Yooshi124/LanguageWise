# Setup

Everything you need to get the Agentic Loop running against this repository.

---

## 1. Prerequisites

| Requirement | Notes |
| --- | --- |
| Python 3.10 or newer | `python --version`. Developed and verified on 3.14. |
| pip | Ships with Python. `python -m pip --version` |
| A Google AI Studio API key | Free tier is enough to start. See step 3. |
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

- `google-genai` — the official Google Gen AI SDK
- `python-dotenv` — loads `.env`
- `pydantic` — validates the model's structured JSON replies
- `rich` — console rendering

---

## 3. Get a Gemini API key

1. Go to <https://aistudio.google.com/apikey>.
2. Click **Create API key** (AI Studio creates a project for you if needed).
3. Copy the key.

The free tier has low rate limits. If you hit `429` errors during large reviews,
either lower `MAX_FILES_IN_CONTEXT` or set up billing in AI Studio.

---

## 4. Create your `.env`

```powershell
cd Tools\AgenticLoop
copy .env.example .env
```

Open `.env` and paste your key:

```ini
GEMINI_API_KEY=AIza...your-key-here
GEMINI_MODEL=gemini-3.7-flash
```

That is the minimum. `.env` is listed in the repository's `.gitignore`, so your
key is never committed.

---

## 5. Full `.env` reference

### Required

| Variable | Default | Meaning |
| --- | --- | --- |
| `GEMINI_API_KEY` | *(none)* | Your Google AI Studio key. Startup fails with a clear message if it is missing. |

### Models

| Variable | Default | Meaning | Example |
| --- | --- | --- | --- |
| `GEMINI_MODEL` | `gemini-3.7-flash` | Model used for analysis and planning. | `gemini-3.7-flash` |
| `GEMINI_SELECTION_MODEL` | falls back to `GEMINI_MODEL` | Model used to pick which files to read. A cheaper model is fine here. | `gemini-3.5-flash-lite` |
| `GEMINI_REVIEW_MODEL` | falls back to `GEMINI_MODEL` | Model used by the critic pass. A stronger model gives sharper critiques. | `gemini-3.7-flash` |
| `ENABLE_REVIEW_AGENT` | `true` | Set to `false` to skip the critic pass — roughly one third fewer tokens per round. | `false` |

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
| `THINKING_LEVEL` | `low` | `minimal`, `low`, `medium`, `high`, or `default` to leave it to the model. Higher means better findings but slower and more tokens. |
| `MAX_RETRIES` | `3` | Attempts per call. Only transient errors (429, 5xx, timeouts) are retried, with exponential backoff. |
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

- The contents of the files selected for a round are **sent to Google's Gemini API**.
  Do not point the tool at a repository you are not permitted to share.
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
| `GEMINI_API_KEY is not set.` | No `.env`, or the key line is blank. Copy `.env.example` to `.env` and paste your key. |
| `Gemini request failed: Error code: 400 - ... 'API key not valid. Please pass a valid API key.'` | The key is wrong, truncated, or has stray whitespace. Regenerate it at <https://aistudio.google.com/apikey>. The round stops cleanly and the failure is written to the evidence log. |
| `Gemini request failed: Error code: 404` | The model name does not exist for your key. Check `GEMINI_MODEL` against <https://ai.google.dev/gemini-api/docs/models>. |
| `TARGETED_DIRECTORY does not exist` | Path typo, or the folder has not been created yet. Use an absolute path. |
| `TARGETED_DIRECTORY (...) must live inside REPO_ROOT (...)` | The target is outside the repository. Either move it inside, or set `REPO_ROOT` to a parent that contains both. |
| `no reviewable files found in scope` | The scope is empty, everything in it is ignored by `IGNORE_DIRS`, or its file types are missing from `INCLUDE_EXTENSIONS`. Run `/status` and `/config` to inspect. |
| `Gemini returned an empty response` | Usually an invalid `GEMINI_MODEL`. Check the model name against <https://ai.google.dev/gemini-api/docs/models>. |
| `ran out of output budget and returned a partial reply` | The reply was cut off. Raise `MAX_OUTPUT_TOKENS`, lower `THINKING_LEVEL`, or narrow the scope. |
| Zero findings on a prompt that should find something | Same cause: the model spent its budget thinking. Lower `THINKING_LEVEL`, raise `MAX_OUTPUT_TOKENS`, or scope the review to one service or folder so there is less code to reason about. |
| `Gemini request failed: ... 429 ...` | Rate limited. Lower `MAX_FILES_IN_CONTEXT`, set `ENABLE_REVIEW_AGENT=false`, or enable billing. |
| `MAX_TOTAL_CONTEXT_BYTES ... must be at least MAX_FILE_BYTES` | The two budgets contradict each other. Raise the total or lower the per-file cap. |
| `Missing prompt template '...'` | A file was deleted from `prompts/`. Restore it from git. |
| `Prompt '...' expects values for: X` | You added a `{{X}}` placeholder the code does not supply. See [HOW_IT_WORKS.md](HOW_IT_WORKS.md#customising-prompts). |
| Garbled characters in the console | Use Windows Terminal or PowerShell 7. The tool forces UTF-8 output where the terminal allows it. |
