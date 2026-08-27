# Feature code — Mini Games / Activities

Owner: **Kyan**

Everything in this folder is Kyan's feature work. It is kept **separate from the
skeleton** and is **not wired up yet**.

## What is in here

- `VocabVoyage.cs`, `WordStrings.cs`, `Associations.cs` — the original flat stubs,
  retained for comparison while the grouped structure is evaluated.

The proposed grouped structure is now also present, with each game following the same
three-part relationship:

```
Feature/<Game>/
  <Game>Models.cs   data passed between the game and API
  <Game>Game.cs     game rules; references <Game>Models.cs
  <Game>Service.cs  session/application boundary; references <Game>Game.cs
```

For example, `VocabVoyageService.SubmitGuess` calls `VocabVoyageGame.SubmitGuess`,
which returns the `VocabVoyageGuessResult` model. The equivalent references exist for
`WordStrings` and `Associations`.

All methods in the new grouped classes are intentionally stubs and are not registered
in `Program.cs` yet.

These are carried over unchanged from the original `Mini Games/backend/` folder. They
are compiled by the API project but nothing calls them yet — there are no endpoints,
tables or repositories behind them.

`VocabVoyage.generateAnswer` currently returns the placeholder `"Vocab"`. It is where
the Ollama LLM call will go once AI-Mode is wired up for Release 0.

## The frontend

This microservice's frontend is Kyan's **Vue + Vite** single-page app, in
`mini-games-service/frontend/src/`. `GamePage.vue` and `VocabVoyage.vue` are unchanged.
The build runs entirely inside the frontend Dockerfile, so no Node install is needed
locally or in CI.

Routes, resolved in `src/main.js`:

| Path | Component |
| --- | --- |
| `/` | `GamePage.vue` |
| `/vocab-voyage` | `VocabVoyage.vue` |
| `/sample-items` | `skeleton/SampleItems.vue` — skeleton demo, not part of the feature |

## What the skeleton is, and why it is separate

The rest of this microservice is a working proof of concept built around a placeholder
`SampleItems` table, showing the wiring end to end:

```
frontend (nginx)  ->  backend API  ->  database service  ->  SQLite
```

Visit <http://localhost:3001/sample-items> to see it. Use it as the reference for how to
plumb a real game through the three tiers.

## Wiring this feature up

1. Add a games table to `database/LanguageWise.MiniGamesService.Db/sql/schema.sql`.
2. Add a repository next to `Data/SampleItemRepository.cs` and map endpoints in the
   database service `Program.cs`.
3. Expose game endpoints from `backend/.../Program.cs` that call `VocabVoyage`.
4. Replace the placeholder in `generateAnswer` with a call to the Ollama service.
5. Point `VocabVoyage.vue` at those endpoints with `fetch`, as `skeleton/SampleItems.vue` does.
6. Add NUnit tests in `tests/` — `getGuessColours` is pure and easy to test first.

## Notes on the code as it stands

- `getGuessColours` assumes both the guess and the answer are exactly five characters;
  a shorter guess will throw an `IndexOutOfRangeException`.
- A letter already matched exactly can still be scored orange elsewhere in the word,
  which differs from how Wordle handles duplicate letters.
- `isGuessCorrect` is private and currently unused.
