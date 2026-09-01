namespace LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;

using System.Globalization;
using System.Text;

public sealed class GuessTheWordGame
{
    private const int WordLength = 5;
    private const int MaximumAttempts = 6;
    private readonly string language;
    private readonly IReadOnlyList<string> candidateWords;
    private readonly List<GuessTheWordGuessResult> guesses = [];
    private string answer;
    private int attempts = 0;
    private bool isComplete = false;
    private bool isWon = false;

    /// <summary>Create a game whose answer is drawn from the user's course vocabulary.</summary>
    public GuessTheWordGame(string language, IReadOnlyList<string> candidateWords)
    {
        this.language = language;
        this.candidateWords = candidateWords;
        answer = SelectAnswer(this.candidateWords);
    }

    public GuessTheWordState GetState() =>
        new(language, attempts, isComplete, isWon, guesses.ToArray(), isComplete ? answer : null);

    public GuessTheWordGuessResult SubmitGuess(string guess)
    {
        var normalisedGuess = NormalizeWord(guess);
        if (string.IsNullOrWhiteSpace(normalisedGuess) ||
            normalisedGuess.Length != WordLength ||
            normalisedGuess.Any(character => !char.IsLetter(character)))
        {
            throw new ArgumentException("The guess must contain exactly five letters.", nameof(guess));
        }

        if (isComplete)
        {
            throw new InvalidOperationException("This game is already complete.");
        }

        var colours = GetGuessColours(normalisedGuess);
        var isCorrect = colours.All(colour => colour == 'G');
        var result = new GuessTheWordGuessResult(normalisedGuess, colours, isCorrect);

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
            .Select(NormalizeWord)
            .Where(candidate => candidate.Length == WordLength && candidate.All(char.IsLetter))
            .ToArray();

        if (validAnswers.Length == 0)
        {
            throw new ArgumentException("At least one valid five-letter answer is required.", nameof(candidateWords));
        }

        return validAnswers[Random.Shared.Next(validAnswers.Length)];
    }

    private char[] GetGuessColours(string guess)
    {
        // Compare letters with diacritics folded away, so typing A still
        // matches Ä in the answer. ß is not a diacritic variant and stays
        // its own letter.
        var guessLetters = guess.Select(FoldLetter).ToArray();
        var answerLetters = answer.Select(FoldLetter).ToArray();
        var colours = Enumerable.Repeat('R', WordLength).ToArray();
        var remainingAnswerLetters = answerLetters.ToArray();

        for (var index = 0; index < WordLength; index++)
        {
            if (guessLetters[index] != answerLetters[index])
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

            var matchingIndex = Array.IndexOf(remainingAnswerLetters, guessLetters[index]);
            if (matchingIndex >= 0)
            {
                colours[index] = 'O';
                remainingAnswerLetters[matchingIndex] = '\0';
            }
        }

        return colours;
    }

    /// <summary>
    /// Trim and uppercase per character (rather than with string.ToUpperInvariant,
    /// which would expand ß into SS and corrupt five-letter words containing it).
    /// </summary>
    private static string NormalizeWord(string? word)
    {
        var trimmed = word?.Trim();
        return string.IsNullOrEmpty(trimmed)
            ? string.Empty
            : new string(trimmed.Select(char.ToUpperInvariant).ToArray());
    }

    /// <summary>Strip diacritics so accented letters compare equal to their base letter (Ä → A).</summary>
    private static char FoldLetter(char letter)
    {
        var decomposed = letter.ToString().Normalize(NormalizationForm.FormD);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                return character;
            }
        }

        return letter;
    }
}
