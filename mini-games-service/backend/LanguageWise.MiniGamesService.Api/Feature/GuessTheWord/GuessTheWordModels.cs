namespace LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;

public sealed record GuessTheWordState(
    string Language,
    int Attempts,
    bool IsComplete,
    bool IsWon,
    IReadOnlyList<GuessTheWordGuessResult> Guesses,
    string? CorrectAnswer);

public sealed record GuessTheWordGuessResult(
    string Guess,
    char[] Colours,
    bool IsCorrect,
    string? CorrectAnswer = null);

public sealed record GuessTheWordGuessRequest(string Guess);
