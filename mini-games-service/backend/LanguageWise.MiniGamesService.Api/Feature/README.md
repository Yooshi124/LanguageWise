# Feature code — Mini Games / Activities

Owner: **Kyan**

Everything in this folder is Kyan's feature work. It is kept separate from the
future cross-service integrations.

## What is in here

- `GuessTheWord`, `WordSearch`, and `Associations` — grouped game implementations.

The grouped structure is now present, with each game following the same
three-part relationship:

```
Feature/<Game>/
  <Game>Models.cs   data passed between the game and API
  <Game>Game.cs     game rules; references <Game>Models.cs
  <Game>Service.cs  session/application boundary; references <Game>Game.cs
```

For example, `GuessTheWordService.SubmitGuess` calls `GuessTheWordGame.SubmitGuess`,
which returns the `GuessTheWordGuessResult` model. The equivalent references exist for
`WordSearch` and `Associations`.

The Guess the word game is registered in `Program.cs` and uses a local fake learning-context
provider until the quizzes/courses service exposes the required API.

Word Search uses a themed board with pointer-drag selection and server-side
progress tracking.

The local provider currently supplies a small Markdown learning context containing
candidate vocabulary. It is deliberately replaceable when the courses API exists.

## The frontend

This microservice's frontend is Kyan's **Vue + Vite** single-page app, in
`mini-games-service/frontend/src/`. `GamePage.vue` and `GuessTheWord.vue` are the
currently supported screens.
The build runs entirely inside the frontend Dockerfile, so no Node install is needed
locally or in CI.

Routes, resolved in `src/main.js`:

| Path | Component |
| --- | --- |
| `/` | `GamePage.vue` |
| `/game/guess-the-word` | `GuessTheWord.vue` |
| `/game/word-search` | `WordSearch.vue` |
| `/game/associations` | `Associations.vue` — game screen stub |

## Wiring this feature up

1. Replace the fake learning-context provider with a client for the quizzes/courses service.
2. Add persistence and authentication once the shared contracts are available.
3. Replace the placeholder answer selection with the agreed content-generation strategy.
4. Add NUnit tests for each new game and integration boundary.

## Notes on the code as it stands

- Learning context is currently fake and is not user-specific.
- Game state is currently held in memory and is shared by requests to the backend process.
