namespace LanguageWise.MiniGamesService.Api.Feature.VocabVoyage;

public sealed record LearningContext(
    int? UserId,
    int CourseId,
    int? LessonId,
    string CourseTitle,
    string LessonTitle,
    string ContentMarkdown,
    IReadOnlyList<string> CompletedTopics);

public interface ILearningContextProvider
{
    LearningContext GetContext(int? userId = null);
}

public sealed class FakeLearningContextProvider : ILearningContextProvider
{
    public LearningContext GetContext(int? userId = null) => new(
        userId,
        1,
        1,
        "Everyday English",
        "Introductions",
        "# Introductions\n\nPractice **vocab**, about, after, again, first, house, learn, light, place, and right.",
        ["greetings"]);
}

public static class VocabularySelector
{
    private static readonly string[] fallbackWords =
    [
        "ABOUT", "AFTER", "AGAIN", "FIRST", "HOUSE", "LEARN", "LIGHT", "PLACE", "RIGHT", "WORLD"
    ];

    public static IReadOnlyList<string> GetFiveLetterWords(string contentMarkdown) =>
        contentMarkdown
            .Split([' ', '\t', '\r', '\n', '#', '*', ',', '.', '!', '?', ':', ';', '(', ')'],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Trim().ToUpperInvariant())
            .Where(word => word.Length == 5 && word.All(character => character is >= 'A' and <= 'Z'))
            .Distinct()
            .ToArray();

    public static IReadOnlyList<string> GetCandidates(LearningContext context)
    {
        var courseWords = GetFiveLetterWords(context.ContentMarkdown);
        return courseWords.Count > 0 ? courseWords : fallbackWords;
    }
}