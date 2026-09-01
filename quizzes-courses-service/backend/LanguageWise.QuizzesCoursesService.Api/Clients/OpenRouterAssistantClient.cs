using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Models;
using LanguageWise.QuizzesCoursesService.Api.Options;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public interface IAssistantCompletionClient
{
    Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken);
}

public sealed class OpenRouterAssistantClient(
    HttpClient httpClient,
    IOptions<OpenRouterOptions> options) : IAssistantCompletionClient
{
    private const int MaximumRateLimitRetries = 3;
    private readonly OpenRouterOptions options = options.Value;

    public async Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(new OpenRouterChatRequest(
                    options.Model,
                    messages,
                    Stream: true,
                    options.MaxOutputTokens))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests &&
                attempt < MaximumRateLimitRetries)
            {
                var retryDelay = response.Headers.RetryAfter?.Delta ??
                    TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                response.Dispose();
                await Task.Delay(
                    retryDelay > TimeSpan.FromSeconds(8)
                        ? TimeSpan.FromSeconds(8)
                        : retryDelay,
                    cancellationToken);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                response.Dispose();
                throw new AssistantProviderException(
                    "The assistant provider rejected the request.",
                    statusCode);
            }

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
}

public sealed class AssistantCompletionStream(
    HttpResponseMessage response,
    Stream responseStream) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
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
