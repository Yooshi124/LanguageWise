using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Clients;

public interface IAssistantCompletionClient
{
    Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken);
}

public sealed class GarryCompletionClient(
    HttpClient client,
    IHttpContextAccessor contextAccessor,
    IMcpToolClient mcpTools) : IAssistantCompletionClient
{
    public async Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages, CancellationToken cancellationToken)
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
            return new AssistantCompletionStream(response, await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}

/// <summary>
/// A source of answer fragments for the SSE relay. Implemented by the live model
/// stream and, when the model cannot be reached, by the canned help text that
/// stands in for it — so both take the same path out to the browser.
/// </summary>
public interface IAssistantEventStream : IAsyncDisposable
{
    IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// The assistant's streaming response. A stream that stops without a done event
/// has lost part of the answer, so it is an error rather than a short reply.
/// </summary>
public sealed class AssistantCompletionStream(
    HttpResponseMessage response,
    Stream responseStream) : IAssistantEventStream
{
    internal const string DeltaType = "delta";
    internal const string DoneType = "done";
    internal const string ToolType = "tool";

    /// <summary>
    /// The done reason used when the answer is the stored help text rather than
    /// the model's own words, so the browser can label it.
    /// </summary>
    internal const string FallbackReason = "fallback";

    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(responseStream);
        string? eventName = null;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventName = line[7..].Trim();
                continue;
            }
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var streamEvent = ParseGarryEvent(eventName, line[6..]);
            eventName = null;
            yield return streamEvent;
            if (streamEvent.Type == DoneType)
            {
                yield break;
            }
        }
        throw new AssistantProviderStreamException();
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
            if (eventName == ToolType)
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
}

public sealed record ProviderStreamEvent(string Type, string? Content, string? Reason, AssistantToolEvent? ToolEvent = null)
{
    public static ProviderStreamEvent Delta(string content) =>
        new(AssistantCompletionStream.DeltaType, content, null);

    public static ProviderStreamEvent Done(string reason = "stop") =>
        new(AssistantCompletionStream.DoneType, null, reason);

    public static ProviderStreamEvent Tool(AssistantToolEvent toolEvent) =>
        new(AssistantCompletionStream.ToolType, null, null, toolEvent);
}

/// <summary>The model refused the request before any of the answer was written.</summary>
public sealed class AssistantProviderException(string message, HttpStatusCode statusCode)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

/// <summary>The model accepted the request, then sent something unreadable.</summary>
public sealed class AssistantProviderStreamException()
    : Exception("The assistant model returned an invalid stream.");
