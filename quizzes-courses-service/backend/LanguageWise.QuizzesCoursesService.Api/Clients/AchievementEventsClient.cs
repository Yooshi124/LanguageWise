using System.Net.Http.Headers;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public sealed class AchievementEventsClient(HttpClient httpClient)
{
    public async Task RecordLessonCompletionAsync(
        int userId,
        string userName,
        int value,
        string token,
        CancellationToken cancellationToken)
        => await RecordAsync(
            "lesson-completion",
            "Completed a LanguageWise lesson",
            userId,
            userName,
            token,
            cancellationToken,
            value);

    public async Task RecordCourseCompletionAsync(
        int userId,
        string userName,
        string token,
        CancellationToken cancellationToken)
        => await RecordAsync(
            "course-completion",
            "Completed a LanguageWise course",
            userId,
            userName,
            token,
            cancellationToken);

    public async Task RecordQuizResultAsync(
        int userId,
        string userName,
        QuizAttemptResult result,
        string token,
        CancellationToken cancellationToken)
        => await RecordAsync(
            "quiz-result",
            $"Completed a LanguageWise quiz with a score of {result.Score} out of {result.TotalQuestions}",
            userId,
            userName,
            token,
            cancellationToken);

    private async Task RecordAsync(
        string trigger,
        string subject,
        int userId,
        string userName,
        string token,
        CancellationToken cancellationToken,
        int? value = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events")
        {
            Content = JsonContent.Create(new
            {
                trigger,
                subject,
                recipientUserId = userId,
                recipientName = userName,
                value
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}