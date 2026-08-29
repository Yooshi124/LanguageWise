using System.Net;
using System.Net.Http.Json;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public sealed class CatalogClient(HttpClient httpClient)
{
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

    public Task<IReadOnlyList<Flashcard>?> GetFlashcardsAsync(
        string courseCode,
        CancellationToken cancellationToken = default) =>
        GetOptionalListAsync<Flashcard>(
            $"api/courses/{Uri.EscapeDataString(courseCode)}/flashcards",
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
}
