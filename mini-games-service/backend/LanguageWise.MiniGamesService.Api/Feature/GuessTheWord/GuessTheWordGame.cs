namespace LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;

public sealed class GuessTheWordGame
{
    private const int WordLength = 5;
    private const int MaximumAttempts = 6;
    private readonly string language;
    private readonly IReadOnlyList<string> candidateWords;
    private readonly IReadOnlyList<string> specialLetters;
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
        specialLetters = candidateWords
            .SelectMany(word => word)
            .Select(char.ToUpperInvariant)
            .Distinct()
            // Letters that fold onto A-Z (Ä, Ñ, Ł…) can be typed as the plain letter;
            // only letters that survive folding untypeable (ß…) need a button.
            .Where(letter => FoldLetter(letter) is < 'A' or > 'Z')
            .OrderBy(letter => letter)
            .Select(letter => letter.ToString())
            .ToArray();
        answer = SelectAnswer(this.candidateWords);
    }

    public GuessTheWordState GetState() =>
        new(language, attempts, isComplete, isWon, guesses.ToArray(), isComplete ? answer : null, specialLetters);

    /// <summary>The current answer, exposed so the round can be persisted when the game starts.</summary>
    public string Solution => answer;

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

    /// <summary>
    /// Fold accented letters onto their base letter (Ä → A) so players can type
    /// plain ASCII. ß is NOT a diacritic variant and deliberately stays its own
    /// letter. Implemented with an explicit table rather than Unicode
    /// normalization because the app runs with InvariantGlobalization, where
    /// NFD decomposition / Unicode categories are unavailable.
    /// </summary>
    private static char FoldLetter(char letter) => letter switch
    {
        >= 'A' and <= 'Z' => letter,

        // A-family
        'À' or 'Á' or 'Â' or 'Ã' or 'Ä' or 'Å' or 'Ā' or 'Ă' or 'Ą' => 'A',
        // AE ligature
        'Æ' => 'A',
        // C-family
        'Ç' or 'Ć' or 'Ĉ' or 'Ċ' or 'Č' => 'C',
        // D-family
        'Ď' or 'Đ' => 'D',
        // E-family
        'È' or 'É' or 'Ê' or 'Ë' or 'Ē' or 'Ĕ' or 'Ė' or 'Ę' or 'Ě' => 'E',
        // G-family
        'Ĝ' or 'Ğ' or 'Ġ' or 'Ģ' => 'G',
        // H-family
        'Ĥ' or 'Ħ' => 'H',
        // I-family
        'Ì' or 'Í' or 'Î' or 'Ï' or 'Ĩ' or 'Ī' or 'Ĭ' or 'Į' or 'İ' => 'I',
        // J
        'Ĵ' => 'J',
        // K
        'Ķ' => 'K',
        // L-family (Ł is deliberately folded to L so plain typing works)
        'Ĺ' or 'Ļ' or 'Ľ' or 'Ŀ' or 'Ł' => 'L',
        // N-family
        'Ñ' or 'Ń' or 'Ņ' or 'Ň' => 'N',
        // O-family
        'Ò' or 'Ó' or 'Ô' or 'Õ' or 'Ö' or 'Ø' or 'Ō' or 'Ŏ' or 'Ő' => 'O',
        // OE ligature
        'Œ' => 'O',
        // R-family
        'Ŕ' or 'Ŗ' or 'Ř' => 'R',
        // S-family
        'Ś' or 'Ŝ' or 'Ş' or 'Š' => 'S',
        // T-family
        'Ţ' or 'Ť' or 'Ŧ' => 'T',
        // U-family
        'Ù' or 'Ú' or 'Û' or 'Ü' or 'Ũ' or 'Ū' or 'Ŭ' or 'Ů' or 'Ű' or 'Ų' => 'U',
        // W
        'Ŵ' => 'W',
        // Y-family
        'Ý' or 'Þ' or 'Ÿ' or 'Ŷ' => 'Y',
        // Z-family
        'Ź' or 'Ż' or 'Ž' => 'Z',

        // Anything else (including ß) is kept as its own letter.
        _ => letter,
    };
}
