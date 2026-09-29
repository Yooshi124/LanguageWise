using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

[TestFixture]
public sealed class AssistantToolEndpointTests
{
	[Test]
	public async Task ListTools_WithoutAuthentication_ReturnsUnauthorized()
	{
		using var fixture = new ToolApiFixture(new FakeMcpTools());
		using var client = fixture.CreateClient();

		var response = await client.GetAsync("/api/assistant/tools");

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
	}

	[Test]
	public async Task ListTools_WhenMcpDisabled_ReturnsServiceUnavailable()
	{
		var tools = new FakeMcpTools { Enabled = false };
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.GetAsync("/api/assistant/tools");
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
			Assert.That(body, Does.Contain("mcp_disabled"));
			Assert.That(tools.ListCalls, Is.Zero);
		});
	}

	[Test]
	public async Task ListTools_ReturnsScopedToolsAndForwardsUserToken()
	{
		var tools = new FakeMcpTools();
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.GetFromJsonAsync<AssistantToolsResponse>("/api/assistant/tools");

		Assert.Multiple(() =>
		{
			Assert.That(response?.Tools.Select(tool => tool.Name), Is.EqualTo(new[] { "courses_list_courses", "courses_get_my_progress" }));
			Assert.That(response?.Tools[1].Title, Is.EqualTo("Get my course progress"));
			Assert.That(tools.LastUserToken, Is.EqualTo(fixture.Token));
		});
	}

	[Test]
	public async Task ListTools_WhenMcpUnreachable_ReturnsBadGateway()
	{
		var tools = new FakeMcpTools { Failure = new HttpRequestException("connection refused") };
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.GetAsync("/api/assistant/tools");
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
			Assert.That(body, Does.Contain("mcp_unavailable"));
			Assert.That(body, Does.Not.Contain("connection refused"));
		});
	}

	[Test]
	public async Task CallTool_ReturnsToolResultAndForwardsArguments()
	{
		var tools = new FakeMcpTools();
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync(
			"/api/assistant/tools/courses_get_my_progress",
			new { courseCode = "it" });
		var result = await response.Content.ReadFromJsonAsync<AssistantToolCallResponse>();

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(result?.Tool, Is.EqualTo("courses_get_my_progress"));
			Assert.That(result?.IsError, Is.False);
			Assert.That(result?.Result.GetProperty("lessonsCompleted").GetInt32(), Is.EqualTo(2));
			Assert.That(tools.LastCall, Is.EqualTo("courses_get_my_progress"));
			Assert.That(tools.LastArguments?["courseCode"].GetString(), Is.EqualTo("it"));
			Assert.That(tools.LastUserToken, Is.EqualTo(fixture.Token));
		});
	}

	[Test]
	public async Task CallTool_WithEmptyBody_SendsNoArguments()
	{
		var tools = new FakeMcpTools();
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsync("/api/assistant/tools/courses_list_courses", null);

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(tools.LastArguments, Is.Empty);
		});
	}

	[TestCase("games_list_games")]
	[TestCase("courses_DROP")]
	[TestCase("courses_")]
	public async Task CallTool_WithToolOutsideScope_ReturnsValidationProblemWithoutCallingMcp(string name)
	{
		var tools = new FakeMcpTools();
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync($"/api/assistant/tools/{name}", new { });

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			Assert.That(tools.CallCalls, Is.Zero);
		});
	}

	[TestCase("[1,2]")]
	[TestCase("not json")]
	public async Task CallTool_WithNonObjectArguments_ReturnsValidationProblem(string body)
	{
		var tools = new FakeMcpTools();
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsync(
			"/api/assistant/tools/courses_list_courses",
			new StringContent(body, Encoding.UTF8, "application/json"));

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			Assert.That(tools.CallCalls, Is.Zero);
		});
	}

	[Test]
	public async Task CallTool_WithOversizedArguments_ReturnsValidationProblem()
	{
		var tools = new FakeMcpTools();
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync(
			"/api/assistant/tools/courses_get_my_progress",
			new { courseCode = new string('a', 4096) });

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			Assert.That(tools.CallCalls, Is.Zero);
		});
	}

	[Test]
	public async Task CallTool_WhenMcpRejectsTool_ReturnsValidationProblem()
	{
		var tools = new FakeMcpTools
		{
			Failure = new McpProtocolException("Unknown tool: 'courses_missing'", McpErrorCode.InvalidParams)
		};
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync("/api/assistant/tools/courses_missing", new { });

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
	}

	[Test]
	public async Task CallTool_WhenMcpUnreachable_ReturnsBadGateway()
	{
		var tools = new FakeMcpTools { Failure = new HttpRequestException("connection refused") };
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync("/api/assistant/tools/courses_list_courses", new { });

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
	}

	[Test]
	public async Task CallTool_WhenMcpDisabled_ReturnsServiceUnavailable()
	{
		var tools = new FakeMcpTools { Enabled = false };
		using var fixture = new ToolApiFixture(tools);
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync("/api/assistant/tools/courses_list_courses", new { });

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
			Assert.That(tools.CallCalls, Is.Zero);
		});
	}

	[Test]
	public async Task PostMessages_RelaysGarryToolEventsBeforeDeltas()
	{
		using var fixture = new ToolApiFixture(new FakeMcpTools(), new GarryStreamCompletion(
			"event: tool\ndata: {\"name\":\"courses_list_courses\",\"arguments\":{},\"isError\":false,\"result\":{\"courses\":[]}}\n\n" +
			"event: delta\ndata: {\"content\":\"Here are the courses.\"}\n\n" +
			"event: done\ndata: {\"reason\":\"stop\"}\n\n"));
		using var client = fixture.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync(
			"/api/assistant/messages",
			new AssistantMessageRequest("Which courses exist?", [], new AssistantRouteContext("home", null, null)));
		var body = await response.Content.ReadAsStringAsync();

		Assert.Multiple(() =>
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(body, Does.Contain("event: tool"));
			Assert.That(body, Does.Contain("\"name\":\"courses_list_courses\""));
			Assert.That(body.IndexOf("event: tool", StringComparison.Ordinal),
				Is.LessThan(body.IndexOf("event: delta", StringComparison.Ordinal)));
			Assert.That(body, Does.Contain("event: done"));
		});
	}

	private sealed class ToolApiFixture(IMcpToolClient tools, IAssistantCompletionClient? completion = null)
		: WebApplicationFactory<Program>
	{
		private readonly RSA rsa = RSA.Create(2048);
		private readonly string publicKeyPath = Path.Combine(
			AppContext.BaseDirectory,
			$"assistant-tool-test-key-{Guid.NewGuid():N}.pem");
		private string? token;

		internal string Token => token ??= CreateToken();

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
			builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
			builder.ConfigureServices(services =>
			{
				services
					.AddHttpClient<CatalogClient>()
					.ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler((request, _) =>
						Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
						{
							Content = new StringContent(
								"""[{"id":1,"code":"it","title":"Italian","description":"Italian course"}]""",
								Encoding.UTF8,
								"application/json"),
							RequestMessage = request
						})));
				services.RemoveAll<IMcpToolClient>();
				services.AddSingleton(tools);
				if (completion is not null)
				{
					services.RemoveAll<IAssistantCompletionClient>();
					services.AddSingleton(completion);
				}
			});
		}

		internal HttpClient CreateAuthenticatedClient()
		{
			var client = CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
			return client;
		}

		private string CreateToken()
		{
			var now = DateTime.UtcNow;
			var descriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity([
					new Claim(JwtRegisteredClaimNames.Sub, "7"),
					new Claim(JwtRegisteredClaimNames.Name, "justin")
				]),
				NotBefore = now.AddMinutes(-1),
				IssuedAt = now.AddMinutes(-1),
				Expires = now.AddMinutes(5),
				SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
			};
			var handler = new JwtSecurityTokenHandler();
			return handler.WriteToken(handler.CreateToken(descriptor));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				rsa.Dispose();
				File.Delete(publicKeyPath);
			}
		}
	}

	private sealed class FakeMcpTools : IMcpToolClient
	{
		public bool Enabled { get; init; } = true;
		public Exception? Failure { get; init; }
		public int ListCalls { get; private set; }
		public int CallCalls { get; private set; }
		public string? LastCall { get; private set; }
		public IReadOnlyDictionary<string, JsonElement>? LastArguments { get; private set; }
		public string? LastUserToken { get; private set; }

		public Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(string? userToken, CancellationToken cancellationToken)
		{
			ListCalls++;
			LastUserToken = userToken;
			if (Failure is not null)
			{
				throw Failure;
			}
			return Task.FromResult<IReadOnlyList<McpToolInfo>>(
			[
				new McpToolInfo("courses_list_courses", "List courses", "Lists every course."),
				new McpToolInfo("courses_get_my_progress", "Get my course progress", "Gets progress.")
			]);
		}

		public Task<McpToolCallResult> CallToolAsync(
			string name,
			IReadOnlyDictionary<string, JsonElement> arguments,
			string? userToken,
			CancellationToken cancellationToken)
		{
			CallCalls++;
			LastCall = name;
			LastArguments = arguments;
			LastUserToken = userToken;
			if (Failure is not null)
			{
				throw Failure;
			}
			return Task.FromResult(new McpToolCallResult(
				false,
				JsonSerializer.SerializeToElement(new { courseCode = "it", lessonsCompleted = 2 })));
		}
	}

	private sealed class GarryStreamCompletion(string sse) : IAssistantCompletionClient
	{
		public Task<AssistantCompletionStream> StartCompletionAsync(
			IReadOnlyList<OpenRouterChatMessage> messages,
			CancellationToken cancellationToken)
		{
			var stream = new MemoryStream(Encoding.UTF8.GetBytes(sse));
			var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };
			return Task.FromResult(new AssistantCompletionStream(response, stream, true));
		}
	}
}
