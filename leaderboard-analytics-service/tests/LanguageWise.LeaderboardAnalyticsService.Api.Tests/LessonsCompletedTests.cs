using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class LessonsCompletedTests
{
    [Test]
    public async Task LessonsCompleted_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/lessons-completed-over-time");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task LessonsCompleted_WithBearerToken_ReturnsSeriesForSixCourses()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(userId: 42));

        var response = await client.GetAsync("/api/lessons-completed-over-time");
        var body = await response.Content.ReadFromJsonAsync<LessonsCompletedResponseDto>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Is.Not.Null);
            Assert.That(body!.UserId, Is.EqualTo(42));
            Assert.That(body.Series, Has.Count.EqualTo(6));
            Assert.That(
                body.Series.Select(s => s.CourseCode),
                Is.EquivalentTo(["de", "fr", "it", "nl", "es", "pl"]));
            Assert.That(body.To, Is.EqualTo(DateOnly.FromDateTime(DateTime.UtcNow)));
            Assert.That(body.From, Is.EqualTo(body.To.AddDays(-29)));
            foreach (var series in body.Series)
            {
                Assert.That(series.Points, Has.Count.EqualTo(30), $"course {series.CourseCode} point count");
                Assert.That(series.Points[0].Date, Is.EqualTo(body.From));
                Assert.That(series.Points[^1].Date, Is.EqualTo(body.To));
            }
        });
    }

    [Test]
    public async Task LessonsCompleted_IsDeterministicPerUser()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken(userId: 99));

        var first = await client.GetFromJsonAsync<LessonsCompletedResponseDto>("/api/lessons-completed-over-time");
        var second = await client.GetFromJsonAsync<LessonsCompletedResponseDto>("/api/lessons-completed-over-time");

        Assert.That(first, Is.Not.Null);
        Assert.That(second, Is.Not.Null);
        for (var i = 0; i < first!.Series.Count; i++)
        {
            var a = first.Series[i].Points.Select(p => p.LessonsCompleted).ToArray();
            var b = second!.Series[i].Points.Select(p => p.LessonsCompleted).ToArray();
            Assert.That(b, Is.EqualTo(a), $"series {first.Series[i].CourseCode} should be stable across requests");
        }
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
