# LanguageWise

A language learning platform built as a set of independent microservices, woven
together with Docker Compose.

Six microservices — one per team member plus a team-owned `shared` service — each with
its own frontend, backend/API and database service container.

---

## Quick start

### Prerequisites

| Tool | Version | Needed for |
| --- | --- | --- |
| [Docker Desktop](https://docs.docker.com/get-started/) | latest | Running the integrated application |
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** | Building and testing the backend and database services |
| [Git](https://git-scm.com/doc) | latest | Source control |
| [VS Code](https://code.visualstudio.com/docs) | latest | The IDE for this unit |

You do **not** need Node installed. The one microservice that uses a JavaScript build
does it inside its Dockerfile.

### Run everything

```bash
docker compose up --build
```

Then open **<http://localhost:3000>** — the unified home page, with a tab for each
feature.

Convenience wrappers are in `scripts/`:

```powershell
.\scripts\up.ps1 -Detach   # build and start, then print every URL
.\scripts\down.ps1         # stop (databases are kept)
.\scripts\down.ps1 -Clean  # stop and wipe the databases
```

```bash
./scripts/up.sh --detach
./scripts/down.sh
./scripts/down.sh --clean
```

> **Already run Ollama natively?** It owns port 11434 and the container will not start.
> Set `OLLAMA_PORT=11435` (the `up` scripts do this for you automatically).

### Verify it all works

With the stack running, this probes every tier of every microservice — health
endpoints, seeded data, the nginx proxy chain, full CRUD, and database isolation:

```bash
python scripts/verify.py
```

It prints a pass/fail line per check and exits non-zero if anything is broken, so it
also works as a smoke test after a merge.

### Build and test without Docker

Each microservice has its own solution, so you only ever have to build your own:

```powershell
.\scripts\build.ps1                                    # all six
.\scripts\test.ps1 -Service quizzes-courses-service     # just yours
```

```bash
./scripts/build.sh
./scripts/test.sh quizzes-courses-service
```

Or go straight at a solution:

```bash
dotnet build quizzes-courses-service/LanguageWise.QuizzesCoursesService.BE.slnx
dotnet test  quizzes-courses-service/LanguageWise.QuizzesCoursesService.BE.slnx
```

---

## Ports

| Microservice | Owner | Feature | Frontend | Backend | Database service |
| --- | --- | --- | --- | --- | --- |
| `shared` | Team | Unified home page | **[3000](http://localhost:3000)** | 5000 | 6000 |
| `mini-games-service` | Kyan | Mini Games / Activities | **[3001](http://localhost:3001)** | 5001 | 6001 |
| `chat-discussion-service` | Lachlan | Discussion / Chat Forum | **[3002](http://localhost:3002)** | 5002 | 6002 |
| `quizzes-courses-service` | Justin | Quizzes and Courses | **[3003](http://localhost:3003)** | 5003 | 6003 |
| `quests-achievements-notifications-service` | Amber | Quests / Achievements / Notifications | **[3004](http://localhost:3004)** | 5004 | 6004 |
| `leaderboard-analytics-service` | Roan | Leaderboard / Analytics | **[3005](http://localhost:3005)** | 5005 | 6005 |
| `ollama` | Team | LLM runtime | — | 11434 | — |

Inside the Docker network every .NET container listens on `8080` and every nginx
container on `80`. See [`docs/architecture.md`](docs/architecture.md) for the full
request flow and API contracts.

---

## Repository structure

Folders are named after the **feature** they deliver, not the person who owns them.

```
.github/workflows/     One workflow per microservice
ai-services/           Ollama / AI-Mode (Release 0 runtime is in docker-compose.yml)
docs/                  Architecture diagrams and documentation
scripts/               build / test / up / down / verify helpers
shared/                Team-owned microservice — the unified home page
mini-games-service/                         Kyan
chat-discussion-service/                    Lachlan
quizzes-courses-service/                    Justin
quests-achievements-notifications-service/  Amber
leaderboard-analytics-service/              Roan
tools/agentic-loop/    Shared rubber-duck reviewer for the whole codebase
docker-compose.yml     The one shared configuration that runs everything
```

Every microservice has the same shape:

```
<service>/
  LanguageWise.<Name>.BE.slnx   Solution for this microservice only
  frontend/    Dockerfile, nginx.conf and the static site or JS app
  backend/     LanguageWise.<Name>.Api        (ASP.NET Core minimal API)
  database/    LanguageWise.<Name>.Db         (ASP.NET Core minimal API + SQLite)
  tests/       LanguageWise.<Name>.Api.Tests  (NUnit)
```

### Microservices are genuinely independent

This is the rule the layout exists to enforce. **Nothing outside your service folder
affects how your service builds, tests or runs.**

- Your `.slnx` contains only your projects, and your `.csproj` files pin their own
  package versions. There is no root `Directory.Build.props` or combined `.sln`.
- Each Docker build context is a single tier folder, so your image physically cannot
  read another microservice's files.
- You have your own CI workflow. A red build points at exactly one service.
- You have your own SQLite database. No volume is shared.
- You choose your own frontend stack (see below).

The only things you share with everyone else are your port numbers, the shape of the
HTTP contracts, and `docker-compose.yml`. When services eventually need each other's
data they will talk **HTTP to the other service's backend** — never a project
reference, never another service's database.

### Tech stack

- **Frontend** — your choice, served by nginx on port 80. Five services use static HTML,
  CSS and [HTMX](https://htmx.org/docs/); `mini-games-service` uses Vue 3 + Vite. There
  is no server-side code in this tier either way.
- **Backend / API** — ASP.NET Core minimal APIs on .NET 10.
- **Database service** — ASP.NET Core minimal APIs on .NET 10 using
  `Microsoft.Data.Sqlite`. This is the only tier that opens a SQLite file.
- **Database** — SQLite, one file per microservice on its own named Docker volume.
- **Testing** — NUnit.
- **AI** — Ollama with an approved open-source LLM (Llama, Qwen or DeepSeek).

---

## Features

Each area is owned by one team member and lives in its own microservice.

### `mini-games-service` — Mini Games / Activities — *Kyan*

Small games for learning vocabulary.

- Crosswords, word jumbles, matching a word to an image (in the spirit of babadum.com)
- A "Squid Game glass bridge", but with words
- **AI integration:** mark the activity (RAG), generate or retrieve the word list to use (RAG)

### `chat-discussion-service` — Discussion / Chat Forum — *Lachlan*

Where students talk to each other about their progress.

- Students make posts
- Like, reply and comment
- Images (nice to have)
- **AI integration:** helps you write posts, summarises threads (RAG)

### `quizzes-courses-service` — Quizzes and Courses — *Justin*

- Prepared, static quizzes — students answer questions and complete exercises
- Prepared, static course content
- Question-and-answer interactions, e.g. clicking words in order to build a sentence
- **AI integration:** generate your own questions and flashcards (RAG), mark quizzes (RAG)

### `quests-achievements-notifications-service` — Quests / Achievements / Notifications — *Amber*

- Event-driven push notifications and emails, e.g. complete 5 courses, earn a silver medal
- Sends emails
- Achievements page showing past achievements (possibly interoperating with the forum)
- Generates completion certificates
- **AI integration:** retrieve achievements and generate a certificate to email (RAG)

### `leaderboard-analytics-service` — Leaderboard / Analytics — *Roan*

- Global analytics comparing you against other students
- Customisable visualisations
- Your rank in each course and language
- Who contributes most on the discussion forum (nice to have)
- **AI integration:** ask an assistant about the analytics (RAG), e.g. *"Who's ranking first in Italian?"*

---

## Working on your microservice

Everything you own lives under your service directory. The skeleton already gives you a
working vertical slice — a `SampleItems` table, CRUD on the database service, an API
endpoint on the backend, and a page that renders it. Build your feature by extending
that slice, then delete the placeholder.

Open `<service>/LanguageWise.<Name>.BE.slnx` in your IDE. It contains your three .NET
projects plus a `frontend` folder so you can edit the whole microservice in one place.

### Adding a table

1. Add the `CREATE TABLE` to `<service>/database/LanguageWise.<Name>.Db/sql/schema.sql`.
2. Add at least **ten** rows to `sql/seed.sql` (specification section 2.2).
3. Add a repository class next to `SampleItemRepository.cs`.
4. Map the CRUD endpoints in `Program.cs`.
5. `docker compose down --volumes` then `docker compose up --build` to re-seed.

The schema is applied on every start-up and the seed only runs when the table is empty,
so both files are safe to re-run.

### Adding an API endpoint

Add it to `<service>/backend/LanguageWise.<Name>.Api/Program.cs`. Return **JSON** under
`/api/...` for API consumers, and an **HTML fragment** if HTMX will swap it straight
into the page.

The backend never opens SQLite. It calls its own database service over HTTP.

### Adding a page

Edit the files in `<service>/frontend/`. Anything nginx can serve on port 80 is fine —
use a framework and a build step if you want one, as `mini-games-service` does, but keep
the build inside the Dockerfile so nobody needs your toolchain installed.

Five services share `css/theme.css`. Because each frontend builds from its own folder,
that file is **copied into** each service rather than shared at build time. If you change
the theme, change `shared/frontend/css/theme.css` first and copy it out — see
[`docs/architecture.md`](docs/architecture.md#the-css-theme).

### Writing tests

Put NUnit tests in `<service>/tests/LanguageWise.<Name>.Api.Tests/`. Use
`StubHttpMessageHandler` when you need to fake the database service so tests stay fast
and need nothing running.

---

## Contributing

### Branching and pull requests

`master` is the integration branch and is protected. Work on a branch and open a pull
request:

```bash
git switch -c feat/quizzes-flashcards
# ...commit your work...
git push -u origin feat/quizzes-flashcards
```

A pull request can only be merged once **all six status checks pass**:

| Check | What it does |
| --- | --- |
| `shared-build-and-test` | Builds and tests `shared`, validates `docker-compose.yml` |
| `mini-games-service-build-and-test` | Builds and tests `mini-games-service` |
| `chat-discussion-service-build-and-test` | Builds and tests `chat-discussion-service` |
| `quizzes-courses-service-build-and-test` | Builds and tests `quizzes-courses-service` |
| `quests-achievements-notifications-service-build-and-test` | Builds and tests `quests-achievements-notifications-service` |
| `leaderboard-analytics-service-build-and-test` | Builds and tests `leaderboard-analytics-service` |

Each one restores, builds and tests that service's `.slnx`, then builds its three Docker
images from their own tier folders.

Every workflow runs on every pull request — there are no path filters. That is
deliberate: GitHub matches required checks by job name, and a workflow that never runs
never reports a result, which would leave the pull request stuck forever.

### Repository settings (owner only)

Branch protection is configured in GitHub, not in this repository. In
**Settings → Rules → Rulesets → New branch ruleset**, targeting `master`:

- Require a pull request before merging
- Require status checks to pass, adding the six job names above, and enable
  *Require branches to be up to date before merging*
- Block force pushes and restrict deletions
- Add **Repository admin** to the bypass list so the owner can still push directly.
  Leave *Do not allow bypassing the above settings* switched **off**, otherwise the
  bypass will not apply.

### Commit convention

This project uses **[Conventional Commits](https://www.conventionalcommits.org/)**,
following [qoomon's cheatsheet](https://gist.github.com/qoomon/5dfcdf8eec66a051ecd85625518cfd13)
as the house style. Please read it before your first commit.

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

```
feat(forum): add image uploads to posts
feat(quizzes): generate flashcards from course content
fix(analytics): correct rank calculation for tied scores
refactor(games): extract the word-matching scorer
docs: add root readme
ops: add docker compose for local development
chore: init
```

### House rules

Please don't commit API keys and `.env` files...thx 🙂

**Do not remove the `!*.Db/` line from `.gitignore`.** Git is case-insensitive on
Windows and macOS, so the `*.db` rule matches the `LanguageWise.*.Db` project folders and
silently stops every database microservice from being committed. The files stay on your
disk and the mistake only surfaces as `MSB1009: Project file does not exist` in CI.

---

## Troubleshooting

**A frontend shows "The database microservice is unavailable."**
The backend could not reach its database service. Check it is healthy:
`docker compose ps` and `docker compose logs chat-discussion-service-db`.

**A port is already in use.**
Something else on your machine owns 3000–3005, 5000–5005, 6000–6005 or 11434. For
Ollama, set `OLLAMA_PORT`. Otherwise stop the other process, or change the host side of
the mapping in `docker-compose.yml`.

**My seed data changed but the page still shows the old rows.**
Seeding only runs when the table is empty. Wipe the volumes and start again:
`docker compose down --volumes && docker compose up --build`.

**CI says `MSB1009: Project file does not exist` but it builds fine locally.**
The file exists on your disk but was never committed. Run
`git check-ignore -v <path>` — a `.gitignore` rule is probably swallowing it. Each
workflow's *Verify the service layout is complete* step catches this early and names the
missing file.

**`git pull` after the folder rename leaves stale `student-N/` directories.**
The rename is tracked, but untracked build output is not. Clean them up with
`git clean -fdx student-1 student-2 student-3 student-4 student-5` once you are sure you
have nothing uncommitted in them.

---

## Documentation

- [Architecture, port map, API contracts](docs/architecture.md)
- [AI services and Ollama](ai-services/README.md)
- [Agentic loop reviewer](tools/agentic-loop/README.md)
