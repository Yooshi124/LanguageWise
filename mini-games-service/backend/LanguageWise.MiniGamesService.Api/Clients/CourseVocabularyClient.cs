using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace LanguageWise.MiniGamesService.Api.Clients;

/// <summary>Represents vocabulary from course content.</summary>
public sealed record VocabularyWord(string Word, string Meaning);

/// <summary>Vocabulary from one milestone-completed lesson.</summary>
public sealed record LessonVocabulary(
    int LessonId,
    string Slug,
    string Title,
    IReadOnlyList<VocabularyWord> Vocabulary);

/// <summary>Vocabulary unlocked in one started course.</summary>
public sealed record CourseVocabulary(
    string Code,
    string Title,
    IReadOnlyList<LessonVocabulary> Lessons);

/// <summary>All vocabulary the user has unlocked across the courses they have started.</summary>
public sealed record UserVocabulary(IReadOnlyList<CourseVocabulary> Courses);

/// <summary>
/// Client for the quizzes-courses API. Vocabulary is user-scoped, so requests forward the
/// caller's JWT as a bearer token and the API resolves the user from it.
/// </summary>
public sealed class CourseVocabularyClient(HttpClient httpClient, IMemoryCache cache)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Vocabulary unlocked by the authenticated user: the courses they have started, limited to
    /// lessons whose milestone they have achieved. Null when the token is missing or the
    /// quizzes-courses service cannot fulfil the request.
    /// </summary>
    /// <remarks>
    /// Cached briefly per user (by token hash): every game init otherwise re-fetches the same
    /// unlocked vocabulary from the courses service, and the result only changes when the user
    /// completes new course content.
    /// </remarks>
    public async Task<UserVocabulary?> GetUserVocabularyAsync(
        string? accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var cacheKey = $"course-vocabulary:{HashToken(accessToken)}";
        if (cache.TryGetValue<UserVocabulary>(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/me/vocabulary");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var vocabulary = await response.Content.ReadFromJsonAsync<UserVocabulary>(JsonOptions, cancellationToken);
            if (vocabulary is not null)
            {
                cache.Set(cacheKey, vocabulary, CacheDuration);
            }

            return vocabulary;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>Never cache the raw token itself; keys are a one-way hash of it.</summary>
    private static string HashToken(string accessToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessToken)));
}
