using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class AssistantEndpointTests
{
    [Test]
    public async Task Assistant_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/assistant/messages", ValidRequest());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Assistant_WithUnsupportedRoute_ReturnsValidationProblem()
    {
        var handler = new QuizzesCoursesFakeHandler();
        var completion = new FakeAssistantCompletionClient();
        using var fixture = new ApiFixture
        {
            QuizzesCoursesHandler = handler,
            AssistantCompletionClient = completion
        };
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            new AssistantMessageRequest(
                "Hi",
                [],
                new AssistantRouteContext("other-route")));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(completion.LastMessages, Is.Null);
    }

    [Test]
    public async Task Assistant_WithBearerToken_StreamsAnswerGroundedInAnalyticsProfile()
    {
        const int callerId = 7;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recentAt = new DateTimeOffset(today.AddDays(-2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var courses = new List<Course> { new(1, "de", "German", "") };
        var lessons = new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = [new(100, "l100", "L100", 1)]
        };
        var milestones = new List<Milestone>
        {
            new(1, callerId, null, 100, null, recentAt)
        };

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage(milestones, null)],
            allPages: [new MilestonePage(milestones, null)],
            courses: courses,
            lessonsByCourseCode: lessons);
        var completion = new FakeAssistantCompletionClient();
        using var fixture = new ApiFixture
        {
            QuizzesCoursesHandler = handler,
            AssistantCompletionClient = completion
        };
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(callerId, "justin"));

        var response = await client.PostAsJsonAsync("/api/assistant/messages", ValidRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
            Assert.That(body, Does.Contain("event: delta"));
            Assert.That(body, Does.Contain("Garry answer"));
            Assert.That(body, Does.Contain("event: done"));
        });
        Assert.That(completion.LastMessages, Is.Not.Null);
        Assert.That(
            completion.LastMessages!.Any(m => m.Content.Contains("\"username\":\"justin\"")),
            Is.True);
        Assert.That(
            completion.LastMessages!.Any(m => m.Content.Contains("\"language\":\"German\"")),
            Is.True);
        Assert.That(
            completion.LastMessages!.Any(m => m.Content == "Explain my ranks."),
            Is.True);
    }

    private static AssistantMessageRequest ValidRequest() => new(
        "Explain my ranks.",
        [],
        new AssistantRouteContext("leaderboard-analytics-home"));
}

internal sealed class FakeAssistantCompletionClient : IAssistantCompletionClient
{
    public IReadOnlyList<AssistantChatMessage>? LastMessages { get; private set; }

    public Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken)
    {
        LastMessages = messages;
        return Task.FromResult<IAssistantEventStream>(new StubAssistantEventStream());
    }
}

internal sealed class StubAssistantEventStream : IAssistantEventStream
{
    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return ProviderStreamEvent.Delta("Garry answer");
        yield return ProviderStreamEvent.Done();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
