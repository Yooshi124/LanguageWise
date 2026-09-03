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
        var handler = new QuizzesCoursesFakeHandler();
        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/lessons-completed-summary", content: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Summary_WithBearerToken_ReturnsSummaryPayload()
    {
        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage([], null)],
            courses: []);
        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
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
        const int callerId = 123;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var completedAt = new DateTimeOffset(today.AddDays(-2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var courses = new List<Course> { new(1, "de", "German", "") };
        var lessonsByCourseCode = new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = [new(100, "l100", "L100", 1), new(101, "l101", "L101", 2)]
        };
        var myMilestones = new List<Milestone>
        {
            new(1, callerId, null, 100, null, completedAt),
            new(2, callerId, null, 101, null, completedAt)
        };
        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage(myMilestones, null)],
            courses: courses,
            lessonsByCourseCode: lessonsByCourseCode);
        var fake = new FakeSummaryGenerator();
        using var fixture = new ApiFixture
        {
            SummaryGenerator = fake,
            QuizzesCoursesHandler = handler
        };
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(userId: callerId));

        var response = await client.PostAsync("/api/lessons-completed-summary", content: null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fake.LastChartData, Is.Not.Null);
            Assert.That(fake.LastChartData!.UserId, Is.EqualTo(callerId));
            Assert.That(fake.LastChartData.Series.Select(s => s.CourseCode), Is.EquivalentTo(["de"]));
            Assert.That(fake.LastChartData.To, Is.EqualTo(today));
            Assert.That(fake.LastChartData.From, Is.EqualTo(today.AddDays(-29)));
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
