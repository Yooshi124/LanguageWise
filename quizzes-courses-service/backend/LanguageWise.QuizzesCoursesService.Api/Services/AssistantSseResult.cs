using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Services;

public sealed class AssistantSseResult(
    AssistantCompletionStream completion,
    ILogger<AssistantSseResult> logger) : IResult
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Append("X-Accel-Buffering", "no");

        var doneSent = false;
        await using (completion)
        {
            try
            {
                await foreach (var streamEvent in completion.ReadEventsAsync(
                    httpContext.RequestAborted))
                {
                    if (streamEvent.Type == "delta")
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
                logger.LogInformation("Assistant stream was cancelled by the client.");
            }
            catch (IOException) when (httpContext.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation("Assistant stream connection was closed by the client.");
            }
            catch (AssistantProviderStreamException)
            {
                logger.LogWarning("Assistant provider stream parsing failed.");
                await WriteTerminalErrorAsync(httpContext.Response, httpContext.RequestAborted);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    "Assistant provider stream failed with HTTP request error type {ErrorType}.",
                    exception.GetType().Name);
                await WriteTerminalErrorAsync(httpContext.Response, httpContext.RequestAborted);
            }
            catch (IOException exception)
            {
                logger.LogWarning(
                    "Assistant provider stream failed with I/O error type {ErrorType}.",
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
