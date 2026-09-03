using System.Net.Http.Headers;

namespace LanguageWise.MiniGamesService.Api.Clients;

public sealed class AchievementEventsClient(HttpClient httpClient)
{
    public async Task RecordAsync(int userId, string userName, int value, string subject, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events")
        {
            Content = JsonContent.Create(new
            {
                trigger = "minigame-win",
                subject,
                recipientUserId = userId,
                recipientName = userName,
                value
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}