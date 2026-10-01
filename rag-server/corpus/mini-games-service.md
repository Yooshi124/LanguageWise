# Mini Games Service

Small games for practising vocabulary.

## For users

Three games are available. Each round is generated fresh, so there is no fixed
daily puzzle and you can play as many rounds as you like.

### Guess the Word

Find the hidden five-letter word in six guesses (Wordle-style).

- Type a five-letter word and submit it. Guesses must be exactly five letters.
- After each guess, every letter is coloured:
  - **Green** — the letter is in the word and in the right position.
  - **Orange** — the letter is in the word but in a different position.
  - **Red** — the letter is not in the word.
- Accented letters such as Ä, Ñ or Ü can be typed as their plain letter (A, N,
  U) and still match. Letters that have no plain equivalent, such as ß or Ł,
  have special-letter buttons below the input — click one to insert it.
- There are no hints in this game.

### Word Search

Find the themed words hidden in an 8 × 6 grid of letters.

- Each board hides between 4 and 10 words of at least three letters. The theme
  shown above the grid tells you which lessons or topic the words come from.
- Words are traced as a chain of neighbouring letters. Each next letter can be
  up, down, left, right or diagonal from the previous one, and a letter cannot
  be used twice in the same word — so words can bend and turn, not just run in
  straight lines.
- **Mouse or touch:** press on the first letter and drag through the rest.
- **Keyboard:** use the arrow keys to move, Enter or Space to add a letter,
  press Enter/Space again on the last letter to submit, and Escape to cancel.
- You have **3 hints** per game. A hint reveals where an unfound word starts;
  after that you can use "Show order" to see its path again.
- **Give up** reveals all remaining words and ends the round.
- There is no countdown; your time is measured and shown when the round ends.

### Associations

Sort sixteen words into four hidden categories of four words each.

- Click four words you think belong together, then press **Submit group**.
- A correct group is locked in and shown with its category name.
- You can make **4 mistakes**. On the fourth wrong guess the round ends and any
  unsolved groups are shown under "Correct associations".
- There are no hints in this game.

### Vocabulary modes

Choose a mode with the toggle on the mini games home page. Your choice is
remembered the next time you visit.

- **Content Focus** uses words from lessons you have completed in your courses.
  Pick one of the languages you have unlocked; it applies to all three games.
  If you have not completed enough lessons yet, the game will tell you there are
  no words available — complete more course content to unlock vocabulary, or
  switch to AI Generation.
- **AI Generation** creates fresh beginner-level word lists — with themes and
  per-word definitions — on demand, in any course language. This makes the
  mini games fully playable even before you have started a course.

### Definitions

When a round ends, press **Word definitions** to open a popup listing the words
from that round with their meanings. The button only appears when definitions
are available — in Content Focus mode this depends on whether the course lesson
includes definitions.

### Stats and streaks

The mini games home page shows your progress per language:

- how many rounds of each game you have completed successfully,
- your best (fastest) winning time for each game,
- your current daily streak — the number of consecutive days (UTC) on which you
  completed at least one round. Playing today or yesterday keeps the streak
  alive.

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
