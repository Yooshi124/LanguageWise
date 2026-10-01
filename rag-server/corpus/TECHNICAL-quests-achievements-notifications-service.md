# TECHNICAL-quests-achievements-notifications: Quests, Achievements & Notifications Service (Technical Overview)

Internal reference for the agentic loop. Not for end-user access.

## Backend

- ASP.NET Core (.NET 10) project `LanguageWise.QuestsAchievementsNotificationsService.Api`.
- Key routes: `GET /api/profile`, `GET /api/achievements`, `GET /api/preferences`,
  `PUT /api/preferences`, `POST /api/events` (the webhook other services call to
  log an achievement/quest-triggering event, which may also enqueue a
  notification/email), `POST /api/assistant/messages`,
  `POST /api/assistant/tools/{name}` (MCP tool handlers).
- Domain entities: `Achievement`, `UserAchievement`, `UserPreferences`,
  `Notification`, `NotificationInput`, `EventRequest`.
- Uses a local Ollama instance for email content generation and SMTP/Gmail for
  delivery; calls garry-ai-service for the in-app assistant.

## Database

- PostgreSQL 18, database `quests_achievements_notifications`, fronted by a
  PostgREST container (`quests-achievements-notifications-service-db-api`,
  internal port `3000`) — the API talks to PostgREST rather than the database
  directly.
- Key tables: `achievements` (achievement_id, name, description, image,
  trigger, progress_needed), `user_achievements` (user_id, achievement_id,
  progress), `user_preferences` (user_id, email, notify_all,
  notify_community_contribution, notify_post_engagement,
  notify_lesson_completion, notify_course_completion, notify_quiz_result,
  notify_minigame_win, notify_login_streak, notify_achievements),
  `notifications` (notification_id, user_id, trigger, time, email_subject,
  email_body).

## Frontend

- Vue 3 + TypeScript, package `languagewise-quests-achievements-frontend`,
  Module Federation remote. Main view is `QuestsDashboard.vue`.

## Deployment

- Backend service `quests-achievements-notifications-service-backend`, host
  port `5004`.
- Env vars: `Services__Database` (points at the PostgREST API), `Services__Ollama`,
  `Services__Garry`.
