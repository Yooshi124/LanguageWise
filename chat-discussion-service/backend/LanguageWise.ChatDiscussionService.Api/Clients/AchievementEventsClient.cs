using System.Net.Http.Headers;

namespace LanguageWise.ChatDiscussionService.Api.Clients;

public sealed class AchievementEventsClient(HttpClient httpClient)
{
    public async Task RecordContributionAsync(
        int userId,
        string userName,
        string subject,
        string token,
        CancellationToken cancellationToken)
        => await RecordAsync(
            "community-contribution",
            userId,
            userName,
            subject,
            token,
            cancellationToken);

    public async Task RecordPostEngagementAsync(
        int userId,
        string userName,
        string subject,
        string token,
        CancellationToken cancellationToken)
        => await RecordAsync(
            "post-engagement",
            userId,
            userName,
            subject,
            token,
            cancellationToken);

    private async Task RecordAsync(
        string trigger,
        int userId,
        string userName,
        string subject,
        string token,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events")
        {
            Content = JsonContent.Create(new
            {
                trigger,
                subject,
                recipientUserId = userId,
                recipientName = userName
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}