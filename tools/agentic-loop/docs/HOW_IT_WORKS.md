# How It Works

The design of the Agentic Loop, and how to change its behaviour without writing
any Python.

---

## The idea

A classic agentic loop is a disciplined cycle with a human in it:

```
PLAN -> ACT -> OBSERVE -> AGENT -> HUMAN REVIEW -> ADAPT
```

The original version of this loop hard-coded its prompts, its file list and its
domain rules in Python, so changing the review focus meant editing source. This
version keeps the discipline and moves every variable part into configuration:

| Was hard-coded | Now |
| --- | --- |
| Prompts embedded in Python strings | Markdown templates in `prompts/` |
| A fixed list of two files | Scope scan plus model-driven file selection |
| One application's domain rules | Whatever the reviewed repository actually contains |
| Ollama with two local models | Google Gemini, model set in `.env` |
| A 1 / 2 / 3 accept menu | Per-finding acceptance in plain English |
| One shared `evidence_log.md` | One `AgenticLoopSession{GUID}.md` per session |
| No actionable output | `AgenticLoopPlan{GUID}.md` per accepted set |

---

## The six stages

| Stage | Module | What happens |
| --- | --- | --- |
| **1. PLAN** | `core/orchestrator.py` | Your prompt is captured verbatim and written to the log. Nothing is inferred or rewritten. |
| **2. ACT** | `collectors/repo_scanner.py`, `agents/file_selector.py`, `collectors/file_reader.py` | The scope is walked, a manifest is built, the model picks the relevant files, and those files are read within budget. |
| **3. OBSERVE** | `collectors/repo_observer.py` | Deterministic facts are computed locally: file and line counts, file types, test files, git branch and commit, and every skip or truncation warning. No model involved. |
| **4. AGENT** | `agents/analyst.py`, `agents/critic.py` | The implementation agent proposes findings; the review agent challenges them. Only survivors are shown. |
| **5. HUMAN REVIEW** | `agents/decision_parser.py` | Your free-text reply is turned into accepted indices. Your exact words are recorded. |
| **6. ADAPT** | `agents/planner.py`, `output/plan_writer.py` | Accepted findings become a detailed implementation plan on disk; the decision and outcome are logged. |

The stage names live in one place, `core/stages.py`. Both the console renderer
and the evidence-log writer read from it, so the banners you see on screen and
the headings in the markdown can never disagree. Even a stage that could not be
reached is written to the log as `(not reached — <reason>)`, so a round's record
is never silently incomplete.

---

## Architecture

```
main.py                     CLI entry point, REPL, slash commands
│
├── config/settings.py      typed configuration from .env, validation, scope rules
│
├── core/
│   ├── stages.py           the six canonical stages (single source of truth)
│   ├── orchestrator.py     runs a round: stage banners + log headings, error handling
│   ├── models.py           pydantic schemas, doubling as the model's JSON schemas
│   ├── gemini_client.py    google-genai wrapper: structured output, retries, usage
│   ├── prompt_registry.py  loads prompts/*.md, strict {{PLACEHOLDER}} rendering
│   ├── session.py          session and round identity, timing, running totals
│   └── console.py          rich rendering, stage banners, findings list
│
├── collectors/
│   ├── repo_scanner.py     scope walk, ignore rules, secret and binary filtering
│   ├── file_reader.py      capped reads with explicit truncation markers
│   └── repo_observer.py    deterministic OBSERVE-stage evidence
│
├── agents/
│   ├── file_selector.py    manifest -> relevant files (with keyword fallback)
│   ├── analyst.py          code -> findings
│   ├── critic.py           findings -> refined findings
│   ├── decision_parser.py  "fix 1 and 2 please" -> [1, 2]
│   └── planner.py          accepted findings -> implementation plan
│
├── output/
│   ├── session_writer.py   Sessions/AgenticLoopSession{GUID}.md
│   └── plan_writer.py      Plans/AgenticLoopPlan{GUID}.md
│
└── prompts/                every word the models are told
```

---

## Why two stages of context

Repositories are far larger than a sensible prompt. Sending everything is slow,
expensive, and dilutes the model's attention.

1. **Manifest.** The scanner produces one line per file — path, size, line count.
   That is cheap even for thousands of files.
2. **Selection.** The manifest and your prompt go to the model, which returns the
   files worth reading in full, with a reason for each.
3. **Validation.** Every returned path is checked against the manifest. Paths that
   do not exist are dropped and recorded in the log as dropped. A hallucinated
   path can never cause a read.
4. **Reading.** The selected files are read under `MAX_FILE_BYTES` and
   `MAX_TOTAL_CONTEXT_BYTES`. Truncation is marked in the text itself, so the
   model knows it is seeing part of a file.

Shortcuts and safety nets:

- If the scope has fewer files than `MAX_FILES_IN_CONTEXT`, selection is skipped
  entirely and every file is included — no API call needed.
- If the selection call fails or returns nothing usable, a local keyword matcher
  takes over so the round still runs. The log records that the fallback was used.

---

## Why two agents

A single model asked to review code will pad its answer. The critic pass exists
to shorten the list, not lengthen it:

- **Implementation agent** (`GEMINI_MODEL`) reads the code and the observations and
  proposes findings, each with a problem, a specific fix, a severity, file paths
  and the evidence that supports it.
- **Review agent** (`GEMINI_REVIEW_MODEL`) sees the same code plus those findings and
  must drop anything unsupported, amend anything vague, and keep only what is
  actionable. It returns a verdict for every original finding.

Both the original findings and the critic's verdicts are recorded, so the log
shows what was filtered and why. Set `ENABLE_REVIEW_AGENT=false` to skip the pass
when speed matters more than precision.

---

## Structured output

Every model call returns JSON validated against a pydantic schema from
`core/models.py` — `FileSelection`, `FindingSet`, `CritiqueResult`,
`ImplementationPlan`, `Decision`. The schema is sent with the request:

```python
response_format={
    "type": "text",
    "mime_type": "application/json",
    "schema": inline_schema_refs(FindingSet.model_json_schema()),
}
```

Nothing is scraped out of prose, so a formatting wobble cannot corrupt a finding.
`inline_schema_refs` flattens pydantic's `$defs`/`$ref` into a self-contained
schema for portability, and pins `propertyOrdering` on every object.

Field order matters more than it looks. Gemini emits JSON keys in whatever order
it likes unless told otherwise, and if a long free-text field such as `summary`
comes first the model treats it as a scratchpad: it reasons there until the
output budget is gone, then closes the response with an empty `findings` array.
Pinning the order (arrays first, prose last) and capping the prose fields with
`maxLength` in the schema keeps the model's budget where it belongs. The caps are
schema-only hints via `json_schema_extra`, so an over-long reply still validates
locally rather than failing the round.

`MAX_OUTPUT_TOKENS` and `THINKING_LEVEL` are sent as `generation_config` on every
call. That budget covers the model's internal thinking as well as the reply, so
setting it too low produces truncated JSON; the client detects that case and says
so explicitly instead of reporting a schema mismatch.

---

## The evidence log

`Sessions/AgenticLoopSession{GUID}.md` is created when the session starts and
**flushed after every stage**, not at the end. If the process is interrupted, the
log still contains everything that happened up to that point.

Each round records:

- **1. PLAN** — your prompt, verbatim, in a fenced block, and the start time
- **2. ACT** — scope, scan counts, skip counts, the selected files with the model's
  reason for each, the selection rationale, dropped hallucinated paths, token usage
- **3. OBSERVE** — the full deterministic observation block
- **4. AGENT** — `4a` the raw proposals, `4b` the critic's verdict per finding,
  `4c` the numbered list exactly as you saw it, plus token usage for both calls
- **5. HUMAN REVIEW** — your reply verbatim, how it was interpreted, and a table
  marking every finding ACCEPTED or REJECTED
- **6. ADAPT** — the plan path, the plan item count, the round duration, and the
  adaptation note

A session footer records rounds completed, findings presented and accepted,
tokens used, and every plan created.

The writer, not the orchestrator, decides which stages made it into the log. If a
round is interrupted part way through a stage, that stage is still written as
`(not reached — <reason>)`, so a round's record can never be silently missing a
heading.

---

## Customising prompts

Every word sent to a model lives in `prompts/`:

```
prompts/
├── selection/   system.md, task.md          which files to read
├── analysis/    system.md, task.md, context.md   how to find problems
├── critique/    system.md, task.md          how to challenge findings
├── planning/    system.md, task.md          how to write the plan
└── decision/    system.md, task.md          how to read your reply
```

Edit the markdown, save, restart the tool. No Python involved.

Templates use `{{PLACEHOLDER}}` substitution and rendering is strict: a missing
template file or a placeholder the code does not supply raises an error rather
than sending a broken prompt. The placeholders available to each template are the
ones already used in it — the safest way to extend a prompt is to add prose around
the existing placeholders.

Ideas that need only a prompt edit:

- Add house rules ("we use MediatR, do not suggest a service locator")
- Ban a class of advice ("never recommend a unique constraint on `subject_code`")
- Change the severity thresholds
- Make the critic stricter or more permissive
- Ask the planner for smaller, more granular steps

Run `/config` in the REPL to see which templates were found on disk.

---

## Cost and tokens

A full round makes up to four calls:

| Call | When it happens | Model |
| --- | --- | --- |
| File selection | Only when the scope exceeds `MAX_FILES_IN_CONTEXT` | `GEMINI_SELECTION_MODEL` |
| Analysis | Always | `GEMINI_MODEL` |
| Critique | When `ENABLE_REVIEW_AGENT=true` and findings exist | `GEMINI_REVIEW_MODEL` |
| Planning | Only when you accept at least one finding | `GEMINI_MODEL` |

Interpreting your acceptance reply normally costs nothing — common phrasings are
parsed locally, and the model is consulted only for genuinely unusual wording.

Levers, cheapest first: narrow the scope, lower `MAX_FILES_IN_CONTEXT`, point
`GEMINI_SELECTION_MODEL` at a lighter model, set `ENABLE_REVIEW_AGENT=false`.

Token usage for every call is recorded in the evidence log and totalled in the
session footer.

---

## Safety properties

- **Read-only.** No code path opens a file in the reviewed tree for writing. The
  only writes go through `output/` into `SESSIONS_DIR` and `PLANS_DIR`.
- **Secrets never leave the machine.** Files matching `.env`, `*.env`, `.env.*`,
  `*.pem`, `*.key`, `*.pfx`, `*.p12`, `id_rsa*`, `id_dsa*`, `secrets.*`,
  `*.secrets`, `credentials*`, `*.keystore` and `*.jks` are excluded before any
  read, and the exclusion is reported in the OBSERVE stage.
- **Binaries excluded.** Files are sniffed for null bytes and undecodable content.
- **Scope containment.** `TARGETED_DIRECTORY` must resolve inside `REPO_ROOT`.
- **Key redaction.** The API key is masked in every log line, error and `/config` view.
- **Symlinks skipped.** The scanner does not follow them, so it cannot escape the scope.

**Privacy note:** the contents of selected files are sent to Google's Gemini API.
Only point the tool at code you are permitted to share.

---

## Failure behaviour

| Failure | Result |
| --- | --- |
| Empty scope | Round stops in ACT with an actionable message; remaining stages logged as not reached. |
| Selection call fails | Local keyword fallback; the round continues and the log says the fallback was used. |
| Analysis call fails | Round stops in AGENT; the failure is written to the log. |
| Critique call fails | The unreviewed findings are shown, with a warning, and the reason is logged. |
| Planning call fails | Round stops in ADAPT; the accepted findings are preserved in the log. |
| Ctrl+C mid-round | Remaining stages are marked interrupted; the log is complete up to that point; the REPL survives. |
| Input closed (Ctrl+Z, or piped stdin ending) at a question | Same as Ctrl+C: the round ends, every stage heading is still recorded. |
| Transient API error (429, 5xx, timeout) | Retried up to `MAX_RETRIES` with exponential backoff before surfacing. |

---

## Extending it

Every agent module has the same shape — *(settings, prompts, client) -> validated
pydantic result* — so adding a pass is mechanical:

1. Add a schema to `core/models.py`.
2. Add a prompt folder, for example `prompts/security/{system,task}.md`.
3. Add `agents/security.py` following the pattern in `agents/analyst.py`.
4. Call it from `Orchestrator._stage_agent` and append its output to the markdown
   that stage writes.

If the new pass is a stage in its own right, add it to `core/stages.py` — the
console banners and the evidence-log headings both pick it up automatically.
