namespace LanguageWise.Shared.Api.Clients;

/// <summary>
/// Talks to the database microservice over HTTP. The backend never opens the SQLite file
/// itself; the database service is the only owner of that file.
/// </summary>
public sealed class UsersClient(HttpClient httpClient)
{
    internal async Task<VerifyResponse> VerifyAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/users/verify", new { Username = username, Password = password }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new VerifyResponse(false, 0);
        }

        var result = await response.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: cancellationToken);
        return result ?? new VerifyResponse(false, 0);
    }

    internal async Task<int?> RecordLoginAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync($"api/users/{userId}/login-streak", null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginStreakResponse>(cancellationToken: cancellationToken))?.Value;
    }
}

internal sealed record LoginStreakResponse(int Value);
