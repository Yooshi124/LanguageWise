# Quests, Achievements and Notifications Service

Tracks achievement progress, keeps a history of notifications, and emails
learners about their activity according to their notification preferences.
It lives on the **Achievements & Notifications** page.

## For users

- The Achievements & Notifications page shows every achievement, your progress
  towards it, and whether you have earned it.
- Your notification history lists every notification LanguageWise has written
  for you, newest first, with its subject and message.
- Notification emails are written by AI to sound friendly and personal. If the AI
  is unavailable, a simpler standard message is used instead.

## Achievements you can earn

Achievements are grouped by the activity that advances them:

- **Lessons**: First Lesson (1 lesson), Committed Learner (5), Lesson Scholar (20).
- **Courses**: First Course (1 course), Course Explorer (3), Course Champion (5).
- **Quizzes**: First Quiz (1 quiz), Quiz Regular (10), Quiz Veteran (50).
- **Mini games**: First Game Win (1 win), Game Night (10), Games Master (50).
- **Community contributions** (for example forum posts): First Contribution (1),
  Community Regular (10), Community Champion (50).
- **Engagement on your posts** (likes and comments from others): First Reaction
  (1), Conversation Starter (10), Community Favourite (50).
- **Login streaks**: Three Day Streak (3 consecutive days), Seven Day Streak (7),
  and Longest Login Streak, which records your best streak rather than having a
  target.

Progress never goes backwards, and an achievement is earned the moment its
target is reached.

## Notification preferences

- A **master switch** turns all notification emails on or off. When it is off,
  nothing is emailed, but notifications still appear in your history.
- You need a **notification email address** before emails can be sent.
- Each category can be turned on or off separately: community contributions,
  post engagement, lesson completion, course completion, quiz results, mini game
  wins, and login streaks.
- **New achievements** is a special category: when it is on, you are emailed
  whenever you earn an achievement, even if that activity's own category is off.
- Turning notifications back on after they were off sends a welcome message
  explaining what you will be notified about.

## For agents

Garry can use these MCP tools (tool scope `quests`) on behalf of the signed-in
learner. Each tool only ever sees or changes that learner's own data.

- `quests_get_my_preferences` (read-only): the master switch, notification email
  address, and every category setting.
- `quests_get_my_achievements` (read-only): every achievement with the learner's
  progress, its target (none for record-style achievements such as Longest Login
  Streak), and whether it has been earned.
- `quests_set_notifications_enabled(enabled)`: turns the master switch on or off,
  keeping category settings. Turning it on from off sends the welcome message.
- `quests_update_my_preferences(...)`: turns one or more categories on or off
  (`communityContribution`, `postEngagement`, `lessonCompletion`,
  `courseCompletion`, `quizResult`, `minigameWin`, `loginStreak`,
  `achievements`); omitted categories are left unchanged.

The two tools that change settings need a notification email address to already
be set, and should only be used when the learner explicitly asks for the change.
In Garry's tools panel on this page, quick actions show achievements and
preferences, and pause or resume notifications.

## TECHNICAL-architecture: Components and data flow

- **Backend**: ASP.NET Core (.NET 10) minimal API,
  `LanguageWise.QuestsAchievementsNotificationsService.Api`. Container
  `quests-achievements-notifications-service-backend`, port 8080, published on
  host port 5004 on all interfaces (other services bind to 127.0.0.1 only).
- **Database**: PostgreSQL (`quests-achievements-notifications-service-db`) with
  the service-owned `api` schema, exposed to the backend by PostgREST
  (`quests-achievements-notifications-service-db-api`, port 3000, not published
  to the host). The backend talks to it through `AppDataClient` over PostgREST
  query strings (for example `user_preferences?user_id=eq.{id}`), and upserts use
  `Prefer: resolution=merge-duplicates`.
- **Email content**: `OllamaEmailGenerator` calls Ollama `api/chat` with model
  `gemma4:e4b` (15 s timeout) and falls back to a fixed template on any failure.
- **Email delivery**: `GmailEmailSender` uses MailKit over SMTP with STARTTLS.
  Delivery is skipped when SMTP credentials are not configured.
- **Frontend**: Vue feature remote served from `/remotes/quests-achievements/`;
  the shared gateway proxies `/quests-and-achievements/api/*` to the backend.
- **Garry**: `POST /api/assistant/messages` builds a prompt containing the
  learner's canonical profile (preferences, achievement progress, full
  notification history) and streams the completion from `garry-ai-service`,
  passing `toolScope: "quests"` when MCP is enabled.
- **MCP**: `McpToolClient` connects to the host MCP server
  (`http://host.docker.internal:8200/mcp`) with the shared API key; the MCP server
  calls back into this backend on `http://localhost:5004`.

## TECHNICAL-api: Backend endpoints

All `/api/*` endpoints require a JWT; `GET /health` is anonymous.

- `GET /api/profile`: username, preferences, achievement progress, and
  newest-first notification history.
- `GET /api/preferences`: preferences only (defaults to everything on and no
  email when the learner has no row). Used by the MCP tools.
- `GET /api/achievements`: every achievement with `achievementId`, `name`,
  `description`, `progress`, and `progressNeeded`. Used by the MCP tools.
- `PUT /api/preferences`: full replacement of the preferences (every flag plus a
  valid `email`, which is required). If `notifyAll` changes from false to true it
  also writes a `notifications-enabled` notification and sends a welcome email.
- `POST /api/events`: `{ trigger, subject, recipientUserId, recipientName, value? }`.
  Called by the shared, chat-discussion, quizzes-courses, and mini-games backends,
  which forward the acting user's JWT.
- `POST /api/assistant/messages`: server-sent events `delta`, `tool`, `done`,
  and `error`.
- `GET /api/assistant/tools` and `POST /api/assistant/tools/{name}`: proxy to the
  MCP server. Tool names must match `^quests_[a-z_]{1,58}$`, and arguments must
  be a JSON object of at most 2048 bytes. Returns 503 with code `mcp_disabled`
  when MCP is off, or 502 with `mcp_unavailable` when it cannot be reached.

## TECHNICAL-data-model: Database tables

- `api.achievements(achievement_id, name unique, description, image, trigger,
  progress_needed)`. `progress_needed` is either positive or `-1`; `-1` marks a
  record-style achievement that stores a best value and is never "earned".
  Seeded with 21 achievements.
- `api.user_achievements(user_id, achievement_id, progress)`, with primary key
  `(user_id, achievement_id)`.
- `api.user_preferences(user_id pk, email, notify_all,
  notify_community_contribution, notify_post_engagement, notify_lesson_completion,
  notify_course_completion, notify_quiz_result, notify_minigame_win,
  notify_login_streak, notify_achievements)`, with every flag defaulting to true.
- `api.notifications(notification_id, user_id, trigger, time, email_subject,
  email_body)`.
- The PostgREST anonymous role `web_anon` has `SELECT, INSERT, UPDATE` on every
  table in `api`.

## TECHNICAL-event-processing: How events update progress and send email

1. Validate the event. `trigger` must be one of `community-contribution`,
   `post-engagement`, `lesson-completion`, `course-completion`, `quiz-result`,
   `minigame-win`, or `login-streak`; `subject` and `recipientName` are required;
   `recipientUserId` must be positive; `value` must not be negative.
2. Load the achievements for the trigger (404 if none are configured).
3. For each one, compute the new progress (`NotificationRules.CalculateProgress`).
   The target is `value` when supplied, otherwise the old progress + 1. Progress
   never decreases, is capped at `progress_needed`, and `NewlyAttained` is set
   when it crosses the target. Record-style achievements (`-1`) keep the maximum.
4. Generate the email, always insert a notification row, then upsert progress.
5. Email only if `NotificationRules.ShouldNotify` passes (the master switch is on
   and either the trigger's category is on, or an achievement was newly attained
   and `notify_achievements` is on), the recipient has an email address, and SMTP
   is configured. Send failures are logged and returned as `email.error`; they
   never fail the request.
6. Any database failure returns 503.

## TECHNICAL-security: Authentication and trust model

- JWTs are RS256, issued by `shared-backend`, and verified with the public key at
  `Auth:VerificationKeyPath`. The token is read from the `Authorization: Bearer`
  header or the `token` cookie. A fallback authorisation policy requires an
  authenticated user everywhere except `/health`. The user id is the numeric
  `sub` claim.
- `POST /api/events` trusts the caller's choice of `recipientUserId`, so that
  post engagement can notify another user. There is no service-to-service
  credential, so any signed-in user can call it directly and advance another
  user's achievements or trigger notification emails to them. Hardening option:
  require a service credential for events, or restrict the recipient to the actor
  for self-only triggers.
- PostgREST requires no authentication and grants `web_anon` write access, so any
  container on the Compose network can read or modify this service's data
  directly.
- SMTP credentials come only from `backend/.env` (`Smtp__Host`, `Smtp__Port`,
  `Smtp__Username`, `Smtp__Password`, `Smtp__FromName`), which is git-ignored
  and excluded from the Docker build context. Never copy their values into
  documentation or logs.
- MCP tools run with the learner's own JWT, forwarded as
  `X-LanguageWise-User-Token`, plus the shared `X-LanguageWise-Mcp-Key`. The
  MCP server only exposes `quests_*` tools to this scope. The settings-changing
  tools are guarded by a Garry system-prompt rule to act only on the learner's
  explicit request; there is no confirmation step, so prompt injection through
  notification text is the main residual risk. The impact is limited to the
  learner's own notification settings.
- Notification bodies are AI-generated and included in Garry's canonical
  context, where they are treated as untrusted data.

## TECHNICAL-operations: Configuration, running, and tests

- Configuration: `Services__Database`, `Services__Ollama`, `Services__Garry`,
  `Ollama__Model`, `Auth__VerificationKeyPath`, `Mcp__Enabled`, `Mcp__Endpoint`,
  `Mcp__ApiKeyPath` (Compose secret `mcp_api_key`), `Mcp__TimeoutSeconds`
  (default 15), and the `Smtp__*` values above.
- Garry's tools need the MCP server running on the host
  (`cd mcp-server/src/LanguageWise.McpServer; dotnet run`) and
  `mcp-server/.mcp-api-key` generated by `mcp-server/scripts/New-McpApiKey.ps1`.
  Without them, the tools endpoint reports `mcp_disabled` or `mcp_unavailable`.
- Tests: `dotnet test` on `LanguageWise.QuestsAchievementsNotificationsService.BE.slnx`.
  Tests for the MCP tools live in `mcp-server/tests` (`McpEndpointTests`).
