using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LanguageWise.McpServer.Security;
using ModelContextProtocol;

namespace LanguageWise.McpServer.Tools;

public sealed class DownstreamClient(IHttpClientFactory httpClientFactory, McpCallerContext caller, IConfiguration configuration, ILogger<DownstreamClient> logger)
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public async Task<T> GetAsync<T>(string service, string relativePath, CancellationToken cancellationToken)
	{
		var token = await caller.GetUserTokenAsync()
			?? throw new McpException("You need to be signed in to use this tool.");

		var maxBytes = configuration.GetValue("Mcp:MaxResultBytes", 32768);
		var client = httpClientFactory.CreateClient(service);
		using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		try
		{
			using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			switch (response.StatusCode)
			{
				case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
					throw new McpException("You are not allowed to access this data.");
				case HttpStatusCode.NotFound:
					throw new McpException("The requested item was not found.");
			}
			if (!response.IsSuccessStatusCode)
			{
				logger.LogWarning("Downstream {Service} returned {Status}", service, (int)response.StatusCode);
				throw new McpException("The service is unavailable right now. Try again later.");
			}
			if (response.Content.Headers.ContentLength > maxBytes)
			{
				throw new McpException("The result was too large to return.");
			}

			var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
			if (body.Length > maxBytes)
			{
				throw new McpException("The result was too large to return.");
			}
			return JsonSerializer.Deserialize<T>(body, JsonOptions)
				?? throw new McpException("The service returned an empty response.");
		}
		catch (Exception exception) when (exception is HttpRequestException or JsonException
			|| (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
		{
			logger.LogWarning("Downstream {Service} call failed: {ErrorType}", service, exception.GetType().Name);
			throw new McpException("The service is unavailable right now. Try again later.");
		}
	}
}
