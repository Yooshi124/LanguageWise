using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.MiniGamesService.Api.Models;

namespace LanguageWise.MiniGamesService.Api.Clients;

public interface IAssistantCompletionClient
{
    Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken);
}

public sealed class GarryCompletionClient(HttpClient client, IHttpContextAccessor contextAccessor) : IAssistantCompletionClient
{
    public async Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<OpenRouterChatMessage> messages, CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext ?? throw new InvalidOperationException("No assistant request context.");
        var authorization = context.Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : context.Request.Cookies["token"];
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/completions")
        {
            Content = JsonContent.Create(new
            {
                message = messages[^1].Content,
                history = messages.Skip(2).SkipLast(1),
                domainRules = messages[0].Content,
                canonicalContext = messages[1].Content
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new AssistantProviderException("Garry could not start the assistant.", status);
        }
        try
        {
            return new AssistantCompletionStream(response, await response.Content.ReadAsStreamAsync(cancellationToken), true);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}

public sealed class AssistantCompletionStream(
    HttpResponseMessage response,
    Stream responseStream,
    bool fromGarry = false) : IAsyncDisposable
{
    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (fromGarry)
        {
            using var garryReader = new StreamReader(responseStream);
            while (await garryReader.ReadLineAsync(cancellationToken) is { } garryLine)
            {
                if (!garryLine.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }
                var streamEvent = ParseGarryEvent(garryLine[6..]);
                yield return streamEvent;
                if (streamEvent.Type == "done")
                {
                    yield break;
                }
            }
            throw new AssistantProviderStreamException();
        }

        using var reader = new StreamReader(responseStream);
        var dataLines = new List<string>();
        var doneReceived = false;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                if (dataLines.Count > 0)
                {
                    foreach (var streamEvent in ParseEvent(dataLines))
                    {
                        doneReceived |= streamEvent.Type == "done";
                        yield return streamEvent;
                    }
                }

                if (!doneReceived)
                {
                    throw new AssistantProviderStreamException();
                }

                yield break;
            }

            if (line.Length == 0)
            {
                if (dataLines.Count == 0)
                {
                    continue;
                }

                foreach (var streamEvent in ParseEvent(dataLines))
                {
                    doneReceived |= streamEvent.Type == "done";
                    yield return streamEvent;
                }

                dataLines.Clear();
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                dataLines.Add(line[5..].TrimStart());
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        responseStream.Dispose();
        response.Dispose();
        return ValueTask.CompletedTask;
    }

    private static ProviderStreamEvent ParseGarryEvent(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("content", out var content))
            {
                return ProviderStreamEvent.Delta(content.GetString()!);
            }
            if (root.TryGetProperty("reason", out var reason))
            {
                return ProviderStreamEvent.Done(reason.GetString() ?? "stop");
            }
        }
        catch (JsonException)
        {
            throw new AssistantProviderStreamException();
        }
        throw new AssistantProviderStreamException();
    }

    private static IReadOnlyList<ProviderStreamEvent> ParseEvent(IReadOnlyList<string> dataLines)
    {
        var data = string.Join('\n', dataLines);
        if (data == "[DONE]")
        {
            return [ProviderStreamEvent.Done()];
        }

        try
        {
            using var document = JsonDocument.Parse(data);
            if (!document.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0)
            {
                throw new AssistantProviderStreamException();
            }

            var choice = choices[0];
            var events = new List<ProviderStreamEvent>(2);
            if (choice.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var content) &&
                content.ValueKind == JsonValueKind.String)
            {
                var value = content.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    events.Add(ProviderStreamEvent.Delta(value));
                }
            }

            if (choice.TryGetProperty("finish_reason", out var finishReason) &&
                finishReason.ValueKind == JsonValueKind.String)
            {
                events.Add(ProviderStreamEvent.Done(finishReason.GetString() ?? "stop"));
            }

            return events;
        }
        catch (JsonException)
        {
            throw new AssistantProviderStreamException();
        }
    }
}

public sealed record ProviderStreamEvent(string Type, string? Content, string? Reason)
{
    public static ProviderStreamEvent Delta(string content) => new("delta", content, null);

    public static ProviderStreamEvent Done(string reason = "stop") => new("done", null, reason);
}

public sealed class AssistantProviderException(string message, System.Net.HttpStatusCode statusCode)
    : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class AssistantProviderStreamException()
    : Exception("The assistant provider returned an invalid stream.");
