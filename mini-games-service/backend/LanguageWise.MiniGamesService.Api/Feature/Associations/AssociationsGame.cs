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

    /// <summary>Create a game whose groups come from the user's course vocabulary (one group per lesson).</summary>
    public AssociationsGame(string language, IReadOnlyList<AssociationGroup> associationCatalog)
    {
        this.language = language;
        this.associationCatalog = associationCatalog
            .Select(group => group with { Words = group.Words.Distinct().Take(GroupSize).ToArray() })
            .ToArray();

        if (this.associationCatalog.Count < GroupCount ||
            this.associationCatalog.Any(group => group.Words.Count < GroupSize) ||
            this.associationCatalog.SelectMany(group => group.Words).Distinct().Count() < GroupCount * GroupSize)
        {
            throw new ArgumentException(
                $"At least {GroupCount} groups of {GroupSize} distinct words are required.", nameof(associationCatalog));
        }

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
}
