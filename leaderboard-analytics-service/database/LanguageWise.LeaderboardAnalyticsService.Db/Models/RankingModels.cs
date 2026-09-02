namespace LanguageWise.LeaderboardAnalyticsService.Db.Models;

public sealed record LanguageRanking(
    int Id,
    int UserId,
    string Language,
    int Score,
    int Rank,
    DateTime UpdatedAt);

public sealed record LanguageRankingInput(int UserId, string? Language, int Score);

public sealed record DiscussionRanking(
    int Id,
    int UserId,
    int PostCount,
    int CommentCount,
    int LikeCount,
    int Score,
    int Rank,
    DateTime UpdatedAt);

public sealed record DiscussionRankingInput(int UserId, int PostCount, int CommentCount, int LikeCount, int Score);
