using System.Net;
using LanguageWise.ChatDiscussionService.Api.Clients;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class AchievementEventsClientTests
{
    [Test]
    public async Task RecordContributionAsync_SendsAuthenticatedContribution()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new AchievementEventsClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://achievements/")
        });

        await client.RecordContributionAsync(
            7,
            "Amber",
            "Created a community post",
            "jwt-token",
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/events"));
            Assert.That(handler.LastAuthorization, Is.EqualTo("Bearer jwt-token"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"trigger\":\"community-contribution\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"recipientUserId\":7"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"recipientName\":\"Amber\""));
            Assert.That(handler.LastRequestBody, Does.Not.Contain("\"value\""));
        });
    }

    [Test]
    public async Task RecordPostEngagementAsync_SendsRecipientAndEngagementTrigger()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new AchievementEventsClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://achievements/")
        });

        await client.RecordPostEngagementAsync(
            9,
            "Justin",
            "Received a like on a community post",
            "jwt-token",
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestBody, Does.Contain("\"trigger\":\"post-engagement\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"recipientUserId\":9"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"recipientName\":\"Justin\""));
        });
    }
}