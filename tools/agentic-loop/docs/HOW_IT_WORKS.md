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
| Ollama with two local models | OpenRouter chat completions, model set in `.env` |
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
| **4. AGENT** | `agents/analyst.py`, `agents/critic.py` | The implementation agent (via OpenRouter) proposes findings; the review agent (local Gemma via Ollama, mandatory) challenges them. Only survivors are shown. |
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
│   ├── models.py           pydantic schemas, doubling as the model's JSON contracts
│   ├── openrouter_client.py OpenRouter wrapper: structured output, retries, usage
│   ├── ollama_client.py    local Ollama wrapper for the mandatory Review Agent
│   ├── prompt_registry.py  loads prompts/*.md, strict {{PLACEHOLDER}} rendering
│   ├── session.py          session and round identity, timing, running totals
│   └── console.py          rich rendering, stage banners, findings list
│
├── collectors/
│   ├── repo_scanner.py     scope walk, ignore rules, secret and binary filtering
│   ├── file_reader.py      capped reads with explicit truncation markers
│   ├── repo_observer.py    deterministic OBSERVE-stage evidence
│   ├── github_actions.py   failed CI runs and job-log excerpts via the GitHub API
│   ├── rag_observer.py     retrieved documentation passages via the local RAG server
│   └── mcp_observer.py     live initialize/tools/list/tools/call evidence via the local MCP server
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
to shorten the list, not lengthen it — and it runs on a genuinely different,
locally-hosted model so it cannot simply agree with itself:

- **Implementation agent** (`OPENROUTER_MODEL`, via OpenRouter) reads the code and the
  observations and proposes findings, each with a problem, a specific fix, a
  severity, file paths and the evidence that supports it.
- **Review agent** (`OLLAMA_REVIEW_MODEL`, a local Gemma model served by Ollama)
  sees the same code plus those findings and critiques them with a keep-first
  bias: keep the sound, amend the vague, and drop only what the supplied code
  actively contradicts. It returns a verdict for every original finding.

The critic's bias is deliberate. Small local models over-fire on scepticism, so
the prompts make KEEP the default verdict, require every drop to cite the
contradicting code, and treat "the code does not prove this" as insufficient
grounds for dropping. As a backstop, a critique that drops every finding from a
non-empty list triggers one mandatory re-check pass with the keep-first bias
restated before the empty list is believed.

Both the original findings and the critic's verdicts are recorded, so the log
shows what was filtered and why. **The review pass is mandatory and cannot be
disabled.** If the local model is unreachable, not pulled, or returns
unusable output, the round aborts with a clear error rather than silently
showing the human unreviewed findings — the whole point is that nothing
reaches a human without a second, independent model having checked it first.

---

## Structured output

Every model call returns JSON validated against a pydantic schema from
`core/models.py` — `FileSelection`, `FindingSet`, `CritiqueResult`,
`ImplementationPlan`, `Decision`. OpenRouter calls (`core/openrouter_client.py`)
use the OpenAI-compatible chat-completions endpoint — the same pattern as the
mini-games service's `OpenRouterVocabularyClient` — and the prompts themselves
instruct the model to answer with only the JSON object:

```python
body = {"model": model, "messages": messages, "stream": False, "max_tokens": ...}
# POST {OPENROUTER_BASE_URL}/chat/completions, Authorization: Bearer <key>
```

If the reply is not valid JSON for the schema, the model gets one repair round
("That was not valid JSON. Reply with only the JSON object...") before the call
is declared failed — again mirroring the mini-games vocabulary provider.

The Review Agent (`core/ollama_client.py`) enforces the identical schema through
Ollama's own structured-output support instead — a `format` field on the
`/api/chat` request carrying the same `inline_schema_refs`-flattened JSON
schema. Both clients hand their raw JSON reply to the same pydantic model for
validation, so `agents/critic.py` cannot tell which backend produced it.

Nothing is scraped out of prose, so a formatting wobble cannot corrupt a finding.
`inline_schema_refs` flattens pydantic's `$defs`/`$ref` into a self-contained
schema for portability, and pins `propertyOrdering` on every object.

Field order matters more than it looks. Models emit JSON keys in whatever order
they like unless told otherwise, and if a long free-text field such as `summary`
comes first the model treats it as a scratchpad: it reasons there until the
output budget is gone, then closes the response with an empty `findings` array.
The Ollama schema pins the order (arrays first, prose last) and the prose fields
are capped with `maxLength` in the schema to keep the model's budget where it
belongs. The caps are
schema-only hints via `json_schema_extra`, so an over-long reply still validates
locally rather than failing the round.

`MAX_OUTPUT_TOKENS` is sent as `max_tokens` on every OpenRouter call (selection,
analysis, planning), and `THINKING_LEVEL` maps to OpenRouter's unified
`reasoning.effort` parameter. That budget covers the model's
internal thinking as well as the reply, so setting it too low produces truncated
JSON; the client detects that case and says so explicitly instead of reporting a
schema mismatch. The local Ollama review agent has its own timeout
(`OLLAMA_REQUEST_TIMEOUT_SECONDS`) instead, since it runs on your hardware rather
than a metered API.

---

## CI-failures mode

`python main.py --ci-failures [workflow]` (or `/ci-failures` in the REPL) runs a
normal round whose evidence comes from GitHub Actions instead of a hand-written
prompt:

1. `collectors/github_actions.py` resolves the repository (`GITHUB_REPO`, else
   the `origin` remote), finds the most recent failed workflow run (optionally
   filtered to one workflow), and lists its failed jobs.
2. For each failed job (up to five) it downloads the job log and extracts the
   failure lines — xUnit `[FAIL]` blocks, compiler errors (`error CS...`),
   vitest failures, exceptions — with a little surrounding context.
3. The resulting report is injected into the OBSERVE stage as an extra section,
   so both agents see it next to the deterministic observations, and the round's
   prompt asks for a root-cause diagnosis with concrete fixes.

Everything else is identical to a normal round: the same six stages, the same
evidence log, the same human review. `GITHUB_TOKEN` is sent only to
`api.github.com` and is redacted from all output like the OpenRouter key.

---

## RAG and MCP validation modes

`--rag-validate [topic]` and `--mcp-validate [tool]` (plus the `/rag-validate`,
`/rag`, `/mcp-validate` and `/mcp` REPL commands) follow the same pattern as
CI-failures mode: a collector gathers live evidence from a local,
non-containerised server, the report is injected into the OBSERVE stage as an
extra section, and a normal round reviews the code in scope against it.

- **RAG** (`core/rag_client.py`, `collectors/rag_observer.py`): queries the RAG
  server's plain `POST /query` REST endpoint (`rag-server/`, default
  `http://localhost:8100`) for passages about a topic; the round flags code that
  contradicts or drifts from the documented behaviour.
- **MCP** (`core/mcp_client.py`, `collectors/mcp_observer.py`): speaks JSON-RPC
  2.0 over streamable HTTP to the shared MCP server (`mcp-server/`, default
  `http://localhost:8200/mcp`) with the same `X-LanguageWise-Mcp-Key` /
  `-Tool-Scope` / `-User-Token` headers the feature backends send. It runs
  `initialize`, `tools/list` for the configured scope, and one real `tools/call`
  probe (a named tool, or the first one needing no arguments); the round
  validates the server code against the observed contract — scope filtering,
  read-only results, safe `isError` payloads.

Both servers are optional and independent: the modes fail fast with a clear
error when the server is not running, and neither server is ever started,
stopped or containerised by the loop. The MCP API key and user token are
registered for redaction exactly like the OpenRouter key.

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

A full round makes up to four model calls — three billed against your OpenRouter
credits, one free and local:

| Call | When it happens | Model |
| --- | --- | --- |
| File selection | Only when the scope exceeds `MAX_FILES_IN_CONTEXT` | `OPENROUTER_SELECTION_MODEL` (OpenRouter, metered) |
| Analysis | Always | `OPENROUTER_MODEL` (OpenRouter, metered) |
| Critique | Always, when findings exist — mandatory, cannot be disabled | `OLLAMA_REVIEW_MODEL` (local via Ollama, free) |
| Planning | Only when you accept at least one finding | `OPENROUTER_MODEL` (OpenRouter, metered) |

Interpreting your acceptance reply normally costs nothing — common phrasings are
parsed locally, and the model is consulted only for genuinely unusual wording.

Levers, cheapest first: narrow the scope, lower `MAX_FILES_IN_CONTEXT`, point
`OPENROUTER_SELECTION_MODEL` at a lighter model. The critique pass is local and free,
so there is no reason to skip it — and no setting to do so.

Token usage for every call is recorded in the evidence log and totalled in the
session footer. The Review Agent's usage (`prompt_eval_count`/`eval_count` from
Ollama) is recorded the same way, even though it costs nothing.

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

**Privacy note:** the contents of selected files are sent to OpenRouter (which
routes them to the provider hosting the configured model) for selection, analysis
and planning. Only point the tool at code you are
permitted to share. The Review Agent's pass (source code + findings) stays on
your machine via the local Ollama daemon.

---

## Failure behaviour

| Failure | Result |
| --- | --- |
| Empty scope | Round stops in ACT with an actionable message; remaining stages logged as not reached. |
| Selection call fails | Local keyword fallback; the round continues and the log says the fallback was used. |
| Analysis call fails | Round stops in AGENT; the failure is written to the log. |
| Critique call fails | Round stops in AGENT; the failure is written to the log. The Review Agent is mandatory, so findings are never shown unreviewed. |
| Planning call fails | Round stops in ADAPT; the accepted findings are preserved in the log. |
| Ctrl+C mid-round | Remaining stages are marked interrupted; the log is complete up to that point; the REPL survives. |
| Input closed (Ctrl+Z, or piped stdin ending) at a question | Same as Ctrl+C: the round ends, every stage heading is still recorded. |
| Transient OpenRouter error (429, 5xx, timeout) | 429s honour the `Retry-After` header (capped at 8s); other transient errors are retried up to `MAX_RETRIES` with exponential backoff before surfacing. Ollama calls (the Review Agent) are not retried — a connection failure there is reported immediately with setup instructions instead. |

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
