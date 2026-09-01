using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;

namespace LanguageWise.MiniGamesService.Api.Feature.WordSearch;

public sealed class WordSearchGame
{
    private const int Rows = 8;
    private const int Columns = 6;
    private const int MaximumHints = 3;
    private const int MaximumWords = 10;

    private readonly string language;
    private readonly string themeHint;
    private readonly IReadOnlyList<string> vocabulary;
    private readonly List<string> foundWords = [];
    private readonly Dictionary<string, IReadOnlyList<int>> wordPaths = [];
    private readonly List<IReadOnlyList<int>> coveragePaths = [];
    private string[] board = [];
    private string featuredWord = string.Empty;
    private string? hintWord;
    private readonly List<string> revealedWords = [];
    private int hintsUsed;
    private int score;
    private bool isGivenUp;
    private bool isComplete;

    /// <summary>Create a game whose board is generated from the user's course vocabulary.</summary>
    /// <param name="language">Display language label for the game.</param>
    /// <param name="themeHint">Hint shown in the sidebar, e.g. the lessons the words came from.</param>
    /// <param name="vocabulary">Candidate words; filtered to playable words (letters only, 3-15 characters).</param>
    public WordSearchGame(string language, string themeHint, IReadOnlyList<string> vocabulary)
    {
        this.language = language;
        this.themeHint = themeHint;
        this.vocabulary = vocabulary
            .Select(word => word.Trim().ToUpperInvariant())
            .Where(PlayableWords.IsPlayable)
            .Distinct()
            .ToArray();

        if (this.vocabulary.Count == 0)
        {
            throw new ArgumentException("At least one playable word is required.", nameof(vocabulary));
        }

        Reset();
    }

    public WordSearchState GetState() => new(
        language, board, Rows, Columns, foundWords.ToArray(), wordPaths.Count, themeHint,
        new Dictionary<string, IReadOnlyList<int>>(wordPaths), coveragePaths.ToArray(), featuredWord,
        hintWord, hintWord is null ? Array.Empty<int>() : wordPaths[hintWord], hintsUsed, MaximumHints,
        revealedWords.ToArray(), isGivenUp, score, isComplete);

    public string[] GetWordChain() => wordPaths.Keys.ToArray();

    public WordSearchMoveResult SubmitWord(string word, IReadOnlyList<int>? indices = null)
    {
        var normalisedWord = word?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalisedWord) || normalisedWord.Length < PlayableWords.MinimumLength ||
            normalisedWord.Any(character => !char.IsLetter(character)))
        {
            throw new ArgumentException("The word must contain at least three letters.", nameof(word));
        }
        if (isComplete) throw new InvalidOperationException("This game is already complete.");

        var isValid = IsValidWord(normalisedWord, indices);
        if (isValid && !foundWords.Contains(normalisedWord))
        {
            foundWords.Add(normalisedWord);
            hintWord = hintWord == normalisedWord ? null : hintWord;
            score += normalisedWord.Length;
            isComplete = foundWords.Count == wordPaths.Count;
        }
        return new WordSearchMoveResult(normalisedWord, isValid, GetState());
    }

    public bool IsValidWord(string word, IReadOnlyList<int>? indices = null)
    {
        var normalisedWord = word.Trim().ToUpperInvariant();
        if (!wordPaths.TryGetValue(normalisedWord, out var path)) return false;
        return indices is null || path.SequenceEqual(indices) || path.Reverse().SequenceEqual(indices);
    }

    public WordSearchHintResult UseHint()
    {
        if (isComplete) throw new InvalidOperationException("This game is already complete.");
        if (hintsUsed >= MaximumHints) throw new InvalidOperationException("You have used all three hints.");
        var availableWords = wordPaths.Keys.Where(word => !foundWords.Contains(word)).ToArray();
        if (availableWords.Length == 0) throw new InvalidOperationException("There are no words left to hint.");
        hintWord = availableWords[Random.Shared.Next(availableWords.Length)];
        hintsUsed++;
        return new WordSearchHintResult(hintWord, wordPaths[hintWord], GetState());
    }

    public WordSearchState GiveUp()
    {
        if (!isComplete)
        {
            revealedWords.Clear();
            revealedWords.AddRange(wordPaths.Keys.Where(word => !foundWords.Contains(word)));
            hintWord = null;
            isGivenUp = true;
            isComplete = true;
        }
        return GetState();
    }

    public void Reset()
    {
        foundWords.Clear();
        wordPaths.Clear();
        coveragePaths.Clear();
        revealedWords.Clear();
        hintsUsed = 0;
        hintWord = null;
        score = 0;
        isGivenUp = false;
        isComplete = false;
        GenerateBoard();
    }

    /// <summary>
    /// Lay vocabulary words out along a serpentine route so every word forms a contiguous path,
    /// then fill the remaining cells with letters drawn from the placed words.
    /// </summary>
    private void GenerateBoard()
    {
        var route = BuildRoute();
        var letters = new string[Rows * Columns];
        var offset = 0;

        foreach (var word in vocabulary.OrderBy(_ => Random.Shared.Next()))
        {
            if (wordPaths.Count >= MaximumWords) break;
            if (word.Length > route.Length - offset) continue;

            var path = route.Skip(offset).Take(word.Length).ToArray();
            for (var index = 0; index < path.Length; index++) letters[path[index]] = word[index].ToString();
            wordPaths[word] = path;
            coveragePaths.Add(path);
            offset += word.Length;
        }

        if (wordPaths.Count == 0)
            throw new ArgumentException("The vocabulary words do not fit on the board.", nameof(vocabulary));

        featuredWord = wordPaths.Keys.OrderByDescending(word => word.Length).First();

        var fillerLetters = string.Concat(wordPaths.Keys).ToCharArray();
        for (var index = 0; index < letters.Length; index++)
        {
            letters[index] ??= fillerLetters[Random.Shared.Next(fillerLetters.Length)].ToString();
        }

        board = letters;
    }

    /// <summary>A snake-like path that visits every cell of the board exactly once.</summary>
    private static int[] BuildRoute()
    {
        var route = new List<int>(Rows * Columns);
        for (var column = 0; column < Columns; column++)
        {
            var rows = column % 2 == 0 ? Enumerable.Range(0, Rows) : Enumerable.Range(0, Rows).Reverse();
            route.AddRange(rows.Select(row => row * Columns + column));
        }
        return route.ToArray();
    }
}
