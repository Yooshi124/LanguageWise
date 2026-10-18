using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class GarryCompletionResult(
	CompletionRequest request,
	string? userToken,
	GarryAgentPipeline pipeline,
	ILoggerFactory loggerFactory) : IResult
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public async Task ExecuteAsync(HttpContext context)
	{
		var logger = loggerFactory.CreateLogger<GarryCompletionResult>();
		var cancellationToken = context.RequestAborted;
		try
		{
			var answer = await pipeline.RunAsync(request, userToken, async toolEvent =>
			{
				StartStream(context.Response);
				await SendAsync(context.Response, "tool", new
				{
					name = toolEvent.Name,
					arguments = toolEvent.Arguments,
					isError = toolEvent.IsError,
					result = toolEvent.Result
				}, cancellationToken);
			}, cancellationToken);
			StartStream(context.Response);
			await SendAsync(context.Response, "delta", new { content = answer }, cancellationToken);
			await SendAsync(context.Response, "done", new { reason = "stop" }, cancellationToken);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		catch (Exception exception) when (exception is HttpRequestException or ProviderException
			|| (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
		{
			logger.LogWarning("Garry agent pipeline could not complete: {ErrorType}", exception.GetType().Name);
			if (!context.Response.HasStarted)
			{
				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
				return;
			}
			await SendErrorAsync(context.Response, cancellationToken);
		}
		catch (IOException) when (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or InvalidOperationException)
		{
			logger.LogWarning("Garry agent response ended unexpectedly: {ErrorType}", exception.GetType().Name);
			StartStream(context.Response);
			await SendErrorAsync(context.Response, cancellationToken);
		}
	}

	private static Task SendErrorAsync(HttpResponse response, CancellationToken cancellationToken) =>
		SendAsync(response, "error", new
		{
			message = "Garry's response was interrupted. Please try again.",
			code = "provider_stream_error"
		}, cancellationToken);

	private static void StartStream(HttpResponse response)
	{
		if (response.HasStarted)
		{
			return;
		}
		response.ContentType = "text/event-stream";
		response.Headers.CacheControl = "no-cache";
		response.Headers.Append("X-Accel-Buffering", "no");
	}

	private static async Task SendAsync<T>(HttpResponse response, string eventName, T payload, CancellationToken cancellationToken)
	{
		await response.WriteAsync($"event: {eventName}\n", cancellationToken);
		await response.WriteAsync($"data: {JsonSerializer.Serialize(payload, JsonOptions)}\n\n", cancellationToken);
		await response.Body.FlushAsync(cancellationToken);
	}
}
