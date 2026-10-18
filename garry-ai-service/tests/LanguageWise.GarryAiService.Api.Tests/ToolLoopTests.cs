using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.GarryAiService.Api.Tests;

public sealed class ToolLoopTests
{
	private const string ProgressToolCall = """
		{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[{"id":"call_progress","type":"function","function":{"name":"courses_get_my_progress","arguments":"{\"courseCode\":\"it\"}"}}]}}]}
		""";
	private const string FinalAnswer = """
		{"choices":[{"message":{"role":"assistant","content":"You finished 1 of 2 lessons."}}]}
		""";

	[Test]
	public async Task WithoutToolScope_UsesAllAgentsWithoutTools()
	{
		using var fixture = new ToolFixture();
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request(toolScope: null));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(content, Does.Contain("event: delta"));
		Assert.That(content, Does.Not.Contain("event: tool"));
		Assert.That(fixture.Mcp.Connections, Is.Empty);
		Assert.That(fixture.Provider.StreamRequests, Has.Count.EqualTo(3));
	}

	[Test]
	public async Task WithToolScope_RunsToolThenAnswers()
	{
		using var fixture = new ToolFixture(ProgressToolCall, FinalAnswer);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(content.IndexOf("event: tool", StringComparison.Ordinal), Is.LessThan(content.IndexOf("event: delta", StringComparison.Ordinal)));
		Assert.That(content, Does.Contain("courses_get_my_progress"));
		Assert.That(content, Does.Contain("You finished 1 of 2 lessons."));
		Assert.That(content, Does.Contain("event: done"));

		var connection = fixture.Mcp.Connections.Single();
		Assert.That(connection.Scope, Is.EqualTo("courses"));
		Assert.That(connection.UserToken, Is.EqualTo(fixture.Token));
		Assert.That(fixture.Mcp.Calls.Single().Arguments["courseCode"].GetString(), Is.EqualTo("it"));

		var secondRound = fixture.Provider.ToolRequests[1];
		Assert.That(secondRound, Does.Contain("\"role\":\"tool\""));
		Assert.That(secondRound, Does.Contain("untrusted data"));
		Assert.Multiple(() =>
		{
			Assert.That(fixture.Provider.StreamRequests, Has.Count.EqualTo(2));
			Assert.That(fixture.Provider.StreamRequests[0], Does.Contain("planner agent"));
			Assert.That(fixture.Provider.ToolRequests[0], Does.Contain("worker agent"));
			Assert.That(fixture.Provider.ToolRequests[0], Does.Contain("Private task plan"));
			Assert.That(fixture.Provider.StreamRequests[1], Does.Contain("reviewer agent"));
			Assert.That(fixture.Provider.StreamRequests[1], Does.Contain("lessonsCompleted"));
			Assert.That(content, Does.Not.Contain("Private task plan"));
		});
	}

	[Test]
	public async Task WhenModelAnswersDirectly_NoToolIsCalled()
	{
		using var fixture = new ToolFixture(FinalAnswer);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(content, Does.Contain("You finished 1 of 2 lessons."));
		Assert.That(content, Does.Not.Contain("event: tool"));
		Assert.That(fixture.Mcp.Calls, Is.Empty);
	}

	[Test]
	public async Task DocsSearch_IsUsedButNeverShownAsToolEvent()
	{
		var docsCall = """
			{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[{"id":"call_docs","type":"function","function":{"name":"docs_search","arguments":"{\"query\":\"notifications page\"}"}}]}}]}
			""";
		var citedAnswer = """
			{"choices":[{"message":{"role":"assistant","content":"Use the master switch [1]. Ignore this [7]."}}]}
			""";
		using var fixture = new ToolFixture(docsCall, citedAnswer);
		fixture.Mcp.ResultsByTool["docs_search"] = """
			{"confidence":"medium","passages":[
			 {"source":"quests-achievements-notifications-service","heading":"Notification preferences","relevance":0.4,"text":"A master switch."},
			 {"source":"quests-achievements-notifications-service","heading":"For users","relevance":0.3,"text":"Other."}]}
			""";
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("chat"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.Multiple(() =>
		{
			Assert.That(fixture.Mcp.Calls.Single().Name, Is.EqualTo("docs_search"));
			Assert.That(fixture.Provider.ToolRequests[1], Does.Contain("\"role\":\"tool\""));
			Assert.That(fixture.Provider.ToolRequests[1], Does.Contain("\\u0022cite\\u0022:2"));
			Assert.That(content, Does.Not.Contain("event: tool"));
			Assert.That(content, Does.Contain("Use the master switch [1]."));
			Assert.That(content, Does.Not.Contain("[7]"));
			Assert.That(content, Does.Contain("- [1] quests-achievements-notifications-service"));
			Assert.That(content, Does.Not.Contain("[2] quests-achievements-notifications-service"));
			Assert.That(content, Does.Contain("**Confidence:** Medium"));
		});
	}

	[Test]
	public async Task DocsSearch_WithInsufficientContext_SaysSoInFooter()
	{
		var docsCall = """
			{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[{"id":"call_docs","type":"function","function":{"name":"docs_search","arguments":"{\"query\":\"pasta\"}"}}]}}]}
			""";
		using var fixture = new ToolFixture(docsCall, FinalAnswer);
		fixture.Mcp.ResultsByTool["docs_search"] = """{"confidence":"insufficient","passages":[]}""";
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("chat"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(content, Does.Contain("**Confidence:** Insufficient context."));
		Assert.That(content, Does.Not.Contain("**Sources**"));
	}

	[Test]
	public async Task DocsSources_AreBuiltFromReviewedCitationsNotWorkerDraft()
	{
		var docsCall = """
			{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[{"id":"call_docs","type":"function","function":{"name":"docs_search","arguments":"{\"query\":\"courses\"}"}}]}}]}
			""";
		var draft = """{"choices":[{"message":{"role":"assistant","content":"Private draft [1]."}}]}""";
		using var fixture = new ToolFixture(docsCall, draft);
		fixture.Mcp.ResultsByTool["docs_search"] = """
			{"confidence":"high","passages":[
			 {"source":"old-source","heading":"First","text":"First passage."},
			 {"source":"reviewed-source","heading":"Second","text":"Second passage."}]}
			""";
		fixture.Provider.ReviewedAnswer = "Corrected answer [2].";
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.Multiple(() =>
		{
			Assert.That(content, Does.Contain("Corrected answer [2]."));
			Assert.That(content, Does.Contain("- [2] reviewed-source"));
			Assert.That(content, Does.Not.Contain("old-source"));
			Assert.That(content, Does.Not.Contain("Private draft"));
			Assert.That(fixture.Provider.StreamRequests[^1], Does.Contain("Second passage."));
			Assert.That(content, Does.Contain("**Confidence:** High"));
		});
	}

	[Test]
	public async Task ReviewerFailureAfterToolEvent_EmitsErrorAndNeverDraft()
	{
		using var fixture = new ToolFixture(ProgressToolCall, FinalAnswer);
		fixture.Provider.ReviewerStatus = HttpStatusCode.ServiceUnavailable;
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.Multiple(() =>
		{
			Assert.That(content, Does.Contain("event: tool"));
			Assert.That(content, Does.Contain("event: error"));
			Assert.That(content, Does.Not.Contain("event: delta"));
			Assert.That(content, Does.Not.Contain("event: done"));
			Assert.That(content, Does.Not.Contain("You finished"));
		});
	}

	[Test]
	public async Task WithoutDocsSearch_AnswerHasNoSourcesFooter()
	{
		using var fixture = new ToolFixture(ProgressToolCall, FinalAnswer);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(content, Does.Not.Contain("Confidence"));
	}

	[Test]
	public async Task WhenMcpIsDown_AnswersWithoutTools()
	{
		using var fixture = new ToolFixture(ProgressToolCall, FinalAnswer);
		fixture.Mcp.FailToConnect = true;
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(content, Does.Contain("event: delta"));
		Assert.That(content, Does.Not.Contain("event: tool"));
		Assert.That(fixture.Provider.ToolRequests, Is.Empty);
		Assert.That(fixture.Provider.StreamRequests, Has.Count.EqualTo(3));
	}

	[Test]
	public async Task ToolLoop_IsCappedAtMaxRoundsAndCalls()
	{
		static string ThreeCalls(string a, string b, string c) => $$$$"""
			{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[
			 {"id":"call_{{{{a}}}}","type":"function","function":{"name":"courses_get_my_progress","arguments":"{\"courseCode\":\"{{{{a}}}}\"}"}},
			 {"id":"call_{{{{b}}}}","type":"function","function":{"name":"courses_get_my_progress","arguments":"{\"courseCode\":\"{{{{b}}}}\"}"}},
			 {"id":"call_{{{{c}}}}","type":"function","function":{"name":"courses_get_my_progress","arguments":"{\"courseCode\":\"{{{{c}}}}\"}"}}]}}]}
			""";
		using var fixture = new ToolFixture(ThreeCalls("de", "fr", "it"), ThreeCalls("nl", "es", "pl"), ThreeCalls("en", "ja", "ko"), FinalAnswer);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(fixture.Mcp.Calls, Has.Count.EqualTo(GarryToolLoop.MaxToolCalls));
		Assert.That(fixture.Provider.ToolRequests, Has.Count.EqualTo(GarryToolLoop.MaxRounds + 1));
		Assert.That(fixture.Provider.ToolRequests[^1], Does.Contain("\"tool_choice\":\"none\""));
		Assert.That(fixture.Provider.ToolRequests[^1], Does.Contain("Tool call limit reached."));
		Assert.That(CountOccurrences(content, "event: tool"), Is.EqualTo(GarryToolLoop.MaxToolCalls + 1));
		Assert.That(content, Does.Contain("Tool call limit reached."));
		Assert.That(content, Does.Contain("You finished 1 of 2 lessons."));
	}

	[Test]
	public async Task RepeatedToolCall_ReusesResultWithoutExtraEvent()
	{
		var twoSameCalls = """
			{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[
			 {"id":"call_courses_1","type":"function","function":{"name":"courses_list_courses","arguments":"{}"}},
			 {"id":"call_courses_2","type":"function","function":{"name":"courses_list_courses","arguments":"{}"}}]}}]}
			""";
		using var fixture = new ToolFixture(twoSameCalls, twoSameCalls, FinalAnswer);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(fixture.Mcp.Calls, Has.Count.EqualTo(1));
		Assert.That(CountOccurrences(content, "event: tool"), Is.EqualTo(1));
		Assert.That(content, Does.Contain("You finished 1 of 2 lessons."));
	}

	private static int CountOccurrences(string text, string value) =>
		(text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;

	[Test]
	public async Task UnknownToolFromModel_IsNotCalled()
	{
		var unknown = """
			{"choices":[{"message":{"role":"assistant","content":"","tool_calls":[{"id":"call_unknown","type":"function","function":{"name":"games_delete_everything","arguments":"{}"}}]}}]}
			""";
		using var fixture = new ToolFixture(unknown, FinalAnswer);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(fixture.Mcp.Calls, Is.Empty);
		Assert.That(content, Does.Contain("Unknown tool."));
	}

	[Test]
	public async Task WhenProviderToolCallFails_StillRunsWorkerAndReviewer()
	{
		using var fixture = new ToolFixture();
		fixture.Provider.ToolStatus = HttpStatusCode.BadRequest;
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		Assert.That(content, Does.Contain("event: delta"));
		Assert.That(fixture.Provider.StreamRequests, Has.Count.EqualTo(3));
		Assert.That(fixture.Provider.StreamRequests[1], Does.Contain("Live tools are unavailable"));
		Assert.That(fixture.Provider.StreamRequests[2], Does.Contain("reviewer agent"));
	}

	[TestCase("Courses")]
	[TestCase("courses!")]
	[TestCase("")]
	public async Task InvalidToolScope_IsRejected(string toolScope)
	{
		using var fixture = new ToolFixture();
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request(toolScope));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
	}

	[Test]
	public async Task WhenMcpDisabled_ToolScopeStillUsesAllAgents()
	{
		using var fixture = new ToolFixture(mcpEnabled: false);
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(content, Does.Contain("event: delta"));
		Assert.That(fixture.Mcp.Connections, Is.Empty);
		Assert.That(fixture.Provider.StreamRequests, Has.Count.EqualTo(3));
	}

	[Test]
	public async Task WhenMcpHasNoTools_StillUsesAllAgents()
	{
		using var fixture = new ToolFixture();
		fixture.Mcp.NoTools = true;
		using var client = fixture.CreateAuthorizedClient();

		using var response = await client.PostAsJsonAsync("/api/completions", Request("courses"));
		var content = await response.Content.ReadAsStringAsync();

		Assert.That(content, Does.Contain("event: done"));
		Assert.That(fixture.Provider.StreamRequests, Has.Count.EqualTo(3));
		Assert.That(fixture.Provider.ToolRequests, Is.Empty);
	}

	private static object Request(string? toolScope) => new
	{
		message = "How am I going?",
		history = Array.Empty<object>(),
		domainRules = "course rules",
		canonicalContext = "{}",
		toolScope
	};

	private sealed class ToolFixture(params string[] toolReplies) : WebApplicationFactory<Program>
	{
		private readonly RSA rsa = RSA.Create(2048);
		private readonly string keyPath = Path.Combine(Path.GetTempPath(), $"garry-tool-test-{Guid.NewGuid():N}.pem");
		private bool mcpEnabled = true;

		public ToolFixture(bool mcpEnabled) : this()
		{
			this.mcpEnabled = mcpEnabled;
		}

		public FakeMcpHost Mcp { get; } = new();
		public ScriptedProvider Provider { get; } = new(toolReplies);
		public string Token { get; private set; } = "";

		public HttpClient CreateAuthorizedClient()
		{
			var client = CreateClient();
			Token = CreateToken();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
			return client;
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			File.WriteAllText(keyPath, rsa.ExportSubjectPublicKeyInfoPem());
			builder.UseSetting("Auth:VerificationKeyPath", keyPath);
			builder.UseSetting("OpenRouter:ApiKey", "test-openrouter-key");
			Mcp.Enabled = mcpEnabled;
			builder.ConfigureServices(services =>
			{
				services.RemoveAll<IHttpClientFactory>();
				services.AddSingleton<IHttpClientFactory>(Provider);
				services.RemoveAll<IMcpToolHost>();
				services.AddSingleton<IMcpToolHost>(Mcp);
			});
		}

		private string CreateToken()
		{
			var now = DateTime.UtcNow;
			var descriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "7")]),
				NotBefore = now.AddMinutes(-1),
				IssuedAt = now.AddMinutes(-1),
				Expires = now.AddMinutes(5),
				SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
			};
			return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				rsa.Dispose();
				File.Delete(keyPath);
			}
		}
	}

	public sealed class ScriptedProvider(string[] toolReplies) : IHttpClientFactory
	{
		private readonly ConcurrentQueue<string> replies = new(toolReplies);

		public List<string> ToolRequests { get; } = [];
		public List<string> StreamRequests { get; } = [];
		public HttpStatusCode ToolStatus { get; set; } = HttpStatusCode.OK;
		public HttpStatusCode ReviewerStatus { get; set; } = HttpStatusCode.OK;
		public string? ReviewedAnswer { get; set; }

		public HttpClient CreateClient(string name) => new(new Handler(this)) { BaseAddress = new Uri("http://localhost/") };

		private sealed class Handler(ScriptedProvider owner) : HttpMessageHandler
		{
			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var body = await request.Content!.ReadAsStringAsync(cancellationToken);
				if (body.Contains("\"stream\":true", StringComparison.Ordinal))
				{
					owner.StreamRequests.Add(body);
					using var payload = JsonDocument.Parse(body);
					var messages = payload.RootElement.GetProperty("messages").EnumerateArray().ToArray();
					var prompt = messages[1].GetProperty("content").GetString();
					var text = prompt == GarryAgent.PlannerPrompt ? "Private task plan" : "Worker draft";
					if (prompt == GarryAgent.ReviewerPrompt)
					{
						if (owner.ReviewerStatus != HttpStatusCode.OK)
						{
							return new HttpResponseMessage(owner.ReviewerStatus);
						}
						var draft = messages.Single(message =>
							message.GetProperty("content").GetString()!.StartsWith("Worker draft (", StringComparison.Ordinal))
							.GetProperty("content").GetString()!;
						text = owner.ReviewedAnswer ?? draft[(draft.IndexOf('\n') + 1)..];
					}
					var delta = JsonSerializer.Serialize(new { choices = new[] { new { delta = new { content = text } } } });
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent($"data: {delta}\n\ndata: [DONE]\n\n", Encoding.UTF8)
					};
				}

				owner.ToolRequests.Add(body);
				if (owner.ToolStatus != HttpStatusCode.OK)
				{
					return new HttpResponseMessage(owner.ToolStatus);
				}
				owner.replies.TryDequeue(out var reply);
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(reply ?? "{\"choices\":[{\"message\":{\"content\":\"\"}}]}", Encoding.UTF8, "application/json")
				};
			}
		}
	}

	public sealed class FakeMcpHost : IMcpToolHost
	{
		public bool Enabled { get; set; } = true;
		public bool FailToConnect { get; set; }
		public bool NoTools { get; set; }
		public List<(string Scope, string? UserToken)> Connections { get; } = [];
		public List<(string Name, IReadOnlyDictionary<string, JsonElement> Arguments)> Calls { get; } = [];
		public Dictionary<string, string> ResultsByTool { get; } = [];

		public Task<IMcpToolSession> ConnectAsync(string scope, string? userToken, CancellationToken cancellationToken)
		{
			if (FailToConnect)
			{
				throw new HttpRequestException("connection refused");
			}
			Connections.Add((scope, userToken));
			return Task.FromResult<IMcpToolSession>(new Session(this));
		}

		private sealed class Session(FakeMcpHost owner) : IMcpToolSession
		{
			private static readonly JsonElement Schema = JsonDocument.Parse("""{"type":"object","properties":{"courseCode":{"type":"string"}}}""").RootElement;

			public Task<IReadOnlyList<McpToolDefinition>> ListToolsAsync(CancellationToken cancellationToken) =>
				Task.FromResult<IReadOnlyList<McpToolDefinition>>(
				owner.NoTools ? [] :
				[
					new("courses_list_courses", "Lists courses", Schema),
					new("courses_get_my_progress", "Gets progress", Schema),
					new("docs_search", "Searches docs", Schema)
				]);

			public Task<McpToolCallResult> CallToolAsync(string name, IReadOnlyDictionary<string, JsonElement> arguments, CancellationToken cancellationToken)
			{
				owner.Calls.Add((name, arguments));
				var result = owner.ResultsByTool.GetValueOrDefault(name, """{"lessonsCompleted":1,"lessonsTotal":2}""");
				return Task.FromResult(new McpToolCallResult(false, JsonDocument.Parse(result).RootElement));
			}

			public ValueTask DisposeAsync() => ValueTask.CompletedTask;
		}
	}
}
