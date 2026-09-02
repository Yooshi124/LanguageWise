using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using Microsoft.Extensions.Options;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Clients;

public sealed class OllamaSummaryGenerator(
    HttpClient httpClient,
    IOptions<OllamaOptions> options,
    ILogger<OllamaSummaryGenerator> logger) : ISummaryGenerator
{
    private const int MaximumSummaryLength = 1200;
    private static readonly string[] AllowedTrends = ["up", "down", "flat"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LessonsCompletedSummaryResponse> GenerateAsync(
        LessonsCompletedResponse chartData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chartJson = JsonSerializer.Serialize(chartData, JsonOptions);
            var courseTitles = string.Join(", ", chartData.Series.Select(s => s.CourseTitle));

            var request = new
            {
                model = options.Value.Model,
                stream = false,
                think = false,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Summarise the user's last 30 days of language lesson progress from the supplied chart data. Return only JSON matching the schema. `summary` must be one or two warm, concise sentences describing overall momentum across courses. `trend` must be exactly one of up, down, or flat, chosen from cumulative growth across the whole window. `bestCourse` must be one of the courseTitle values verbatim."
                    },
                    new
                    {
                        role = "user",
                        content = $"Course titles: {courseTitles}\nChart data (JSON): {chartJson}"
                    }
                },
                format = new
                {
                    type = "object",
                    properties = new
                    {
                        summary = new { type = "string" },
                        trend = new { type = "string", @enum = AllowedTrends },
                        bestCourse = new { type = "string" }
                    },
                    required = new[] { "summary", "trend", "bestCourse" }
                },
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.95,
                    top_k = 64,
                    num_predict = 256
                }
            };

            using var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
            var generated = JsonSerializer.Deserialize<GeneratedSummary>(payload?.Message.Content ?? string.Empty, JsonOptions);

            if (string.IsNullOrWhiteSpace(generated?.Summary)
                || string.IsNullOrWhiteSpace(generated.Trend)
                || string.IsNullOrWhiteSpace(generated.BestCourse))
            {
                throw new JsonException("Ollama returned an incomplete summary.");
            }

            var trend = NormaliseTrend(generated.Trend);
            var bestCourse = NormaliseBestCourse(generated.BestCourse, chartData);

            return new LessonsCompletedSummaryResponse(
                Truncate(generated.Summary.Trim(), MaximumSummaryLength),
                trend,
                bestCourse);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested
            && exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Ollama summary generation failed; using the fallback template.");
            return Fallback(chartData);
        }
    }

    private static LessonsCompletedSummaryResponse Fallback(LessonsCompletedResponse chartData)
    {
        var bestSeries = chartData.Series
            .OrderByDescending(series => series.Points.Count == 0 ? 0 : series.Points[^1].LessonsCompleted)
            .FirstOrDefault();
        var bestCourse = bestSeries?.CourseTitle ?? "your top course";
        var trend = ComputeTrendFromSeries(bestSeries);
        var totalLessons = chartData.Series.Sum(series => series.Points.Count == 0 ? 0 : series.Points[^1].LessonsCompleted);

        var summary = totalLessons > 0
            ? $"You completed {totalLessons} lessons across {chartData.Series.Count} courses over the last 30 days, with {bestCourse} leading the way."
            : $"No lessons were logged in the last 30 days across your {chartData.Series.Count} courses — a great moment to jump back in.";

        return new LessonsCompletedSummaryResponse(
            Truncate(summary, MaximumSummaryLength),
            trend,
            bestCourse);
    }

    private static string ComputeTrendFromSeries(LessonsCompletedSeries? series)
    {
        if (series is null || series.Points.Count < 14)
        {
            return "flat";
        }

        // Points are cumulative, so per-window deltas approximate lessons completed in that window.
        var points = series.Points;
        var last = points[^1].LessonsCompleted;
        var sevenDaysAgo = points[^8].LessonsCompleted;
        var fourteenDaysAgo = points[^15].LessonsCompleted;
        var recent = last - sevenDaysAgo;
        var previous = sevenDaysAgo - fourteenDaysAgo;

        if (recent > previous) return "up";
        if (recent < previous) return "down";
        return "flat";
    }

    private static string NormaliseTrend(string value)
    {
        var lowered = value.Trim().ToLowerInvariant();
        return AllowedTrends.Contains(lowered) ? lowered : "flat";
    }

    private static string NormaliseBestCourse(string value, LessonsCompletedResponse chartData)
    {
        var trimmed = value.Trim();
        var match = chartData.Series
            .Select(series => series.CourseTitle)
            .FirstOrDefault(title => string.Equals(title, trimmed, StringComparison.OrdinalIgnoreCase));
        return match ?? trimmed;
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private sealed record OllamaChatResponse(OllamaMessage Message);
    private sealed record OllamaMessage(string Content);
    private sealed record GeneratedSummary(string Summary, string Trend, string BestCourse);
}
