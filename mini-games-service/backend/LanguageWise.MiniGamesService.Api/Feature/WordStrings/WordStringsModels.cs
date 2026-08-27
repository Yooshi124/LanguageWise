namespace LanguageWise.MiniGamesService.Api.Feature.WordStrings;

public sealed record WordStringsState(
    string Language,
    IReadOnlyList<string> Words,
    int Score,
    bool IsComplete);

public sealed record WordStringsMoveResult(
    string Word,
    bool IsValid,
    WordStringsState State);
