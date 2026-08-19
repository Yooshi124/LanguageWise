# Agentic Loop — Rubber Duck Code Review

A read-only CLI harness that reviews a codebase with Google Gemini and walks a
disciplined agentic loop:

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
copy .env.example .env      # then paste your Gemini API key into it
python main.py
```

## Documentation

| Guide | What it covers |
| --- | --- |
| [docs/SETUP.md](docs/SETUP.md) | Install, API key, full `.env` reference, scoping, troubleshooting |
| [docs/USAGE.md](docs/USAGE.md) | Commands, slash commands, prompt tips, a full worked example |
| [docs/HOW_IT_WORKS.md](docs/HOW_IT_WORKS.md) | Architecture, the six stages, how to customise it without writing code |

## At a glance

- **Model:** Google Gemini via the `google-genai` SDK, default `gemini-3.7-flash`, set in `.env`
- **Scope:** the whole repository by default, or one directory via `TARGETED_DIRECTORY`
- **Two agents:** an implementation agent proposes findings, a review agent critiques them
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
