using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class ToolCompletionResult(
	CompletionRequest request,
	string? userToken,
	GarryToolLoop toolLoop,
	CompletionProviders providers,
	ILoggerFactory loggerFactory) : IResult
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public async Task ExecuteAsync(HttpContext context)
	{
		var logger = loggerFactory.CreateLogger<ToolCompletionResult>();
		var cancellationToken = context.RequestAborted;
		string? answer = null;
		try
		{
			answer = await toolLoop.RunAsync(request, userToken, async toolEvent =>
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
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		catch (Exception exception)
		{
			logger.LogWarning("Tool loop failed; answering without tools: {ErrorType}", exception.GetType().Name);
		}

		if (!string.IsNullOrWhiteSpace(answer))
		{
			StartStream(context.Response);
			await SendAsync(context.Response, "delta", new { content = answer }, cancellationToken);
			await SendAsync(context.Response, "done", new { reason = "stop" }, cancellationToken);
			return;
		}

		ProviderStream completion;
		try
		{
			completion = await providers.StartAsync(request.BuildMessages(), cancellationToken);
		}
		catch (Exception exception) when (exception is HttpRequestException or ProviderException
			|| (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
		{
			logger.LogWarning("Both assistant providers could not start: {ErrorType}", exception.GetType().Name);
			if (!context.Response.HasStarted)
			{
				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
				return;
			}
			await SendAsync(context.Response, "error", new
			{
				message = "Garry's response was interrupted. Please try again.",
				code = "provider_stream_error"
			}, cancellationToken);
			return;
		}

		await new CompletionResult(completion, loggerFactory.CreateLogger<CompletionResult>()).ExecuteAsync(context);
	}

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
