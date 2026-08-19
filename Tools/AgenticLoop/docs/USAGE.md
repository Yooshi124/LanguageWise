# Usage

How to run the Agentic Loop, what every command does, and a complete worked
example from prompt to saved plan.

---

## Running it

All commands run from `LanguageWise\Tools\AgenticLoop`.

| Command | What it does |
| --- | --- |
| `python main.py` | Interactive session. Run as many review rounds as you like; all of them are recorded in one evidence log. |
| `python main.py --prompt "review test coverage"` | Runs a single round, writes the session log, exits. Useful for scripting. |
| `python main.py --scope ..\..\DatabaseService` | Overrides `TARGETED_DIRECTORY` for this run only. |
| `python main.py --env C:\path\to\other.env` | Uses an alternative configuration file. |
| `python main.py --help` | Shows the flags. |

---

## Slash commands

Inside the REPL, anything starting with `/` is a command; anything else is a
review prompt.

| Command | Purpose |
| --- | --- |
| `/help` | List the commands. |
| `/stages` | Show the six loop stages and what each one does. |
| `/status` | Current model, scope, and the path of this session's evidence log. |
| `/config` | Full configuration (API key redacted) plus the prompt templates found on disk. |
| `/scope <path>` | Narrow the review. Relative paths resolve from the repository root. |
| `/scope reset` | Return to the configured scope. |
| `/session` | Print the evidence log path. |
| `/exit` | End the session, write the log footer, and list any plans created. |

---

## Writing a good prompt

The loop is a rubber duck, not a search engine. Point it at one concern at a
time — a narrow prompt produces a short list of sharp findings, while a vague one
produces noise.

**Good**

```
review the database validation rules for the students table
is the error handling in the enrolment API consistent?
check test coverage for the user validation service
look for SQL injection risks in the data access layer
are the service boundaries between the frontend and enrolment services clean?
```

**Too vague**

```
review my code
make it better
find bugs
```

If you want several concerns covered, run several rounds. Each one is appended to
the same evidence log, so the session tells a coherent story.

---

## What happens in a round

Each round prints six banners in order and records the same six headings in the
evidence log:

```
STAGE 1/6 - PLAN            your prompt is captured verbatim
STAGE 2/6 - ACT             the scope is scanned; relevant files are selected and read
STAGE 3/6 - OBSERVE         deterministic facts about the code are gathered locally
STAGE 4/6 - AGENT           findings are proposed, then critiqued
STAGE 5/6 - HUMAN REVIEW    you accept or reject each suggestion
STAGE 6/6 - ADAPT           a plan is written for what you accepted
```

---

## Worked example

### Starting a session

```
> cd Tools\AgenticLoop
> python main.py

╭─ Agentic Loop - Rubber Duck Code Review ─────────────────────────────────╮
│ Model          gemini-3.7-flash                                          │
│ Review agent   on                                                        │
│ Repo root      C:\Users\justi\source\repos\LanguageWise                  │
│ Scope          C:\Users\justi\source\repos\LanguageWise  (whole repo)    │
│ Evidence log   ...\Sessions\AgenticLoopSession6f2a...c91b.md             │
│ Mode           read-only - no source file is ever modified               │
╰──────────────────────────────────────────────────────────────────────────╯
Loop stages: PLAN -> ACT -> OBSERVE -> AGENT -> HUMAN REVIEW -> ADAPT   (/stages for detail)
Type a review prompt, or /help for commands.

agentic-loop >
```

### Narrowing the scope, then asking

```
agentic-loop > /scope DatabaseService
Scope set to C:\Users\justi\source\repos\LanguageWise\DatabaseService

agentic-loop > review the database validation and its test coverage
```

### PLAN

```
──────────────────────────── STAGE 1/6 - PLAN ─────────────────────────────
The human states a targeted review goal.
Prompt: "review the database validation and its test coverage"
```

### ACT

```
───────────────────────────── STAGE 2/6 - ACT ─────────────────────────────
Scan the scope and gather the relevant source files.
Scope: C:\Users\justi\source\repos\LanguageWise\DatabaseService
Scanned 84 reviewable file(s).
Selected 6 file(s) for review.
  - DatabaseService/app.py
  - DatabaseService/init_db.py
  - DatabaseService/services/student_repository.py
  - DatabaseService/services/validation.py
  - DatabaseService/tests/test_repository.py
  - DatabaseService/requirements.txt
```

The model chose those six from a manifest of all 84 files. Any path it returns
that is not in the manifest is discarded before anything is read.

### OBSERVE

```
─────────────────────────── STAGE 3/6 - OBSERVE ───────────────────────────
Collect deterministic, verifiable evidence about the code.
Scope: C:\Users\justi\source\repos\LanguageWise\DatabaseService
Reviewable files: 84
Total lines: 4127
Total bytes: 118433
File types: .py x61, .sql x12, .md x6, .yml x5
Test files detected: 3
Test file examples: DatabaseService/tests/test_repository.py, ...
Git: branch=main commit=4c1e9a2
Warnings:
  - 1 secret-like file(s) skipped and never sent to the model: DatabaseService/.env
```

Nothing here comes from a model. These are facts both you and the agent can rely on.

### AGENT

```
──────────────────────────── STAGE 4/6 - AGENT ────────────────────────────
Implementation agent proposes findings; review agent critiques them.
Implementation agent reading 6 file(s) (41208 bytes) with gemini-3.7-flash...
Implementation agent proposed 4 finding(s).
Review agent critiquing with gemini-3.7-flash...

1. Problem: Student records are inserted without any field validation, so a null
   student_name or a non-integer student_id reaches the database. (high)
   Suggested fix: Add a DatabaseValidationService in
   DatabaseService/services/validation.py that rejects null or empty
   student_name, non-integer student_id, and blank subject_code, and call it
   from StudentRepository.insert before the INSERT statement.
   Files: DatabaseService/services/student_repository.py
   Evidence: insert() passes the request payload straight into the SQL parameters

2. Problem: The validation module has no tests, so none of its rules are
   exercised. (medium)
   Suggested fix: Add DatabaseService/tests/test_validation.py with
   test_validate_rejects_null_name, test_validate_rejects_non_integer_id and
   test_validate_accepts_valid_record.
   Files: DatabaseService/tests/test_repository.py
   Evidence: tests/ contains only test_repository.py, which covers reads

3. Problem: init_db.py creates the students table without a NOT NULL constraint
   on student_name. (medium)
   Suggested fix: Add NOT NULL to student_name in the CREATE TABLE statement in
   DatabaseService/init_db.py and document the migration.
   Files: DatabaseService/init_db.py
   Evidence: CREATE TABLE students (student_id INTEGER, student_name TEXT, ...)

Validation is applied inconsistently: reads are well covered, writes are not.
```

The implementation agent proposed four findings; the review agent dropped one it
could not support from the code. Both the original four and the critic's verdict
on each are recorded in the evidence log.

### HUMAN REVIEW

```
───────────────────────── STAGE 5/6 - HUMAN REVIEW ────────────────────────
The human accepts or rejects each suggestion.

Which suggestions would you like to accept? (e.g. 'fix 1 and 2 please', 'all', 'none'): fix 1 and 2 please
Accepted suggestion(s) 1, 2 of 3.
Rejected: 3
```

Replies are interpreted flexibly — `1 and 2`, `1,2`, `1-2`, `all`, `all except 3`,
`none`, `no thanks` all work. Common shapes are parsed locally with no API call.

### ADAPT

```
──────────────────────────── STAGE 6/6 - ADAPT ────────────────────────────
Record the decision and save an implementation plan.
Building an implementation plan for 2 accepted finding(s)...

╭─ ADAPT ──────────────────────────────────────────────────────────────────╮
│ Implementation plan saved to:                                            │
│ ...\Tools\AgenticLoop\Plans\AgenticLoopPlan8b41d0e7-...-2f5a.md          │
╰──────────────────────────────────────────────────────────────────────────╯
```

### Ending the session

```
agentic-loop > /exit

Evidence log: ...\Tools\AgenticLoop\Sessions\AgenticLoopSession6f2a...c91b.md
Plans created this session:
  ...\Tools\AgenticLoop\Plans\AgenticLoopPlan8b41d0e7-...-2f5a.md
```

---

## When nothing is found

A clean result is a valid result, and the loop no longer leaves you at a dead end:

```
──────────────────────────── STAGE 4/6 - AGENT ────────────────────────────
Implementation agent reading 6 file(s) (41208 bytes) with gemini-3.7-flash...
Implementation agent proposed 0 finding(s).

╭────────────────────────────────────────────────────────╮
│ No evidence-backed issues identified for this request. │
╰────────────────────────────────────────────────────────╯
The validation rules are consistently applied and covered by tests.

───────────────────────── STAGE 5/6 - HUMAN REVIEW ────────────────────────
Nothing to review - the agent raised no issues.

────────────────────────────── STAGE 6/6 - ADAPT ──────────────────────────
No findings were raised, so there was nothing to adapt.

Nothing to accept. Type another prompt to try again, Enter to carry on, or 'q' to quit:
```

The question comes after ADAPT, so all six stages always print in order.

Three ways forward:

| You type | What happens |
| --- | --- |
| *(nothing, just Enter)* | The round closes normally and you return to `agentic-loop >`. |
| A new prompt | That prompt runs immediately as the next round — useful when the first was too vague. |
| `q` | The session ends, the footer is written, and the evidence log path is printed. |

Your choice is recorded in the log as a **Human next step** note at the end of the
round, so the evidence shows the review came back clean and what you decided to
do about it.

### If findings come back empty when you expected some

The model has a fixed output budget that also covers its internal thinking. If it
spends that budget before writing the findings array you get an empty result, or a
`ran out of output budget` error. Raise `MAX_OUTPUT_TOKENS`, lower
`THINKING_LEVEL`, or narrow the scope so there is less code to reason about.

---

## What the plan file looks like

`Plans/AgenticLoopPlan{GUID}.md` (excerpt):

```markdown
# Validation Hardening Plan

- **Plan ID:** 8b41d0e7-...-2f5a
- **Session:** 6f2a...c91b (round 1)
- **Created:** 2026-08-19 08:41:12 UTC
- **Model:** gemini-3.7-flash
- **Review scope:** `C:\...\LanguageWise\DatabaseService`

## Original Request

> review the database validation and its test coverage

## Summary

Write-path validation is missing and untested. Add a validation service, call it
from the repository, and cover its rules with unit tests.

## Accepted Findings

1. **Missing write-path validation** (high) — Student records are inserted without ...
2. **No tests for validation rules** (medium) — The validation module has no tests ...

## Implementation Items

### 1. Missing write-path validation

**Problem:** Student records are inserted without any field validation.

**Goal:** No invalid student record can reach the database.

**Steps**

1. Create `DatabaseValidationService` — a class with a `validate(record)` method
   returning `(ok, message)` (files: `DatabaseService/services/validation.py`)
2. Enforce the three rules — reject null/empty `student_name`, non-integer
   `student_id`, blank `subject_code`. Do not add a uniqueness rule to
   `subject_code`; multiple students share a subject.
3. Call the validator from `StudentRepository.insert` before the INSERT and
   raise `ValidationError` on failure.

**Files to change**

- `DatabaseService/services/validation.py`
- `DatabaseService/services/student_repository.py`

**Tests to add or update**

- test_validate_rejects_null_name
- test_validate_rejects_non_integer_id

**Risks and trade-offs**

- Existing callers that relied on lenient inserts will now receive an error.

**Acceptance criteria**

- Inserting a record with a null student_name raises ValidationError.
- All existing repository tests still pass.

## Open Questions

- Should an empty subject_code be rejected, or defaulted?

---

_Generated by the Agentic Loop rubber duck. This tool is read-only: no source
file was modified. Implement the steps above yourself, or hand this plan to a
coding agent._
```

---

## What the evidence log looks like

`Sessions/AgenticLoopSession{GUID}.md` (excerpt). Every round contains all six
stage headings in order, so the loop is auditable end to end:

````markdown
# Agentic Loop Session 6f2a...c91b

- **Started:** 2026-08-19 08:37:02 UTC
- **Repo root:** C:\Users\justi\source\repos\LanguageWise
- **Scope:** C:\...\LanguageWise\DatabaseService
- **Scope mode:** TARGETED_DIRECTORY
- **Analysis model:** gemini-3.7-flash
- **Selection model:** gemini-3.7-flash
- **Review model:** gemini-3.7-flash

This is an evidence log. Each round below records the full loop: PLAN → ACT → OBSERVE → AGENT → HUMAN REVIEW → ADAPT.

---

## Round 1

### 1. PLAN

_The human states a targeted review goal._

**Original prompt (verbatim):**

```text
review the database validation and its test coverage
```

- Round started: 2026-08-19 08:37:44 UTC

### 2. ACT

_Scan the scope and gather the relevant source files._

- Scope: `C:\...\LanguageWise\DatabaseService`
- Files scanned: 84 (118433 bytes, 4127 lines)
- Secret-like files skipped: 1
- Selection method: model

**Files selected for review**

- DatabaseService/services/student_repository.py — Performs the inserts under review.
- DatabaseService/services/validation.py — Holds the validation rules.
...

### 3. OBSERVE

_Collect deterministic, verifiable evidence about the code._
...

### 4. AGENT

_Implementation agent proposes findings; review agent critiques them._

#### 4a. Implementation Agent
1. **Missing write-path validation** (high) ...
4. **Unused import in app.py** (low) ...

#### 4b. Review Agent
- **Missing write-path validation** → `kept` — Directly visible in insert().
- **Unused import in app.py** → `dropped` — Not relevant to the review request.

#### 4c. Findings Presented to the Human
1. **Problem:** Student records are inserted without any field validation ...

### 5. HUMAN REVIEW

_The human accepts or rejects each suggestion._

**User response (verbatim):**

```text
fix 1 and 2 please
```

| # | Finding | Decision |
| --- | --- | --- |
| 1 | Missing write-path validation | **ACCEPTED** |
| 2 | No tests for validation rules | **ACCEPTED** |
| 3 | Missing NOT NULL constraint | REJECTED |

### 6. ADAPT

_Record the decision and save an implementation plan._

- Accepted findings: 2 of 3
- Implementation plan saved to: `...\Plans\AgenticLoopPlan8b41d0e7-...-2f5a.md`
- Round duration: 46.2s
````

---

## Tips

- **Nothing found is a real result.** If the agent reports no evidence-backed
  issues, it asks what you want to do next: press Enter to carry on, type a
  different prompt to try again immediately, or `q` to end the session. The empty
  result and your choice are both recorded.
- **Run a round after implementing a plan.** The loop is a cycle: adapt, then
  re-observe. The second session log becomes your proof the issue was fixed.
- **Reject freely.** Rejections are recorded too, so the log shows why something
  was deliberately not done.
- **Narrow the scope before asking about one service.** It is faster, cheaper and
  produces more precise findings than reviewing the whole repository.
- **The console shows a summary; the log has everything.** Long file lists are
  truncated on screen with `... and N more`; the complete list is always in the
  evidence log.
- **HTTP request lines are hidden.** Set `LOG_LEVEL=DEBUG` in `.env` if you need
  them while diagnosing a connection problem.
