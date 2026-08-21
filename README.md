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

### Run everything

```bash
docker compose up --build
```

Then open **<http://localhost:3000>** — the unified home page, with a tab for each
student feature.

Convenience wrappers are in `scripts/`:

```powershell
.\scripts\up.ps1          # build and start, then print every URL
.\scripts\down.ps1        # stop (databases are kept)
.\scripts\down.ps1 -Clean # stop and wipe the databases
```

```bash
./scripts/up.sh
./scripts/down.sh
./scripts/down.sh --clean
```

### Build and test without Docker

```powershell
.\scripts\build.ps1
.\scripts\test.ps1                  # all 6 test projects
.\scripts\test.ps1 -Service student-3
```

```bash
dotnet build LanguageWise.sln
dotnet test  LanguageWise.sln
```

---

## Ports

| Microservice | Owner | Feature | Frontend | Backend | Database service |
| --- | --- | --- | --- | --- | --- |
| `shared` | Team | Unified home page | **[3000](http://localhost:3000)** | 5000 | 6000 |
| `student-1` | Kyan | Mini Games / Activities | **[3001](http://localhost:3001)** | 5001 | 6001 |
| `student-2` | Lachlan | Discussion / Chat Forum | **[3002](http://localhost:3002)** | 5002 | 6002 |
| `student-3` | Justin | Quizzes and Courses | **[3003](http://localhost:3003)** | 5003 | 6003 |
| `student-4` | Amber | Quests / Achievements | **[3004](http://localhost:3004)** | 5004 | 6004 |
| `student-5` | Roan | Leaderboard / Analytics | **[3005](http://localhost:3005)** | 5005 | 6005 |
| `ollama` | Team | LLM runtime | — | 11434 | — |

Inside the Docker network every .NET container listens on `8080` and every nginx
container on `80`. See [`docs/architecture.md`](docs/architecture.md) for the full
request flow and API contracts.

---

## Repository structure

```
.github/workflows/     shared.yml + student-1.yml … student-5.yml
ai-services/           Ollama / AI-Mode (Release 0 runtime is in docker-compose.yml)
docs/                  Architecture diagrams and documentation
scripts/               build / test / up / down helpers (.ps1 and .sh)
shared/                Team-owned microservice — the unified home page and CSS theme
  frontend/            nginx: index.html, css/theme.css, js/htmx.min.js, nginx.conf
  backend/             LanguageWise.Shared.Api      (ASP.NET Core minimal API)
  database/            LanguageWise.Shared.Db       (ASP.NET Core minimal API + SQLite)
  tests/               LanguageWise.Shared.Api.Tests (NUnit)
student-1/ … student-5/  Same four-folder shape, one per team member
tools/agentic-loop/    Shared rubber-duck reviewer for the whole codebase
docker-compose.yml     The one shared configuration that runs everything
LanguageWise.sln       All 18 .NET projects
```

### Tech stack

- **Frontend** — static HTML, CSS and JavaScript with [HTMX](https://htmx.org/docs/),
  served by nginx. There is no server-side code in this tier.
- **Backend / API** — ASP.NET Core minimal APIs on .NET 10.
- **Database service** — ASP.NET Core minimal APIs on .NET 10 using
  `Microsoft.Data.Sqlite`. This is the only tier that opens a SQLite file.
- **Database** — SQLite, one file per microservice on its own named Docker volume.
- **Testing** — NUnit.
- **AI** — Ollama with an approved open-source LLM (Llama, Qwen or DeepSeek).

---

## Features

Each area is owned by one team member and lives in its own microservice.

### Student 1: Mini Games / Activities — *Kyan*

Small games for learning vocabulary.

- Crosswords, word jumbles, matching a word to an image (in the spirit of babadum.com)
- A "Squid Game glass bridge", but with words
- **AI integration:** mark the activity (RAG), generate or retrieve the word list to use (RAG)

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

### Student 4: Quests / Achievements / Notifications — *Amber*

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

## Working on your microservice

Everything you own lives under your `student-N/` directory. The skeleton already gives
you a working vertical slice — a `SampleItems` table, CRUD on the database service, an
API endpoint on the backend and a page that renders it with HTMX. Build your feature by
extending that slice.

### Adding a table

1. Add the `CREATE TABLE` to `student-N/database/LanguageWise.StudentN.Db/sql/schema.sql`.
2. Add at least **ten** rows to `sql/seed.sql` (specification section 2.2).
3. Add a repository class next to `SampleItemRepository.cs`.
4. Map the CRUD endpoints in `Program.cs`.
5. `docker compose down --volumes` then `docker compose up --build` to re-seed.

The schema is applied on every start-up and the seed only runs when the table is empty,
so both files are safe to re-run.

### Adding an API endpoint

Add it to `student-N/backend/LanguageWise.StudentN.Api/Program.cs`. Return **JSON**
under `/api/...` for API consumers, and an **HTML fragment** if HTMX will swap it
straight into the page.

### Adding a page

Edit `student-N/frontend/index.html`. Use the classes from the shared theme so your
pages match everyone else's. Do not copy `theme.css` into your own folder — it is copied
into your image from `shared/frontend/css/` at build time, and the whole application is
required to share one consistent theme.

### Writing tests

Put NUnit tests in `student-N/tests/LanguageWise.StudentN.Api.Tests/`. Use
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
| `shared-build-and-test` | Builds and tests the shared microservice, validates `docker-compose.yml` |
| `student-1-build-and-test` … `student-5-build-and-test` | Builds each student's projects, runs their NUnit tests, builds their three Docker images |

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

---

## Troubleshooting

**A frontend shows "The database microservice is unavailable."**
The backend could not reach its database service. Check it is healthy:
`docker compose ps` and `docker compose logs student-3-db`.

**A port is already in use.**
Something else on your machine owns 3000–3005, 5000–5005, 6000–6005 or 11434. Stop it,
or change the host side of the mapping in `docker-compose.yml`.

**My seed data changed but the page still shows the old rows.**
Seeding only runs when the table is empty. Wipe the volumes and start again:
`docker compose down --volumes && docker compose up --build`.

**`dotnet build` cannot find a package version.**
Package versions are managed centrally in `Directory.Packages.props`. Add a
`PackageVersion` there, then reference the package without a version in your `.csproj`.

---

## Documentation

- [Architecture, port map, API contracts](docs/architecture.md)
- [AI services and Ollama](ai-services/README.md)
- [Agentic loop reviewer](tools/agentic-loop/README.md)
