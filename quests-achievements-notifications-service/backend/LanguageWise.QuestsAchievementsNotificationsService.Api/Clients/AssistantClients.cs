using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";
    public string ApiKey { get; init; } = string.Empty;
}

public interface IAssistantCompletionClient
{
    Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken);
}

public interface IAssistantEventStream : IAsyncDisposable
{
    IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(CancellationToken cancellationToken);
}

public sealed class FallbackAssistantCompletionClient(
    OpenRouterAssistantClient openRouterClient,
    OllamaAssistantClient ollamaClient,
    IOptions<OpenRouterOptions> openRouterOptions,
    ILogger<FallbackAssistantCompletionClient> logger) : IAssistantCompletionClient
{
    public async Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(openRouterOptions.Value.ApiKey))
        {
            try
            {
                return await openRouterClient.StartCompletionAsync(messages, cancellationToken);
            }
            catch (AssistantProviderException exception)
            {
                logger.LogWarning(
                    "OpenRouter rejected the assistant request with HTTP status {HttpStatus}; falling back to Ollama.",
                    (int)exception.StatusCode);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    "OpenRouter was unreachable with error type {ErrorType}; falling back to Ollama.",
                    exception.GetType().Name);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("OpenRouter timed out; falling back to Ollama.");
            }
        }

        return await ollamaClient.StartCompletionAsync(messages, cancellationToken);
    }
}

public sealed class OpenRouterAssistantClient(
    HttpClient httpClient,
    IOptions<OpenRouterOptions> options)
{
    internal const string Model = "google/gemma-4-26b-a4b-it";
    internal const int MaxOutputTokens = 1024;
    private readonly OpenRouterOptions options = options.Value;

    public async Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(new OpenRouterChatRequest(
                Model,
                messages,
                Stream: true,
                MaxOutputTokens))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            response.Dispose();
            throw new AssistantProviderException("OpenRouter rejected the request.", statusCode);
        }

        return await OpenRouterEventStream.CreateAsync(response, cancellationToken);
    }
}

public sealed class OllamaAssistantClient(
    HttpClient httpClient,
    IOptions<OllamaOptions> options)
{
    private readonly OllamaOptions options = options.Value;

    public async Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(new OllamaChatRequest(
                options.Model,
                messages,
                Stream: true,
                Think: false))
        };

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            response.Dispose();
            throw new AssistantProviderException("Ollama rejected the request.", statusCode);
        }

        return await OllamaEventStream.CreateAsync(response, cancellationToken);
    }
}

internal abstract class ProviderEventStream(
    HttpResponseMessage response,
    Stream responseStream) : IAssistantEventStream
{
    protected Stream ResponseStream { get; } = responseStream;

    public abstract IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        CancellationToken cancellationToken);

    public ValueTask DisposeAsync()
    {
        ResponseStream.Dispose();
        response.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class OpenRouterEventStream(
    HttpResponseMessage response,
    Stream responseStream) : ProviderEventStream(response, responseStream)
{
    internal static async Task<OpenRouterEventStream> CreateAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return new OpenRouterEventStream(
                response,
                await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public override async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(ResponseStream);
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
                if (dataLines.Count > 0)
                {
                    foreach (var streamEvent in ParseEvent(dataLines))
                    {
                        doneReceived |= streamEvent.Type == "done";
                        yield return streamEvent;
                    }

                    dataLines.Clear();
                }

                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                dataLines.Add(line[5..].TrimStart());
            }
        }
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
            if (!document.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
            {
                throw new AssistantProviderStreamException();
            }

            var choice = choices[0];
            var events = new List<ProviderStreamEvent>(2);
            if (choice.TryGetProperty("delta", out var delta)
                && delta.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String
                && content.GetString() is { Length: > 0 } value)
            {
                events.Add(ProviderStreamEvent.Delta(value));
            }

            if (choice.TryGetProperty("finish_reason", out var finishReason)
                && finishReason.ValueKind == JsonValueKind.String)
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

internal sealed class OllamaEventStream(
    HttpResponseMessage response,
    Stream responseStream) : ProviderEventStream(response, responseStream)
{
    internal static async Task<OllamaEventStream> CreateAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return new OllamaEventStream(
                response,
                await response.Content.ReadAsStreamAsync(cancellationToken));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public override async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(ResponseStream);
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
                doneReceived |= streamEvent.Type == "done";
                yield return streamEvent;
            }
        }
    }

    private static IReadOnlyList<ProviderStreamEvent> ParseChunk(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.TryGetProperty("error", out _))
            {
                throw new AssistantProviderStreamException();
            }

            var events = new List<ProviderStreamEvent>(2);
            if (root.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String
                && content.GetString() is { Length: > 0 } value)
            {
                events.Add(ProviderStreamEvent.Delta(value));
            }

            if (root.TryGetProperty("done", out var done) && done.ValueKind == JsonValueKind.True)
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
    public static ProviderStreamEvent Delta(string content) => new("delta", content, null);

    public static ProviderStreamEvent Done(string reason = "stop") => new("done", null, reason);
}

public sealed class AssistantProviderException(string message, HttpStatusCode statusCode)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class AssistantProviderStreamException()
    : Exception("The assistant provider returned an invalid stream.");