namespace LanguageWise.LeaderboardAnalyticsService.Api.Models;

public sealed class OllamaOptions
{
    public string Model { get; set; } = "gemma4:e4b";
}

public sealed record LessonsCompletedSummaryResponse(
    string Summary,
    string Trend,
    string BestCourse);

public interface ISummaryGenerator
{
    Task<LessonsCompletedSummaryResponse> GenerateAsync(
        LessonsCompletedResponse chartData,
        CancellationToken cancellationToken = default);
}
