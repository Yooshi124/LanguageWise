namespace LanguageWise.MiniGamesService.Api.Feature.WordStrings;

public sealed class WordStringsGame
{
    private const int Columns = 10;
    private const string ThemeHint = "Things you might spot beyond our atmosphere";
    private static readonly string[] WordOptions =
    [
        "SUN", "MOON", "STAR", "COMET", "EARTH", "ORBIT", "ROCKET", "PLANET", "GALAXY", "NEBULA"
    ];

    private readonly string language;
    private readonly List<string> words = [];
    private readonly IReadOnlyList<string> board;
    private int score = 0;
    private bool isComplete = false;

    public WordStringsGame(string language)
    {
        this.language = language;
        board = WordOptions.SelectMany(word => word).Select(character => character.ToString()).ToArray();
    }

    public WordStringsState GetState() =>
        new(language, board, Columns, words.ToArray(), WordOptions.Length, ThemeHint, score, isComplete);

    public string[] GetWordChain()
    {
        return WordOptions.ToArray();
    }

    public WordStringsMoveResult SubmitWord(string word)
    {
        var normalisedWord = word?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalisedWord) ||
            normalisedWord.Length < 3 ||
            normalisedWord.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("The word must contain at least three letters.", nameof(word));
        }

        if (isComplete)
        {
            throw new InvalidOperationException("This game is already complete.");
        }

        var isValid = IsValidWord(normalisedWord);
        if (isValid && !words.Contains(normalisedWord))
        {
            words.Add(normalisedWord);
            score += normalisedWord.Length;
            isComplete = words.Count == WordOptions.Length;
        }

        return new WordStringsMoveResult(normalisedWord, isValid, GetState());
    }

    public bool IsValidWord(string word)
    {
        return WordOptions.Contains(word.Trim().ToUpperInvariant());
    }

    public void Reset()
    {
        words.Clear();
        score = 0;
        isComplete = false;
    }
}
