using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Services;

/// <summary>
/// Relays the provider's stream to the browser as server-sent events, flushing
/// after each one so the answer appears a word at a time.
///
/// Every ending is deliberate. A client that walks away is logged and dropped;
/// a provider that breaks mid-answer gets one terminal 'error' event, so the
/// browser never sits waiting on a stream that will not continue.
/// </summary>
public sealed class AssistantSseResult(
    IAssistantEventStream completion,
    ILogger<AssistantSseResult> logger) : IResult
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";

        // Belt and braces with the nginx configuration: without it a buffering
        // proxy holds the whole answer back and streaming gains nothing.
        httpContext.Response.Headers.Append("X-Accel-Buffering", "no");

        var doneSent = false;

        await using (completion)
        {
            try
            {
                await foreach (var streamEvent in completion.ReadEventsAsync(httpContext.RequestAborted))
                {
                    if (streamEvent.Type == AssistantCompletionStream.DeltaType)
                    {
                        await WriteEventAsync(
                            httpContext.Response,
                            "delta",
                            new AssistantDeltaEvent(streamEvent.Content!),
                            httpContext.RequestAborted);
                    }
                    else if (!doneSent)
                    {
                        await WriteEventAsync(
                            httpContext.Response,
                            "done",
                            new AssistantDoneEvent(streamEvent.Reason ?? "stop"),
                            httpContext.RequestAborted);
                        doneSent = true;
                    }
                }

                if (!doneSent)
                {
                    await WriteEventAsync(
                        httpContext.Response,
                        "done",
                        new AssistantDoneEvent("stop"),
                        httpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation("The assistant stream was cancelled by the client.");
            }
            catch (IOException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation("The assistant stream connection was closed by the client.");
            }
            catch (AssistantProviderStreamException)
            {
                logger.LogWarning("Parsing the assistant provider stream failed.");
                await WriteTerminalErrorAsync(httpContext.Response, httpContext.RequestAborted);
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException)
            {
                logger.LogWarning(
                    "The assistant provider stream failed with error type {ErrorType}.",
                    exception.GetType().Name);
                await WriteTerminalErrorAsync(httpContext.Response, httpContext.RequestAborted);
            }
        }
    }

    private static Task WriteTerminalErrorAsync(
        HttpResponse response,
        CancellationToken cancellationToken) =>
        WriteEventAsync(
            response,
            "error",
            new AssistantErrorEvent(
                "The assistant response was interrupted. Please try again.",
                "provider_stream_error"),
            cancellationToken);

    private static async Task WriteEventAsync<T>(
        HttpResponse response,
        string eventName,
        T payload,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await response.WriteAsync(
            $"data: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n",
            cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
