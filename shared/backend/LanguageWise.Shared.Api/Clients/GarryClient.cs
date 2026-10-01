using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace LanguageWise.Shared.Api.Clients;

public sealed class GarryException(HttpStatusCode statusCode)
    : Exception($"Garry returned {(int)statusCode}.")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>Collects one complete (non-tool) Garry answer from its streaming completions endpoint.</summary>
public sealed class GarryClient(HttpClient httpClient)
{
    public async Task<string> CompleteAsync(
        string userToken,
        string domainRules,
        string canonicalContext,
        string message,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/completions")
        {
            Content = JsonContent.Create(new
            {
                message,
                history = Array.Empty<object>(),
                domainRules,
                canonicalContext
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new GarryException(response.StatusCode);
        }

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        var answer = new StringBuilder();
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line[6..]);
            var root = document.RootElement;
            if (root.TryGetProperty("code", out _))
            {
                throw new GarryException(HttpStatusCode.BadGateway);
            }
            if (root.TryGetProperty("content", out var content))
            {
                answer.Append(content.GetString());
            }
            else if (root.TryGetProperty("reason", out _))
            {
                return answer.ToString();
            }
        }

        throw new GarryException(HttpStatusCode.BadGateway);
    }
}
