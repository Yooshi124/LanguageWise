# Quests, Achievements, and Notifications Service Plan

## Requirements

- Provide an HTMX frontend using the shared LanguageWise CSS.
- Display the authenticated user's name, notification email, notification preferences, and achievement progress.
- Authenticate users using JWTs issued by the shared service.
- Accept noteworthy user-triggered events from other services.
- Track achievement progress and send personalised emails for enabled event categories.
- Generate email content with Gemma 4 12B Q4_0 through Ollama.
- Send email through Gmail SMTP using MailKit.
- Store achievements, user progress, notification history, and user preferences in PostgreSQL.

## Agreed Decisions

- The AI model is Gemma 4 12B Q4_0, released July 2026. The exact Ollama model tag must be confirmed when the container is implemented.
- Email will be sent using MailKit and Gmail SMTP. Credentials will be supplied through environment variables or Docker secrets and will not be committed.
- Event requests will use the triggering user's JWT. The actor user ID will always be derived from the token's `sub` claim rather than accepted from the request body.
- An event may identify a different recipient user, such as user A liking user B's post and notifying user B.
- Initial notification categories are post engagement, course completion, quiz results, streaks, and achievements.
- Every enabled noteworthy event may produce an email. When an event unlocks an achievement, the email will include achievement-specific content.
- User IDs in this service are external references to IDs in the shared database; no cross-database foreign key or direct join will be used.

## Implementation Steps

### 1. Database

- Replace the sample schema with the following entities:
  - `Achievements`: name, image, event type, and progress needed.
  - `UserAchievements`: user ID, achievement ID, progress, attained time, and a unique user/achievement constraint.
  - `Notifications`: event ID, actor user ID, recipient user ID, trigger, time, email, generated subject/body, delivery status, and delivery error.
  - `UserPreferences`: user ID, email, and flags for post engagement, course completion, quiz results, streaks, and achievements.
- Require a unique event ID to make event processing idempotent.
- Add an atomic PostgreSQL function or transaction-backed operation that records an event, updates progress, unlocks achievements, and returns newly attained achievements.
- Seed representative achievements and default preferences suitable for local demonstrations.
- Retain PostgREST as the database API used by the backend.

### 2. Authentication and Event Trust

- Configure JWT bearer authentication in the backend using the existing RSA public/signing key material.
- Read the actor user ID from `sub` and username from `name`.
- Mount the signing key into the service backend through Docker Compose.
- Protect all profile, preference, achievement, and event endpoints.
- Do not accept `actorUserId` from an event payload.
- Permit a validated `recipientUserId` because an actor can trigger an event concerning another user.
- Require `eventId`, `eventType`, `subjectId`, `recipientUserId`, `occurredAt`, optional progress value, and optional metadata.
- Validate event types and values and reject duplicate event IDs without applying progress twice.

User JWT authentication proves who triggered the request but does not independently prove the claimed domain event occurred. Initially, owning services will call the event endpoint while forwarding the user's JWT. Service-to-service authentication can be added later if stronger event provenance becomes necessary.

### 3. Backend API

- Add `GET /api/profile/fragment` to render the authenticated user's identity, notification preferences, and achievement progress for HTMX.
- Add `PUT /api/preferences` to validate and update the user's email and category preferences, returning an HTMX status fragment.
- Add `POST /api/events` for other services to report noteworthy events.
- Process each event atomically and return the updated and newly attained achievements.
- Add structured validation and appropriate `400`, `401`, `404`, `409`, and `503` responses.
- HTML-encode all values rendered into fragments.

### 4. Ollama and Email

- Add an Ollama client that calls `/api/chat` using Gemma 4 12B Q4_0.
- Give the model a constrained prompt containing the event, recipient name, and newly attained achievement details.
- Treat model output as untrusted content and constrain the generated subject and body lengths.
- Add a deterministic email template fallback so event processing and notification delivery can continue if Ollama is unavailable.
- Send generated messages through Gmail SMTP using MailKit with TLS.
- Configure SMTP host, port, username, password/app password, sender address, and sender name externally.
- Send only when the recipient has enabled the matching notification category.
- Record generated content, attempts, delivery status, timestamp, and errors in `Notifications`.

### 5. Docker Compose

- Add an Ollama service and a persistent model volume.
- Add a model initialization container or startup command that pulls the confirmed Gemma 4 12B Q4_0 Ollama tag.
- Add Ollama health checks and backend dependency wiring.
- Configure backend URLs for PostgREST and Ollama.
- Configure JWT key mounting and Gmail SMTP settings without committing credentials.
- Preserve the existing PostgreSQL, PostgREST, backend, and frontend service boundaries.

### 6. Frontend

- Replace the sample-items page with an HTMX dashboard.
- Display the username from the authenticated JWT.
- Provide an editable notification email field.
- Provide checkboxes for post engagement, course completion, quiz results, streaks, and achievements.
- Display achievement images, names, progress bars, progress totals, and attained state.
- Include loading, signed-out, save-success, validation-error, and service-error states.
- Continue using the shared CSS copied into this frontend; extend the local theme only where form and progress styles are missing.
- Keep the page responsive and accessible with labelled controls and meaningful progress elements.

### 7. Tests and Documentation

- Unit-test JWT identity extraction and endpoint authorization.
- Test preference validation and persistence.
- Test event validation, recipient handling, progress calculation, achievement unlocking, and duplicate-event idempotency.
- Test notification-category filtering.
- Test Ollama request/response handling and deterministic fallback generation.
- Test MailKit delivery behavior through an injectable abstraction without contacting Gmail in unit tests.
- Test HTML rendering and encoding.
- Add database integration coverage for atomic progress updates and achievement unlocking.
- Run `dotnet test`, container health checks, and an event-to-email smoke test.
- Verify the frontend at desktop and mobile widths.
- Update project documentation with startup instructions, required SMTP configuration, example event requests, and API contracts.

## Proposed Event Contract

```json
{
  "eventId": "forum-like-post-123-user-7",
  "eventType": "post-engagement",
  "subjectId": "post-123",
  "recipientUserId": 7,
  "occurredAt": "2026-08-27T10:00:00Z",
  "value": 1,
  "metadata": {
    "action": "like",
    "totalLikes": 10
  }
}
```

The backend derives the actor user ID from the JWT and never trusts an actor ID supplied by the caller.

## Suggested Implementation Order

1. Database schema, seed data, and atomic event processing.
2. JWT authentication and backend data clients.
3. Profile, preferences, achievements, and event endpoints.
4. Ollama generation and MailKit delivery.
5. HTMX frontend and shared-theme extensions.
6. Docker Compose integration.
7. Automated tests, end-to-end validation, and README updates.
