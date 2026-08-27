namespace LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

public sealed record VocabVoyageState(
    string Language,
    int Attempts,
    bool IsComplete,
    bool IsWon,
    IReadOnlyList<VocabVoyageGuessResult> Guesses);

public sealed record VocabVoyageGuessResult(
    string Guess,
    char[] Colours,
    bool IsCorrect);

public sealed record VocabVoyageGuessRequest(string Guess);
