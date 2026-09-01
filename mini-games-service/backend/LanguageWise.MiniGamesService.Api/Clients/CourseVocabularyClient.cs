using System.Net.Http.Json;
using System.Text.Json;

namespace LanguageWise.MiniGamesService.Api.Clients;

/// <summary>Represents vocabulary from course content.</summary>
public sealed record VocabularyWord(string Word, string Meaning);

/// <summary>Represents a lesson with vocabulary.</summary>
public sealed record LessonDetail(
    int Id,
    string Slug,
    string Title,
    int SortOrder,
    string ContentMarkdown,
    IReadOnlyList<VocabularyWord> Vocabulary);

/// <summary>Represents course progress for a user.</summary>
public sealed record CourseProgress(
    bool CourseCompleted,
    bool CourseEligible,
    IReadOnlyList<LessonProgress> Lessons,
    IReadOnlyList<QuizProgress> Quizzes);

public sealed record LessonProgress(int LessonId, bool Completed);
public sealed record QuizProgress(int QuizId, int LessonId, bool Completed, int? BestScore, int TotalQuestions);

/// <summary>Client for accessing course content and vocabulary from the quizzes-courses service.</summary>
public sealed class CourseVocabularyClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Get a lesson with its vocabulary content.</summary>
    public async Task<LessonDetail?> GetLessonAsync(
        string courseCode,
        string lessonSlug,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<LessonDetail>(
                $"api/courses/{Uri.EscapeDataString(courseCode)}/lessons/{Uri.EscapeDataString(lessonSlug)}",
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>Get course progress for a user, which includes completed milestones.</summary>
    public async Task<CourseProgress?> GetCourseProgressAsync(
        string courseCode,
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<CourseProgress>(
                $"api/courses/{Uri.EscapeDataString(courseCode)}/progress/{userId}",
                JsonOptions,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>Get all lessons for a course.</summary>
    public async Task<IReadOnlyList<LessonSummary>?> GetLessonsAsync(
        string courseCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<LessonSummary>>(
                $"api/courses/{Uri.EscapeDataString(courseCode)}/lessons",
                JsonOptions,
                cancellationToken) as IReadOnlyList<LessonSummary>;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}

public sealed record LessonSummary(int Id, string Slug, string Title, int SortOrder);
