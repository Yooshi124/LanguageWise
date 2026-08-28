namespace LanguageWise.MiniGamesService.Api.Feature.Associations;

public sealed class AssociationsGame
{
    private const int GroupSize = 4;
    private const int GroupCount = 4;
    private const int MaximumFailures = 4;
    private readonly string language;
    private readonly IReadOnlyList<AssociationGroup> associationCatalog;
    private readonly List<AssociationGroup> groups = [];
    private readonly List<string> words = [];
    private readonly List<string> selectedWords = [];
    private readonly List<AssociationGroup> solvedGroups = [];
    private int failedAttempts;
    private bool isComplete = false;
    private bool isWon = false;

    public AssociationsGame(string language, IReadOnlyList<AssociationGroup>? associationCatalog = null)
    {
        this.language = language;
        this.associationCatalog = associationCatalog ?? DefaultGroups;
        Reset();
    }

    public AssociationsState GetState() =>
        new(language, words.ToArray(), selectedWords.ToArray(), solvedGroups.ToArray(), failedAttempts, isComplete, isWon,
            isComplete && !isWon ? groups.Except(solvedGroups).ToArray() : []);

    public string[] GetWords()
    {
        return words.ToArray();
    }

    public AssociationResult SubmitGuess(IReadOnlyList<string> guessedWords)
    {
        if (isComplete)
        {
            throw new InvalidOperationException("This game is already complete.");
        }

        var normalisedWords = guessedWords
            .Select(word => word.Trim().ToUpperInvariant())
            .Distinct()
            .ToArray();
        if (normalisedWords.Length != GroupSize || normalisedWords.Any(word => !words.Contains(word)))
        {
            throw new ArgumentException("Choose exactly four available words.", nameof(guessedWords));
        }

        var group = groups.FirstOrDefault(candidate =>
            candidate.Words.OrderBy(word => word).SequenceEqual(normalisedWords.OrderBy(word => word)));
        if (group is not null)
        {
            solvedGroups.Add(group);
            foreach (var word in normalisedWords)
            {
                words.Remove(word);
            }

            isWon = solvedGroups.Count == GroupCount;
            isComplete = isWon;
        }
        else
        {
            failedAttempts++;
            isComplete = failedAttempts >= MaximumFailures;
        }

        selectedWords.Clear();
        return new AssociationResult(group is not null, group, GetState());
    }

    public AssociationResult SelectPair(string firstWord, string secondWord) =>
        throw new NotSupportedException("Associations guesses must contain four words.");

    public bool IsAssociation(string firstWord, string secondWord) => false;

    public void Reset()
    {
        groups.Clear();
        groups.AddRange(associationCatalog.OrderBy(_ => Random.Shared.Next()).Take(GroupCount));
        words.Clear();
        words.AddRange(groups.SelectMany(group => group.Words).OrderBy(_ => Random.Shared.Next()));
        selectedWords.Clear();
        solvedGroups.Clear();
        failedAttempts = 0;
        isComplete = false;
        isWon = false;
    }

    private static readonly AssociationGroup[] DefaultGroups =
    [
        new("Fruit", ["APPLE", "MANGO", "PEACH", "GRAPE"]),
        new("Things in a classroom", ["DESK", "CHAIR", "BOARD", "PENCIL"]),
        new("Modes of transport", ["TRAIN", "PLANE", "BOAT", "TRUCK"]),
        new("Weather", ["CLOUD", "RAINY", "STORM", "SUNNY"]),
        new("Kitchen items", ["SPOON", "KNIFE", "PLATE", "OVEN"]),
        new("Body parts", ["HEART", "BRAIN", "MOUTH", "TEETH"]),
        new("Musical instruments", ["PIANO", "DRUMS", "FLUTE", "GUITAR"]),
        new("Outdoor activities", ["HIKE", "CAMP", "SWIM", "CLIMB"]),
        new("Colours", ["GREEN", "BLACK", "WHITE", "BROWN"]),
        new("Things that shine", ["LIGHT", "STARS", "CROWN", "JEWEL"])
    ];
}
