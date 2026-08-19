# LanguageWise

A language learning platform built as a set of independent microservices, woven
together with Docker Compose. Think Duolingo, plus a student community, plus the
analytics to see how you stack up against everyone else.

The repository also hosts a shared development tool: an **agentic loop** that acts
as a rubber duck reviewer over the whole codebase.

---

## Features

Each area is owned by one team member and will live in its own service.

### Discussion / Chat Forum — *Lachlan*

Where students talk to each other about their progress.

- Students make posts
- Like, reply and comment
- Images (nice to have)
- **AI integration:** helps you write posts, summarises threads (RAG)

### Quizzes and Courses — *Justin*

- Prepared, static quizzes — students answer questions, Duolingo style
- Prepared, static course content
- Question-and-answer interactions, e.g. clicking words in order to build a sentence
- **AI integration:** generate your own questions and flashcards (RAG), mark quizzes (Synapse)

### Mini Games / Activities — *Kyan*

Small games for learning vocabulary.

- Crosswords, word jumbles, matching a word to an image (in the spirit of babadum.com)
- A "Squid Game glass bridge", but with words
- **AI integration:** mark the activity (RAG), generate or retrieve the word list to use (RAG)

### Leaderboard / Analytics — *Roan*

- Global analytics comparing you against other students
- Customisable visualisations
- Your rank in each course and language
- Who contributes most on the discussion forum (nice to have)
- **AI integration:** ask an assistant about the analytics (RAG), e.g. *"Who's ranking first in Italian?"*

### Quests / Achievements / Notifications — *Amber*

- Event-driven push notifications and emails, e.g. complete 5 courses, earn a silver medal
- Sends emails
- Achievements page showing past achievements (possibly interoperating with the forum)
- Generates completion certificates
- **AI integration:** retrieve achievements and generate a certificate to email (RAG)

---

## Repository layout

Every service sits in its own top-level folder, example:

```
LanguageWise/
├── Tools/
│   └── AgenticLoop/        shared dev tool: the rubber duck code reviewer
├── DiscussionService/      (planned)
├── QuizService/            (planned)
├── GamesService/           (planned)
├── AnalyticsService/       (planned)
├── QuestsService/          (planned)
└── docker-compose.yml      (planned) runs the services together
```

Services are independent: each owns its data, its API and its deployment unit, and
they are composed locally with Docker Compose.

---

## The Agentic Loop (rubber duck tool)

`Tools/AgenticLoop` is a CLI code reviewer powered by Google Gemini. You give it a
targeted prompt — *"look at the database validation"*, *"check test coverage"* — and
it runs a six-stage loop:

**PLAN → ACT → OBSERVE → AGENT → HUMAN REVIEW → ADAPT**

It reads the code, proposes numbered findings, asks which ones you accept, writes an
implementation plan for the accepted ones, and records the whole round — prompt,
evidence, suggestions, your decisions — to a markdown evidence log.

The tool is **read-only**: it never edits a source file. It reviews the entire
repository by default, so any service in this repo can use it, or you can point it
at one service with `TARGETED_DIRECTORY` in your `.env`.

```powershell
cd Tools\AgenticLoop
pip install -r requirements.txt
copy .env.example .env    # then add your Gemini API key
python main.py
```

Full documentation:

| Document | Contents |
| --- | --- |
| [`Tools/AgenticLoop/README.md`](Tools/AgenticLoop/README.md) | Overview and quick start |
| [`Tools/AgenticLoop/docs/SETUP.md`](Tools/AgenticLoop/docs/SETUP.md) | API key, `.env` reference, troubleshooting |
| [`Tools/AgenticLoop/docs/USAGE.md`](Tools/AgenticLoop/docs/USAGE.md) | Running a review, worked examples |
| [`Tools/AgenticLoop/docs/HOW_IT_WORKS.md`](Tools/AgenticLoop/docs/HOW_IT_WORKS.md) | Architecture and design decisions |

---

## Commit convention

This project uses **[Conventional Commits](https://www.conventionalcommits.org/)**,
following [qoomon's cheatsheet](https://gist.github.com/qoomon/5dfcdf8eec66a051ecd85625518cfd13)
as the house style. Please read it before your first commit.

```
<type>(<optional scope>): <description>

<optional body>

<optional footer>
```

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

### Scope

The scope is optional and gives context. In this repo, prefer the service or tool
you touched: `forum`, `quizzes`, `games`, `analytics`, `quests`, `agentic-loop`,
`infra`. Do **not** use issue identifiers as scopes.

### Description

- Imperative, present tense: "add", not "added" or "adds" — *this commit will...*
- Do not capitalise the first letter
- Do not end with a full stop

### Breaking changes

Mark a breaking change with `!` before the colon, and explain it in the footer:

```
feat(quizzes)!: remove the legacy quiz endpoint

BREAKING CHANGE: /api/quiz/list has been replaced by /api/quizzes.
```

### Examples

```
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

1. Branch from `main`.
2. Keep changes inside your own service where possible; shared changes are worth a
   quick heads-up to the team.
3. Commit using the convention above.
4. Before opening a pull request, consider running the agentic loop over your
   service as a self-review:
   ```powershell
   cd Tools\AgenticLoop
   python main.py --scope ..\..\QuizService
   ```
   Accepted findings become an implementation plan in `Tools/AgenticLoop/Plans`, and
   the full review is recorded in `Tools/AgenticLoop/Sessions`.

Never commit secrets. API keys belong in a local `.env`, which is gitignored.
