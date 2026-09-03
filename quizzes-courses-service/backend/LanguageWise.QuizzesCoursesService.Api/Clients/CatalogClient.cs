using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public sealed class CatalogClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DatabaseResponse<DatabaseHealthResponse>> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("health", cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        DatabaseHealthResponse? health = null;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                health = JsonSerializer.Deserialize<DatabaseHealthResponse>(responseBody, JsonOptions);
            }
            catch (JsonException) when (!response.IsSuccessStatusCode)
            {
                // Preserve a non-JSON failure response as dependency error detail.
            }
        }

        if (response.IsSuccessStatusCode && health is null)
        {
            throw new JsonException("The database health response was empty or invalid.");
        }

        return new DatabaseResponse<DatabaseHealthResponse>(
            response.StatusCode,
            health,
            response.IsSuccessStatusCode ? null : responseBody,
            response.Content.Headers.ContentType?.ToString());
    }

    public async Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<Course>>("api/courses", cancellationToken) ?? [];

    public Task<Course?> GetCourseAsync(string code, CancellationToken cancellationToken = default) =>
        GetOptionalAsync<Course>($"api/courses/{Uri.EscapeDataString(code)}", cancellationToken);

    public Task<IReadOnlyList<LessonSummary>?> GetLessonsAsync(
        string courseCode,
        CancellationToken cancellationToken = default) =>
        GetOptionalListAsync<LessonSummary>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/lessons",
            cancellationToken);

    public Task<LessonDetail?> GetLessonAsync(
        string courseCode,
        string lessonSlug,
        CancellationToken cancellationToken = default) =>
        GetOptionalAsync<LessonDetail>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/lessons/{Uri.EscapeDataString(lessonSlug)}",
            cancellationToken);

    public Task<IReadOnlyList<QuizSummary>?> GetQuizzesAsync(
        string courseCode,
        CancellationToken cancellationToken = default) =>
        GetOptionalListAsync<QuizSummary>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/quizzes",
            cancellationToken);

    public Task<QuizDetail?> GetQuizAsync(
        int quizId,
        CancellationToken cancellationToken = default) =>
        GetOptionalAsync<QuizDetail>($"api/quizzes/{quizId}", cancellationToken);

    public Task<IReadOnlyList<FlashcardDeckSummary>?> GetFlashcardDecksAsync(
        string courseCode,
        CancellationToken cancellationToken = default) =>
        GetOptionalListAsync<FlashcardDeckSummary>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/flashcard-decks",
            cancellationToken);

    public Task<FlashcardDeck?> GetFlashcardDeckAsync(
        string courseCode,
        string lessonSlug,
        CancellationToken cancellationToken = default) =>
        GetOptionalAsync<FlashcardDeck>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/flashcard-decks/{Uri.EscapeDataString(lessonSlug)}",
            cancellationToken);

    public Task<CourseProgress?> GetCourseProgressAsync(
        string courseCode,
        int userId,
        CancellationToken cancellationToken = default) =>
        GetOptionalAsync<CourseProgress>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/progress/{userId}",
            cancellationToken);

    /// <summary>Progress for every course the user has started, with per-lesson milestone state.</summary>
    public async Task<IReadOnlyList<StartedCourseProgress>> GetStartedCourseProgressAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<StartedCourseProgress>>(
            $"api/users/{userId}/course-progress",
            cancellationToken) ?? [];

    public async Task<MilestonePage> GetMilestonesAsync(
        int afterId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<MilestonePage>(
            $"api/milestones?afterId={afterId}&limit={limit}",
            cancellationToken)
        ?? throw new JsonException("The milestone page response was empty.");

    public async Task<MilestonePage> GetUserMilestonesAsync(
        int userId,
        int afterId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<MilestonePage>(
            $"api/users/{userId}/milestones?afterId={afterId}&limit={limit}",
            cancellationToken)
        ?? throw new JsonException("The user milestone page response was empty.");

    public Task<DatabaseResponse<QuizAttempt>> StartQuizAttemptAsync(
        int quizId,
        int userId,
        CancellationToken cancellationToken = default) =>
        SendAsync<InternalStartQuizAttemptRequest, QuizAttempt>(
            HttpMethod.Post,
            $"api/quizzes/{quizId}/attempts",
            new InternalStartQuizAttemptRequest(userId),
            cancellationToken);

    public Task<DatabaseResponse<QuizAttemptResult>> SubmitQuizAttemptAsync(
        int attemptId,
        int userId,
        SubmitQuizAttemptRequest request,
        CancellationToken cancellationToken = default) =>
        SendAsync<InternalSubmitQuizAttemptRequest, QuizAttemptResult>(
            HttpMethod.Post,
            $"api/quiz-attempts/{attemptId}/submit",
            new InternalSubmitQuizAttemptRequest(userId, request.Answers),
            cancellationToken);

    public Task<DatabaseResponse<MilestoneState>> SetLessonMilestoneAsync(
        int lessonId,
        int userId,
        bool completed,
        CancellationToken cancellationToken = default) =>
        SendWithoutBodyAsync<MilestoneState>(
            completed ? HttpMethod.Put : HttpMethod.Delete,
            $"api/lessons/{lessonId}/milestones/{userId}",
            cancellationToken);

    public Task<DatabaseResponse<object>> SetCourseMilestoneAsync(
        string courseCode,
        int userId,
        bool completed,
        CancellationToken cancellationToken = default) =>
        SendWithoutBodyAsync<object>(
            completed ? HttpMethod.Put : HttpMethod.Delete,
            $"api/courses/{Uri.EscapeDataString(courseCode)}/milestones/{userId}",
            cancellationToken);

    private async Task<T?> GetOptionalAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private async Task<IReadOnlyList<T>?> GetOptionalListAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken) ?? [];
    }

    private async Task<DatabaseResponse<TResponse>> SendAsync<TRequest, TResponse>(
        HttpMethod method,
        string path,
        TRequest body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<TResponse>(response, cancellationToken);
    }

    private async Task<DatabaseResponse<T>> SendWithoutBodyAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<T>(response, cancellationToken);
    }

    private static async Task<DatabaseResponse<T>> ReadResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var contentType = response.Content.Headers.ContentType?.ToString();
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new DatabaseResponse<T>(response.StatusCode, default, null, contentType);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return new DatabaseResponse<T>(response.StatusCode, value, null, contentType);
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return new DatabaseResponse<T>(response.StatusCode, default, errorBody, contentType);
    }
}

public sealed record DatabaseResponse<T>(
    HttpStatusCode StatusCode,
    T? Value,
    string? ErrorBody,
    string? ContentType)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
}

public sealed record DatabaseHealthResponse(
    string Status,
    string Service,
    long? Courses,
    string? Error);
