# LanguageWise

A language learning platform built as a set of independent microservices, woven
together with Docker Compose.

The repository also hosts a shared development tool: an **agentic loop** that acts
as a rubber duck reviewer over the whole codebase.

---

## Features

Each area is owned by one team member and will live in its own microservice.

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

The quizzes and courses service includes Garry, a language-learning assistant
powered through OpenRouter. The backend owns Garry's prompt, course and lesson
context, model settings, and provider credentials. The browser only renders the
streamed response and keeps a bounded transcript in `sessionStorage`; chats are
not written to the database.

Copy the example environment file and add an OpenRouter API key:

```powershell
Copy-Item quizzes-courses-service\backend\.env.example quizzes-courses-service\backend\.env
```

```text
OpenRouter__ApiKey=your-openrouter-api-key
```

Docker Compose loads this ignored file into the quizzes and courses backend.
The default model is `google/gemma-4-26b-a4b-it`; override
`OpenRouter__Model` in the same file if the model identifier changes. When no
key is configured, the rest of the service remains available and Garry returns
a clear unavailable response without exposing configuration details.
