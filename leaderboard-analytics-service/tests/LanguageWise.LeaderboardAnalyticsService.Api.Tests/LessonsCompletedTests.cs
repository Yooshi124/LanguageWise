using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class LessonsCompletedTests
{
    [Test]
    public async Task LessonsCompleted_WithoutToken_ReturnsUnauthorized()
    {
        var handler = new QuizzesCoursesFakeHandler();
        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/lessons-completed-over-time");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task LessonsCompleted_BuildsSeriesFromMilestonesForCoursesWithActivity()
    {
        const int callerId = 42;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recentDay = today.AddDays(-5);
        var recentAt = new DateTimeOffset(recentDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var outOfWindowAt = new DateTimeOffset(today.AddDays(-60).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var courses = new List<Course>
        {
            new(1, "de", "German", ""),
            new(2, "fr", "French", ""),
            new(3, "it", "Italian", "")
        };

        var lessonsByCourseCode = new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = [new(100, "l100", "L100", 1), new(101, "l101", "L101", 2), new(102, "l102", "L102", 3)],
            ["fr"] = [new(200, "l200", "L200", 1)],
            ["it"] = []
        };

        var myMilestones = new List<Milestone>
        {
            // Two German lesson milestones in-window.
            new(1, callerId, null, 100, null, recentAt),
            new(2, callerId, null, 101, null, recentAt),
            // One French lesson milestone in-window.
            new(3, callerId, null, 200, null, recentAt),
            // Italian: only course-level milestone (no LessonId) — must be excluded.
            new(4, callerId, 3, null, null, recentAt),
            // German lesson before the 30-day window — must be excluded.
            new(5, callerId, null, 102, null, outOfWindowAt),
        };

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage(myMilestones, null)],
            courses: courses,
            lessonsByCourseCode: lessonsByCourseCode);

        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(callerId));

        var response = await client.GetAsync("/api/lessons-completed-over-time");
        var body = await response.Content.ReadFromJsonAsync<LessonsCompletedResponseDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(body!.UserId, Is.EqualTo(callerId));
            Assert.That(body.To, Is.EqualTo(today));
            Assert.That(body.From, Is.EqualTo(today.AddDays(-29)));
            Assert.That(
                body.Series.Select(s => s.CourseCode),
                Is.EquivalentTo(["de", "fr"]),
                "Italian has no lesson milestones so it must not appear");
            foreach (var series in body.Series)
            {
                Assert.That(series.Points, Has.Count.EqualTo(30));
                Assert.That(series.Points[0].Date, Is.EqualTo(body.From));
                Assert.That(series.Points[^1].Date, Is.EqualTo(body.To));
            }

            var german = body.Series.Single(s => s.CourseCode == "de");
            Assert.That(
                german.Points[^1].LessonsCompleted,
                Is.EqualTo(2),
                "cumulative German count over the window should reflect only in-window lessons");

            var french = body.Series.Single(s => s.CourseCode == "fr");
            Assert.That(french.Points[^1].LessonsCompleted, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task LessonsCompleted_IgnoresCourseCompletionMilestones()
    {
        const int callerId = 42;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recentAt = new DateTimeOffset(today.AddDays(-3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var courses = new List<Course> { new(1, "de", "German", "") };
        var lessonsByCourseCode = new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = [new(100, "l100", "L100", 1)]
        };

        // Only a course-completion milestone (CourseId set, LessonId null) — matches the DB CHECK constraint.
        var myMilestones = new List<Milestone>
        {
            new(1, callerId, 1, null, null, recentAt),
        };

        var handler = new QuizzesCoursesFakeHandler(
            myPages: [new MilestonePage(myMilestones, null)],
            courses: courses,
            lessonsByCourseCode: lessonsByCourseCode);

        using var fixture = new ApiFixture { QuizzesCoursesHandler = handler };
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(callerId));

        var response = await client.GetAsync("/api/lessons-completed-over-time");
        var body = await response.Content.ReadFromJsonAsync<LessonsCompletedResponseDto>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Is.Not.Null);
        Assert.That(
            body!.Series,
            Is.Empty,
            "course-completion milestones must not count as lesson completions");
    }

    private sealed record LessonsCompletedResponseDto(
        int UserId,
        DateOnly From,
        DateOnly To,
        IReadOnlyList<LessonsCompletedSeriesDto> Series);

    private sealed record LessonsCompletedSeriesDto(
        string CourseCode,
        string CourseTitle,
        IReadOnlyList<LessonsCompletedPointDto> Points);

    private sealed record LessonsCompletedPointDto(DateOnly Date, int LessonsCompleted);
}

