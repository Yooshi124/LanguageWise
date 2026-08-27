
using System.Net.Http.Json;
using LanguageWise.Shared.Api.Models;

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
}
