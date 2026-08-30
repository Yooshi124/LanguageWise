namespace LanguageWise.MiniGamesService.Api.Feature.WordStrings;

public sealed record WordStringsState(
    string Language,
    IReadOnlyList<string> Board,
    int Columns,
    IReadOnlyList<string> Words,
    int TotalWords,
    string ThemeHint,
    int Score,
    bool IsComplete);

public sealed record WordStringsMoveResult(
    string Word,
    bool IsValid,
    WordStringsState State);
