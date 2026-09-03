namespace LanguageWise.LeaderboardAnalyticsService.Api.Models;

public sealed record LanguageRanking(
    int Id,
    int UserId,
    string Language,
    int Score,
    int Rank,
    DateTime UpdatedAt);
