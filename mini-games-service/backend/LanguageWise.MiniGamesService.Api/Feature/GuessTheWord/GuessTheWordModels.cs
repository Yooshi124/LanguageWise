namespace LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;

public sealed record GuessTheWordState(
    string Language,
    int Attempts,
    bool IsComplete,
    bool IsWon,
    IReadOnlyList<GuessTheWordGuessResult> Guesses,
    string? CorrectAnswer,
    IReadOnlyList<string> SpecialLetters,
    IReadOnlyDictionary<string, string>? Definitions = null,
    // Seconds since the round started; only populated once the round is complete, for the
    // end-of-round summary panel.
    int? ElapsedSeconds = null);

public sealed record GuessTheWordGuessResult(
    string Guess,
    char[] Colours,
    bool IsCorrect,
    string? CorrectAnswer = null,
    IReadOnlyDictionary<string, string>? Definitions = null,
    int? ElapsedSeconds = null);

public sealed record GuessTheWordGuessRequest(string Guess);
