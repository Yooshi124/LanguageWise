using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class CompletionProviders(IHttpClientFactory clients, IConfiguration configuration)
{
    public async Task<ProviderStream> StartAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ProviderException(System.Net.HttpStatusCode.Unauthorized);
        }
        var model = configuration["OpenRouter:Model"] ?? "google/gemma-4-26b-a4b-it";
        var maxTokens = configuration.GetValue("OpenRouter:MaxOutputTokens", 1024);
        return await StartProviderAsync("openrouter", "chat/completions", new
        {
            model,
            messages,
            stream = true,
            max_tokens = maxTokens
        }, apiKey, cancellationToken);
    }

    private async Task<ProviderStream> StartProviderAsync(
        string provider, string path, object payload, string? apiKey, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        if (apiKey is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        var response = await clients.CreateClient(provider).SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new ProviderException(status);
        }

        try
        {
            return new ProviderStream(response, await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}

public sealed class ProviderException(System.Net.HttpStatusCode status) : Exception("Provider refused the request")
{
    public System.Net.HttpStatusCode Status { get; } = status;
}

public sealed class ProviderStream(HttpResponseMessage response, Stream body) : IAsyncDisposable
{
    public async IAsyncEnumerable<(string Type, string Value)> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(body);
        var data = new List<string>();
        var done = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                data.Add(line[5..].TrimStart());
            }
            if (line.Length != 0 || data.Count == 0)
            {
                continue;
            }
            line = string.Join('\n', data);
            data.Clear();

            if (line == "[DONE]")
            {
                done = true;
                yield return ("done", "stop");
                break;
            }

            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out _))
            {
                throw new InvalidDataException("Provider stream error");
            }

            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                throw new InvalidDataException("Invalid OpenRouter response");
            }
            var choice = choices[0];
            if (choice.TryGetProperty("delta", out var delta)
                && delta.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String && content.GetString() is { Length: > 0 } text)
            {
                yield return ("delta", text);
            }
            if (choice.TryGetProperty("finish_reason", out var finish)
                && finish.ValueKind == JsonValueKind.String)
            {
                done = true;
                yield return ("done", finish.GetString() ?? "stop");
                break;
            }
        }

        if (!done)
        {
            throw new InvalidDataException("Truncated provider stream");
        }
    }

    public ValueTask DisposeAsync()
    {
        body.Dispose();
        response.Dispose();
        return ValueTask.CompletedTask;
    }
}