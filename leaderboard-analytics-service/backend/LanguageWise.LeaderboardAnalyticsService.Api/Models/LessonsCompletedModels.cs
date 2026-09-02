namespace LanguageWise.LeaderboardAnalyticsService.Api.Models;

public sealed record LessonsCompletedPoint(DateOnly Date, int LessonsCompleted);

public sealed record LessonsCompletedSeries(
    string CourseCode,
    string CourseTitle,
    IReadOnlyList<LessonsCompletedPoint> Points);

public sealed record LessonsCompletedResponse(
    int UserId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<LessonsCompletedSeries> Series);
