using System.Net;
using System.Security.Cryptography;
using System.Text;
using LanguageWise.McpServer.Security;
using LanguageWise.McpServer.Tools.ChatDiscussion;
using LanguageWise.McpServer.Tools.MiniGames;
using LanguageWise.McpServer.Tools.QuizzesCourses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.Client;

namespace LanguageWise.McpServer.Tests;

public sealed class McpServerFactory : WebApplicationFactory<Program>
{
	public const string ApiKey = "test-api-key-0123456789abcdefghijklmnopqrstuvwxyz";

	private readonly RSA signingKey = RSA.Create(2048);
	private readonly string publicKeyPath = Path.Combine(Path.GetTempPath(), $"lw-mcp-test-{Guid.NewGuid():N}.pem");

	public StubHttpMessageHandler Downstream { get; } = new();

	public McpServerFactory()
	{
		File.WriteAllText(publicKeyPath, signingKey.ExportSubjectPublicKeyInfoPem());
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseSetting("Mcp:ApiKey", ApiKey);
		builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
		builder.UseSetting("Services:QuizzesCourses", "http://quizzes.test");
		builder.UseSetting("Services:MiniGames", "http://mini-games.test");
		builder.UseSetting("Services:ChatDiscussion", "http://chat-discussion.test");
		builder.ConfigureServices(services =>
		{
			services.AddHttpClient(QuizzesCoursesTools.ServiceName)
				.ConfigurePrimaryHttpMessageHandler(() => Downstream);
			services.AddHttpClient(MiniGamesTools.ServiceName)
				.ConfigurePrimaryHttpMessageHandler(() => Downstream);
			services.AddHttpClient(ChatDiscussionTools.ServiceName)
				.ConfigurePrimaryHttpMessageHandler(() => Downstream);
		});
	}

	public string CreateUserToken(TimeSpan? lifetime = null, RSA? key = null)
	{
		var now = DateTime.UtcNow;
		var expires = now + (lifetime ?? TimeSpan.FromMinutes(5));
		return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
		{
			Claims = new Dictionary<string, object> { ["sub"] = "42", ["name"] = "tester" },
			NotBefore = expires < now ? expires.AddMinutes(-5) : now,
			IssuedAt = expires < now ? expires.AddMinutes(-5) : now,
			Expires = expires,
			SigningCredentials = new SigningCredentials(new RsaSecurityKey(key ?? signingKey), SecurityAlgorithms.RsaSha256)
		});
	}

	public async Task<McpClient> CreateMcpClientAsync(string? scope, string? userToken = null, string apiKey = ApiKey)
	{
		var headers = new Dictionary<string, string> { [McpHeaders.ApiKey] = apiKey };
		if (scope is not null)
		{
			headers[McpHeaders.ToolScope] = scope;
		}
		if (userToken is not null)
		{
			headers[McpHeaders.UserToken] = userToken;
		}

		var transport = new HttpClientTransport(
			new HttpClientTransportOptions
			{
				Endpoint = new Uri(Server.BaseAddress, "mcp"),
				TransportMode = HttpTransportMode.StreamableHttp,
				AdditionalHeaders = headers,
				Name = "tests"
			},
			CreateClient(),
			ownsHttpClient: true);
		return await McpClient.CreateAsync(transport);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			signingKey.Dispose();
			File.Delete(publicKeyPath);
		}
	}
}

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
	public List<HttpRequestMessage> Requests { get; } = [];
	public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
	public string Body { get; set; } = "[]";
	public Dictionary<string, string> BodiesByPath { get; } = [];
	public bool Throw { get; set; }

	public void Reset()
	{
		lock (Requests)
		{
			Requests.Clear();
		}
		StatusCode = HttpStatusCode.OK;
		Body = "[]";
		BodiesByPath.Clear();
		Throw = false;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		lock (Requests)
		{
			Requests.Add(request);
		}
		if (Throw)
		{
			throw new HttpRequestException("connection refused");
		}
		var body = BodiesByPath.TryGetValue(request.RequestUri!.PathAndQuery, out var routed) ? routed : Body;
		return Task.FromResult(new HttpResponseMessage(StatusCode)
		{
			Content = new StringContent(body, Encoding.UTF8, "application/json")
		});
	}
}
