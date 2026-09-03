using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

internal sealed class QuizzesCoursesFakeHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private readonly List<(string Path, string? Authorization)> requests = new();
    private readonly object requestsLock = new();
    private readonly ConcurrentDictionary<string, int> pageCursors = new();

    internal QuizzesCoursesFakeHandler(
        IReadOnlyList<MilestonePage>? myPages = null,
        IReadOnlyList<MilestonePage>? allPages = null,
        IReadOnlyList<Course>? courses = null,
        IReadOnlyDictionary<string, IReadOnlyList<LessonSummary>>? lessonsByCourseCode = null)
    {
        MyPages = myPages ?? [new MilestonePage([], null)];
        AllPages = allPages ?? [new MilestonePage([], null)];
        Courses = courses ?? [];
        LessonsByCourseCode = lessonsByCourseCode
            ?? new Dictionary<string, IReadOnlyList<LessonSummary>>(StringComparer.OrdinalIgnoreCase);
    }

    internal IReadOnlyList<MilestonePage> MyPages { get; }
    internal IReadOnlyList<MilestonePage> AllPages { get; }
    internal IReadOnlyList<Course> Courses { get; }
    internal IReadOnlyDictionary<string, IReadOnlyList<LessonSummary>> LessonsByCourseCode { get; }

    internal IReadOnlyList<(string Path, string? Authorization)> Requests
    {
        get
        {
            lock (requestsLock)
            {
                return requests.ToList();
            }
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri ?? throw new InvalidOperationException("Request URI missing.");
        lock (requestsLock)
        {
            requests.Add((uri.PathAndQuery, request.Headers.Authorization?.ToString()));
        }

        var path = uri.AbsolutePath.TrimStart('/');

        if (path.StartsWith("api/courses/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith("/lessons", StringComparison.OrdinalIgnoreCase))
        {
            var code = path.Substring("api/courses/".Length, path.Length - "api/courses/".Length - "/lessons".Length);
            var decoded = Uri.UnescapeDataString(code);
            if (LessonsByCourseCode.TryGetValue(decoded, out var lessons))
            {
                return Task.FromResult(JsonResponse(lessons));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        return path switch
        {
            "api/me/milestones" => Task.FromResult(NextPage("api/me/milestones", MyPages)),
            "api/milestones" => Task.FromResult(NextPage("api/milestones", AllPages)),
            "api/courses" => Task.FromResult(JsonResponse(Courses)),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
        };
    }

    private HttpResponseMessage NextPage(string key, IReadOnlyList<MilestonePage> pages)
    {
        var index = pageCursors.AddOrUpdate(key, 0, (_, current) => current + 1);
        var page = index < pages.Count ? pages[index] : new MilestonePage([], null);
        return JsonResponse(page);
    }

    private static HttpResponseMessage JsonResponse<T>(T payload)
    {
        var content = JsonContent.Create(payload, options: JsonOptions);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }
}

