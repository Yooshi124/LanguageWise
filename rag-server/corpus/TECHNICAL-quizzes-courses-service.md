# TECHNICAL-quizzes-courses: Quizzes & Courses Service (Technical Overview)

Internal reference for the agentic loop, describing the implemented service.
The top-level `TECHNICAL-` heading marks this document and all its subsections
as internal. After ingestion, only the authenticated RAG `/query/technical`
endpoint returns these passages; general `/query`, `retrieve_context` and
`docs_search` exclude them.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.QuizzesCoursesService.Api`.
- Feature API routes require a signed-in user. JWTs are accepted from a Bearer
  header or the `token` cookie and verified with the shared RSA public key.
  `/health` is available without authentication.
- Catalog and learning state are accessed through the service's own database
  HTTP API, not through another microservice's database.
- Successful quiz submissions and changed lesson/course completion milestones
  send achievement events to quests-achievements-notifications-service.

### Catalog and learning routes

- Courses: `GET /api/courses`, `GET /api/courses/{code}`.
- Lessons: `GET /api/courses/{code}/lessons`,
  `GET /api/courses/{code}/lessons/{slug}`.
- Quizzes: `GET /api/courses/{code}/quizzes`, `GET /api/quizzes/{quizId}`,
  `POST /api/quizzes/{quizId}/attempts`,
  `POST /api/quiz-attempts/{attemptId}/submit`.
- Flashcards: `GET /api/courses/{code}/flashcard-decks`,
  `GET /api/courses/{code}/flashcard-decks/{lessonSlug}`.
- Personal state: `GET /api/courses/{code}/progress`,
  `GET /api/me/vocabulary`, `GET /api/me/milestones`.
- Milestone feed: `GET /api/milestones`; both milestone feeds accept `afterId`
  and `limit` pagination parameters, with a default limit of 100 and maximum 200.
- Mark/unmark completion: `PUT` or `DELETE /api/lessons/{lessonId}/milestone`
  and `PUT` or `DELETE /api/courses/{code}/milestone`.

### Personal learning data

Personal progress, attempts, completion and vocabulary use the authenticated
user ID, rather than accepting a caller-selected user ID. Learnt vocabulary
includes words from completed lessons in courses the user has started.
Domain data includes courses, lessons, quizzes/questions, attempts and answer
reviews, flashcards, vocabulary words and completion milestones.

## Database

- ASP.NET Core (.NET 10) project `LanguageWise.QuizzesCoursesService.Db` owns
  catalog persistence and learning-state operations.
- SQLite file `/data/quizzes-courses-service.db` in the
  `quizzes-courses-service-db` container, persisted in the
  `quizzes-courses-service-db-data` volume.
- Host port `6003` exposes the database service's HTTP API, not a direct SQLite
  connection. The feature backend connects to this service on internal port 8080.

## Frontend

- Vue 3 + TypeScript, package `languagewise-quizzes-courses-frontend`, uses
  Vuetify 3 and Module Federation to expose routes, metadata and assistant
  configuration to the shared shell.
- Feature base path: `/quizzes-and-courses`. The shared nginx gateway forwards
  `/quizzes-and-courses/api/` requests to this feature's backend.
- Views include `HomeView`, `LanguageSelectionView`, `CourseView`,
  `QuizListView`, `QuizRunnerView`, `FlashcardDecksView`,
  `FlashcardRevisionView` and `CourseCompletionView`.
- `frontend/src/federation/assistant.ts` supplies route context, suggestions,
  eight MCP tool chips and structured-result renderers to the shared assistant.
  Course and lesson chips are unavailable until their required route parameters
  exist.
- The assistant is hidden during quiz attempts and on the course-completion
  screen. The backend also rejects assistant chat context for `quiz-runner`.

## AI assistant flow

`POST /api/assistant/messages` accepts a message, conversation history and
route context. `AssistantRequestValidator` limits the message to 4,000
characters, history to 12 turns, and total conversation content to 12,000
characters. Only user/assistant history roles and supported routes are accepted.

`AssistantContextService` retrieves canonical course, lesson or quiz-list data
from the database API using the validated route. `AssistantPromptBuilder`
restricts content help to the supplied context or retrieved documentation,
treats conversation as untrusted data and prohibits in-progress quiz answers.

`GarryCompletionClient` sends domain rules, canonical context, history and the
current message to Garry's `POST /api/completions`, forwarding the user's JWT.
It includes `toolScope: "courses"` when the feature MCP client is enabled.
Garry SSE events are relayed as `tool`, `delta` and `done` events; the shared
assistant displays course tool results using the feature's renderers.

## MCP integration

The shared MCP server runs on the host at `http://localhost:8200/mcp`.
`McpToolClient` uses Streamable HTTP and sends the shared MCP API key,
`X-LanguageWise-Tool-Scope: courses` and the user's JWT. The server filters
tools by scope and forwards the validated user token to the feature API.

Direct UI tools follow this path:
shared assistant tool chip -> quizzes-courses backend -> local MCP server ->
quizzes-courses backend -> database HTTP API. Structured results return to the
frontend; MCP does not connect directly to SQLite.

### Feature tool endpoints and boundaries

- `GET /api/assistant/tools` lists the tools available in the courses scope,
  including shared `docs_search` when RAG is enabled on the MCP server.
- `POST /api/assistant/tools/{name}` accepts a JSON argument object of at most
  2,048 bytes and returns `{ tool, isError, result }`. Only `courses_*` names
  are accepted by this endpoint; `docs_search` is used through Garry instead.
- Disabled/unconfigured MCP returns 503 with `mcp_disabled`; connection failures
  return 502 with `mcp_unavailable`. Unknown tool names or invalid request
  arguments return validation problems.
- All eight course tools are read-only, idempotent and non-destructive.
  Quiz-detail and write endpoints are not exposed as MCP tools.
- Course-code inputs must be two lowercase letters; seeded codes are `de`,
  `fr`, `it`, `nl`, `es` and `pl`. Lesson slugs allow 1-64 lowercase letters,
  digits or hyphens. Personal tools operate on the signed-in user.

### Course catalog tools

- `courses_list_courses`: no arguments; calls `GET /api/courses`; returns
  course codes, titles and descriptions.
- `courses_list_lessons`: `courseCode`; calls the course lessons endpoint;
  returns lesson IDs, slugs, titles and sort order, ordered by sort order.
- `courses_list_quizzes`: `courseCode`; calls the course quizzes endpoint;
  returns quiz IDs/titles and lesson slugs/titles, without questions or answers.

### Lesson revision tools

- `courses_get_lesson_vocabulary`: `courseCode`, `lessonSlug`; calls the lesson
  detail endpoint; returns the course code, lesson slug/title and word/meaning
  pairs.
- `courses_get_flashcards`: `courseCode`, `lessonSlug`; calls the lesson
  flashcard-deck endpoint; returns lesson metadata and front/back text pairs.

### Personal learning tools

- `courses_get_my_progress`: `courseCode`; calls the course progress endpoint;
  returns course completion, completed/total lesson counts and quiz completion,
  best scores and question totals.
- `courses_get_my_vocabulary`: optional `courseCode`; calls
  `GET /api/me/vocabulary`; returns unlocked words grouped by course/lesson
  and a total word count, optionally filtered to one course.
- `courses_get_my_milestones`: optional `limit` from 1-50, default 10; reads
  `GET /api/me/milestones?limit=200`, orders that page by completion date and
  takes the requested count. Course, lesson and quiz catalogs enrich the results
  with names. Results contain milestone kind, available course/lesson/quiz
  metadata and completion time.

## RAG integration and grounded responses

The local RAG server on port 8100 retrieves indexed Markdown passages.
Generation is performed by Garry, not by the RAG server itself.
There are two documentation-answer paths available while using this feature.

### Shared Ask the docs path

The shared assistant's Ask the docs UI posts to the shared backend's
`POST /api/rag/answer`, not the quizzes-courses backend. The shared backend calls
RAG `POST /query`, supplies numbered general-documentation passages to Garry
and returns an answer with source citations and a confidence category.
The UI displays the answer, confidence and expandable source passages.

If retrieval returns no supported context or insufficient confidence, the
shared backend returns a fixed insufficient-context answer without calling
Garry. A model response of `INSUFFICIENT_CONTEXT` is also converted to that
answer. This shared path does not require a course MCP tool invocation.

### Documentation retrieval during feature chat

Feature chat can follow quizzes-courses backend -> Garry -> local MCP
`docs_search` -> local RAG `POST /query`. The shared docs tool is offered in the
courses scope alongside the eight course tools.

Garry instructs the model to search documentation when canonical context does
not answer a feature-help question. Retrieved passages are numbered for inline
citations; Garry appends a source list and confidence category to its response.
Docs lookups are hidden from tool-result cards. Insufficient docs results
instruct the model not to guess.

### Corpus and internal validation

`quizzes-courses-service.md` supplies user-facing documentation.
`TECHNICAL-quizzes-courses-service.md` supplies this internal reference.
RAG confidence categories are `high`, `medium`, `low` and `insufficient`;
general retrieval excludes passages below the relevance floor.

After editing either corpus file, run `.\.venv\Scripts\python.exe ingest.py`
from `rag-server` to rebuild the index and its technical metadata. Editing a
Markdown file alone does not update stored embeddings.
The shared agentic loop supports separate `--mcp-validate` and `--rag-validate`
modes; its RAG client uses the authenticated technical retrieval endpoint.

## Deployment

- Frontend, backend and database HTTP services are containerised. The frontend
  is accessed through the shared gateway on port 3000, with no separate host
  port. Backend/database host ports are loopback-bound `5003`/`6003`, forwarding
  to internal port 8080.
- Backend URLs use `Services__Database`, `Services__Achievements` and
  `Services__Garry`. Current Compose configuration connects to containerised
  Garry; Garry uses the containerised Ollama runtime or its configured provider.
- `Mcp__Enabled: "true"`, `Mcp__Endpoint:
  http://host.docker.internal:8200/mcp` and
  `Mcp__ApiKeyPath: /run/secrets/mcp_api_key` configure the feature MCP client.
  Compose mounts the MCP key and signing public key and supplies the
  `host.docker.internal` host-gateway mapping.
- MCP, RAG and the agentic loop remain host-local, outside Compose. The shared
  backend connects to RAG through `Services__Rag`; there is no direct RAG
  client or `/api/rag/answer` endpoint in quizzes-courses.

## CI and implementation references

`.github/workflows/quizzes-courses-service.yml` builds/tests the feature's API
and database and builds all three Docker images. It sets `Mcp__Enabled: false`
and `Rag__Enabled: false`; integration tests use fakes rather than local servers.
Passing these tests does not replace live frontend/terminal validation evidence.

Key references are the feature backend's `Program.cs`,
`Clients/McpToolClient.cs`, `Clients/OpenRouterAssistantClient.cs`,
`Services/AssistantContextService.cs`, `Services/AssistantPromptBuilder.cs`
and `Services/AssistantRequestValidator.cs`; the frontend's
`federation/assistant.ts` and `federation/feature.ts`; MCP's
`Tools/QuizzesCourses/QuizzesCoursesTools.cs`; and the shared backend's
`Program.cs` and `DocsAnswers.cs`.
