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

        var myMilestones = new List<Milestone>
        {
            // German: 3 milestones for caller.
            new(1, callerId, 1, 10, null, completedAt),
            new(2, callerId, 1, 11, null, completedAt),
            new(3, callerId, 1, 12, null, completedAt.AddDays(1)),
            // French: 2 milestones for caller.
            new(4, callerId, 2, 20, null, completedAt),
            new(5, callerId, 2, 21, null, completedAt),
        };

        var allMilestones = new List<Milestone>(myMilestones)
        {
            // Another user beats caller in German (5 > 3).
            new(6, 99, 1, 10, null, completedAt),
            new(7, 99, 1, 11, null, completedAt),
            new(8, 99, 1, 12, null, completedAt),
            new(9, 99, 1, 13, null, completedAt),
            new(10, 99, 1, 14, null, completedAt),
            // A third user ties caller in French (2 == 2).
            new(11, 77, 2, 20, null, completedAt),
            new(12, 77, 2, 21, null, completedAt),
        };

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage(myMilestones, null)],
            allPages: [new MilestonePage(allMilestones, null)],
            courses: courses);

        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = AuthenticatedClient(fixture, callerId);

        var response = await client.GetAsync("/api/my-language-rankings");
        var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<LanguageRanking>>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Is.Not.Null);
        var rankings = body!;
        Assert.That(rankings, Has.Count.EqualTo(2));

        var french = rankings.Single(r => r.Language == "fr");
        var german = rankings.Single(r => r.Language == "de");

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
        var myPage1 = new MilestonePage(
            [new Milestone(1, callerId, 1, 100, null, completedAt)],
            NextCursor: 1);
        var myPage2 = new MilestonePage(
            [new Milestone(2, callerId, 1, 101, null, completedAt)],
            NextCursor: null);

        var allPage1 = new MilestonePage(
            [new Milestone(1, callerId, 1, 100, null, completedAt)],
            NextCursor: 1);
        var allPage2 = new MilestonePage(
            [new Milestone(2, callerId, 1, 101, null, completedAt)],
            NextCursor: null);

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [myPage1, myPage2],
            allPages: [allPage1, allPage2],
            courses: courses);

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
