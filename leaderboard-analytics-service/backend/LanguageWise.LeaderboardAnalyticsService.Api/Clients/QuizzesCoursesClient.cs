using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Clients;

public sealed class QuizzesCoursesClient(HttpClient httpClient, IMemoryCache cache)
{
    private const int PageSize = 200;
    private const string CoursesCacheKey = "quizzes-courses:courses";
    private const string LessonCourseMapCacheKey = "quizzes-courses:lesson-course-map";
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

    public async Task<IReadOnlyDictionary<int, int>> GetLessonToCourseMapAsync(
        string bearerToken,
        CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(LessonCourseMapCacheKey, out IReadOnlyDictionary<int, int>? cached) && cached is not null)
        {
            return cached;
        }

        var courses = await GetCoursesAsync(bearerToken, cancellationToken);
        var perCourse = await Task.WhenAll(courses.Select(course =>
            GetLessonsForCourseAsync(course, bearerToken, cancellationToken)));

        var map = new Dictionary<int, int>();
        foreach (var (courseId, lessons) in perCourse)
        {
            foreach (var lesson in lessons)
            {
                map[lesson.Id] = courseId;
            }
        }

        var readOnly = (IReadOnlyDictionary<int, int>)map;
        cache.Set(LessonCourseMapCacheKey, readOnly, CoursesCacheDuration);
        return readOnly;
    }

    private async Task<(int CourseId, IReadOnlyList<LessonSummary> Lessons)> GetLessonsForCourseAsync(
        Course course,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/courses/{Uri.EscapeDataString(course.Code)}/lessons");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return (course.Id, Array.Empty<LessonSummary>());
        }

        response.EnsureSuccessStatusCode();
        var lessons = await response.Content.ReadFromJsonAsync<List<LessonSummary>>(cancellationToken)
            ?? new List<LessonSummary>();
        return (course.Id, lessons);
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
