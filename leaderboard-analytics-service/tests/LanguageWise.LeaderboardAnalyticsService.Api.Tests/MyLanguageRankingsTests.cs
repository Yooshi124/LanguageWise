using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class MyLanguageRankingsTests
{
    [Test]
    public async Task MyLanguageRankings_WithoutToken_ReturnsUnauthorized()
    {
        var handler = new QuizzesCoursesFakeHandler();
        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/my-language-rankings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task MyLanguageRankings_ComputesScoreLanguageAndGlobalRank()
    {
        const int callerId = 42;
        var completedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        var courses = new List<Course>
        {
            new(1, "de", "German", ""),
            new(2, "fr", "French", ""),
            new(3, "it", "Italian", "")
        };

        var lessonsByCourseCode = new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = [new(10, "l10", "L10", 1), new(11, "l11", "L11", 2), new(12, "l12", "L12", 3), new(13, "l13", "L13", 4), new(14, "l14", "L14", 5)],
            ["fr"] = [new(20, "l20", "L20", 1), new(21, "l21", "L21", 2)],
            ["it"] = []
        };

        var myMilestones = new List<Milestone>
        {
            // German: 3 lesson milestones for caller.
            new(1, callerId, null, 10, null, completedAt),
            new(2, callerId, null, 11, null, completedAt),
            new(3, callerId, null, 12, null, completedAt.AddDays(1)),
            // French: 2 lesson milestones for caller.
            new(4, callerId, null, 20, null, completedAt),
            new(5, callerId, null, 21, null, completedAt),
        };

        var allMilestones = new List<Milestone>(myMilestones)
        {
            // Another user beats caller in German (5 > 3).
            new(6, 99, null, 10, null, completedAt),
            new(7, 99, null, 11, null, completedAt),
            new(8, 99, null, 12, null, completedAt),
            new(9, 99, null, 13, null, completedAt),
            new(10, 99, null, 14, null, completedAt),
            // A third user ties caller in French (2 == 2).
            new(11, 77, null, 20, null, completedAt),
            new(12, 77, null, 21, null, completedAt),
        };

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage(myMilestones, null)],
            allPages: [new MilestonePage(allMilestones, null)],
            courses: courses,
            lessonsByCourseCode: lessonsByCourseCode);

        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = AuthenticatedClient(fixture, callerId);

        var response = await client.GetAsync("/api/my-language-rankings");
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<LanguageRanking>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Is.Not.Null);
        var rankings = body!;
        Assert.That(rankings, Has.Count.EqualTo(2));

        var french = rankings.Single(r => r.Language == "French");
        var german = rankings.Single(r => r.Language == "German");

        Assert.Multiple(() =>
        {
            Assert.That(french.Score, Is.EqualTo(2));
            Assert.That(french.Rank, Is.EqualTo(1), "tied for top score in French");
            Assert.That(french.UserId, Is.EqualTo(callerId));

            Assert.That(german.Score, Is.EqualTo(3));
            Assert.That(german.Rank, Is.EqualTo(2), "one other user has a strictly higher German score");
            Assert.That(german.UserId, Is.EqualTo(callerId));

            // Ordered by rank ascending.
            Assert.That(rankings[0].Rank, Is.LessThanOrEqualTo(rankings[1].Rank));
        });
    }

    [Test]
    public async Task MyLanguageRankings_WalksPaginationAndForwardsBearerToken()
    {
        const int callerId = 7;
        var completedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

        var courses = new List<Course> { new(1, "de", "German", "") };
        var lessonsByCourseCode = new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = [new(100, "l100", "L100", 1), new(101, "l101", "L101", 2)]
        };
        var myPage1 = new MilestonePage(
            [new Milestone(1, callerId, null, 100, null, completedAt)],
            NextCursor: 1);
        var myPage2 = new MilestonePage(
            [new Milestone(2, callerId, null, 101, null, completedAt)],
            NextCursor: null);

        var allPage1 = new MilestonePage(
            [new Milestone(1, callerId, null, 100, null, completedAt)],
            NextCursor: 1);
        var allPage2 = new MilestonePage(
            [new Milestone(2, callerId, null, 101, null, completedAt)],
            NextCursor: null);

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [myPage1, myPage2],
            allPages: [allPage1, allPage2],
            courses: courses,
            lessonsByCourseCode: lessonsByCourseCode);

        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        var token = fixture.CreateToken(callerId);
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/my-language-rankings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var requests = handler.Requests;
        Assert.Multiple(() =>
        {
            Assert.That(
                requests.Count(r => r.Path.StartsWith("/api/me/milestones")),
                Is.EqualTo(2),
                "should walk both pages of /me/milestones");
            Assert.That(
                requests.Count(r => r.Path.StartsWith("/api/milestones")),
                Is.EqualTo(2),
                "should walk both pages of /milestones");
            Assert.That(
                requests.Any(r => r.Path.StartsWith("/api/courses")),
                Is.True,
                "should fetch /api/courses");
            Assert.That(
                requests.All(r => string.Equals(r.Authorization, $"Bearer {token}", StringComparison.Ordinal)),
                Is.True,
                "every downstream request must forward the caller's bearer token");
        });
    }

    private static HttpClient AuthenticatedClient(ApiFixture fixture, int userId)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(userId));
        return client;
    }
}
