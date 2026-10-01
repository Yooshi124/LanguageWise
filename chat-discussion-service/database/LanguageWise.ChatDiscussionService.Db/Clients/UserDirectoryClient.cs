using System.Net.Http.Json;
using LanguageWise.ChatDiscussionService.Db.Models;

namespace LanguageWise.ChatDiscussionService.Db.Clients;

public sealed class UserDirectoryClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<DirectoryUser>> GetUsersAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var query = string.Join("&", userIds.Select(id => $"ids={id}"));
        return await httpClient.GetFromJsonAsync<List<DirectoryUser>>($"api/users?{query}", cancellationToken) ?? [];
    }
}
