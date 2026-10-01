# TECHNICAL-leaderboard-analytics: Leaderboard & Analytics Service (Technical Overview)

Internal reference for the agentic loop. Not for end-user access.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.LeaderboardAnalyticsService.Api`.
- Key routes: `GET /api/my-language-rankings`, `GET /api/lessons-completed-over-time`,
  `POST /api/lessons-completed-summary` (optional AI-generated insights),
  `POST /api/assistant/messages`.
- Domain models: `RankingData`, `LessonsCompletedModels`, `AiSummaryModels`.
- Stateless — it has no database of its own and computes everything on the fly
  by calling quizzes-courses-service over HTTP (`Services__QuizzesCourses`).
- Uses a local Ollama instance (`Services__Ollama`) and garry-ai-service
  (`Services__Garry`) to generate natural-language performance summaries.

## Database

- None. All data is sourced live from quizzes-courses-service.

## Frontend

- Vue 3 + TypeScript, package `languagewise-leaderboard-analytics-frontend`,
  Module Federation remote.
- Single dashboard view (`HomeView`) built with Highcharts 11 and
  `@tanstack/vue-query` for data fetching/caching.

## Deployment

- Backend service `leaderboard-analytics-service-backend`, host port `5005`.
- Frontend is served only through the gateway shell at `/analytics/`, no
  direct host port.
