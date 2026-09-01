using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LanguageWise.MiniGamesService.Api.Models;

namespace LanguageWise.MiniGamesService.Api.Clients;

public sealed class GamesDatabaseClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DatabaseResponse<DatabaseHealthResponse>> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("health", cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        DatabaseHealthResponse? health = null;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                health = JsonSerializer.Deserialize<DatabaseHealthResponse>(responseBody, JsonOptions);
            }
            catch (JsonException) when (!response.IsSuccessStatusCode)
            {
                // Preserve a non-JSON failure response as dependency error detail.
            }
        }

        if (response.IsSuccessStatusCode && health is null)
        {
            throw new JsonException("The database health response was empty or invalid.");
        }

        return new DatabaseResponse<DatabaseHealthResponse>(
            (int)response.StatusCode,
            health,
            response.IsSuccessStatusCode ? null : responseBody,
            response.Content.Headers.ContentType?.ToString());
    }

    public async Task<GameResponse?> GetGameAsync(int gameId, CancellationToken cancellationToken = default) =>
        await GetOptionalAsync<GameResponse>($"api/games/{gameId}", cancellationToken);

    public async Task<IReadOnlyList<GameResponse>> GetGamesByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<GameResponse>>($"api/games/user/{userId}", cancellationToken) ?? [];

    public async Task<IReadOnlyList<GameResponse>> GetGamesByUserIdAndTypeAsync(int userId, string gameType, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<GameResponse>>($"api/games/user/{userId}/type/{gameType}", cancellationToken) ?? [];

    public async Task<GameResponse?> CreateGameAsync(
        string gameType,
        int userId,
        string courseCode,
        string solution,
        IReadOnlyList<string> words,
        string difficulty = "intermediate",
        string? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/games",
            new { gameType, userId, courseCode, solution, words, difficulty, expiresAt },
            JsonOptions,
            cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<GameResponse>(JsonOptions, cancellationToken)
            : null;
    }

    public async Task<bool> DeleteGameAsync(int gameId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/games/{gameId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    // GameAttempts endpoints
    public async Task<GameAttemptResponse?> GetGameAttemptAsync(int attemptId, CancellationToken cancellationToken = default) =>
        await GetOptionalAsync<GameAttemptResponse>($"api/game-attempts/{attemptId}", cancellationToken);

    public async Task<IReadOnlyList<GameAttemptResponse>> GetGameAttemptsByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<GameAttemptResponse>>($"api/game-attempts/user/{userId}", cancellationToken) ?? [];

    public async Task<IReadOnlyList<GameAttemptResponse>> GetGameAttemptsByGameIdAsync(int gameId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<GameAttemptResponse>>($"api/game-attempts/game/{gameId}", cancellationToken) ?? [];

    public async Task<GameAttemptResponse?> GetLatestGameAttemptAsync(int gameId, int userId, CancellationToken cancellationToken = default) =>
        await GetOptionalAsync<GameAttemptResponse>($"api/game-attempts/game/{gameId}/user/{userId}/latest", cancellationToken);

    public async Task<GameAttemptResponse?> CreateGameAttemptAsync(
        int gameId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/game-attempts",
            new { gameId, userId },
            JsonOptions,
            cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<GameAttemptResponse>(JsonOptions, cancellationToken)
            : null;
    }

    public async Task<GameAttemptResponse?> UpdateGameAttemptAsync(
        int attemptId,
        int? score = null,
        bool? isWon = null,
        bool? isComplete = null,
        int? attemptCount = null,
        string? completedAt = null,
        int? timeSpentSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var updateRequest = new
        {
            score,
            isWon,
            isComplete,
            attemptCount,
            completedAt,
            timeSpentSeconds
        };

        using var response = await httpClient.PatchAsJsonAsync(
            $"api/game-attempts/{attemptId}",
            updateRequest,
            JsonOptions,
            cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<GameAttemptResponse>(JsonOptions, cancellationToken)
            : null;
    }

    public async Task<bool> DeleteGameAttemptAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/game-attempts/{attemptId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<T?> GetOptionalAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
            : default(T);
    }
}
