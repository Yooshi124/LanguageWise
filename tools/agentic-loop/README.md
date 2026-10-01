# Agentic Loop — Rubber Duck Code Review

A read-only CLI harness that reviews a codebase with models via OpenRouter and
walks a disciplined agentic loop:

```
PLAN -> ACT -> OBSERVE -> AGENT -> HUMAN REVIEW -> ADAPT
```

You give it a targeted prompt ("review the database validation"). It reads the
relevant code, proposes numbered findings, asks which ones you accept, writes an
implementation plan for the accepted ones, and records the entire exchange as an
evidence log.

```
1. Problem: Database items are not validated before insert
   Suggested fix: Add DatabaseValidationService with the XYZ business rules

2. Problem: No test coverage for the database validator
   Suggested fix: Add UserValidationService_ChecksNullRecords and ...

Which suggestions would you like to accept? fix 1 and 2 please
```

## Read-only guarantee

This tool never edits your source. The only files it writes are its own:

- `Sessions/AgenticLoopSession{GUID}.md` — the evidence log for a session
- `Plans/AgenticLoopPlan{GUID}.md` — an implementation plan for accepted findings

## Quick start

```powershell
cd Tools\AgenticLoop
pip install -r requirements.txt
copy .env.example .env      # then paste your OpenRouter API key into it

# The review agent runs locally and is mandatory — install Ollama and pull its model:
#   https://ollama.com
ollama pull gemma4:e2b

python main.py

# Or diagnose the latest failed GitHub Actions run against the code:
python main.py --ci-failures

# Or cross-check the code against the local RAG server's documentation corpus
# (rag-server/, started separately with `python server.py`):
python main.py --rag-validate "how does the achievements service work"

# Or exercise the shared local MCP server and validate it against its code
# (mcp-server/, started separately with `dotnet run`):
python main.py --mcp-validate
```

## Documentation

| Guide | What it covers |
| --- | --- |
| [docs/SETUP.md](docs/SETUP.md) | Install, API key, full `.env` reference, scoping, troubleshooting |
| [docs/USAGE.md](docs/USAGE.md) | Commands, slash commands, prompt tips, a full worked example |
| [docs/HOW_IT_WORKS.md](docs/HOW_IT_WORKS.md) | Architecture, the six stages, how to customise it without writing code |

## At a glance

- **Model:** OpenRouter chat completions, default `google/gemma-4-26b-a4b-it`
  (the same provider pattern as the mini-games AI mode), set in `.env`
- **Scope:** the whole repository by default, or one directory via `TARGETED_DIRECTORY`
- **Two agents, two models:** an implementation agent (OpenRouter) proposes findings; a
  mandatory, local review agent (Gemma via Ollama) independently critiques them — it
  cannot be disabled
- **CI-failures mode:** `--ci-failures` pulls the latest failed GitHub Actions run
  and its test-log excerpts, then runs a round that diagnoses the root cause
  against the actual code
- **RAG validation mode:** `--rag-validate [topic]` retrieves documentation
  passages from the local RAG server (`rag-server/`, not containerised) and runs
  a round that flags code which contradicts or drifts from documented behaviour;
  `/rag <question>` queries the RAG server directly without running a full round
- **MCP validation mode:** `--mcp-validate [tool]` exercises the shared local MCP
  server (`mcp-server/`, not containerised) with `initialize`, `tools/list` and a
  real `tools/call` probe, then runs a round validating the server code against
  the observed behaviour; `/mcp [tool]` lists or calls tools directly
- **Prompts:** every prompt lives in `prompts/` as markdown — change behaviour without touching Python
- **Privacy:** `.env` files, keys, certificates and binaries are never sent to the API

## Where it lives

```
LanguageWise/
├── Tools/
│   └── AgenticLoop/        <- this tool (development only)
├── DatabaseService/        <- your services, reviewed by default
└── OtherService/
```
