using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class LessonsCompletedSummaryTests
{
    [Test]
    public async Task Summary_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/lessons-completed-summary", content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Summary_WithBearerToken_ReturnsSummaryPayload()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(userId: 42));

        var response = await client.PostAsync("/api/lessons-completed-summary", content: null);
        var body = await response.Content.ReadFromJsonAsync<LessonsCompletedSummaryDto>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Is.Not.Null);
            Assert.That(body!.Summary, Is.Not.Empty);
            Assert.That(body.Trend, Is.AnyOf("up", "down", "flat"));
            Assert.That(body.BestCourse, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Summary_ForwardsCurrentUsersChartDataToGenerator()
    {
        var fake = new FakeSummaryGenerator();
        using var fixture = new ApiFixture { SummaryGenerator = fake };
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(userId: 123));

        var response = await client.PostAsync("/api/lessons-completed-summary", content: null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fake.LastChartData, Is.Not.Null);
            Assert.That(fake.LastChartData!.UserId, Is.EqualTo(123));
            Assert.That(fake.LastChartData.Series, Has.Count.EqualTo(6));
            Assert.That(fake.LastChartData.To, Is.EqualTo(DateOnly.FromDateTime(DateTime.UtcNow)));
            Assert.That(fake.LastChartData.From, Is.EqualTo(fake.LastChartData.To.AddDays(-29)));
        });
    }

    [Test]
    public async Task Generator_WhenOllamaTimesOut_ReturnsDeterministicFallback()
    {
        using var httpClient = new HttpClient(new TimeoutHandler())
        {
            BaseAddress = new Uri("http://ollama/")
        };
        var generator = new OllamaSummaryGenerator(
            httpClient,
            Options.Create(new OllamaOptions()),
            NullLogger<OllamaSummaryGenerator>.Instance);
        var chartData = new LessonsCompletedResponse(
            42,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2),
            [new LessonsCompletedSeries("de", "German", [
                new LessonsCompletedPoint(new DateOnly(2026, 1, 1), 1),
                new LessonsCompletedPoint(new DateOnly(2026, 1, 2), 3)
            ])]);

        var result = await generator.GenerateAsync(chartData);

        Assert.Multiple(() =>
        {
            Assert.That(result.Summary, Is.EqualTo("You completed 3 lessons across 1 courses over the last 30 days, with German leading the way."));
            Assert.That(result.Trend, Is.EqualTo("flat"));
            Assert.That(result.BestCourse, Is.EqualTo("German"));
        });
    }

    private sealed record LessonsCompletedSummaryDto(string Summary, string Trend, string BestCourse);

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Ollama timed out."));
    }
}
