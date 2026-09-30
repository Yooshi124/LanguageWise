using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LanguageWise.Shared.Api.Clients;

public sealed record RagResult(string Source, string Heading, double Relevance, string Text);

public sealed record RagQueryResponse(IReadOnlyList<RagResult> Results, int ResultCount);

/// <summary>
/// Client for the host RAG server's general endpoint (<c>POST /query</c>), which never returns
/// TECHNICAL- (internal) passages.
/// </summary>
public sealed class RagClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RagQueryResponse> QueryAsync(
        string query,
        int? nResults,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "query",
            new RagQueryBody(query, nResults),
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RagQueryResponse>(JsonOptions, cancellationToken);
        return payload ?? new RagQueryResponse([], 0);
    }

    // The RAG server expects snake_case for n_results; everything else matches its default casing.
    private sealed record RagQueryBody(string Query, [property: JsonPropertyName("n_results")] int? NResults);
}
