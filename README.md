# LanguageWise

A language learning platform built as independently deployed microservices and
one federated Vue 3 browser application, orchestrated with Docker Compose.

The repository also hosts a shared development tool: an **agentic loop** that acts
as a rubber duck reviewer over the whole codebase.

## Frontend architecture

Open the application at [http://localhost:3000](http://localhost:3000). The
`shared-frontend` Vue SPA owns navigation, authentication, routing, Vuetify,
TanStack Query, and all authored CSS. It lazy-loads five independently built
feature remotes through the same-origin gateway:

- `/remotes/quizzes-courses/remoteEntry.js`
- `/remotes/mini-games/remoteEntry.js`
- `/remotes/chat-discussion/remoteEntry.js`
- `/remotes/quests-achievements/remoteEntry.js`
- `/remotes/leaderboard-analytics/remoteEntry.js`

Feature API calls remain under their owning path, such as
`/mini-games/api/*` and `/analytics/api/*`. A failed remote displays an
isolated Retry view without preventing the host or other features from loading.

Start the complete system from the repository root:

```powershell
docker compose up -d --build
```

On machines using Podman, run `podman compose up -d --build` instead.

MCP and RAG run inside the Compose network; no host-side server processes are
required. Containers use `mcp-server:8080` and `rag-server:8100`. Host debugging
remains available on loopback-only ports `8200` and `8100`. RAG rebuilds its search
index at each start and caches its CPU embedding model in a named volume; the
first start downloads that model. Corpus edits require rebuilding the RAG image.
See `mcp-server\README.md` and `rag-server\README.md` for setup details.

Real cross-service MCP/RAG tests run in the `ai-e2e` GitHub Actions job without
GPUs or paid APIs. To run the same isolated suite locally:

```powershell
pwsh tests\e2e\Invoke-E2E.ps1
```

See `tests\e2e\README.md` for coverage and diagnostics.

## Local PostgreSQL

All five data owners use PostgreSQL 18 on one shared-owned server. The
`shared-db` container hosts the following separate databases and non-superuser
logins; its bootstrap lives under `shared/database/postgres`. Quests retains
ownership of its schema and seed files, supplied as a named image-build context.

| Owner | Database | Login |
| --- | --- | --- |
| Shared | `shared` | `shared_app` |
| Mini-games | `mini_games` | `mini_games_app` |
| Quizzes/courses | `quizzes_courses` | `quizzes_courses_app` |
| Chat/discussion | `chat_discussion` | `chat_discussion_app` |
| Quests | `quests_achievements_notifications` | `quests_app` |

The four C# database APIs and quests PostgREST retain their existing HTTP
contracts. `shared-db-api` serves the shared HTTP API on port 6000, while
`shared-db` serves PostgreSQL on port 5432. Database connections replace the old
SQLite `Database:Path` setting
with `Database:ConnectionString` (`Database__ConnectionString` in environment
variables). PostgreSQL is published only on `127.0.0.1:5432`; the checked-in
password `languagewise` and bootstrap administrator are for local demo use only.
Use new credentials and private connectivity in any deployed environment.

Start just the database tier without starting AI or frontend services:

```powershell
docker compose up -d --build --wait shared-db-api mini-games-service-db quizzes-courses-service-db chat-discussion-service-db quests-achievements-notifications-service-db
```

On an empty `postgres-data` volume, PostgreSQL creates the databases/roles and
quests schema/seeds. The C# APIs apply their schemas/seeds on startup without
resetting existing user changes. Mini-games has no static seed: games are created
through its API. PostgreSQL data and uploaded shared/chat images use separate
named volumes and survive application container replacement.

This is a fresh, seeded demo migration, not an import. Old SQLite volumes and
their uploaded images are not copied into the new storage. To deliberately
erase **all Compose data, including uploads**, and reseed from scratch:

```powershell
docker compose down --volumes
docker compose up -d --build
```

### PostgreSQL Tests

The migrated database tests require real PostgreSQL and create a unique schema
per fixture. Run an isolated test server so tests never touch application data:

```powershell
docker run --detach --rm --name languagewise-postgres-tests --publish 127.0.0.1:15432:5432 --env POSTGRES_USER=languagewise --env POSTGRES_PASSWORD=languagewise --env POSTGRES_DB=postgres postgres:18
docker exec languagewise-postgres-tests pg_isready -U languagewise -d postgres
dotnet test shared/LanguageWise.Shared.BE.slnx
dotnet test mini-games-service/LanguageWise.MiniGamesService.BE.slnx
dotnet test quizzes-courses-service/LanguageWise.QuizzesCoursesService.BE.slnx
dotnet test chat-discussion-service/LanguageWise.ChatDiscussionService.BE.slnx
dotnet test quests-achievements-notifications-service/LanguageWise.QuestsAchievementsNotificationsService.BE.slnx
docker stop languagewise-postgres-tests
```

Wait for `pg_isready` to report readiness before running tests. To use another
test server, set `LANGUAGEWISE_TEST_POSTGRES` to its Npgsql connection string; its
login must be able to create and drop schemas in a disposable test database.
CI starts PostgreSQL 18 automatically for each migrated service's test job.

---

## Features

Each area is owned by one team member and will live in its own microservice.

### Student 1: Mini Games / Activities — *Kyan*

Small games for practising vocabulary.

- **Guess the Word** — find the hidden five-letter word in six guesses (Wordle-style),
  with special-letter buttons for non-ASCII letters like ß
- **Word Search** — trace a chain of connected letters to find themed words hidden in the grid
- **Associations** — group sixteen words into four categories of four
- **Two vocabulary modes:** *Content Focus* uses words from the lessons you have completed in
  your courses; *AI Generation* creates fresh beginner-level word lists (with themes and
  definitions) on demand, so the service is fully playable standalone
- **AI integration:** generates the word list, themes, and per-word definitions to play with
  (OpenRouter); definitions are revealed in a popup once a round ends

### Student 2: Discussion / Chat Forum — *Lachlan*

Where students talk to each other about their progress.

- Students make posts
- Like, reply and comment
- Images (nice to have)
- **AI integration:** helps you write posts, summarises threads (RAG)

### Student 3: Quizzes and Courses — *Justin*

- Prepared, static quizzes — students answer questions and complete exercises
- Prepared, static course content
- Question-and-answer interactions, e.g. clicking words in order to build a sentence
- **AI integration:** generate your own questions and flashcards (RAG), mark quizzes (RAG)

### Student 4: Achievements / Notifications — *Amber*

- Event-driven push notifications and emails, e.g. complete 5 courses, earn a silver medal
- Sends emails
- Achievements page showing past achievements (possibly interoperating with the forum)
- Generates completion certificates
- **AI integration:** retrieve achievements and generate a certificate to email (RAG)

### Student 5: Leaderboard / Analytics — *Roan*

- Global analytics comparing you against other students
- Customisable visualisations
- Your rank in each course and language
- Who contributes most on the discussion forum (nice to have)
- **AI integration:** ask an assistant about the analytics (RAG), e.g. *"Who's ranking first in Italian?"*

---

## Commit convention

This project uses **[Conventional Commits](https://www.conventionalcommits.org/)**,
following [qoomon's cheatsheet](https://gist.github.com/qoomon/5dfcdf8eec66a051ecd85625518cfd13)
as the house style. Please read it before your first commit.

### Types

| Type | Use it for |
| --- | --- |
| `feat` | Adding, adjusting or removing a feature of the API or UI |
| `fix` | Fixing an API or UI bug in a previous `feat` |
| `refactor` | Restructuring code without changing API or UI behaviour |
| `perf` | A `refactor` that specifically improves performance |
| `style` | Formatting, whitespace, semicolons — no behaviour change |
| `test` | Adding missing tests or correcting existing ones |
| `docs` | Documentation only |
| `build` | Build tooling, dependencies, project version |
| `ops` | Infrastructure, deployment scripts, CI/CD, monitoring, backups |
| `chore` | Everything else, e.g. initial commit, `.gitignore` changes |

### Examples

```text
feat(forum): add image uploads to posts
feat(quizzes): generate flashcards from course content
fix(analytics): correct rank calculation for tied scores
refactor(games): extract the word-matching scorer
docs: add root readme
ops: add docker compose for local development
chore: init
```

---

## Contributing

Please don't commit API keys and `.env` files...thx 🙂

## Pre-commit security checks

Every commit runs these scans; the `pre-commit-security` workflow re-runs them in CI.

| Hook | Checks |
| --- | --- |
| `ruff-check` | Python security rules (`S`, Bandit port) in `rag-server/` and `tools/agentic-loop/` |
| `detect-secrets` | Every tracked file against the reviewed `.secrets.baseline` |
| `pip-audit` | Known CVEs in the Python `requirements.txt` files |
| `npm-audit` | High/critical CVEs in each frontend's production dependencies |
| `dotnet-vulnerable` | High/critical CVEs in each `.slnx`, including transitive packages |

Requires Python 3.10+, Node, and the .NET 10 SDK on `PATH`. Set up once per clone:

```bash
pipx install pre-commit   # or: pip install pre-commit
pre-commit install
pre-commit run --all-files
```

- A flagged false positive in `detect-secrets`: run `detect-secrets scan --baseline .secrets.baseline`, then `detect-secrets audit .secrets.baseline` and mark it, and commit the baseline.
- A deliberate Ruff finding: add `# noqa: S###` with a short reason.
- Skip one hook in an emergency only: `SKIP=npm-audit git commit ...` — CI still runs it.

## Notification email configuration

The quests, achievements, and notifications service uses OpenRouter to compose
notification emails. Set `OPENROUTER_API_KEY` in the root `.env` file before
starting the stack.

Create `quests-achievements-notifications-service/backend/.env` with these
values to enable Gmail SMTP for that backend only:

```text
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=your-google-account@example.com
Smtp__Password=your-google-app-password
Smtp__FromName=LanguageWise
```

Use a Google app password rather than the account password. When these values
are absent, events and achievement progress still work but email is skipped.
The authenticated SMTP username is always used as the sender address.

## Garry assistant configuration

Garry runs in a private container shared by all five assistant endpoints. Each
feature backend verifies the signed-in user and supplies its authorized domain
rules and current context; Garry adds the same personality everywhere and streams
the reply. Garry uses OpenRouter as its only model provider; if OpenRouter is
unavailable, it does not try another model provider. The feature backends do not choose a provider. The
browser keeps its existing assistant URLs and stores bounded transcripts in
`sessionStorage`; chats are not written to the database.

Every valid completion runs a **planner -> worker -> reviewer** pipeline, including
requests without tools and requests made while MCP is unavailable. Each stage is
a fresh agent instance with its own role-specific system prompt and private
message context. All three receive the authorized domain rules, canonical context,
conversation history and current message. The worker receives the planner's task
plan and retains Garry's scoped MCP tool access; the reviewer receives the plan,
draft and bounded tool evidence to independently check and correct the answer.
Only explicit handoffs cross agent contexts, and no agent state is reused between
requests or users.

Only the reviewed answer is sent to the learner. The SSE contract remains
`tool`, `delta`, `done`, and `error`; tool events can arrive during worker execution,
but answer text is buffered until review completes. Documentation sources and
confidence are appended by the server after review. This adds at least three
model calls and additional response latency per message. All agents use the
configured OpenRouter model and output-token limit (tool rounds continue to use
`OpenRouter:ToolModel` when configured). Cancellation stops the pipeline; an agent
failure returns an unavailable/error response rather than exposing an unreviewed
draft. If tool execution fails, the worker can continue without tools and its
answer still goes through the reviewer.

Copy the root example environment file and add an OpenRouter API key:

```powershell
Copy-Item .env.example .env
```

```text
OPENROUTER_API_KEY=your-openrouter-api-key
```

Docker Compose passes this ignored file's key to Garry, analytics, and
notification email generation. The default OpenRouter model is `google/gemma-4-26b-a4b-it`; set
`OPENROUTER_MODEL`, `OPENROUTER_BASE_URL`, or `OPENROUTER_MAX_OUTPUT_TOKENS`
in the root `.env` to override it. Without a key, Garry has no model provider.
If OpenRouter cannot start, the discussion assistant uses its
help articles; other assistants report temporary unavailability. Garry applies one
10-request-per-minute limit per user across all five assistants. This limit is
in memory, so Garry must remain a single replica unless a distributed limiter
is introduced. Garry has no public port: the private Compose network is the
trust boundary for service-supplied rules, while the forwarded user JWT verifies
the learner's identity.
