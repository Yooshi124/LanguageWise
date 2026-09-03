using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Clients;

public sealed class QuizzesCoursesClient(HttpClient httpClient, IMemoryCache cache)
{
    private const int PageSize = 200;
    private const string CoursesCacheKey = "quizzes-courses:courses";
    private static readonly TimeSpan CoursesCacheDuration = TimeSpan.FromMinutes(5);

    public Task<IReadOnlyList<Milestone>> GetAllMyMilestonesAsync(
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        WalkMilestonesAsync("api/me/milestones", bearerToken, cancellationToken);

    public Task<IReadOnlyList<Milestone>> GetAllMilestonesAsync(
        string bearerToken,
        CancellationToken cancellationToken = default) =>
        WalkMilestonesAsync("api/milestones", bearerToken, cancellationToken);

    public async Task<IReadOnlyList<Course>> GetCoursesAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CoursesCacheKey, out IReadOnlyList<Course>? cached) && cached is not null)
        {
            return cached;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "api/courses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var courses = await response.Content.ReadFromJsonAsync<List<Course>>(cancellationToken)
            ?? new List<Course>();
        cache.Set(CoursesCacheKey, (IReadOnlyList<Course>)courses, CoursesCacheDuration);
        return courses;
    }

    private async Task<IReadOnlyList<Milestone>> WalkMilestonesAsync(
        string path,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        var results = new List<Milestone>();
        int? cursor = null;

        while (true)
        {
            var url = cursor is null
                ? $"{path}?limit={PageSize}"
                : $"{path}?afterId={cursor.Value}&limit={PageSize}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<MilestonePage>(cancellationToken)
                ?? new MilestonePage([], null);
            results.AddRange(page.Items);

            if (page.NextCursor is null)
            {
                return results;
            }

            cursor = page.NextCursor;
        }
    }
}
