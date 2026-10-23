using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class CompletionProviders(IHttpClientFactory clients, IConfiguration configuration)
{
    public async Task<ProviderStream> StartAsync(IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken,
        int? outputTokens = null, double? temperature = null)
    {
        var apiKey = configuration["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ProviderException(System.Net.HttpStatusCode.Unauthorized);
        }
        var model = configuration["OpenRouter:Model"] ?? "google/gemma-4-26b-a4b-it";
        var maxTokens = configuration.GetValue("OpenRouter:MaxOutputTokens", 1024);
        maxTokens = Math.Min(outputTokens ?? maxTokens, maxTokens);
        return await StartProviderAsync("openrouter", "chat/completions", new
        {
            model,
            messages = messages.Append(new ChatMessage("system", """
                No tools are available for this response. Do not call tools, emit tool-call syntax, or claim
                to have searched documentation or fetched live data. Earlier tool calls in the conversation
                do not grant tool access. If asked to use a tool, explain that tools are unavailable and
                answer only from the supplied context when possible.
                """)),
            stream = true,
            tool_choice = "none",
            max_tokens = maxTokens,
            temperature
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
        var toolSyntax = new ToolSyntaxGuard();
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
                if (toolSyntax.Push(string.Empty, final: true) is { Length: > 0 } remaining)
                    yield return ("delta", remaining);
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
            if (choice.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object)
            {
                if (delta.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array && calls.GetArrayLength() > 0
                    || delta.TryGetProperty("function_call", out var function) && function.ValueKind == JsonValueKind.Object)
                {
                    if (toolSyntax.Block() is { Length: > 0 } unavailable) yield return ("delta", unavailable);
                }
                if (delta.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String && content.GetString() is { Length: > 0 } text
                    && toolSyntax.Push(text) is { Length: > 0 } safeText)
                {
                    yield return ("delta", safeText);
                }
            }
            if (choice.TryGetProperty("finish_reason", out var finish)
                && finish.ValueKind == JsonValueKind.String)
            {
                done = true;
                if (finish.GetString() is "tool_calls" or "function_call"
                    && toolSyntax.Block() is { Length: > 0 } unavailable) yield return ("delta", unavailable);
                if (toolSyntax.Push(string.Empty, final: true) is { Length: > 0 } remaining)
                    yield return ("delta", remaining);
                yield return ("done", toolSyntax.Blocked ? "stop" : finish.GetString() ?? "stop");
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

    private sealed class ToolSyntaxGuard
    {
        private static readonly string[] Markers = ["<|tool_call>", "<|tool_call|>", "<tool_call|>", "<tool_call>", "[TOOL_CALLS]", "<function=", "<|python_tag|>"];
        private string pending = string.Empty;
        public bool Blocked { get; private set; }

        public string Block()
        {
            if (Blocked) return string.Empty;
            Blocked = true;
            pending = string.Empty;
            return "\n\nTools are unavailable for this response, so I cannot run that search or call a tool.";
        }

        public string Push(string text, bool final = false)
        {
            if (Blocked) return string.Empty;
            pending += text;
            var markerIndex = Markers.Select(marker => pending.IndexOf(marker, StringComparison.OrdinalIgnoreCase))
                .Where(index => index >= 0).DefaultIfEmpty(-1).Min();
            if (markerIndex >= 0)
            {
                var prefix = pending[..markerIndex];
                return prefix + Block();
            }
            var retained = 0;
            if (!final)
            {
                for (var length = Math.Min(pending.Length, Markers.Max(marker => marker.Length) - 1); length > 0; length--)
                {
                    if (Markers.Any(marker => marker.StartsWith(pending[^length..], StringComparison.OrdinalIgnoreCase)))
                    {
                        retained = length;
                        break;
                    }
                }
            }
            var output = pending[..(pending.Length - retained)];
            pending = pending[(pending.Length - retained)..];
            return output;
        }
    }
}