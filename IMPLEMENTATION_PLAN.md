# LanguageWise Federated Frontend Migration Plan

## Status

Phases 0 through 7 are complete. `leaderboard-analytics-service` is present
with a frontend, backend, database, Docker wiring, and backend tests. The
migration can proceed to the Phase 8 Leaderboard and Analytics remote.

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

The shared frontend has now adopted the Quizzes and Courses visual language in
Vue: its responsive sidebar, spacing, typography, colour usage, module cards,
and Vuetify controls are the host-shell baseline. Quizzes and Courses remains
the feature-content reference while Shared is the canonical shell reference.

## Current-State Findings

### Frontend stacks

| Application | Current stack | Shell/routing | Main issue |
| --- | --- | --- | --- |
| Shared | Vue 3, TypeScript, Vue Router, Vuetify, Vite | Own responsive sidebar/router/login | Host foundation, auth boundary, query provider, and federation runtime exist; feature migrations remain |
| Quizzes and Courses | Vue 3, TypeScript, Vue Router, Vuetify | Own sidebar/top bar/router | Best visual baseline, but duplicates shell/auth |
| Mini Games | Vue 3, JavaScript | Own sidebar and manual pathname routing | Duplicates shell/auth and has no Vue Router |
| Discussion Forum | Vue 3, JavaScript, Vue Router | Own header/nav/router | Different layout and auth conventions |
| Q/A/N | Static HTML, HTMX, vanilla JS | One dashboard page | Requires complete Vue conversion |
| Leaderboard and Analytics | Vue 3, TypeScript, Vuetify, TanStack Query, Highcharts | Own sidebar and single view | Duplicates shell/auth/CSS and needs host query-provider integration |

Auth is still checked in several ways during migration:

- Shared `POST /api/check-login` is cookie-only and returns `{ id, name }`.
  Shared retains the full authenticated user and bootstraps it once at the host
  boundary.
- Quizzes and Courses `GET /api/me` returns `{ id, username }`.
- Discussion Forum `GET /api/me` returns `{ id, username }`.
- Leaderboard and Analytics `GET /api/me` returns `{ id, username }`.
- Mini Games consumes the shared identity response but temporarily constructs a
  username-only local user until its remote migration.
- Q/A/N infers signed-in state from `GET /api/profile` and renders an HTMX auth fragment.

Shared now provides the intended canonical navigation, login/logout UI, app
shell, icon handling, and responsive sidebar behavior. Equivalent concerns are
still duplicated across feature frontends. Authored CSS is spread across the
new Shared `src/styles.css`, service-level stylesheets, many Vue single-file
component `<style>` blocks, and remaining inline/login styles.

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
- Remove HTMX from Q/A/N; Shared has already completed that cleanup.

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

#### Verified baseline

- All seven backend test projects pass: 88 passed, 0 failed.
- `docker compose config --quiet` passes and `docker compose up -d --build`
  completes successfully. All long-running containers are up and healthy;
  the one-shot `ollama-model-init` container exits as designed, and the Q/A/N
  database API is running without a configured health check.
- All six authenticated application entry points render through
  `http://localhost:3000`. Representative Quizzes, Mini Games, and Discussion
  deep links return their SPA entry points. Public APIs return `200`, while
  protected Q/A/N and Analytics APIs return `401` without a session.
- Desktop (1440 x 900) and mobile (390 x 844) screenshots for all six current
  frontends are stored in `docs/frontend-baseline`. No tested mobile entry
  point has horizontal overflow.
- Shared desktop and mobile screenshots were refreshed after its Vue/Vuetify
  redesign. The production Docker build and responsive shell both pass.
- The Analytics baseline renders personal French and Spanish rankings, six
  deterministic 30-day course series, and a warmed Ollama summary with summary
  text, trend, and best course.
- The shared endpoint table now uses the Leaderboard database host port `5006`.
- The configured corporate npm registry returned HTTP 403 for the federation
  package. Clean installs succeeded through `https://registry.npmjs.org`, and
  the resulting exact dependency versions are recorded in both lockfiles.
- Q/A/N currently makes ten avoidable `404` requests for root-relative
  achievement images before its fallback image renders. Analytics has no
  failed requests, but Highcharts reports that its accessibility module is not
  installed.
- Mini Games deep links render, but the seeded `amber` account cannot start a
  round: content mode returns `422 NO_VOCABULARY` because it has no unlocked
  course vocabulary, while AI mode returns `503 AI_UNAVAILABLE` when no
  OpenRouter key is configured. Both frontend error states work, but successful
  gameplay needs a deterministic test fixture before parity can be measured.

The exact common toolchain selected for Phase 1 is Vue 3.5.42, Vue Router
4.6.4, Vuetify 3.13.2, Vite 7.3.6, `@vitejs/plugin-vue` 6.0.8, `@mdi/js`
7.4.47, and TypeScript 5.8.3. These versions already coexist in the current
lockfiles and satisfy their recorded peer ranges. Keep TanStack Vue Query
5.102.8 shared for Analytics and Highcharts 11.4.8 feature-local. Do not pair
Vite 7 with the older `@vitejs/plugin-vue` 5.2.4 used by the Vite 6 projects.

### Phase 1 - Federation and router spike

- Use the existing Vite/Vue/Vuetify Shared host as the spike foundation; do not
  replace its current shell, home, login, router, or production build setup.
- First restore the selected pinned toolchain from an accessible npm registry
  and prove a clean, uncached install; do not rewrite lockfiles while registry
  access returns HTTP 403.
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

#### Verified Phase 1 result

- Shared and Quizzes/Courses clean installs and production builds pass with
  exact dependency pins. Their rebuilt Docker frontend containers are healthy.
- Quizzes/Courses exposes a CSS-free reference module; Shared dynamically adds
  its root and `details` child routes while retaining the existing shell.
- A fresh Home load makes no `/remotes/` requests. Entering the reference route
  loads `remoteEntry.js` and every referenced chunk from
  `/remotes/quizzes-courses/`; no remote stylesheet is loaded.
- Direct refresh at `/federation-spike/details`, child navigation, and browser
  back/forward all render correctly. Client-side navigation retains one browser
  navigation entry, proving that it does not reload the document.
- Runtime inspection shows one compatible shared provider for Vue 3.5.42, Vue
  Router 4.6.4, Vuetify 3.13.2, and `@mdi/js` 7.4.47 across the host and remote.
- During a deliberate remote outage, Shared remains healthy and displays only
  the feature fallback. After the remote returns, Retry uses a cache-busted
  entry and restores the same deep route without a document reload.
- `remoteEntry.js` returns `200` as JavaScript with no-store/no-cache headers;
  hashed JavaScript chunks return `200` with a one-year immutable policy.
- The repeatable interface, ownership rules, loading pattern, nginx policy, and
  migration checks are documented in `docs/federation-module-contract.md`.

Static host remote declarations are not the migration template. With
`@module-federation/vite` 1.21.2 they fetched the reference entry during Home
bootstrap even when snapshot and Vite module preloading were disabled. Future
features must use route-time `registerRemotes` and `loadRemote`, with a unique
entry query on retry to bypass the browser's failed-module cache.

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

#### Verified Phase 2 result

- Token validation returns a typed authenticated user only when the RS256
  signature and lifetime are valid and both a positive numeric `sub` and a
  nonblank `name` claim are present. Expiry uses zero clock skew.
- `POST /api/check-login` reads only the HttpOnly `token` cookie and returns
  `{ id, name }`; a valid token supplied only in a JSON request body receives
  `401 Unauthorized`.
- Eight focused endpoint tests cover a valid cookie, absent cookie, invalid
  signature, expired token, missing subject, invalid subject, missing name, and
  rejection of request-body tokens. The complete Shared test project passes
  with 13 tests.
- `/api/login`, `/api/logout`, `/api/check-login/fragment`, and feature-local
  `/api/me` endpoints remain available for migration compatibility.
- Shared and Mini Games parse the new response without changing their broader
  local auth models. Their production builds pass, and a live `amber` session
  displays correctly in both frontends.
- Rebuilt Shared backend, Shared frontend, and Mini Games frontend containers
  run successfully through the production gateway.

### Phase 3 - Shared Vue host and visual system

- Preserve the existing package/lockfile, Vite, TypeScript, Vue entry point,
  Vue Router, Node-build/nginx Dockerfile, and responsive Vuetify shell.
- Extend the existing centralized five-service navigation registry for
  federated route metadata rather than creating a second registry.
- Keep the existing host auth state, login view, logout action, and safe return
  URL handling. Upgrade it to `{ id, name }`, add route guards, and ensure auth
  bootstrap occurs once at the host boundary rather than in the sidebar.
- Extend the route-time federation registry established by the Phase 1 spike,
  and add the TanStack Query provider. Do not add static host remote entries.
- Preserve the redesigned Shared home and runtime map. The former HTMX
  sample-items view has already been intentionally removed.
- Add accessible loading, signed-out, missing-remote, and general error states.
- Treat the existing Shared `src/styles.css`, Vuetify theme, icons, and assets
  as the initial host-owned visual system, then absorb common/feature CSS into
  namespaced host styles during each remote migration.
- Keep the current `/login.html` Vue route during rollout and add `/login` as
  the canonical route, with `/login.html` retained as a compatibility redirect.

Exit criteria: the host shell and auth work without any feature remote and
match the Quizzes and Courses visual baseline on desktop and mobile.

#### Verified Phase 3 result

- The centralized five-feature navigation registry now owns labels, paths,
  icons, Home-card content, and target federation route metadata. Sidebar
  active state is derived from the current route rather than hard-coded.
- Shared stores the complete `{ id, name }` identity, performs one settled
  session check at the app boundary, refreshes identity after login, and passes
  the reactive user through the federation host context.
- Protected routes redirect signed-out users to a host-owned view while
  preserving their full return URL. `/login` is canonical and `/login.html`
  redirects with query/hash intact. Successful login returns through Vue Router
  without reloading the document.
- Accessible host loading, signed-out, general-error/retry, and missing-remote
  states are present. A deliberate Shared backend outage displayed only the
  general-error state; Retry restored Home in the same document.
- `@tanstack/vue-query` 5.102.8 is installed at the host boundary and configured
  as a strict federation singleton. Highcharts remains feature-local.
- Eight focused Vitest tests cover auth request deduplication, identity state,
  signed-out caching, login refresh, canonical return URLs, compatibility
  redirect, protected-route handling, and app-boundary bootstrap.
- Production builds and the rebuilt Shared container pass. At 1440 x 900 and
  390 x 844, Home renders five feature cards without horizontal overflow and
  exposes the mobile navigation trigger only at the mobile viewport.
- With the reference remote stopped, Home and Login remain functional and make
  no `/remotes/` requests. The remote was restored and all affected containers
  are healthy.

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

#### Verified Phase 4 result

- Quizzes and Courses exposes a shell-free `QuizzesCoursesComponent`, metadata,
  and ten relative route records. The Shared host registers and loads them only
  when `/quizzes-and-courses/*` is entered.
- Course, lesson, quiz list/runner, flashcard list/deck/revision, and completion
  deep links all survive direct production refreshes. Named navigation remains
  client-side beneath the host route, and Garry is suppressed on quiz-runner
  and completion routes as intended.
- Browser traffic uses `/quizzes-and-courses/api/*` for feature data and
  `/remotes/quizzes-courses/*` for the remote entry, chunks, flags, and Garry
  image. No remote CSS request or failed asset request occurs.
- Authored feature CSS now lives in Shared under a feature scope. The remote
  no longer imports authored CSS; its remaining standalone build stylesheet is
  Vuetify's framework CSS only.
- Shared nginx serves feature pages from the host SPA while retaining the more
  specific feature API and 120-second unbuffered assistant locations.
- At 1440 x 900 and 390 x 844, representative home, course, lesson, quiz,
  flashcard, and completion views have no horizontal overflow. The Shared
  mobile navigation trigger remains available on the narrow viewport.
- Signing out from the feature redirects to the host-owned signed-out view with
  the full deep link preserved. Signing in returns directly to that route.
- Stopping the remote produces an isolated host-owned fallback. Restarting it
  and selecting Retry restores the feature in the same document through a
  cache-busted remote entry URL.
- The assistant request reaches the stable endpoint and accepts the federated
  home context after normalizing its host-only route name. The current Ollama
  environment returns the expected deterministic `503`, so successful token
  streaming remains an environment-dependent recheck rather than a blocker to
  the frontend migration.
- Shared's eight Vitest tests and both production frontend builds pass. The
  obsolete Phase 1 reference remote files have been removed.

### Phase 5 - Mini Games remote

- Add federation configuration and expose `MiniGamesComponent` and routes.
- Replace manual pathname selection with host Vue Router route records.
- Remove the duplicated sidebar/navigation and remote auth composable.
- Add JWT bearer/cookie validation to the Mini Games backend, derive the user ID
  from the authenticated `sub` claim, remove the localStorage/default user ID,
  and reject caller-supplied identity for user-specific operations.
- Add authorization and user-isolation tests before changing those API inputs.
- Add deterministic seeded course vocabulary for the Playwright account (or a
  test-only equivalent) so Guess the Word, Word Search, and Associations can be
  exercised without OpenRouter. Test the optional AI-unavailable path
  separately from successful content-mode gameplay.
- Preserve game selection, language/mode loading, completion statistics,
  Guess the Word, Word Search, Associations, help, generation, and error states.
- Move all global and scoped component CSS into host-owned namespaced files.
- Replace `BASE_URL` API and navigation assumptions with stable host routes.
- Add the `/remotes/mini-games/` gateway proxy and switch `/mini-games/*` page
  routes to the host SPA while retaining `/mini-games/api/*`.

Exit criteria: direct game links, gameplay, generated content, and completion
statistics behave as before inside the host shell.

#### Verified Phase 5 result

- Mini Games exposes a shell-free `MiniGamesComponent` and four relative route
  records. Home and all three game deep links render through the Shared router
  without a document reload.
- The backend validates the shared RS256 JWT from the HttpOnly cookie or bearer
  header, requires a positive numeric `sub`, and derives every game/session and
  completion-statistics user ID from that claim. Legacy caller-supplied
  `userId` query parameters are explicitly rejected.
- Seven HTTP integration cases cover missing/invalid authentication, invalid
  subjects, valid subject access, and identity-override rejection. The complete
  Mini Games backend suite passes with 46 tests.
- An idempotent development seed marks German lessons complete for the primary
  `amber` account. Guess the Word, Word Search, and Associations all initialize
  successfully in content mode without OpenRouter, while AI availability
  remains an independent optional state.
- Browser traffic uses `/mini-games/api/*` and `/remotes/mini-games/*`; game
  requests contain no user ID and the remote loads no CSS. All former SFC CSS
  is host-owned with per-component scopes, including a separate global scope
  only for the teleported definitions dialog.
- Home and all three games render without horizontal overflow at 1440 x 900 and
  390 x 844. The Guess the Word alphabet uses flexible mobile columns.
- Stopping the Mini Games frontend displays the isolated host fallback on a
  deep link. Restarting the remote and selecting Retry restores that route in
  the same document through a cache-busted entry.
- The local auth composable, duplicated sidebar/navigation, manual pathname
  router, global remote stylesheet, and obsolete shell component are removed.
- Mini Games and Shared production builds, Shared's eight frontend tests,
  Docker Compose validation, and all affected service health checks pass.

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

Verified result:

- Discussion Forum is exposed as a shell-free `chat_discussion` remote and is
  registered only when the Shared router enters `/chat-discussion`.
- All six forum routes retain filtering, search, pagination, post and comment
  workflows, likes, My Posts, validation, and ownership-sensitive controls.
- Shared route metadata requires authentication for every Discussion route;
  signed-out deep links preserve their complete return URL, while the backend
  continues to enforce authorization and ownership for write operations.
- The obsolete remote header, cross-service navigation, and app shell are
  removed. The feature retains only its Forums, My Posts, and New Post nav.
- All ten SFC style blocks are host-owned under the Discussion feature scope.
  The remote production build emits `remoteEntry.js` and zero CSS files.
- Shared nginx serves remote artifacts from `/remotes/chat-discussion/`, keeps
  `/chat-discussion/api/` stable, and sends page routes to the host SPA.
- The production workflow passed create, edit, delete, comment, like, My Posts,
  direct deep-link, logout, and return-to-route checks. The 390px layout has no
  horizontal overflow.
- Stopping only the Discussion frontend displays the isolated Shared fallback;
  restarting it and selecting Retry restores the deep link in the same page.
- Four Shared router tests, 68 Discussion backend tests, both production
  frontend builds, Docker Compose validation, and gateway health checks pass.

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
  ownership location without changing the visual content. Import achievement
  images or resolve them from the explicit remote asset base so the current ten
  root-relative image `404`s are eliminated rather than preserved.
- Add the `/remotes/quests-achievements/` gateway proxy and switch
  `/quests-and-achievements/*` page routes to the host SPA while retaining its
  API path.
- Remove `htmx.min.js`, the vanilla `app.js`, and static templates only after
  Vue parity tests pass.

Exit criteria: profile, achievements, preferences, and notification history
have feature parity in Vue and match the shared Quizzes-style UI.

Verified result:

- The static HTMX dashboard is replaced by a typed Vue 3/Vite remote exposing
  a shell-free component and relative dashboard route.
- Profile loading, username and achievement summaries, earned/progress states,
  email and all preference controls, master-toggle disabling without value
  loss, and save loading/success/validation/failure feedback are reactive.
- Preferences are sent as JSON while the backend temporarily retains form
  compatibility. Three component tests cover profile mapping, toggle value
  retention, the JSON contract, save feedback, and Escape dialog closure.
- Notifications are rendered newest-first with count and empty states. Details
  open in an accessible native dialog and close by button, backdrop, or Escape.
- The remote owns one explicit achievement fallback asset at
  `/remotes/quests-achievements/achievement.svg`; all ten cards load it without
  the previous root-relative image 404s.
- All authored Q/A/N CSS is host-owned and feature-scoped. The remote emits no
  CSS files, and the 390px production layout has no horizontal overflow.
- Shared nginx serves `/remotes/quests-achievements/*`, retains the stable
  `/quests-and-achievements/api/*` path, and routes feature pages to the SPA.
- Stopping only the Q/A/N frontend displays the isolated Shared fallback;
  restarting and selecting Retry restores the dashboard in the same document.
- The legacy `app.js`, bundled HTMX library, static templates, duplicated shell,
  and imperative DOM mutation are removed. Three frontend tests, 34 backend
  tests, both production builds, Compose validation, and gateway checks pass.

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
- Load and configure the Highcharts accessibility module, then add an
  accessible chart description and verify the current console warning is gone.
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
| npm registry blocks clean host installs | Prove an uncached restore before Phase 1 lockfile changes and retain the Docker baseline until it passes |
| Duplicate Vue or Vuetify runtime | Configure singleton sharing and verify one instance in runtime tests |
| Remote CSS leaks or disappears | Move styles incrementally, namespace by feature root, screenshot before deletion |
| Remote chunks load from host SPA paths | Give each remote an explicit `/remotes/<feature>/` production public path and test every emitted asset |
| `BASE_URL` points API calls at remote assets | Use explicit same-origin feature API prefixes |
| A remote outage crashes the SPA | Lazy load per route with host-owned timeout, retry, and error boundary |
| Stale `remoteEntry.js` references old chunks | Revalidate the entry; use hashed immutable chunks |
| Auth migration breaks writes | Change shared endpoint first, migrate consumers, delete old endpoints last |
| Q/A/N loses HTMX edge behavior | Component tests and an explicit parity checklist gate deletion |
| Q/A/N root-relative achievement images return 404 | Import images or use the remote asset base and assert no failed image requests |
| Mini Games accepts another user's ID | Add JWT validation and derive identity server-side before remote cutover |
| Mini Games seed cannot start a successful round | Add deterministic unlocked vocabulary for the E2E account before gameplay parity testing |
| Leaderboard queries lack a provider in the host | Install and singleton-share TanStack Query before mounting the remote |
| Highcharts keeps stale dimensions or instances | Reflow on host layout changes and destroy the chart on unmount |
| Highcharts chart lacks accessibility metadata | Load its accessibility module and test the chart's accessible description |
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
