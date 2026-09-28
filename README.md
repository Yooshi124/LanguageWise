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

## Notification email configuration

The quests, achievements, and notifications service uses Ollama with
`gemma4:e4b` to compose notification emails. Docker Compose downloads the model
into the persistent `ollama-data` volume on its first start.

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
the reply. Garry tries OpenRouter first and falls back to the shared Ollama model
when OpenRouter cannot start. The feature backends do not choose a provider. The
browser keeps its existing assistant URLs and stores bounded transcripts in
`sessionStorage`; chats are not written to the database.

Copy the example environment file and add an OpenRouter API key:

```powershell
Copy-Item garry-ai-service\backend\.env.example garry-ai-service\backend\.env
```

```text
OpenRouter__ApiKey=your-openrouter-api-key
```

Docker Compose loads this ignored file only into Garry. The default OpenRouter
model is `google/gemma-4-26b-a4b-it`; `OpenRouter__Model`,
`OpenRouter__BaseUrl`, and `OpenRouter__MaxOutputTokens` can be overridden there. Without a key, Garry uses
Ollama. If neither provider can start, the discussion assistant uses its help
articles; other assistants report temporary unavailability. Garry applies one
10-request-per-minute limit per user across all five assistants. This limit is
in memory, so Garry must remain a single replica unless a distributed limiter
is introduced. Garry has no public port: the private Compose network is the
trust boundary for service-supplied rules, while the forwarded user JWT verifies
the learner's identity.
