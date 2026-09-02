using System.Net.Http.Json;
using LanguageWise.ChatDiscussionService.Db.Models;

namespace LanguageWise.ChatDiscussionService.Db.Clients;

/// <summary>
/// Reads the course list from the quizzes and courses database service, which owns
/// the catalogue and needs no token.
/// </summary>
public sealed class CourseCatalogClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<CatalogCourse>> GetCoursesAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<List<CatalogCourse>>("api/courses", cancellationToken) ?? [];
}
