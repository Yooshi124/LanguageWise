using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class RankingProxyTests
{
    [TestCase("/api/language-rankings?language=French&limit=12&offset=3", "api/language-rankings?language=French&limit=12&offset=3")]
    [TestCase("/api/discussion-rankings?limit=8&offset=2", "api/discussion-rankings?limit=8&offset=2")]
    public async Task RankingList_ForwardsQueryToDatabase(string requestPath, string expectedDatabasePath)
    {
        var handler = new RecordingHandler("[]");
        using var fixture = new ApiFixture { LeaderboardHandler = handler };
        using var client = AuthenticatedClient(fixture);

        var response = await client.GetAsync(requestPath);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(handler.RequestPath, Is.EqualTo(expectedDatabasePath));
        });
    }

    [TestCase("/api/language-rankings/99", "api/language-rankings/99")]
    [TestCase("/api/discussion-rankings/user/99", "api/discussion-rankings/user/99")]
    public async Task MissingRanking_PropagatesNotFound(string requestPath, string expectedDatabasePath)
    {
        var handler = new RecordingHandler(null, HttpStatusCode.NotFound);
        using var fixture = new ApiFixture { LeaderboardHandler = handler };
        using var client = AuthenticatedClient(fixture);

        var response = await client.GetAsync(requestPath);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(handler.RequestPath, Is.EqualTo(expectedDatabasePath));
        });
    }

    private static HttpClient AuthenticatedClient(ApiFixture fixture)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());
        return client;
    }

    private sealed class RecordingHandler(string? body, HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        internal string? RequestPath { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestPath = request.RequestUri?.PathAndQuery.TrimStart('/');
            var response = new HttpResponseMessage(statusCode);
            if (body is not null)
            {
                response.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }
    }
}