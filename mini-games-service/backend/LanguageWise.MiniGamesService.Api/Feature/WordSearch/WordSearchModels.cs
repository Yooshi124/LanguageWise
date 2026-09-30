namespace LanguageWise.MiniGamesService.Api.Feature.WordSearch;

public sealed record WordSearchState(
    string Language,
    IReadOnlyList<string> Board,
    int Rows,
    int Columns,
    IReadOnlyList<string> Words,
    int TotalWords,
    string ThemeHint,
    IReadOnlyDictionary<string, IReadOnlyList<int>> WordPaths,
    IReadOnlyList<IReadOnlyList<int>> CoveragePaths,
    string FeaturedWord,
    string? HintWord,
    IReadOnlyList<int> HintPath,
    int HintsUsed,
    int MaximumHints,
    IReadOnlyList<string> RevealedWords,
    bool IsGivenUp,
    int Score,
    bool IsComplete,
    IReadOnlyDictionary<string, string>? Definitions = null,
    // Seconds since the round started; only populated once the round is complete, for the
    // end-of-round summary panel.
    int? ElapsedSeconds = null);

public sealed record WordSearchMoveResult(
    string Word,
    bool IsValid,
    WordSearchState State);

public sealed record WordSearchGuessRequest(string Word, IReadOnlyList<int> Indices);

public sealed record WordSearchHintResult(string Word, IReadOnlyList<int> Path, WordSearchState State);
