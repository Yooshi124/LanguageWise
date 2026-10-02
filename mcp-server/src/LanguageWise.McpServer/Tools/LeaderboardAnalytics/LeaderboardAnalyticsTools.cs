using System.ComponentModel;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Tools.LeaderboardAnalytics;

[McpServerToolType]
public sealed class LeaderboardAnalyticsTools(DownstreamClient downstream)
{
	public const string ServiceName = "LeaderboardAnalytics";

	[McpServerTool(Name = "leaderboard_get_my_language_rankings", Title = "Get my language rankings", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the signed-in user's score and rank in each language they are studying. Rankings are calculated from completed lesson milestones.")]
	public Task<List<LanguageRankingResult>> GetMyLanguageRankingsAsync(CancellationToken cancellationToken) =>
		downstream.GetAsync<List<LanguageRankingResult>>(ServiceName, "api/my-language-rankings", cancellationToken);

	[McpServerTool(Name = "leaderboard_get_my_lessons_completed_over_time", Title = "Get my lessons completed over time", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the signed-in user's daily cumulative lesson completions by course for the last 30 days.")]
	public Task<LessonsCompletedResult> GetMyLessonsCompletedOverTimeAsync(CancellationToken cancellationToken) =>
		downstream.GetAsync<LessonsCompletedResult>(ServiceName, "api/lessons-completed-over-time", cancellationToken);
}

public sealed record LanguageRankingResult(
	int Id,
	int UserId,
	string Language,
	int Score,
	int Rank,
	DateTime UpdatedAt);

public sealed record LessonsCompletedResult(
	int UserId,
	DateOnly From,
	DateOnly To,
	IReadOnlyList<LessonsCompletedSeriesResult> Series);

public sealed record LessonsCompletedSeriesResult(
	string CourseCode,
	string CourseTitle,
	IReadOnlyList<LessonsCompletedPointResult> Points);

public sealed record LessonsCompletedPointResult(DateOnly Date, int LessonsCompleted);