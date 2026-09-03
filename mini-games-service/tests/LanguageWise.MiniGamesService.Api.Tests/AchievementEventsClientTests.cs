using System.Net;
using LanguageWise.MiniGamesService.Api.Clients;

namespace LanguageWise.MiniGamesService.Api.Tests;

public sealed class AchievementEventsClientTests
{
    [Test]
    public async Task RecordAsync_SendsAuthenticatedAbsoluteWinCount()
    {
        var handler = new RecordingHandler();
        var client = new AchievementEventsClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://achievements/")
        });

        await client.RecordAsync(7, "Amber", 12, "Won a Word Search mini-game", "jwt-token");

        Assert.Multiple(() =>
        {
            Assert.That(handler.Path, Is.EqualTo("/api/events"));
            Assert.That(handler.Authorization, Is.EqualTo("Bearer jwt-token"));
            Assert.That(handler.Body, Does.Contain("\"trigger\":\"minigame-win\""));
            Assert.That(handler.Body, Does.Contain("\"recipientUserId\":7"));
            Assert.That(handler.Body, Does.Contain("\"recipientName\":\"Amber\""));
            Assert.That(handler.Body, Does.Contain("\"value\":12"));
        });
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}