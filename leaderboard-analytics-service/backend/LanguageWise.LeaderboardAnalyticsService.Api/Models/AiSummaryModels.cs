namespace LanguageWise.LeaderboardAnalyticsService.Api.Models;

public sealed class OpenRouterOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string Model { get; set; } = "google/gemma-4-26b-a4b-it";
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
