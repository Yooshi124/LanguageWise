using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public interface IAssistantCompletionClient
{
    Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken);
}

public sealed class GarryCompletionClient(
    HttpClient client,
    IHttpContextAccessor contextAccessor,
    IMcpToolClient mcpTools) : IAssistantCompletionClient
{
    public async Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<OpenRouterChatMessage> messages, CancellationToken cancellationToken)
    {
        var context = contextAccessor.HttpContext ?? throw new InvalidOperationException("No assistant request context.");
        var token = UserTokenReader.Read(context.Request);
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/completions")
        {
            Content = JsonContent.Create(new
            {
                message = messages[^1].Content,
                history = messages.Skip(2).SkipLast(1),
                domainRules = messages[0].Content,
                canonicalContext = messages[1].Content,
                toolScope = mcpTools.Enabled ? McpToolClient.Scope : null
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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (fromGarry)
        {
            using var garryReader = new StreamReader(responseStream);
            string? eventName = null;
            while (await garryReader.ReadLineAsync(cancellationToken) is { } garryLine)
            {
                if (garryLine.StartsWith("event: ", StringComparison.Ordinal))
                {
                    eventName = garryLine[7..].Trim();
                    continue;
                }
                if (!garryLine.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }
                var streamEvent = ParseGarryEvent(eventName, garryLine[6..]);
                eventName = null;
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

    private static ProviderStreamEvent ParseGarryEvent(string? eventName, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (eventName == "tool")
            {
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("name", out var name) ||
                    name.ValueKind != JsonValueKind.String)
                {
                    throw new AssistantProviderStreamException();
                }
                return ProviderStreamEvent.Tool(new AssistantToolEvent(
                    name.GetString()!,
                    root.TryGetProperty("arguments", out var arguments) ? arguments.Clone() : null,
                    root.TryGetProperty("isError", out var isError) && isError.ValueKind == JsonValueKind.True,
                    root.TryGetProperty("result", out var result) ? result.Clone() : null));
            }
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

public sealed record ProviderStreamEvent(string Type, string? Content, string? Reason, AssistantToolEvent? ToolEvent = null)
{
    public static ProviderStreamEvent Delta(string content) => new("delta", content, null);

    public static ProviderStreamEvent Done(string reason = "stop") => new("done", null, reason);

    public static ProviderStreamEvent Tool(AssistantToolEvent toolEvent) => new("tool", null, null, toolEvent);
}

public sealed class AssistantProviderException(string message, System.Net.HttpStatusCode statusCode)
    : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class AssistantProviderStreamException()
    : Exception("The assistant provider returned an invalid stream.");
