# Quests, Achievements, and Notifications Service

This service tracks achievement progress, stores notification preferences and event history, and sends AI-written notification emails. Its Vue 3 feature remote is rendered inside the Shared LanguageWise SPA.

## Components

- PostgreSQL stores achievements, user progress, preferences, and notification history.
- PostgREST exposes the service-owned `api` schema to the backend.
- ASP.NET Core validates shared-service JWTs and owns event processing.
- Ollama runs the official `gemma4:e4b` model and persists it in the `ollama-data` Docker volume.
- MailKit sends plain-text email through Gmail SMTP with STARTTLS.
- nginx serves the federation entry and chunks; the Shared gateway proxies `/quests-and-achievements/api/*` to the backend.

## Configuration

Generate the shared JWT key pair from the repository root:

```powershell
./tools/gen-signing-key/gen-signing-key.ps1
```

Create `backend/.env` with the Gmail account and a Google app password:

```text
Smtp__Host=smtp.gmail.com
Smtp__Port=587
Smtp__Username=sender@example.com
Smtp__Password=google-app-password-without-spaces
Smtp__FromName=LanguageWise
```

The file is the sole source of SMTP configuration. It is ignored by Git and excluded from the backend Docker build context. `Smtp__Username` is always used as the sender address. If the file or credentials are absent, events still update progress but email is skipped.

## Start

From the repository root:

```powershell
docker compose up -d --build quests-achievements-notifications-service-frontend
```

The first start downloads the Ollama image and `gemma4:e4b` model. The model initializer exits successfully after populating the persistent volume. Open [http://localhost:3000/quests-and-achievements/](http://localhost:3000/quests-and-achievements/) after the backend and frontend become healthy.

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

Returns the authenticated username, preferences, achievement progress, and newest-first notification history as JSON:

```json
{
  "username": "amber",
  "preferences": {
    "email": "amber@example.com",
    "notifyAll": true,
    "notifyCourseCompletion": true
  },
  "achievements": [
    {
      "achievementId": 1,
      "name": "First Course",
      "image": "/images/achievements/first-course.png",
      "progress": 1,
      "progressNeeded": 1
    }
  ],
  "notifications": [
    {
      "notificationId": 2,
      "trigger": "post-engagement",
      "time": "2026-08-29T14:15:00Z",
      "emailSubject": "Consectetur adipiscing elit",
      "emailBody": "Ut enim ad minim veniam."
    }
  ]
}
```

### `PUT /api/preferences`

Accepts JSON:

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

A valid email is required. The record is upserted by authenticated user ID. Preferences control email delivery only; every accepted event still creates in-app notification history and updates achievement progress.

### `POST /api/events`

Accepts a noteworthy event from another service:

```json
{
  "trigger": "post-engagement",
  "subject": "Tips for practising Spanish every day",
  "recipientUserId": 7
}
```

Supported triggers are `post-engagement`, `course-completion`, `quiz-result`, and `streak`. The subject is a human-readable description used to generate the notification. Every accepted request represents a new occurrence, adds one progress unit to every achievement tier mapped to that trigger, and uses the server's current time for notification history.

A successful response includes all updated achievements, stored notification content, preference eligibility, and email status:

```json
{
  "achievements": [
    {
      "achievementId": 4,
      "name": "First Applause",
      "progress": 1,
      "progressNeeded": 1,
      "newlyAttained": true
    }
  ],
  "notification": {
    "subject": "Achievement unlocked: First Applause",
    "body": "You unlocked First Applause. Congratulations!",
    "usedFallback": false
  },
  "shouldNotify": true,
  "email": {
    "sent": true,
    "configured": true,
    "error": null
  }
}
```

Ollama output is constrained to structured JSON, thinking is disabled, and generation is capped at 192 tokens. If generation fails or times out, the backend stores a deterministic fallback. SMTP failure is logged and returned as an email error without undoing the notification or progress.

## Verification

Run automated tests:

```powershell
dotnet test quests-achievements-notifications-service/LanguageWise.QuestsAchievementsNotificationsService.BE.slnx
```

The suite covers JWT bearer/cookie authorization, validation, progress and attainment rules, notification filtering, repeated event processing, PostgREST persistence, structured Ollama output and fallback, and MailKit message construction without contacting Gmail.
