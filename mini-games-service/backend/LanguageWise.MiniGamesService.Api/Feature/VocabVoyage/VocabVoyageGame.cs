namespace LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

public sealed class VocabVoyageGame
{
    private const int WordLength = 5;
    private const int MaximumAttempts = 6;
    private readonly string language;
    private readonly List<VocabVoyageGuessResult> guesses = [];
    private string answer;
    private int attempts = 0;
    private bool isComplete = false;
    private bool isWon = false;

    public VocabVoyageGame(string language)
    {
        this.language = language;
        answer = GenerateAnswer(language);
    }

    public VocabVoyageState GetState() =>
        new(language, attempts, isComplete, isWon, guesses);

    public VocabVoyageGuessResult SubmitGuess(string guess)
    {
        if (string.IsNullOrWhiteSpace(guess) || guess.Trim().Length != WordLength)
        {
            throw new ArgumentException("The guess must contain exactly five letters.", nameof(guess));
        }

        if (isComplete)
        {
            throw new InvalidOperationException("This game is already complete.");
        }

        var normalisedGuess = guess.Trim().ToUpperInvariant();
        var colours = GetGuessColours(normalisedGuess);
        var isCorrect = colours.All(colour => colour == 'G');
        var result = new VocabVoyageGuessResult(normalisedGuess, colours, isCorrect);

        guesses.Add(result);
        attempts++;
        isWon = isCorrect;
        isComplete = isCorrect || attempts >= MaximumAttempts;

        return result;
    }

    public void Reset()
    {
        guesses.Clear();
        attempts = 0;
        isComplete = false;
        isWon = false;
        answer = GenerateAnswer(language);
    }

    private static string GenerateAnswer(string language)
    {
        return "VOCAB";
    }

    private char[] GetGuessColours(string guess)
    {
        var colours = new char[WordLength];
        for (var index = 0; index < WordLength; index++)
        {
            colours[index] = guess[index] == answer[index]
                ? 'G'
                : answer.Contains(guess[index]) ? 'O' : 'R';
        }

        return colours;
    }
}
