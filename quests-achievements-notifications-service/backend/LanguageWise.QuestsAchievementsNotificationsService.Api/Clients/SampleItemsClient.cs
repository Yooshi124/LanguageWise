using System.Net.Http.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

/// <summary>
/// Talks to PostgREST over HTTP. The backend does not connect to PostgreSQL directly.
/// </summary>
public sealed class SampleItemsClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<SampleItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await httpClient.GetFromJsonAsync<List<SampleItem>>("sample_items?order=id", cancellationToken);
        return items ?? [];
    }
}
