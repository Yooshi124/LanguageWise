using LanguageWise.MiniGamesService.Api.Clients;

namespace LanguageWise.MiniGamesService.Api.Feature.Vocabulary;

/// <summary>A named group of playable vocabulary words, typically the words from one completed lesson.</summary>
public sealed record VocabularyGroup(string Title, IReadOnlyList<string> Words);

/// <summary>Provides vocabulary words from course content based on the user's completed milestones.</summary>
public interface IVocabularyProvider
{
    /// <summary>
    /// All playable vocabulary from the lessons the user has completed in the courses they have
    /// started. Empty when the user has completed nothing. When <paramref name="courseCode"/> is
    /// given, only that course's vocabulary is included.
    /// </summary>
    Task<IReadOnlyList<string>> GetVocabularyAsync(string? courseCode, string? accessToken, CancellationToken cancellationToken = default);

    /// <summary>Playable vocabulary grouped by the lesson it came from (completed lessons only).</summary>
    Task<IReadOnlyList<VocabularyGroup>> GetVocabularyGroupsAsync(string? courseCode, string? accessToken, CancellationToken cancellationToken = default);
}

/// <summary>Thrown when a game cannot start because the user has no playable vocabulary yet.</summary>
public sealed class NoVocabularyAvailableException(string gameName) : Exception(
    $"There are no words available to start {gameName}. Completing more course content will unlock new vocabulary.")
{
    public string GameName { get; } = gameName;
}

/// <summary>Normalises course vocabulary entries into words the games can use.</summary>
public static class PlayableWords
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 15;

    /// <summary>A word the games can render and validate: letters only (any alphabet, so umlauts etc. work).</summary>
    public static bool IsPlayable(string word) =>
        word.Length is >= MinimumLength and <= MaximumLength && word.All(char.IsLetter);

    /// <summary>
    /// Extract playable words from raw course vocabulary entries. Entries are normalised to uppercase
    /// letter-only tokens, so phrases like "Guten Tag" contribute GUTEN and TAG. Tokens that appear in
    /// more than one entry (e.g. articles such as "die" in "die Familie", "die Mutter") are dropped,
    /// because they are connectors rather than vocabulary.
    /// </summary>
    public static IReadOnlyList<string> Extract(IEnumerable<string> entries)
    {
        var tokensPerEntry = entries
            .Select(Tokenize)
            .Where(tokens => tokens.Length > 0)
            .ToList();

        return tokensPerEntry
            .SelectMany(tokens => tokens)
            .Distinct()
            .Where(token => tokensPerEntry.Count(tokens => tokens.Contains(token)) == 1)
            .ToList();
    }

    private static string[] Tokenize(string entry)
    {
        var tokens = new List<string>();
        var current = new List<char>();

        foreach (var character in entry)
        {
            if (char.IsLetter(character))
            {
                current.Add(char.ToUpperInvariant(character));
            }
            else if (current.Count > 0)
            {
                Flush(current, tokens);
            }
        }

        if (current.Count > 0)
        {
            Flush(current, tokens);
        }

        return tokens.Distinct().ToArray();

        static void Flush(List<char> current, List<string> tokens)
        {
            var token = new string(current.ToArray());
            if (IsPlayable(token))
            {
                tokens.Add(token);
            }
            current.Clear();
        }
    }
}

/// <summary>
/// Vocabulary provider that resolves the user's unlocked vocabulary from the quizzes-courses API:
/// the courses they have started, limited to lessons whose milestone they have achieved.
/// </summary>
public sealed class CourseVocabularyProvider(CourseVocabularyClient courseClient, ILogger<CourseVocabularyProvider> logger) : IVocabularyProvider
{
    public async Task<IReadOnlyList<string>> GetVocabularyAsync(string? courseCode, string? accessToken, CancellationToken cancellationToken = default)
    {
        var groups = await GetVocabularyGroupsAsync(courseCode, accessToken, cancellationToken);
        return groups.SelectMany(group => group.Words).Distinct().ToList();
    }

    public async Task<IReadOnlyList<VocabularyGroup>> GetVocabularyGroupsAsync(string? courseCode, string? accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var vocabulary = await courseClient.GetUserVocabularyAsync(accessToken, cancellationToken);
            if (vocabulary is null)
            {
                logger.LogWarning("User vocabulary unavailable from the courses service (course filter: {CourseCode})", courseCode ?? "none");
                return [];
            }

            var courses = vocabulary.Courses.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(courseCode))
            {
                courses = courses.Where(course => string.Equals(course.Code, courseCode, StringComparison.OrdinalIgnoreCase));
            }

            var groups = new List<VocabularyGroup>();
            foreach (var course in courses)
            {
                foreach (var lesson in course.Lessons)
                {
                    var words = PlayableWords.Extract(lesson.Vocabulary.Select(entry => entry.Word));
                    if (words.Count > 0)
                    {
                        groups.Add(new VocabularyGroup($"{course.Title} — {lesson.Title}", words));
                    }
                }
            }

            if (groups.Count == 0)
            {
                logger.LogInformation("No unlocked vocabulary for the user (course filter: {CourseCode})", courseCode ?? "none");
            }

            return groups;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error retrieving vocabulary from the courses service (course filter: {CourseCode})", courseCode ?? "none");
            return [];
        }
    }
}
