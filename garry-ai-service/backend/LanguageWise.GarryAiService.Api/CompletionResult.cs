using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class CompletionResult(ProviderStream stream, ILogger<CompletionResult> logger) : IResult
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Append("X-Accel-Buffering", "no");
        await using (stream)
        {
            try
            {
                await foreach (var (type, value) in stream.ReadAsync(context.RequestAborted))
                {
                    if (type == "delta")
                    {
                        await SendAsync(context.Response, type, new { content = value }, context.RequestAborted);
                    }
                    else
                    {
                        await SendAsync(context.Response, type, new { reason = value }, context.RequestAborted);
                    }
                }
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
            catch (IOException) when (context.RequestAborted.IsCancellationRequested) { }
            catch (Exception exception) when (exception is IOException or JsonException or HttpRequestException or InvalidOperationException)
            {
                logger.LogWarning("Provider stream ended unexpectedly: {ErrorType}", exception.GetType().Name);
                await SendAsync(context.Response, "error", new
                {
                    message = "Garry's response was interrupted. Please try again.",
                    code = "provider_stream_error"
                }, context.RequestAborted);
            }
        }
    }

    private static async Task SendAsync<T>(HttpResponse response, string eventName, T payload, CancellationToken cancellationToken)
    {
        await response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await response.WriteAsync($"data: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}