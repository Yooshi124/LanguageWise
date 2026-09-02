using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Services;

// Placeholder data source until the endpoint is wired to the quizzes-courses-service Milestones table.
internal static class MockLessonsCompletedGenerator
{
    private static readonly (string Code, string Title)[] Courses =
    [
        ("de", "German"),
        ("fr", "French"),
        ("it", "Italian"),
        ("nl", "Dutch"),
        ("es", "Spanish"),
        ("pl", "Polish"),
    ];

    public static LessonsCompletedResponse GenerateForLast30Days(int userId)
    {
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        return Generate(userId, to.AddDays(-29), to);
    }

    public static LessonsCompletedResponse Generate(int userId, DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var dayCount = to.DayNumber - from.DayNumber + 1;
        var series = new List<LessonsCompletedSeries>(Courses.Length);

        foreach (var (code, title) in Courses)
        {
            var random = new Random(HashCode.Combine(userId, code));
            var points = new List<LessonsCompletedPoint>(dayCount);
            var lessonsCompletedCount = 0;

            for (var i = 0; i < dayCount; i++)
            {
                var value = (int)Math.Round(random.NextDouble() * 2.0);
                lessonsCompletedCount += value;
                points.Add(new LessonsCompletedPoint(from.AddDays(i), lessonsCompletedCount));
            }

            series.Add(new LessonsCompletedSeries(code, title, points));
        }

        return new LessonsCompletedResponse(userId, from, to, series);
    }
}
