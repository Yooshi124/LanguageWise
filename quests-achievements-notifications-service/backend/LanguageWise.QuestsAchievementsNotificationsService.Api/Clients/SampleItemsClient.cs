using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

/// <summary>
/// Talks to PostgREST over HTTP. The backend does not connect to PostgreSQL directly.
/// </summary>
public sealed class SampleItemsClient(IPostgrestClient postgrestClient)
{
    public async Task<SampleItem> CreateAsync(SampleItem item, CancellationToken cancellationToken = default)
    {
        var response = await postgrestClient
            .Table<SampleItemEntity>()
            .Insert(SampleItemEntity.FromModel(item), cancellationToken: cancellationToken);

        return response.Models.Single().ToModel();
    }

    public async Task<IReadOnlyList<SampleItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await postgrestClient
            .Table<SampleItemEntity>()
            .Order(item => item.Id, Constants.Ordering.Ascending)
            .Get(cancellationToken);

        return response.Models.Select(item => item.ToModel()).ToList();
    }

    public async Task<SampleItem> UpdateAsync(SampleItem item, CancellationToken cancellationToken = default)
    {
        var response = await postgrestClient
            .Table<SampleItemEntity>()
            .Update(SampleItemEntity.FromModel(item), cancellationToken: cancellationToken);

        return response.Models.Single().ToModel();
    }

    public Task DeleteAsync(SampleItem item, CancellationToken cancellationToken = default) =>
        postgrestClient
            .Table<SampleItemEntity>()
            .Delete(SampleItemEntity.FromModel(item), cancellationToken: cancellationToken);
}
