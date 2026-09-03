using System.Net.Http.Headers;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public sealed class AchievementEventsClient(HttpClient httpClient)
{
    public async Task RecordLessonCompletionAsync(
        int userId,
        string userName,
        int value,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events")
        {
            Content = JsonContent.Create(new
            {
                trigger = "lesson-completion",
                subject = "Completed a LanguageWise lesson",
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