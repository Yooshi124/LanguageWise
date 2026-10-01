using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LanguageWise.GarryAiService.Api;

public sealed record McpToolDefinition(string Name, string Description, JsonElement InputSchema);

public sealed record McpToolCallResult(bool IsError, JsonElement Result);

public interface IMcpToolSession : IAsyncDisposable
{
	Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken);
	Task<McpToolCallResult> CallToolAsync(string name, IReadOnlyDictionary<string, JsonElement> arguments, CancellationToken cancellationToken);
}

public interface IMcpToolHost
{
	bool Enabled { get; }
	Task<IMcpToolSession> ConnectAsync(string scope, string? userToken, CancellationToken cancellationToken);
}

public sealed class McpToolHost : IMcpToolHost
{
	public const string HttpClientName = "mcp";

	private readonly IHttpClientFactory httpClientFactory;
	private readonly ILoggerFactory loggerFactory;
	private readonly Uri? endpoint;
	private readonly string? apiKey;

	public McpToolHost(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
	{
		this.httpClientFactory = httpClientFactory;
		this.loggerFactory = loggerFactory;
		if (!configuration.GetValue("Mcp:Enabled", false))
		{
			return;
		}

		var logger = loggerFactory.CreateLogger<McpToolHost>();
		apiKey = LoadApiKey(configuration);
		if (apiKey is null)
		{
			logger.LogWarning("MCP is enabled but no API key was found; Garry will answer without tools");
			return;
		}
		endpoint = new Uri(configuration["Mcp:Endpoint"] ?? "http://localhost:8200/mcp");
	}

	public bool Enabled => endpoint is not null;

	public async Task<IMcpToolSession> ConnectAsync(string scope, string? userToken, CancellationToken cancellationToken)
	{
		if (!Enabled)
		{
			throw new InvalidOperationException("MCP is disabled.");
		}

		var headers = new Dictionary<string, string>
		{
			["X-LanguageWise-Mcp-Key"] = apiKey!,
			["X-LanguageWise-Tool-Scope"] = scope
		};
		if (!string.IsNullOrWhiteSpace(userToken))
		{
			headers["X-LanguageWise-User-Token"] = userToken;
		}

		var transport = new HttpClientTransport(
			new HttpClientTransportOptions
			{
				Endpoint = endpoint!,
				TransportMode = HttpTransportMode.StreamableHttp,
				AdditionalHeaders = headers,
				Name = "garry"
			},
			httpClientFactory.CreateClient(HttpClientName),
			loggerFactory,
			ownsHttpClient: true);
		var client = await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: cancellationToken);
		return new Session(client);
	}

	private static string? LoadApiKey(IConfiguration configuration)
	{
		var key = configuration["Mcp:ApiKey"];
		if (string.IsNullOrWhiteSpace(key))
		{
			var path = configuration["Mcp:ApiKeyPath"] ?? "/run/secrets/mcp_api_key";
			key = File.Exists(path) ? File.ReadAllText(path) : null;
		}
		return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
	}

	private sealed class Session(McpClient client) : IMcpToolSession
	{
		public async Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken)
		{
			var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
			return tools.Select(tool => new McpToolDefinition(tool.Name, tool.Description ?? "", tool.JsonSchema)).ToList();
		}

		public async Task<McpToolCallResult> CallToolAsync(
			string name, IReadOnlyDictionary<string, JsonElement> arguments, CancellationToken cancellationToken)
		{
			var result = await client.CallToolAsync(
				name,
				arguments.ToDictionary(pair => pair.Key, pair => (object?)pair.Value),
				cancellationToken: cancellationToken);
			var payload = result.StructuredContent is { } structured
				? structured
				: JsonSerializer.SerializeToElement(new
				{
					text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text))
				});
			return new McpToolCallResult(result.IsError == true, payload);
		}

		public ValueTask DisposeAsync() => client.DisposeAsync();
	}
}
