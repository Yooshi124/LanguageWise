using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;

namespace LanguageWise.MiniGamesService.Api.Services;

/// <summary>
/// Manages per-user game instances. Playable state lives in memory for the session, while
/// every round is mirrored to the database (a Games row plus a GameAttempts row) so game
/// history and completion stats survive restarts.
/// </summary>
public sealed class GameSessionManager(
    IVocabularyProvider vocabularyProvider,
    IAiVocabularyProvider aiVocabularyProvider,
    GamesDatabaseClient databaseClient,
    ILogger<GameSessionManager> logger)
{
    private const string Language = "English";
    private const int GuessTheWordLength = 5;
    private const int MinimumWordSearchWords = 4;
    private const int AssociationGroupSize = 4;
    private const int AssociationGroupCount = 4;

    private readonly Dictionary<string, TrackedGame> games = new();

    // ------------------------------------------------------------ Guess the Word

    /// <summary>Create and store a new Guess the Word game for the user.</summary>
    public async Task<GuessTheWordState> StartGuessTheWordGameAsync(int userId, string? courseCode, string? accessToken, string mode = GameModes.Content, string? language = null)
    {
        var vocabulary = await ResolveVocabularyAsync(mode, "guess_the_word", courseCode, accessToken, language);
        var candidates = vocabulary.Where(entry => entry.Word.Length == GuessTheWordLength).ToList();
        if (candidates.Count == 0)
        {
            ThrowNoWords(mode, "Guess the Word");
        }

        var candidateWords = candidates.Select(entry => entry.Word).ToArray();
        var gameLanguage = ResolveLanguage(mode, language);
        var game = new GuessTheWordGame(gameLanguage, candidateWords);
        // Definitions are an AI-mode feature; content-mode words are defined in the course itself.
        var definitions = IsAi(mode) ? BuildDefinitions(candidates) : null;
        games[GuessTheWordKey(userId)] = await PersistNewGameAsync(
            "guess_the_word", userId, courseCode, game.Solution, candidateWords, game, definitions);

        logger.LogInformation("Started Guess the Word for user {UserId} with {WordCount} candidate words (mode {Mode})", userId, candidateWords.Length, mode);
        return game.GetState();
    }

    /// <summary>Get the current Guess the Word game for the user.</summary>
    public GuessTheWordState? GetGuessTheWordGameState(int userId) =>
        games.TryGetValue(GuessTheWordKey(userId), out var tracked) && tracked.Game is GuessTheWordGame guessTheWord
            ? WithDefinitions(guessTheWord.GetState(), tracked)
            : null;

    /// <summary>Submit a guess to the user's Guess the Word game.</summary>
    public async Task<GuessTheWordGuessResult> SubmitGuessTheWordGuessAsync(int userId, string guess)
    {
        var tracked = GetTrackedGame(GuessTheWordKey(userId), "Guess the Word");
        var game = (GuessTheWordGame)tracked.Game;
        var result = game.SubmitGuess(guess);
        var state = game.GetState();
        await PersistCompletionAsync(tracked, state.IsComplete, state.IsWon, score: 0, state.Attempts);
        return state.IsComplete && tracked.Definitions.Count > 0
            ? result with { Definitions = tracked.Definitions }
            : result;
    }

    /// <summary>Reset the user's Guess the Word game; the next init starts (and persists) a fresh round.</summary>
    public void ResetGuessTheWordGame(int userId) => games.Remove(GuessTheWordKey(userId));

    // ---------------------------------------------------------------- Word Search

    /// <summary>Create and store a new Word Search game for the user.</summary>
    public async Task<WordSearchState> StartWordSearchGameAsync(int userId, string? courseCode, string? accessToken, string mode = GameModes.Content, string? language = null)
    {
        var groups = await ResolveVocabularyGroupsAsync(mode, "word_search", courseCode, accessToken, language);
        var words = groups.SelectMany(group => group.Words).DistinctBy(entry => entry.Word).ToList();
        if (words.Count < MinimumWordSearchWords)
        {
            ThrowNoWords(mode, "Word Search");
        }

        var themeHint = "Words from " + string.Join(" · ", groups.Select(group => group.Title));
        var wordList = words.Select(entry => entry.Word).ToArray();
        var gameLanguage = ResolveLanguage(mode, language);
        var game = new WordSearchGame(gameLanguage, themeHint, wordList);
        // Only the words that actually fit on the board are in play; an over-long candidate that
        // was skipped should not get a definition in the end-of-round popup.
        var placedWords = game.GetWordChain().ToHashSet();
        var definitions = IsAi(mode) ? BuildDefinitions(words.Where(entry => placedWords.Contains(entry.Word))) : null;
        games[WordSearchKey(userId)] = await PersistNewGameAsync(
            "word_search", userId, courseCode, string.Join(", ", game.GetWordChain()), wordList, game, definitions);

        logger.LogInformation("Started Word Search for user {UserId} with {WordCount} words (mode {Mode})", userId, wordList.Length, mode);
        return game.GetState();
    }

    /// <summary>Get the current Word Search game for the user.</summary>
    public WordSearchState? GetWordSearchGameState(int userId) =>
        games.TryGetValue(WordSearchKey(userId), out var tracked) && tracked.Game is WordSearchGame wordSearch
            ? WithDefinitions(wordSearch.GetState(), tracked)
            : null;

    /// <summary>Submit a word to the user's Word Search game.</summary>
    public async Task<WordSearchMoveResult> SubmitWordSearchWordAsync(int userId, string word, IReadOnlyList<int>? indices = null)
    {
        var tracked = GetTrackedGame(WordSearchKey(userId), "Word Search");
        var result = ((WordSearchGame)tracked.Game).SubmitWord(word, indices ?? []);
        await PersistCompletionAsync(
            tracked, result.State.IsComplete, result.State.IsComplete && !result.State.IsGivenUp, result.State.Score, result.State.Words.Count);
        return result with { State = WithDefinitions(result.State, tracked) };
    }

    /// <summary>Use a hint in the user's Word Search game.</summary>
    public WordSearchHintResult UseWordSearchHint(int userId) =>
        ((WordSearchGame)GetTrackedGame(WordSearchKey(userId), "Word Search").Game).UseHint();

    /// <summary>Give up on the user's Word Search game.</summary>
    public async Task<WordSearchState> GiveUpWordSearchAsync(int userId)
    {
        var tracked = GetTrackedGame(WordSearchKey(userId), "Word Search");
        var state = ((WordSearchGame)tracked.Game).GiveUp();
        await PersistCompletionAsync(tracked, state.IsComplete, isWon: false, state.Score, state.Words.Count);
        return WithDefinitions(state, tracked);
    }

    /// <summary>Reset the user's Word Search game; the next init starts (and persists) a fresh round.</summary>
    public void ResetWordSearchGame(int userId) => games.Remove(WordSearchKey(userId));

    // ---------------------------------------------------------------- Associations

    /// <summary>Create and store a new Associations game for the user.</summary>
    public async Task<AssociationsState> StartAssociationsGameAsync(int userId, string? courseCode, string? accessToken, string mode = GameModes.Content, string? language = null)
    {
        var groups = await ResolveVocabularyGroupsAsync(mode, "associations", courseCode, accessToken, language);

        // One association group per source group; a word may only appear in one group.
        var seenWords = new HashSet<string>();
        var catalog = groups
            .Select(group => new AssociationGroup(
                group.Title,
                group.Words.Where(entry => seenWords.Add(entry.Word)).Select(entry => entry.Word).Take(AssociationGroupSize).ToArray()))
            .Where(group => group.Words.Count == AssociationGroupSize)
            .ToList();

        if (catalog.Count < AssociationGroupCount)
        {
            ThrowNoWords(mode, "Associations");
        }

        var gameLanguage = ResolveLanguage(mode, language);
        var game = new AssociationsGame(gameLanguage, catalog);
        // Only the 16 words actually in play (4 chosen groups) should appear in the popup; the
        // catalogue may hold extra groups/words that were not used this round.
        var playedWords = game.GetWords().ToHashSet();
        var definitions = IsAi(mode)
            ? BuildDefinitions(groups.SelectMany(group => group.Words).Where(entry => playedWords.Contains(entry.Word)))
            : null;
        games[AssociationsKey(userId)] = await PersistNewGameAsync(
            "associations", userId, courseCode, game.Solution, game.GetWords(), game, definitions);

        logger.LogInformation("Started Associations for user {UserId} with {GroupCount} groups (mode {Mode})", userId, catalog.Count, mode);
        return game.GetState();
    }

    /// <summary>Get the current Associations game for the user.</summary>
    public AssociationsState? GetAssociationsGameState(int userId) =>
        games.TryGetValue(AssociationsKey(userId), out var tracked) && tracked.Game is AssociationsGame associations
            ? WithDefinitions(associations.GetState(), tracked)
            : null;

    /// <summary>Submit a guess to the user's Associations game.</summary>
    public async Task<AssociationResult> SubmitAssociationsGuessAsync(int userId, IReadOnlyList<string> words)
    {
        var tracked = GetTrackedGame(AssociationsKey(userId), "Associations");
        var result = ((AssociationsGame)tracked.Game).SubmitGuess(words);
        await PersistCompletionAsync(
            tracked, result.State.IsComplete, result.State.IsWon, result.State.SolvedGroups.Count, result.State.FailedAttempts + result.State.SolvedGroups.Count);
        return result with { State = WithDefinitions(result.State, tracked) };
    }

    /// <summary>Reset the user's Associations game; the next init starts (and persists) a fresh round.</summary>
    public void ResetAssociationsGame(int userId) => games.Remove(AssociationsKey(userId));

    // --------------------------------------------------------------------- Shared

    private static string GuessTheWordKey(int userId) => $"guess-word-{userId}";
    private static string WordSearchKey(int userId) => $"word-search-{userId}";
    private static string AssociationsKey(int userId) => $"associations-{userId}";

    /// <summary>Resolve flat vocabulary for the requested mode: course content or AI generation.</summary>
    private async Task<IReadOnlyList<WordEntry>> ResolveVocabularyAsync(string mode, string gameKind, string? courseCode, string? accessToken, string? language)
    {
        if (string.Equals(mode, GameModes.Ai, StringComparison.OrdinalIgnoreCase))
        {
            // The frontend sends a language code ("de"); the prompt needs the full name ("German").
            var groups = await aiVocabularyProvider.GenerateGroupsAsync(gameKind, SupportedLanguages.ResolveTitle(language ?? Language));
            return groups.SelectMany(group => group.Words).DistinctBy(entry => entry.Word).ToList();
        }

        return await vocabularyProvider.GetVocabularyAsync(courseCode, accessToken);
    }

    /// <summary>Resolve grouped vocabulary for the requested mode: course content or AI generation.</summary>
    private async Task<IReadOnlyList<VocabularyGroup>> ResolveVocabularyGroupsAsync(string mode, string gameKind, string? courseCode, string? accessToken, string? language)
    {
        if (string.Equals(mode, GameModes.Ai, StringComparison.OrdinalIgnoreCase))
        {
            return await aiVocabularyProvider.GenerateGroupsAsync(gameKind, SupportedLanguages.ResolveTitle(language ?? Language));
        }

        return await vocabularyProvider.GetVocabularyGroupsAsync(courseCode, accessToken);
    }

    /// <summary>The display language for the round; AI mode can target any supported language.</summary>
    private static string ResolveLanguage(string mode, string? language) =>
        string.Equals(mode, GameModes.Ai, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(language)
            ? SupportedLanguages.ResolveTitle(language)
            : Language;

    /// <summary>True when the round is played in AI generation mode.</summary>
    private static bool IsAi(string mode) =>
        string.Equals(mode, GameModes.Ai, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// No playable words. In AI mode that means generation produced nothing usable, so surface the
    /// AI-unavailable error rather than the "complete more course content" message, which only
    /// makes sense for content mode.
    /// </summary>
    private static void ThrowNoWords(string mode, string gameName)
    {
        if (IsAi(mode))
        {
            throw new AiVocabularyUnavailableException($"AI word generation produced no usable words for {gameName}. Try again.");
        }

        throw new NoVocabularyAvailableException(gameName);
    }

    /// <summary>Uppercased word → definition map kept in memory for the end-of-game popup.</summary>
    private static IReadOnlyDictionary<string, string> BuildDefinitions(IEnumerable<WordEntry> entries)
    {
        var definitions = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Definition))
            {
                definitions.TryAdd(entry.Word.ToUpperInvariant(), entry.Definition);
            }
        }

        return definitions;
    }

    private static GuessTheWordState WithDefinitions(GuessTheWordState state, TrackedGame tracked) =>
        state.IsComplete && tracked.Definitions.Count > 0 ? state with { Definitions = tracked.Definitions } : state;

    private static WordSearchState WithDefinitions(WordSearchState state, TrackedGame tracked) =>
        state.IsComplete && tracked.Definitions.Count > 0 ? state with { Definitions = tracked.Definitions } : state;

    private static AssociationsState WithDefinitions(AssociationsState state, TrackedGame tracked) =>
        state.IsComplete && tracked.Definitions.Count > 0 ? state with { Definitions = tracked.Definitions } : state;

    private TrackedGame GetTrackedGame(string sessionKey, string gameName) =>
        games.TryGetValue(sessionKey, out var tracked)
            ? tracked
            : throw new InvalidOperationException($"No active {gameName} game. Start a new game first.");

    /// <summary>
    /// Mirror a new round to the database (a game row plus the attempt row for this user).
    /// Persistence failures are logged and swallowed so a database outage never blocks play.
    /// </summary>
    private async Task<TrackedGame> PersistNewGameAsync(
        string gameType, int userId, string? courseCode, string solution, IReadOnlyList<string> words, object game,
        IReadOnlyDictionary<string, string>? definitions = null)
    {
        try
        {
            var createdGame = await databaseClient.CreateGameAsync(gameType, userId, courseCode ?? "all", solution, words);
            if (createdGame is null)
            {
                logger.LogWarning("Could not persist a new {GameType} game for user {UserId}: the database rejected the create", gameType, userId);
                return new TrackedGame(game, definitions: definitions);
            }

            var attempt = await databaseClient.CreateGameAttemptAsync(createdGame.Id, userId);
            if (attempt is null)
            {
                logger.LogWarning("Persisted {GameType} game {GameId} for user {UserId} without an attempt row", gameType, createdGame.Id, userId);
            }

            return new TrackedGame(game, createdGame.Id, attempt?.Id, definitions);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not persist a new {GameType} game for user {UserId}; continuing in memory only", gameType, userId);
            return new TrackedGame(game, definitions: definitions);
        }
    }

    /// <summary>Patch the tracked attempt once the round reaches a completed state.</summary>
    private async Task PersistCompletionAsync(TrackedGame tracked, bool isComplete, bool isWon, int score, int attemptCount)
    {
        if (!isComplete || tracked.CompletionPersisted || tracked.AttemptId is not int attemptId)
        {
            return;
        }

        tracked.CompletionPersisted = true;
        try
        {
            await databaseClient.UpdateGameAttemptAsync(
                attemptId,
                score: score,
                isWon: isWon,
                isComplete: true,
                attemptCount: attemptCount,
                completedAt: DateTime.UtcNow.ToString("O"),
                timeSpentSeconds: (int)(DateTime.UtcNow - tracked.StartedAt).TotalSeconds);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not persist completion of attempt {AttemptId}", attemptId);
        }
    }

    /// <summary>An in-memory game plus the database rows that mirror it.</summary>
    private sealed class TrackedGame(
        object game,
        int? gameId = null,
        int? attemptId = null,
        IReadOnlyDictionary<string, string>? definitions = null)
    {
        public object Game { get; } = game;
        public int? GameId { get; } = gameId;
        public int? AttemptId { get; } = attemptId;
        public IReadOnlyDictionary<string, string> Definitions { get; } = definitions ?? new Dictionary<string, string>();
        public DateTime StartedAt { get; } = DateTime.UtcNow;
        public bool CompletionPersisted { get; set; }
    }
}
