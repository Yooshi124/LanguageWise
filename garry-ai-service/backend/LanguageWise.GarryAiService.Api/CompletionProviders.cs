using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class CompletionProviders(IHttpClientFactory clients, IConfiguration configuration, ILogger<CompletionProviders> logger)
{
    public async Task<ProviderStream> StartAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken)
    {
        var apiKey = configuration["OpenRouter:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
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
            catch (Exception exception) when (exception is HttpRequestException or ProviderException
                || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                logger.LogWarning("OpenRouter could not start; trying Ollama: {ErrorType}", exception.GetType().Name);
            }
        }

        return await StartProviderAsync("ollama", "api/chat", new
        {
            model = configuration["Ollama:Model"] ?? "gemma4:e4b",
            messages,
            stream = true,
            think = false
        }, null, cancellationToken);
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
            return new ProviderStream(response, await response.Content.ReadAsStreamAsync(cancellationToken), provider);
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

public sealed class ProviderStream(HttpResponseMessage response, Stream body, string provider) : IAsyncDisposable
{
    public async IAsyncEnumerable<(string Type, string Value)> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(body);
        var data = new List<string>();
        var done = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (provider == "openrouter")
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
            }
            else if (line.Length == 0)
            {
                continue;
            }

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

            if (provider == "openrouter")
            {
                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                {
                    throw new InvalidDataException("Invalid provider response");
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
            else
            {
                if (root.TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String && content.GetString() is { Length: > 0 } text)
                {
                    yield return ("delta", text);
                }
                if (root.TryGetProperty("done", out var ended) && ended.ValueKind == JsonValueKind.True)
                {
                    done = true;
                    yield return ("done", root.TryGetProperty("done_reason", out var reason)
                        ? reason.GetString() ?? "stop" : "stop");
                    break;
                }
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