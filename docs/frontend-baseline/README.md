# Phase 0 Frontend Baseline

Captured from the production Docker Compose stack at `http://localhost:3000`
using the repository-seeded development account.

## Screenshots

Each application has a desktop viewport capture at 1440 x 900 and a mobile
viewport capture at 390 x 844:

| Application | Desktop | Mobile |
| --- | --- | --- |
| Shared home | [desktop](shared-desktop.png) | [mobile](shared-mobile.png) |
| Quizzes and Courses | [desktop](quizzes-courses-desktop.png) | [mobile](quizzes-courses-mobile.png) |
| Mini Games | [desktop](mini-games-desktop.png) | [mobile](mini-games-mobile.png) |
| Discussion Forum | [desktop](chat-discussion-desktop.png) | [mobile](chat-discussion-mobile.png) |
| Q/A/N | [desktop](quests-achievements-desktop.png) | [mobile](quests-achievements-mobile.png) |
| Leaderboard and Analytics | [desktop](leaderboard-analytics-desktop.png) | [mobile](leaderboard-analytics-mobile.png) |

All 12 PNG files decode successfully. None of the six mobile entry points had
horizontal overflow at capture time.

## Route Fixtures

| Application | Current browser routes |
| --- | --- |
| Shared | `/`, `/index.html`, `/login.html` |
| Quizzes and Courses | `/quizzes-and-courses/`, `/courses/:courseCode`, `/courses/:courseCode/lessons/:lessonSlug`, `/courses/:courseCode/completion`, `/quizzes`, `/quizzes/:courseCode`, `/quizzes/:courseCode/:quizId`, `/flashcards`, `/flashcards/:courseCode`, `/flashcards/:courseCode/:lessonSlug` |
| Mini Games | `/mini-games/`, `/game`, `/game/guess-the-word`, `/game/word-search`, `/game/associations` beneath the service base |
| Discussion Forum | `/chat-discussion/`, `/forums/:code`, `/my-posts`, `/new`, `/posts/:id`, `/posts/:id/edit` beneath the service base |
| Q/A/N | `/quests-and-achievements/` |
| Leaderboard and Analytics | `/analytics/` |

Representative deep links returned `200` with the application HTML through
the shared gateway.

## API Fixtures

| Request while signed out | Expected status |
| --- | ---: |
| `GET /api/sample-items` | 200 |
| `GET /quizzes-and-courses/api/courses` | 401 |
| `GET /mini-games/api/game-languages` | 200 |
| `GET /chat-discussion/api/forums` | 200 |
| `GET /quests-and-achievements/api/profile` | 401 |
| `GET /analytics/api/lessons-completed-over-time` | 401 |

The authenticated baseline renders all six applications. Analytics displays
personal rankings, six course chart series, and a completed AI summary. Q/A/N
renders its fallback achievement art but first requests ten incorrect
root-relative `/images/achievements/*` URLs. Highcharts also emits its missing
accessibility-module warning. Both findings are carried into the migration
plan instead of being accepted as target behavior.

The representative Quizzes and Discussion nested routes render their expected
views. Mini Games also resolves `/mini-games/game/guess-the-word`, but the
seeded account has no unlocked vocabulary: content initialization returns
`422 NO_VOCABULARY`. Selecting AI mode without an OpenRouter key returns
`503 AI_UNAVAILABLE`. These are handled UI states, not a successful gameplay
fixture, so Phase 5 must add deterministic content-mode test data.

## Build And Test Baseline

- Seven backend test projects: 88 passed, 0 failed.
- `docker compose config --quiet`: passed.
- `docker compose up -d --build`: passed; all long-running services started.
- Fresh local `npm ci`: blocked by npm registry HTTP 403 for all four current
  Vite frontends. Offline installation is unavailable because the exact
  lockfile packages are not fully cached.
