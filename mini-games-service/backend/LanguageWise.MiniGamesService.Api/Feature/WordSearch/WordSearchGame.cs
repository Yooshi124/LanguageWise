namespace LanguageWise.MiniGamesService.Api.Feature.WordSearch;

public sealed class WordSearchGame
{
    private const int Rows = 8;
    private const int Columns = 6;
    private const int MaximumHints = 3;
    private sealed record BoardDefinition(string Hint, string[] Words, string StretchWord, int StretchColumn);

    private static readonly BoardDefinition[] Boards =
    [
        new("Things you might spot beyond our atmosphere", ["ASTRONAUT", "ECLIPSE", "GALAXY", "ROCKET", "COMET", "MARS", "MOON", "RING", "SUN"], "ASTRONAUT", 0),
        new("A place where stories, songs, and performances come alive", ["SPOTLIGHT", "ACTOR", "SCRIPT", "STAGE", "DRAMA", "SCENE", "PLAYS", "ROLE", "ARTS"], "SPOTLIGHT", 5),
        new("Clues from a wild place where leaves hide the path", ["WILDLIFE", "JAGUAR", "CANOPY", "MOSS", "TROPIC", "VINES", "FERNS", "TREE", "BATS"], "WILDLIFE", 2),
        new("Small discoveries waiting in a kitchen drawer", ["COOKWARE", "PLATES", "SPICES", "WHISK", "OVEN", "CUP", "LADLE", "KNIFE", "TEA", "POT"], "COOKWARE", 3)
    ];

    private readonly string language;
    private readonly List<string> words = [];
    private readonly Dictionary<string, IReadOnlyList<int>> wordPaths = [];
    private readonly List<IReadOnlyList<int>> coveragePaths = [];
    private string[] board = [];
    private string featuredWord = string.Empty;
    private string themeHint = string.Empty;
    private string? hintWord;
    private readonly List<string> revealedWords = [];
    private int hintsUsed;
    private int score;
    private bool isGivenUp;
    private bool isComplete;

    public WordSearchGame(string language)
    {
        this.language = language;
        Reset();
    }

    public WordSearchState GetState() => new(
        language, board, Rows, Columns, words.ToArray(), wordPaths.Count, themeHint,
        new Dictionary<string, IReadOnlyList<int>>(wordPaths), coveragePaths.ToArray(), featuredWord,
        hintWord, hintWord is null ? Array.Empty<int>() : wordPaths[hintWord], hintsUsed, MaximumHints,
        revealedWords.ToArray(), isGivenUp, score, isComplete);

    public string[] GetWordChain() => wordPaths.Keys.ToArray();

    public WordSearchMoveResult SubmitWord(string word, IReadOnlyList<int>? indices = null)
    {
        var normalisedWord = word?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalisedWord) || normalisedWord.Length < 3 ||
            normalisedWord.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("The word must contain at least three letters.", nameof(word));
        }
        if (isComplete) throw new InvalidOperationException("This game is already complete.");

        var isValid = IsValidWord(normalisedWord, indices);
        if (isValid && !words.Contains(normalisedWord))
        {
            words.Add(normalisedWord);
            hintWord = hintWord == normalisedWord ? null : hintWord;
            score += normalisedWord.Length;
            isComplete = words.Count == wordPaths.Count;
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
        var availableWords = wordPaths.Keys.Where(word => !words.Contains(word)).ToArray();
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
            revealedWords.AddRange(wordPaths.Keys.Where(word => !words.Contains(word)));
            hintWord = null;
            isGivenUp = true;
            isComplete = true;
        }
        return GetState();
    }

    public void Reset()
    {
        words.Clear();
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

    private void GenerateBoard()
    {
        var definition = Boards[Random.Shared.Next(Boards.Length)];
        featuredWord = definition.StretchWord;
        themeHint = definition.Hint;
        var featuredPath = Enumerable.Range(0, Rows).Select(row => row * Columns + definition.StretchColumn).ToArray();
        if (definition.StretchWord.Length > Rows)
        {
            featuredPath = featuredPath.Append((Rows - 1) * Columns + (definition.StretchColumn == Columns - 1 ? Columns - 2 : definition.StretchColumn + 1)).ToArray();
        }
        var route = BuildRoute(definition.StretchColumn, featuredPath);
        var letters = new string[Rows * Columns];
        AddPlacement(featuredWord, featuredPath, letters);
        var offset = 0;

        foreach (var word in definition.Words.Skip(1))
        {
            var path = route.Skip(offset).Take(word.Length).ToArray();
            if (path.Length != word.Length) throw new InvalidOperationException("Word set does not fit the board.");
            for (var index = 0; index < path.Length; index++) letters[path[index]] = word[index].ToString();
            wordPaths[word] = path;
            coveragePaths.Add(path);
            offset += word.Length;
        }

        if (offset != route.Length || letters.Any(string.IsNullOrEmpty))
            throw new InvalidOperationException("Word set does not cover the board.");
        board = letters;
    }

    private static int[] BuildRoute(int excludedColumn, IReadOnlyList<int> excludedCells)
    {
        if (excludedCells.Count > Rows)
        {
            var sideRoute = new List<int>();
            for (var row = 0; row < Rows - 1; row++)
            {
                var columns = Enumerable.Range(0, Columns).Where(column => column != excludedColumn);
                if ((row + (excludedColumn == Columns - 1 ? 1 : 0)) % 2 == 1) columns = columns.Reverse();
                sideRoute.AddRange(columns.Select(column => row * Columns + column));
            }
            var bottomColumns = Enumerable.Range(0, Columns)
                .Where(column => column != excludedColumn && column != (excludedColumn == Columns - 1 ? Columns - 2 : excludedColumn + 1));
            if (excludedColumn != Columns - 1) bottomColumns = bottomColumns.Reverse();
            sideRoute.AddRange(bottomColumns.Select(column => (Rows - 1) * Columns + column));
            return sideRoute.ToArray();
        }

        var route = new List<int>();
        var routeColumnIndex = 0;
        foreach (var column in Enumerable.Range(0, Columns).Where(column => column != excludedColumn))
        {
            var rows = routeColumnIndex % 2 == 0 ? Enumerable.Range(0, Rows) : Enumerable.Range(0, Rows).Reverse();
            route.AddRange(rows.Select(row => row * Columns + column));
            routeColumnIndex++;
        }
        return route.Where(cell => !excludedCells.Contains(cell)).ToArray();
    }

    private void AddPlacement(string word, int[] path, string[] letters)
    {
        for (var index = 0; index < path.Length; index++) letters[path[index]] = word[index].ToString();
        wordPaths[word] = path;
        coveragePaths.Add(path);
    }
}
