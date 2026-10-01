# Mini Games Service

Small games for practising vocabulary.

## For users

Three games are available:

- **Guess the Word** — find the hidden five-letter word in six guesses
  (Wordle-style). Special-letter buttons let you enter non-ASCII letters such as
  ß.
- **Word Search** — trace a chain of connected letters to find themed words
  hidden in the grid.
- **Associations** — group sixteen words into four categories of four.

Two vocabulary modes:

- **Content Focus** uses words from lessons you have completed in your courses.
- **AI Generation** creates fresh beginner-level word lists — with themes and
  per-word definitions — on demand, so the service is fully playable standalone.

When a round ends, definitions are revealed in a popup.

## For agents

The shared MCP server (`mcp-server/`) exposes two read-only mini games tools
under the `games` tool scope. Both act on behalf of the signed-in user and return
an error asking them to sign in when no user token is supplied.

- **`games_get_completion_stats`** — the user's successful completion counts,
  best times and current daily streak for Guess the Word, Word Search and
  Associations. Takes an optional two-letter `courseCode` (for example `it` or
  `fr`) to scope the stats to one language.
- **`games_list_game_languages`** — the languages the user has unlocked
  vocabulary for and can play the mini games in. Takes no arguments.
