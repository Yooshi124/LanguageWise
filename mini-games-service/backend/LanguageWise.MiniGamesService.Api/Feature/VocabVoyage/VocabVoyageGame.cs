namespace LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

public sealed class VocabVoyageGame
{
    private const int WordLength = 5;
    private const int MaximumAttempts = 6;
    private readonly string language;
    private readonly IReadOnlyList<string> candidateWords;
    private readonly List<VocabVoyageGuessResult> guesses = [];
    private string answer;
    private int attempts = 0;
    private bool isComplete = false;
    private bool isWon = false;

    public VocabVoyageGame(string language, IReadOnlyList<string>? candidateWords = null)
    {
        this.language = language;
        this.candidateWords = candidateWords ?? ["VOCAB"];
        answer = SelectAnswer(this.candidateWords);
    }

    public VocabVoyageState GetState() =>
        new(language, attempts, isComplete, isWon, guesses.ToArray(), isComplete ? answer : null);

    public VocabVoyageGuessResult SubmitGuess(string guess)
    {
        var normalisedGuess = guess?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalisedGuess) ||
            normalisedGuess.Length != WordLength ||
            normalisedGuess.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("The guess must contain exactly five letters.", nameof(guess));
        }

        if (isComplete)
        {
            throw new InvalidOperationException("This game is already complete.");
        }

        var colours = GetGuessColours(normalisedGuess);
        var isCorrect = colours.All(colour => colour == 'G');
        var result = new VocabVoyageGuessResult(normalisedGuess, colours, isCorrect);

        guesses.Add(result);
        attempts++;
        isWon = isCorrect;
        isComplete = isCorrect || attempts >= MaximumAttempts;

        if (isComplete && !isWon)
        {
            result = result with { CorrectAnswer = answer };
            guesses[^1] = result;
        }

        return result;
    }

    public void Reset()
    {
        guesses.Clear();
        attempts = 0;
        isComplete = false;
        isWon = false;
        answer = SelectAnswer(candidateWords);
    }

    private static string SelectAnswer(IReadOnlyList<string> candidateWords)
    {
        var validAnswers = candidateWords
            .Select(candidate => candidate.Trim().ToUpperInvariant())
            .Where(candidate => candidate.Length == WordLength &&
                candidate.All(character => character is >= 'A' and <= 'Z'))
            .ToArray();

        if (validAnswers.Length == 0)
        {
            throw new ArgumentException("At least one valid five-letter answer is required.", nameof(candidateWords));
        }

        return validAnswers[Random.Shared.Next(validAnswers.Length)];
    }

    private char[] GetGuessColours(string guess)
    {
        var colours = Enumerable.Repeat('R', WordLength).ToArray();
        var remainingAnswerLetters = answer.ToCharArray();

        for (var index = 0; index < WordLength; index++)
        {
            if (guess[index] != answer[index])
            {
                continue;
            }

            colours[index] = 'G';
            remainingAnswerLetters[index] = '\0';
        }

        for (var index = 0; index < WordLength; index++)
        {
            if (colours[index] == 'G')
            {
                continue;
            }

            var matchingIndex = Array.IndexOf(remainingAnswerLetters, guess[index]);
            if (matchingIndex >= 0)
            {
                colours[index] = 'O';
                remainingAnswerLetters[matchingIndex] = '\0';
            }
        }

        return colours;
    }
}
