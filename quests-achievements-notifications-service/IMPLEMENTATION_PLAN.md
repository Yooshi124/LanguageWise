# Notification History and Trigger-Based Achievement Plan

## Goal

Change event processing so callers identify an activity by trigger rather than achievement ID. One event updates every achievement tier mapped to that trigger, always generates and stores notification content, optionally sends that content by email, and exposes notification history on the Quests & Achievements page.

## Confirmed Current Behaviour

- `POST /api/events` currently requires one `achievementId`, so one request can update only one achievement.
- `achievements` has no trigger column.
- `notifications.email` currently stores the recipient email address.
- Notification content is generated only when preferences permit email delivery and SMTP is configured.
- The profile response and frontend do not expose notification history.

## Decisions

- Use these canonical trigger values in API and database records:
  - `course-completion`
  - `post-engagement`
  - `quiz-result`
  - `streak`
- Remove `achievementId` from `EventRequest`; rename `eventType` to `trigger` for one consistent term.
- Apply an event's positive `value` to every achievement with the matching trigger, capping each row at its own `progress_needed` value.
- Generate one notification per event. Its content summarizes all affected achievements and highlights newly attained tiers.
- Generate and store notification content regardless of notification preferences, email address, SMTP configuration, or delivery outcome.
- Use preferences only to decide whether the stored notification is also sent by email.
- Keep `event_id` unique so retries return `409 Conflict` and do not increment progress twice or create duplicate history entries.
- Update the fresh schema and seed scripts directly; do not add migration-only `ALTER TABLE` statements.

## 1. Database Schema and Seed Data

Update `database/sql/schema.sql`:

- Add `trigger text NOT NULL` to `api.achievements`.
- Add an index on `api.achievements(trigger)` for event lookup.
- Replace `api.notifications.email` with:
  - `email_subject text NOT NULL`
  - `email_body text NOT NULL`
- Retain `event_id`, `user_id`, `trigger`, and `time` for idempotency, ownership, filtering, and display ordering.

Update `database/sql/seed.sql` so achievement tiers share triggers:

| Trigger | Achievements |
| --- | --- |
| `course-completion` | First Course, Course Explorer, Course Champion |
| `post-engagement` | First Applause, Crowd Pleaser, Community Favourite |
| `quiz-result` | Quiz Starter, Quiz Master |
| `streak` | Three Day Streak, Seven Day Streak |

Recreate the development database volume when validating because this project initializes a fresh schema rather than applying migrations.

## 2. Backend Models and Data Client

Update `Models/ApiModels.cs`:

- Add `Trigger` to `Achievement`.
- Change `EventRequest` to accept `Trigger` and remove `AchievementId`/`EventType`.
- Replace `NotificationInput.Email` with `EmailSubject` and `EmailBody`.
- Add a notification response model containing ID, event ID, trigger, time, subject, and body.
- Add a response model for each affected achievement's new progress and attainment state.

Update `Clients/AppDataClient.cs`:

- Replace `GetAchievementAsync(achievementId)` with `GetAchievementsByTriggerAsync(trigger)`.
- Add a bulk progress upsert accepting all affected `UserAchievement` rows in one PostgREST request.
- Update notification insertion for subject/body fields.
- Add `GetNotificationsAsync(userId)`, ordered by `time.desc,notification_id.desc`.
- Add an event-ID existence check so obvious retries are rejected before invoking Ollama; retain the unique database constraint for concurrent retries.

## 3. Event Processing

Refactor `POST /api/events`:

1. Authenticate the actor and validate event ID, trigger, subject ID, recipient user ID, occurrence time, value, and metadata.
2. Reject unsupported triggers using the canonical trigger set.
3. Reject an already-recorded event with `409 Conflict` before generating content.
4. Load every achievement mapped to the trigger; return `404` if none are configured.
5. Load the recipient's current progress and calculate the capped update for each matched achievement.
6. Generate one notification subject/body from the trigger, subject ID, and complete list of affected/newly attained achievements. Use the deterministic fallback if Ollama fails.
7. Insert the notification history record with generated subject/body.
8. Bulk-upsert all affected progress rows.
9. Evaluate email preferences after storage. Send the same stored subject/body only when the trigger is enabled, an email address exists, and SMTP is configured.
10. Return all affected achievement results plus notification generation and email-delivery status.

Missing preferences or a missing email address must no longer prevent progress updates or notification-history creation.

### Consistency Constraint

PostgREST cannot make the notification insert and progress upsert atomic across separate HTTP requests. Keep the existing event-first ordering and unique `event_id` guard for this MVP, and document the partial-failure limitation. Do not introduce a database RPC or direct PostgreSQL credentials without a separate architecture decision.

## 4. Notification Generation

Update `EmailContext` and `OllamaEmailGenerator` to accept a collection of affected achievements rather than one achievement.

The prompt and fallback must:

- Produce one concise subject and body for the event.
- Mention newly attained achievements when present.
- Otherwise summarize progress toward the relevant tiers.
- Continue enforcing subject/body length limits.
- Produce content independently of whether email delivery is enabled.

## 5. Profile and Notification API

Add notification history to the authenticated user experience.

Preferred MVP approach:

- Include `notifications` in `GET /api/profile` alongside preferences and achievement progress.
- Return only notifications where `user_id` matches the authenticated JWT subject.
- Order newest first.

This avoids another frontend request and keeps initial dashboard rendering within the existing profile load.

## 6. Frontend Notification History

Extend the Quests & Achievements dashboard:

- Add a full-width **Past notifications** section below the preferences/achievements grid.
- Render notifications newest first.
- Show each notification's subject as the primary list action, with trigger and formatted time as secondary text.
- Show an empty state when the user has no notifications.
- Add one native `<dialog>` modal reused for all rows.
- Clicking a subject opens the dialog with the stored subject, body, trigger, and time.
- Include a clear close button and support native Escape/backdrop behaviour.
- Populate all text with `textContent`, not HTML insertion.

Add the required component styles to `shared/frontend/css/theme.css`; do not create a service-local stylesheet.

## 7. Tests

Update unit tests for:

- Event validation with `trigger` and no `achievementId`.
- Trigger-to-multiple-achievement progress calculations.
- Notification generation context containing multiple tiers.
- Notification subject/body serialization and history query ordering.
- Missing preferences/email still producing progress and stored notification content.
- Preferences controlling delivery only, not generation/storage.

Update the Docker-backed integration test to verify:

1. A `course-completion` event updates First Course, Course Explorer, and Course Champion in one request.
2. Each tier is capped independently and newly attained tiers are reported correctly.
3. One notification row stores non-empty `email_subject` and `email_body`.
4. The notification is stored when SMTP is disabled.
5. Replaying the same `event_id` returns `409` and does not change any progress or add history.
6. Notification history is returned only for the authenticated user and newest first.

Run the complete service suite with the existing command:

```powershell
dotnet test quests-achievements-notifications-service/LanguageWise.QuestsAchievementsNotificationsService.BE.slnx
```

## 8. Documentation and Validation

Update the service README:

- Replace the event request example with the trigger-only contract.
- Document canonical trigger values.
- Describe multi-tier progress updates.
- Clarify that in-app notifications are always generated/stored and email preferences affect delivery only.
- Update the profile response example to include notification history.

Final validation:

- Recreate the quests database from the updated schema/seed files.
- Run all tests.
- Build the database, backend, frontend, and shared frontend images.
- Exercise one event for each trigger.
- Confirm notification history and modal behaviour on desktop and mobile.
- Confirm email-disabled users still receive in-app history.
- Confirm duplicate events remain idempotent.

## Acceptance Criteria

- Event callers provide no achievement ID.
- One event updates every configured achievement tier for its trigger.
- Generated notification subject/body are persisted for every accepted event.
- Email delivery uses the persisted content and remains optional.
- Authenticated users can view all their past notifications and open the full body in a modal.
- Duplicate event IDs neither update progress nor create duplicate notifications.
- Existing authentication, preferences, shared navigation, and shared-CSS behaviour continue to work.
