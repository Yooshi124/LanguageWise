using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LanguageWise.QuizzesCoursesService.Api.Clients;

public sealed record McpToolInfo(string Name, string Title, string Description);

public sealed record McpToolCallResult(bool IsError, JsonElement Result);

public interface IMcpToolClient
{
	bool Enabled { get; }
	Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(string? userToken, CancellationToken cancellationToken);
	Task<McpToolCallResult> CallToolAsync(
		string name,
		IReadOnlyDictionary<string, JsonElement> arguments,
		string? userToken,
		CancellationToken cancellationToken);
}

public sealed class McpToolClient : IMcpToolClient
{
	public const string HttpClientName = "mcp";
	public const string Scope = "courses";

	private readonly IHttpClientFactory httpClientFactory;
	private readonly ILoggerFactory loggerFactory;
	private readonly Uri? endpoint;
	private readonly string? apiKey;

	public McpToolClient(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
	{
		this.httpClientFactory = httpClientFactory;
		this.loggerFactory = loggerFactory;
		if (!configuration.GetValue("Mcp:Enabled", false))
		{
			return;
		}

		apiKey = LoadApiKey(configuration);
		if (apiKey is null)
		{
			loggerFactory.CreateLogger<McpToolClient>()
				.LogWarning("MCP is enabled but no API key was found; assistant tools are disabled");
			return;
		}
		endpoint = new Uri(configuration["Mcp:Endpoint"] ?? "http://localhost:8200/mcp");
	}

	public bool Enabled => endpoint is not null;

	public async Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(string? userToken, CancellationToken cancellationToken)
	{
		await using var client = await ConnectAsync(userToken, cancellationToken);
		var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
		return tools
			.Select(tool => new McpToolInfo(tool.Name, tool.Title ?? tool.Name, tool.Description ?? ""))
			.ToList();
	}

	public async Task<McpToolCallResult> CallToolAsync(
		string name,
		IReadOnlyDictionary<string, JsonElement> arguments,
		string? userToken,
		CancellationToken cancellationToken)
	{
		await using var client = await ConnectAsync(userToken, cancellationToken);
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

	private async Task<McpClient> ConnectAsync(string? userToken, CancellationToken cancellationToken)
	{
		if (!Enabled)
		{
			throw new InvalidOperationException("MCP is disabled.");
		}

		var headers = new Dictionary<string, string>
		{
			["X-LanguageWise-Mcp-Key"] = apiKey!,
			["X-LanguageWise-Tool-Scope"] = Scope
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
				Name = "quizzes-courses-service"
			},
			httpClientFactory.CreateClient(HttpClientName),
			loggerFactory,
			ownsHttpClient: true);
		return await McpClient.CreateAsync(transport, loggerFactory: loggerFactory, cancellationToken: cancellationToken);
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
}
