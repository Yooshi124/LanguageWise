using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Options;
using Microsoft.Extensions.Options;

namespace LanguageWise.ChatDiscussionService.Api.Clients;

public interface IAssistantCompletionClient
{
    Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken);
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
/// AI mode's model call. Opens a streaming chat completion against Ollama and
/// hands back the raw response stream, so the first token can reach the browser
/// long before the last one is written.
/// </summary>
public sealed class OllamaAssistantClient(
    HttpClient httpClient,
    IOptions<OllamaOptions> options) : IAssistantCompletionClient
{
    private readonly OllamaOptions options = options.Value;

    public async Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(new OllamaChatRequest(
                options.Model,
                messages,
                Stream: true,
                Think: false,
                new OllamaModelOptions(
                    options.Temperature,
                    options.TopP,
                    options.MaxOutputTokens)))
        };

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            response.Dispose();

            // A 404 here is almost always the model not having been pulled, which
            // is worth separating from the model being there and refusing.
            throw new AssistantProviderException(
                "The assistant model rejected the request.",
                statusCode);
        }

        // The response and its stream both stay alive until the caller disposes
        // the AssistantCompletionStream, so only dispose here if handing it over fails.
        var disposeResponse = true;
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            disposeResponse = false;
            return new AssistantCompletionStream(response, stream);
        }
        finally
        {
            if (disposeResponse)
            {
                response.Dispose();
            }
        }
    }
}

/// <summary>
/// Ollama's streaming response, read one chunk at a time. The wire format is
/// newline-delimited JSON rather than server-sent events: one object per line,
/// each carrying a fragment, and the last one flagged done.
///
/// A stream that stops without that flag has lost part of the answer, so it is
/// an error rather than a short reply — a truncated response is never passed off
/// as a complete one.
/// </summary>
public sealed class AssistantCompletionStream(
    HttpResponseMessage response,
    Stream responseStream) : IAssistantEventStream
{
    internal const string DeltaType = "delta";
    internal const string DoneType = "done";

    /// <summary>
    /// The done reason used when the answer is the stored help text rather than
    /// the model's own words, so the browser can label it.
    /// </summary>
    internal const string FallbackReason = "fallback";

    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(responseStream);
        var doneReceived = false;

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                if (!doneReceived)
                {
                    throw new AssistantProviderStreamException();
                }

                yield break;
            }

            if (line.Length == 0)
            {
                continue;
            }

            foreach (var streamEvent in ParseChunk(line))
            {
                doneReceived |= streamEvent.Type == DoneType;
                yield return streamEvent;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        responseStream.Dispose();
        response.Dispose();
        return ValueTask.CompletedTask;
    }

    private static IReadOnlyList<ProviderStreamEvent> ParseChunk(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new AssistantProviderStreamException();
            }

            // Ollama reports a model-side failure inside the stream rather than
            // by status code, so a chunk carrying 'error' ends the answer.
            if (root.TryGetProperty("error", out _))
            {
                throw new AssistantProviderStreamException();
            }

            var events = new List<ProviderStreamEvent>(2);

            if (root.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                var value = content.GetString();

                if (!string.IsNullOrEmpty(value))
                {
                    events.Add(ProviderStreamEvent.Delta(value));
                }
            }

            if (root.TryGetProperty("done", out var done)
                && done.ValueKind == JsonValueKind.True)
            {
                var reason = root.TryGetProperty("done_reason", out var doneReason)
                    && doneReason.ValueKind == JsonValueKind.String
                        ? doneReason.GetString()
                        : null;

                events.Add(ProviderStreamEvent.Done(reason ?? "stop"));
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
    public static ProviderStreamEvent Delta(string content) =>
        new(AssistantCompletionStream.DeltaType, content, null);

    public static ProviderStreamEvent Done(string reason = "stop") =>
        new(AssistantCompletionStream.DoneType, null, reason);
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
