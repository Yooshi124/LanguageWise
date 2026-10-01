# TECHNICAL-mini-games: Mini Games Service (Technical Overview)

Internal reference for the agentic loop. Not for end-user access.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.MiniGamesService.Api`.
- Key routes: `GET /api/game-languages`, `GET /api/game-modes`,
  `GET /api/stats/completions`, `POST /api/guess-the-word/{init,guess,reset}`,
  `POST /api/word-search/{init,guess,hint,give-up,reset}`,
  `POST /api/associations/{init,guess,reset}`, `POST /api/assistant/messages`.
- Domain responses: `GameResponse`, `GameAttemptResponse`, `CompletionStatsResponse`.
- Depends on quizzes-courses-service for vocabulary, quests-achievements-notifications-service
  for event webhooks, garry-ai-service for hints, the host rag-server for documentation
  lookups (`Services__Rag`), and OpenRouter for vocabulary generation.

## Database

- SQLite, file `mini-games-service.db`, container `mini-games-service-db`,
  host port `6005`.
- Key tables: `Games` (id, gameType, courseCode, solution, words, difficulty,
  createdAt, expiresAt) and `GameAttempts` (id, gameId, userId, score, isWon,
  isComplete, attemptCount, startedAt, completedAt, timeSpentSeconds).

## Frontend

- Vue 3 (JavaScript), package `mini-games-frontend`, Module Federation remote.
- Key components: `GuessTheWord.vue`, `WordSearch.vue`, `Associations.vue`,
  `GamePage.vue`.

## Deployment

- Backend service `mini-games-service-backend`, host port `5001`.
- Env vars: `Services__Database`, `Services__Courses`, `Services__Achievements`,
  `Services__Garry`, `Services__Rag` (points at `host.docker.internal:8100`).
