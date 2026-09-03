using System.Net.Http.Headers;

namespace LanguageWise.Shared.Api.Clients;

public sealed class AchievementsClient(HttpClient httpClient)
{
    internal async Task RecordLoginStreakAsync(
        AuthenticatedUser user,
        int value,
        string token,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events")
        {
            Content = JsonContent.Create(new
            {
                trigger = "login-streak",
                subject = value == 0
                    ? "Started a new daily login streak"
                    : $"Continued a daily login streak for {value} consecutive days",
                recipientUserId = user.Id,
                recipientName = user.Name,
                value
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}