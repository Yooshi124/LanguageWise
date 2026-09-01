using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;

namespace LanguageWise.MiniGamesService.Api.Services;

/// <summary>Manages per-user game instances, stored in memory for the session.</summary>
public sealed class GameSessionManager(IVocabularyProvider vocabularyProvider, ILogger<GameSessionManager> logger)
{
    private const string Language = "English";
    private const int GuessTheWordLength = 5;
    private const int MinimumWordSearchWords = 4;
    private const int AssociationGroupSize = 4;
    private const int AssociationGroupCount = 4;

    private readonly Dictionary<string, object> games = new();

    // ------------------------------------------------------------ Guess the Word

    /// <summary>Create and store a new Guess the Word game for the user.</summary>
    public async Task<GuessTheWordState> StartGuessTheWordGameAsync(int userId, string courseCode)
    {
        var vocabulary = await vocabularyProvider.GetVocabularyAsync(courseCode, userId);
        var candidates = vocabulary.Where(word => word.Length == GuessTheWordLength).ToList();
        if (candidates.Count == 0)
        {
            throw new NoVocabularyAvailableException("Guess the Word");
        }

        var game = new GuessTheWordGame(Language, candidates);
        games[GuessTheWordKey(userId)] = game;

        logger.LogInformation("Started Guess the Word for user {UserId} with {WordCount} candidate words", userId, candidates.Count);
        return game.GetState();
    }

    /// <summary>Get the current Guess the Word game for the user.</summary>
    public GuessTheWordState? GetGuessTheWordGameState(int userId) =>
        games.TryGetValue(GuessTheWordKey(userId), out var game) && game is GuessTheWordGame guessTheWord
            ? guessTheWord.GetState()
            : null;

    /// <summary>Submit a guess to the user's Guess the Word game.</summary>
    public GuessTheWordGuessResult SubmitGuessTheWordGuess(int userId, string guess) =>
        GetGame<GuessTheWordGame>(GuessTheWordKey(userId), "Guess the Word").SubmitGuess(guess);

    /// <summary>Reset the user's Guess the Word game.</summary>
    public void ResetGuessTheWordGame(int userId)
    {
        if (games.TryGetValue(GuessTheWordKey(userId), out var game) && game is GuessTheWordGame guessTheWord)
        {
            guessTheWord.Reset();
        }
    }

    // ---------------------------------------------------------------- Word Search

    /// <summary>Create and store a new Word Search game for the user.</summary>
    public async Task<WordSearchState> StartWordSearchGameAsync(int userId, string courseCode)
    {
        var groups = await vocabularyProvider.GetVocabularyGroupsAsync(courseCode, userId);
        var words = groups.SelectMany(group => group.Words).Distinct().ToList();
        if (words.Count < MinimumWordSearchWords)
        {
            throw new NoVocabularyAvailableException("Word Search");
        }

        var themeHint = "Words from your lessons: " + string.Join(" · ", groups.Select(group => group.Title));
        var game = new WordSearchGame(Language, themeHint, words);
        games[WordSearchKey(userId)] = game;

        logger.LogInformation("Started Word Search for user {UserId} with {WordCount} words", userId, words.Count);
        return game.GetState();
    }

    /// <summary>Get the current Word Search game for the user.</summary>
    public WordSearchState? GetWordSearchGameState(int userId) =>
        games.TryGetValue(WordSearchKey(userId), out var game) && game is WordSearchGame wordSearch
            ? wordSearch.GetState()
            : null;

    /// <summary>Submit a word to the user's Word Search game.</summary>
    public WordSearchMoveResult SubmitWordSearchWord(int userId, string word, IReadOnlyList<int>? indices = null) =>
        GetGame<WordSearchGame>(WordSearchKey(userId), "Word Search").SubmitWord(word, indices ?? []);

    /// <summary>Use a hint in the user's Word Search game.</summary>
    public WordSearchHintResult UseWordSearchHint(int userId) =>
        GetGame<WordSearchGame>(WordSearchKey(userId), "Word Search").UseHint();

    /// <summary>Give up on the user's Word Search game.</summary>
    public WordSearchState GiveUpWordSearch(int userId) =>
        GetGame<WordSearchGame>(WordSearchKey(userId), "Word Search").GiveUp();

    /// <summary>Reset the user's Word Search game.</summary>
    public void ResetWordSearchGame(int userId)
    {
        if (games.TryGetValue(WordSearchKey(userId), out var game) && game is WordSearchGame wordSearch)
        {
            wordSearch.Reset();
        }
    }

    // ---------------------------------------------------------------- Associations

    /// <summary>Create and store a new Associations game for the user.</summary>
    public async Task<AssociationsState> StartAssociationsGameAsync(int userId, string courseCode)
    {
        var groups = await vocabularyProvider.GetVocabularyGroupsAsync(courseCode, userId);

        // One association group per completed lesson; a word may only appear in one group.
        var seenWords = new HashSet<string>();
        var catalog = groups
            .Select(group => new AssociationGroup(
                group.Title,
                group.Words.Where(word => seenWords.Add(word)).Take(AssociationGroupSize).ToArray()))
            .Where(group => group.Words.Count == AssociationGroupSize)
            .ToList();

        if (catalog.Count < AssociationGroupCount)
        {
            throw new NoVocabularyAvailableException("Associations");
        }

        var game = new AssociationsGame(Language, catalog);
        games[AssociationsKey(userId)] = game;

        logger.LogInformation("Started Associations for user {UserId} with {GroupCount} groups", userId, catalog.Count);
        return game.GetState();
    }

    /// <summary>Get the current Associations game for the user.</summary>
    public AssociationsState? GetAssociationsGameState(int userId) =>
        games.TryGetValue(AssociationsKey(userId), out var game) && game is AssociationsGame associations
            ? associations.GetState()
            : null;

    /// <summary>Submit a guess to the user's Associations game.</summary>
    public AssociationResult SubmitAssociationsGuess(int userId, IReadOnlyList<string> words) =>
        GetGame<AssociationsGame>(AssociationsKey(userId), "Associations").SubmitGuess(words);

    /// <summary>Reset the user's Associations game.</summary>
    public void ResetAssociationsGame(int userId)
    {
        if (games.TryGetValue(AssociationsKey(userId), out var game) && game is AssociationsGame associations)
        {
            associations.Reset();
        }
    }

    // --------------------------------------------------------------------- Shared

    private static string GuessTheWordKey(int userId) => $"guess-word-{userId}";
    private static string WordSearchKey(int userId) => $"word-search-{userId}";
    private static string AssociationsKey(int userId) => $"associations-{userId}";

    private TGame GetGame<TGame>(string sessionKey, string gameName) where TGame : class =>
        games.TryGetValue(sessionKey, out var game) && game is TGame typedGame
            ? typedGame
            : throw new InvalidOperationException($"No active {gameName} game. Start a new game first.");
}
