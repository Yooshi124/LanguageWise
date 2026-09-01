# Feature code — Mini Games / Activities

Owner: **Kyan**

Everything in this folder is Kyan's feature work. It is kept separate from the
future cross-service integrations.

## What is in here

- `GuessTheWord`, `WordSearch`, and `Associations` — the three game implementations.
- `Vocabulary` — resolves playable words from the quizzes/courses service based on
  the user's completed lesson milestones.

Each game follows the same two-part structure:

```
Feature/<Game>/
  <Game>Models.cs   data passed between the game and API
  <Game>Game.cs     game rules; references <Game>Models.cs
```

Game instances are created per user by `Services/GameSessionManager.cs`, which asks
the `IVocabularyProvider` for the user's course vocabulary and throws
`NoVocabularyAvailableException` when there is nothing to play with. The API maps
that to a `422` with `{ "code": "NO_VOCABULARY" }` so the frontend can show a
friendly "complete more course content" message instead of a broken board.

## Vocabulary rules

- Words come from completed lessons of the user's course (milestones in the
  quizzes/courses database service).
- Entries are split into letter-only tokens (so "Guten Tag" yields GUTEN and TAG),
  uppercased, and must be 3–15 letters long. Any alphabet is allowed, so German
  umlauts and ß work.
- Tokens shared by several entries of a lesson (e.g. articles like "die") are dropped.
- **Guess the Word** needs at least one five-letter word.
- **Word Search** needs at least four words; the board is generated along a
  serpentine route and leftover cells are filled with letters from the placed words.
- **Associations** needs at least four lessons with four words each; each lesson
  becomes one association group.

## The frontend

This microservice's frontend is Kyan's **Vue + Vite** single-page app, in
`mini-games-service/frontend/src/`. All API calls go through `src/api.js`, which
attaches the `userId` query parameter and translates error responses into typed
errors (including the `NO_VOCABULARY` code).
The build runs entirely inside the frontend Dockerfile, so no Node install is needed
locally or in CI.

Routes, resolved in `src/main.js`:

| Path | Component |
| --- | --- |
| `/` | `GamePage.vue` |
| `/game/guess-the-word` | `GuessTheWord.vue` |
| `/game/word-search` | `WordSearch.vue` |
| `/game/associations` | `Associations.vue` |

## Wiring this feature up

1. Add persistence and authentication once the shared contracts are available
   (the games database service and `GamesDatabaseClient` are already in place).
2. Add integration tests for the courses-service boundary.
