using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Clients;

public sealed class LeaderboardClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<LanguageRanking>> GetLanguageRankingsAsync(
        string? language,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = await BuildQuery(
            ("language", language),
            ("limit", limit.ToString(CultureInfo.InvariantCulture)),
            ("offset", offset.ToString(CultureInfo.InvariantCulture)));

        return await httpClient.GetFromJsonAsync<List<LanguageRanking>>(
            $"api/language-rankings{query}",
            cancellationToken) ?? [];
    }

    public async Task<LanguageRanking?> GetLanguageRankingAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await GetOrNullAsync<LanguageRanking>($"api/language-rankings/{id}", cancellationToken);

    public async Task<IReadOnlyList<LanguageRanking>> GetLanguageRankingsByUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<LanguageRanking>>(
            $"api/language-rankings/user/{userId}",
            cancellationToken) ?? [];

    public async Task<IReadOnlyList<DiscussionRanking>> GetDiscussionRankingsAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = await BuildQuery(
            ("limit", limit.ToString(CultureInfo.InvariantCulture)),
            ("offset", offset.ToString(CultureInfo.InvariantCulture)));

        return await httpClient.GetFromJsonAsync<List<DiscussionRanking>>(
            $"api/discussion-rankings{query}",
            cancellationToken) ?? [];
    }

    public async Task<DiscussionRanking?> GetDiscussionRankingAsync(
        int id,
        CancellationToken cancellationToken = default) =>
        await GetOrNullAsync<DiscussionRanking>($"api/discussion-rankings/{id}", cancellationToken);

    public async Task<DiscussionRanking?> GetDiscussionRankingByUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await GetOrNullAsync<DiscussionRanking>($"api/discussion-rankings/user/{userId}", cancellationToken);

    private async Task<T?> GetOrNullAsync<T>(string path, CancellationToken cancellationToken) where T : class
    {
        using var response = await httpClient.GetAsync(path, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private static async Task<string> BuildQuery(params (string Name, string? Value)[] parameters)
    {
        using var content = new FormUrlEncodedContent(
            parameters
                .Where(p => !string.IsNullOrWhiteSpace(p.Value))
                .Select(p => new KeyValuePair<string, string>(p.Name, p.Value!)));

        var encoded = await content.ReadAsStringAsync();
        return string.IsNullOrEmpty(encoded) ? string.Empty : "?" + encoded;
    }
}
