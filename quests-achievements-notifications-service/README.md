# Quests, Achievements, and Notifications Service

This service tracks achievement progress, stores notification preferences and event history, and sends AI-written notification emails. The browser-facing dashboard uses HTMX for requests and renders backend JSON with frontend JavaScript.

## Components

- PostgreSQL stores achievements, user progress, preferences, and processed event IDs.
- PostgREST exposes the service-owned `api` schema to the backend.
- ASP.NET Core validates shared-service JWTs and owns event processing.
- Ollama runs the official `gemma4:12b` model and persists it in the `ollama-data` Docker volume.
- MailKit sends plain-text email through Gmail SMTP with STARTTLS.
- nginx serves the frontend and proxies `/api/*` to the backend.

## Configuration

Generate the shared JWT key pair from the repository root:

```powershell
./tools/gen-signing-key/gen-signing-key.ps1
```

Create `backend/.env` with the Gmail account and a Google app password:

```text
SMTP_USERNAME=sender@example.com
SMTP_PASSWORD=google-app-password-without-spaces
SMTP_FROM_NAME=LanguageWise
```

The file is ignored by Git and excluded from the backend Docker build context. `SMTP_USERNAME` is always used as the sender address. If the file or credentials are absent, events still update progress but email is skipped.

## Start

From the repository root:

```powershell
docker compose up -d --build quests-achievements-notifications-service-frontend
```

The first start downloads the Ollama image and approximately 7.4 GB for `gemma4:12b`. The model initializer exits successfully after populating the persistent volume. Open [http://localhost:3004/](http://localhost:3004/) after the backend and frontend become healthy.

Check status without displaying environment values:

```powershell
docker compose ps --all
docker compose logs --tail 50 quests-achievements-notifications-service-backend
```

## Authentication

All `/api/*` endpoints require an RS256 JWT issued by the shared service. Supply it either as an HttpOnly `token` cookie or an `Authorization: Bearer <token>` header. `GET /health` is anonymous.

The backend derives the actor user ID from the numeric JWT `sub` claim. It never accepts an actor ID from an event body.

## API

### `GET /api/profile`

Returns the authenticated username, preferences, and achievement progress as JSON.

### `PUT /api/preferences`

Accepts JSON or an HTMX form. JSON example:

```json
{
  "email": "learner@example.com",
  "notifyAll": true,
  "notifyPostEngagement": true,
  "notifyCourseCompletion": true,
  "notifyQuizResults": true,
  "notifyStreaks": true,
  "notifyAchievements": true
}
```

A valid email is required. The record is upserted by authenticated user ID.

### `POST /api/events`

Accepts a noteworthy event from another service:

```json
{
  "eventId": "forum-like-post-123-user-7",
  "eventType": "post-engagement",
  "subjectId": "post-123",
  "recipientUserId": 7,
  "achievementId": 4,
  "occurredAt": "2026-08-27T10:00:00Z",
  "value": 1,
  "metadata": {
    "action": "like"
  }
}
```

Supported event types are `post-engagement`, `course-completion`, `quiz-result`, and `streak`. `eventId` is unique; replaying it returns `409 Conflict` without applying progress twice.

A successful response includes updated progress, whether the achievement was newly attained, preference eligibility, and email status:

```json
{
  "shouldNotify": true,
  "email": {
    "sent": true,
    "configured": true,
    "usedFallback": false,
    "error": null
  }
}
```

Ollama output is constrained to structured JSON, thinking is disabled, and generation is capped at 192 tokens. If generation fails or times out, the backend sends a deterministic fallback. SMTP failure is logged and returned as an email error without undoing the recorded event or progress.

## Verification

Run automated tests:

```powershell
dotnet test quests-achievements-notifications-service/LanguageWise.QuestsAchievementsNotificationsService.BE.slnx
```

The suite covers JWT bearer/cookie authorization, validation, progress and attainment rules, notification filtering, PostgREST persistence and duplicate handling, structured Ollama output and fallback, and MailKit message construction without contacting Gmail.
