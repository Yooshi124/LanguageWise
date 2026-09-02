# LanguageWise Federated Frontend Migration Plan

## Status

Planning only. `leaderboard-analytics-service` is now present with a frontend,
backend, database, Docker wiring, and backend tests. The migration can begin
with Phase 0 baseline verification.

The completed system will contain one shared host application and five feature
remotes:

1. Quizzes and Courses
2. Mini Games
3. Discussion Forum
4. Quests, Achievements, and Notifications
5. Leaderboard and Analytics

## Goal

Replace the current collection of independent HTMX/Vue pages with one Vue 3
SPA hosted by `shared-frontend`. The host owns the application shell,
navigation, authentication state, Vue Router, Vuetify setup, loading/error
boundaries, and all authored CSS. Each feature frontend remains independently
built and served by its own Docker container, but exposes a Vue feature module
through `@module-federation/vite` for the host to load dynamically.

The visual baseline is the current Quizzes and Courses frontend: its sidebar,
top bar, spacing, typography, colour usage, responsive behavior, and Vuetify
controls should become the shared design language.

## Current-State Findings

### Frontend stacks

| Application | Current stack | Shell/routing | Main issue |
| --- | --- | --- | --- |
| Shared | Static HTML, HTMX, vanilla JS | Full-page links | No SPA host or Vue toolchain |
| Quizzes and Courses | Vue 3, TypeScript, Vue Router, Vuetify | Own sidebar/top bar/router | Best visual baseline, but duplicates shell/auth |
| Mini Games | Vue 3, JavaScript | Own sidebar and manual pathname routing | Duplicates shell/auth and has no Vue Router |
| Discussion Forum | Vue 3, JavaScript, Vue Router | Own header/nav/router | Different layout and auth conventions |
| Q/A/N | Static HTML, HTMX, vanilla JS | One dashboard page | Requires complete Vue conversion |
| Leaderboard and Analytics | Vue 3, TypeScript, Vuetify, TanStack Query, Highcharts | Own sidebar and single view | Duplicates shell/auth/CSS and needs host query-provider integration |

Auth is currently checked in three incompatible ways:

- Shared `POST /api/check-login` returns only a JSON string containing the name.
- Quizzes and Courses `GET /api/me` returns `{ id, username }`.
- Discussion Forum `GET /api/me` returns `{ id, username }`.
- Leaderboard and Analytics `GET /api/me` returns `{ id, username }`.
- Mini Games calls the shared check directly and constructs a username-only user.
- Q/A/N infers signed-in state from `GET /api/profile` and renders an HTMX auth fragment.

Navigation, login URLs, logout behavior, app shell markup, icon handling, and
responsive sidebar behavior are duplicated across feature frontends. Authored
CSS is spread across shared `theme.css`, three service-level stylesheets, many
Vue single-file component `<style>` blocks, and inline login-page CSS.

### Existing gateway behavior to preserve

`shared-frontend` nginx is already the browser's single origin at
`http://localhost:3000`. It proxies each service API to the owning backend.
The Quizzes and Courses assistant endpoint has special 120-second unbuffered
streaming behavior; that location must remain more specific than its general
API proxy.

Feature APIs and data ownership do not move into the shared backend. Only
session discovery is consolidated there. Feature backends that already validate
JWTs retain that authorization, and Mini Games must stop trusting its current
caller-supplied/localStorage user ID by adopting the same JWT identity model.

## Target Architecture

### Runtime topology

```text
Browser
  |
  v
shared-frontend nginx :3000
  |-- /, /login, /quizzes-and-courses/*, ... -> shared Vue SPA
  |-- /api/*                                  -> shared-backend
  |-- /<feature>/api/*                        -> owning feature backend
  `-- /remotes/<feature>/*                    -> owning frontend container
                                                    |
                                                    `-- remoteEntry.js + chunks
```

The frontend containers remain separate Docker services. They do not get
copied into the shared image. This preserves team ownership and independently
buildable artifacts while keeping every browser request same-origin through
the gateway.

### Federation contract

Use `@module-federation/vite` in the shared host and every feature remote.
Pin one tested version of Vite, the federation plugin, Vue, Vue Router,
Vuetify, and `@mdi/js` across all frontends before migration.

Each feature remote exposes a small module with:

- A named root component, for example `QuizzesCoursesComponent`.
- Route records beneath its assigned host path where the feature has internal
  navigation.
- Feature metadata: key, display name, icon, base path, and whether auth is
  required.
- A typed root-prop contract containing the authenticated user and host actions
  needed by the feature.

The shared host owns the only Vue application, Vue Router, Vuetify instance,
`v-app`, sidebar, and top-level error/loading boundary. Remotes must not call
`createApp`, install their own router/Vuetify instance, render a second shell,
or perform their own session check when consumed by the host. Standalone entry
files may remain for local remote development, but they must be adapters around
the same exposed feature component.

Share at least `vue`, `vue-router`, `vuetify`, and `@mdi/js` as singleton
dependencies. The host must also install a TanStack Query provider and share
`@tanstack/vue-query` as a singleton for the Leaderboard component. Do not use
`eager` sharing unless the federation spike proves it necessary. Remotes may
bundle feature-only dependencies such as `markdown-it` and `highcharts`.

### Host and routing contract

The shared host owns browser history and these top-level paths:

- `/` - shared home
- `/login` - shared sign-in view
- `/quizzes-and-courses/*`
- `/mini-games/*`
- `/chat-discussion/*`
- `/quests-and-achievements/*`
- `/analytics/*`

The federation spike must prove how remote child route records are registered
under those paths before broad migration begins. Deep links, browser refresh,
back/forward navigation, and remote-to-remote sidebar navigation must all use
the single host router. No remote may create a second web-history router.

Use same-origin, stable remote URLs:

```text
/remotes/quizzes-courses/remoteEntry.js
/remotes/mini-games/remoteEntry.js
/remotes/chat-discussion/remoteEntry.js
/remotes/quests-achievements/remoteEntry.js
/remotes/leaderboard-analytics/remoteEntry.js
```

`remoteEntry.js` must be served with revalidation/no-cache headers. Hashed
chunks may be immutable. The host must show a feature-specific retry/error view
if one remote is unavailable instead of crashing the entire SPA.

Each remote build must set and verify a production public path rooted at its
own `/remotes/<feature>/` namespace. `remoteEntry.js`, lazy JavaScript chunks,
imported images, fonts, and other emitted assets must all resolve under that
namespace. Legacy application bases such as `/quizzes-and-courses/` must not be
embedded in remote chunk URLs because those paths will belong to the host SPA.

### API paths

Remote code must not derive API URLs from the federation asset base. Use the
stable same-origin API prefix assigned to the feature:

- `/quizzes-and-courses/api`
- `/mini-games/api`
- `/chat-discussion/api`
- `/quests-and-achievements/api`
- `/analytics/api`

This separates bundle loading (`/remotes/...`) from API ownership and avoids
`import.meta.env.BASE_URL` coupling. Static feature assets should either be
imported so Vite emits them with the remote chunks or use an explicit remote
asset base.

### Authentication contract

Keep one session endpoint in `shared-backend`:

```http
POST /api/check-login
Cookie: token=<HttpOnly JWT>

200 OK
{
  "id": 7,
  "name": "amber"
}
```

The browser contract is cookie-only; remove support for supplying a JWT in a
JSON request body once no caller needs it. Invalid, absent, or expired cookies
return `401`. Refactor token validation to return both the numeric `sub` claim
and name claim, and retain signature and lifetime validation.

The host calls this endpoint once during bootstrap and owns the reactive auth
state. It also owns login, logout, return URL handling, signed-out views, and
route guards. It passes `{ id, name }` and required host actions to remotes.
Feature backends continue validating the JWT independently for API
authorization. Mini Games is the current exception and must add JWT validation,
derive user identity from the `sub` claim for user-specific operations, and
reject caller-controlled user IDs before its frontend migration is complete.

After all consumers migrate:

- Delete Quizzes and Courses `GET /api/me`.
- Delete Discussion Forum `GET /api/me`.
- Delete Leaderboard and Analytics `GET /api/me`.
- Delete shared `POST /api/check-login/fragment`.
- Delete remote auth composables and hard-coded login/logout URLs.
- Remove HTMX from shared and Q/A/N frontends.

### CSS ownership

All authored CSS lives under `shared/frontend` and is imported only by the
host. The host also imports `vuetify/styles` and supplies the shared Vuetify
theme. Remote builds must not emit or inject authored CSS.

Migration work includes:

- Move the Quizzes and Courses shell/theme styles into the shared host first.
- Move Mini Games and Discussion Forum service styles and every component
  `<style>` block into host-owned, feature-namespaced stylesheets.
- Move Leaderboard and Analytics `styles.css`, duplicated public `theme.css`,
  component `<style>` blocks, and inline style attributes into the host.
- Move Q/A/N dashboard, achievement, preference, and notification styles into
  the same host styling structure.
- Move login-page inline CSS into the host stylesheets.
- Prefix feature selectors with a stable root class such as
  `.feature-quizzes` to prevent accidental cross-feature collisions.
- Remove remote links/imports of `theme.css`, `styles.css`, and SFC style
  blocks after visual parity is verified.

The DOM boundary created by Vue components does not block normal CSS
inheritance. Vuetify theme variables and global host styles therefore apply to
remote components as long as remotes use the host's Vue/Vuetify instances and
do not use Shadow DOM.

## Implementation Phases

### Phase 0 - Readiness verification and baseline

- Verify the now-present Leaderboard and Analytics frontend, backend, database,
  Docker services, and tests as part of the full-stack baseline.
- Record its current functionality: authenticated personal language rankings,
  deterministic 30-day lesson series for six courses, a Highcharts line chart,
  and an Ollama-generated summary containing summary text, trend, and best
  course with a deterministic fallback.
- Correct the shared endpoint reference table to use the Leaderboard database's
  actual host port `5006` instead of the duplicated Mini Games port `6005`.
- Record all six frontend dependency versions and select one compatible set.
- Include Leaderboard's `@tanstack/vue-query`, `highcharts`, Vue 3.5.18,
  Vuetify 3.9.4, and Vite 7 toolchain in compatibility decisions.
- Run every existing backend test suite and every frontend production build.
- Capture desktop and mobile screenshots of each current workflow.
- Record deep links and API calls for each workflow as regression fixtures.
- Confirm a clean `docker compose up -d --build` baseline.

Exit criteria: all five services work before migration and their behavior can
be compared objectively afterward.

### Phase 1 - Federation and router spike

- Create a minimal Vite/Vue host in `shared/frontend`.
- Add `@module-federation/vite` to the host and one temporary/reference remote.
- Serve that remote's `remoteEntry.js` through shared nginx at `/remotes/...`.
- Prove singleton Vue, Vue Router, Vuetify, and icon dependencies.
- Prove dynamic child-route registration, direct deep-link refresh, navigation,
  lazy chunk loading, and remote load failure recovery.
- Prove all remote chunks and imported assets load from the remote's dedicated
  `/remotes/<feature>/` public path in a production build.
- Prove that the remote renders with host-only CSS and emits no authored CSS.
- Verify production Docker builds, not only Vite development servers.
- Document the final federation module interface before migrating features.

Exit criteria: a production-built reference component works through nginx with
one router/Vuetify instance and a documented repeatable remote template.

### Phase 2 - Shared authentication backend

- Change shared token validation to return an authenticated-user record.
- Change `POST /api/check-login` to return `{ id, name }` from the cookie.
- Remove request-body token handling.
- Add tests for valid cookie, missing cookie, invalid signature, expired token,
  missing/invalid subject, and missing name.
- Keep `/api/login` and `/api/logout`, preserving the HttpOnly cookie behavior.
- Do not delete `/api/me` or the fragment endpoint until all consumers move.

Exit criteria: the new endpoint contract is tested and temporarily coexists
with old consumers.

### Phase 3 - Shared Vue host and visual system

- Add `package.json`, lockfile, Vite config, TypeScript config, Vue entry point,
  Vue Router, and federation host config under `shared/frontend`.
- Convert the shared Dockerfile to a Node build stage plus nginx runtime stage.
- Build the shared shell from the Quizzes and Courses sidebar/top-bar design.
- Centralize the navigation registry for all five services.
- Create host auth state, route guards, login view, logout action, and return URL
  handling.
- Convert the current shared home/sample-items view to Vue or explicitly remove
  it only after a product decision; do not lose it accidentally.
- Add accessible loading, signed-out, missing-remote, and general error states.
- Move all common CSS, Vuetify setup, icons, and assets into the host.
- Preserve `/login.html` as a temporary redirect to `/login` during rollout.

Exit criteria: the host shell and auth work without any feature remote and
match the Quizzes and Courses visual baseline on desktop and mobile.

### Phase 4 - Quizzes and Courses reference remote

- Add the federation plugin and expose `QuizzesCoursesComponent` plus its route
  records.
- Separate the current `App.vue` shell from the feature content.
- Remove its sidebar, top bar ownership, auth check, logout behavior, and
  top-level `v-app`.
- Register course, lesson, quiz, flashcard, completion, and assistant behavior
  beneath the host path.
- Replace API URLs derived from `BASE_URL` with the stable feature API prefix.
- Preserve Garry Assistant streaming and its nginx timeout/buffering settings.
- Move all authored CSS to shared and verify visual parity first.
- Add the `/remotes/quizzes-courses/` gateway proxy and switch
  `/quizzes-and-courses/*` page routes to the host SPA as part of this phase;
  keep `/quizzes-and-courses/api/*` routed directly to the backend.
- Retain a thin standalone dev bootstrap if useful to that service team.

Exit criteria: all current Quizzes and Courses routes and workflows operate as
a federated component and establish the template for other remotes.

### Phase 5 - Mini Games remote

- Add federation configuration and expose `MiniGamesComponent` and routes.
- Replace manual pathname selection with host Vue Router route records.
- Remove the duplicated sidebar/navigation and remote auth composable.
- Add JWT bearer/cookie validation to the Mini Games backend, derive the user ID
  from the authenticated `sub` claim, remove the localStorage/default user ID,
  and reject caller-supplied identity for user-specific operations.
- Add authorization and user-isolation tests before changing those API inputs.
- Preserve game selection, language/mode loading, completion statistics,
  Guess the Word, Word Search, Associations, help, generation, and error states.
- Move all global and scoped component CSS into host-owned namespaced files.
- Replace `BASE_URL` API and navigation assumptions with stable host routes.
- Add the `/remotes/mini-games/` gateway proxy and switch `/mini-games/*` page
  routes to the host SPA while retaining `/mini-games/api/*`.

Exit criteria: direct game links, gameplay, generated content, and completion
statistics behave as before inside the host shell.

### Phase 6 - Discussion Forum remote

- Add federation configuration and expose `ChatDiscussionComponent` and routes.
- Remove `AppHeader`, `AppNav`, duplicated auth state, and hard-coded shared API
  paths from the remote shell.
- Preserve forum index, forum filtering/search/sort/pagination, post detail,
  comments, likes, My Posts, create, edit, validation, and ownership rules.
- Preserve the current frontend behavior in which every Discussion route
  requires login, even though the backend permits anonymous reads. Express the
  requirement in host route metadata and retain backend write authorization.
- Add explicit signed-out route and protected-action tests so a later decision
  to expose anonymous reads can be made separately.
- Move every scoped style block into shared feature-namespaced CSS.
- Add the `/remotes/chat-discussion/` gateway proxy and switch
  `/chat-discussion/*` page routes to the host SPA while retaining its API path.

Exit criteria: all forum routes and authorization-sensitive actions work from
the shared shell, including deep links and logout.

### Phase 7 - Q/A/N Vue and federation migration

- Scaffold a TypeScript Vue/Vite remote using the reference configuration.
- Expose `QuestsAchievementsNotificationsComponent` and its route records.
- Replace static HTML, HTMX, template cloning, and imperative DOM mutation with
  Vue components and reactive state.
- Preserve the complete current behavior:
  - authenticated profile loading and username display;
  - completed/total achievement summary;
  - achievement cards, progress, earned state, and image fallback;
  - email address and all notification preference toggles;
  - master toggle disabling the individual options without clearing their
    checked values;
  - save loading, success, validation, and failure feedback;
  - notification count, empty state, newest-first list, date formatting;
  - notification detail dialog, backdrop click, Close, and Escape behavior;
  - service loading, signed-out, and unavailable states.
- Keep `/api/profile`, `/api/preferences`, and `/api/events` semantics intact.
- Send preferences as JSON; retain form support temporarily until rollout is
  complete, then remove it only with backend test updates.
- Move all Q/A/N CSS and fallback assets to the appropriate host/remote asset
  ownership location without changing the visual content.
- Add the `/remotes/quests-achievements/` gateway proxy and switch
  `/quests-and-achievements/*` page routes to the host SPA while retaining its
  API path.
- Remove `htmx.min.js`, the vanilla `app.js`, and static templates only after
  Vue parity tests pass.

Exit criteria: profile, achievements, preferences, and notification history
have feature parity in Vue and match the shared Quizzes-style UI.

### Phase 8 - Leaderboard and Analytics remote

- Add federation configuration and expose the single-view
  `LeaderboardAnalyticsComponent` at `/analytics`.
- Separate the current `App.vue` shell from `HomeView` and remove the local
  sidebar, navigation registry, auth composable, top-level `v-app`, Vuetify
  instance, and duplicated icon ownership.
- Use the host's `{ id, name }` auth state and remove the feature `/api/me`
  endpoint only after the federated consumer is live.
- Install the TanStack Query provider in the host, share
  `@tanstack/vue-query` as a singleton, and preserve the existing query keys,
  five-minute AI-summary stale time, and disabled focus refetch.
- Keep Highcharts feature-local. Preserve UTC date conversion, six course
  series, integer y-axis, shared tooltip, empty/loading/error states, chart
  destruction on unmount, and responsive reflow when the host sidebar or
  viewport changes the content width.
- Preserve personal language ranking loading/error/empty states, rank badges,
  scores, and authenticated user isolation through
  `/analytics/api/my-language-rankings`.
- Preserve AI summary loading/error/empty states, trend labels and glyphs, best
  course, structured Ollama output, and deterministic backend fallback.
- Benchmark the summary endpoint with the warmed GPU model and set its Ollama
  client timeout below the gateway timeout with explicit persistence/response
  headroom; the current 30-second client and 30-second proxy budgets are too
  tightly coupled. Add timeout/fallback coverage.
- Replace `BASE_URL` API construction with the stable `/analytics/api` prefix.
- Move `styles.css`, public `theme.css`, all scoped styles, and inline layout
  styles into shared host-owned `.feature-analytics` CSS.
- Keep existing language-ranking and discussion-ranking backend APIs intact
  even though the current frontend only displays personal language rankings.
- Add missing backend coverage for language/discussion ranking proxy endpoints
  and frontend component coverage for ranking, chart, and summary states.
- Add its `/remotes/leaderboard-analytics/` proxy and switch `/analytics/*`
  page routes to the host SPA while retaining `/analytics/api/*`.

Exit criteria: the fifth remote loads at `/analytics`, uses host auth/query/UI
providers, and preserves ranking, chart, and AI-summary behavior with no local
shell or authored CSS.

### Phase 9 - Gateway and legacy cleanup

- Confirm every incrementally migrated `/remotes/<feature>/` proxy and SPA
  route is present; do not defer feature cutover until this phase.
- Confirm all non-API application paths use the shared SPA fallback while
  direct feature API proxies and the Quizzes SSE exception remain.
- Set `remoteEntry.js` to no-cache/revalidate and hashed chunks to immutable.
- Add frontend remote health checks that verify `remoteEntry.js` exists.
- Do not health-gate shared-host startup on remote frontend containers. Keep
  remote health checks for observability and full-stack validation while
  allowing the host and unaffected features to run when one remote is absent.
- Remove legacy full-page frontend proxy locations and obsolete standalone
  host-port assumptions.
- Delete service-local `/api/me`, shared auth fragment, duplicated auth/nav
  components, HTMX files, remote CSS, and dead nginx routes.
- Update Docker comments and all READMEs to describe host/remotes and dev flow.

Exit criteria: one browser SPA and five separately served remote artifacts
remain, with no legacy auth or navigation path in use.

## Verification Strategy

### Static and build checks

- `npm ci` and production `npm run build` in all six frontends.
- Type-check all TypeScript frontends.
- Assert every remote build contains `remoteEntry.js` and referenced chunks.
- Assert remote bundles do not emit authored CSS.
- Assert all frontends resolve one compatible Vue/Vue Router/Vuetify version.
- Run `docker compose config --quiet`.

### Backend checks

- Run all existing .NET service test projects.
- Add shared auth contract tests before changing frontend consumers.
- Retain Q/A/N event/progress/notification/SMTP/Ollama tests unchanged unless
  an API contract intentionally changes.
- Add tests proving removed `/api/me` and fragment routes are no longer relied
  upon before deleting them.

### Frontend component tests

Add Vitest and Vue Test Utils coverage for:

- Host auth bootstrap, login, logout, route guard, return URL, and one-check
  caching behavior.
- Sidebar selection, mobile drawer, remote loading, retry, and error isolation.
- Each remote's route module and root-prop/auth contract.
- Q/A/N profile rendering, preference updates, achievement states, image
  fallback, newest-first notification formatting, dialog interaction, and
  preservation of subordinate preference values while the master toggle is
  disabled.
- Leaderboard ranking, chart lifecycle/reflow, TanStack Query cache behavior,
  AI summary, fallback, and authenticated/signed-out states.
- Existing service-specific state transitions and API errors.

### Docker end-to-end checks

Use Playwright against `http://localhost:3000` at desktop and mobile sizes:

- Login once; verify `/api/check-login` returns ID/name and is not redundantly
  called by each remote.
- Open every sidebar destination without full-page reload.
- Refresh representative deep links in every remote.
- Exercise each feature's primary create/read/update workflow.
- Verify Quizzes assistant streaming still renders incrementally.
- Verify Q/A/N profile, achievements, preference save, notification list, and
  dialog.
- Verify Leaderboard personal rankings, the six-series 30-day chart, responsive
  chart resizing, AI summary/fallback states, and authenticated user isolation.
- Verify a deliberately unavailable remote shows only its fallback and other
  routes continue working.
- Verify no `remoteEntry.js`, chunk, asset, API, or source-map 404s.
- Verify no horizontal overflow, overlap, or unreadable text at target
  breakpoints and compare against baseline screenshots.
- Verify only shared-host CSS/Vuetify styles are loaded.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Nested/incompatible routers break deep links | Prove one-router route registration in Phase 1 before feature work |
| Vite 6/7 and plugin version mismatch | Pin one tested toolchain and lockfiles across all six frontends |
| Duplicate Vue or Vuetify runtime | Configure singleton sharing and verify one instance in runtime tests |
| Remote CSS leaks or disappears | Move styles incrementally, namespace by feature root, screenshot before deletion |
| Remote chunks load from host SPA paths | Give each remote an explicit `/remotes/<feature>/` production public path and test every emitted asset |
| `BASE_URL` points API calls at remote assets | Use explicit same-origin feature API prefixes |
| A remote outage crashes the SPA | Lazy load per route with host-owned timeout, retry, and error boundary |
| Stale `remoteEntry.js` references old chunks | Revalidate the entry; use hashed immutable chunks |
| Auth migration breaks writes | Change shared endpoint first, migrate consumers, delete old endpoints last |
| Q/A/N loses HTMX edge behavior | Component tests and an explicit parity checklist gate deletion |
| Mini Games accepts another user's ID | Add JWT validation and derive identity server-side before remote cutover |
| Leaderboard queries lack a provider in the host | Install and singleton-share TanStack Query before mounting the remote |
| Highcharts keeps stale dimensions or instances | Reflow on host layout changes and destroy the chart on unmount |
| Leaderboard AI call reaches the nginx deadline | Benchmark warm inference and leave explicit headroom below the proxy timeout |

## Deliberate Non-Goals

- Combining feature backends or databases.
- Moving feature API logic into the shared backend.
- Copying all remote builds into the shared frontend image.
- Introducing Shadow DOM or CSS-in-JS.
- Replacing working backend domain contracts solely for frontend consistency.
- Preserving duplicated standalone shells in production.

## Definition of Done

- `shared-frontend` is the only user-facing SPA and owns shell, auth, router,
  Vuetify, navigation, and CSS.
- Five feature frontend containers independently expose working federated Vue
  components through the shared nginx gateway.
- Switching sidebar destinations dynamically loads remotes without a page
  reload, while deep links and refresh work.
- The only session discovery endpoint is shared `POST /api/check-login`, which
  returns `{ id, name }`.
- Feature backends still own and authorize their APIs.
- Q/A/N retains profile, achievement, preference, notification, event, Ollama,
  and email behavior.
- The UI is visually consistent with the current Quizzes and Courses service
  on desktop and mobile.
- All backend tests, frontend tests/builds, federation smoke checks, and Docker
  end-to-end workflows pass.
