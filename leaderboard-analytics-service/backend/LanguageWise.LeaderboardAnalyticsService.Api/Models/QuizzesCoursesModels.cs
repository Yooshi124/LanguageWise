namespace LanguageWise.LeaderboardAnalyticsService.Api.Models;

public sealed record Milestone(
    int Id,
    int UserId,
    int? CourseId,
    int? LessonId,
    int? QuizId,
    DateTimeOffset CompletedAt);

public sealed record MilestonePage(
    IReadOnlyList<Milestone> Items,
    int? NextCursor);

public sealed record Course(int Id, string Code, string Title, string Description);
