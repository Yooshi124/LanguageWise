using System.Net.Http.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

/// <summary>
/// Talks to the database microservice over HTTP. The backend never opens the SQLite file
/// itself; the database service is the only owner of that file.
/// </summary>
public sealed class SampleItemsClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<SampleItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await httpClient.GetFromJsonAsync<List<SampleItem>>("api/items", cancellationToken);
        return items ?? [];
    }
}
