using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

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
            Assert.That(response?.Tools.Select(tool => tool.Name), Is.EqualTo(new[]
            {
                "quests_get_my_preferences",
                "quests_set_notifications_enabled",
                "quests_update_my_preferences",
                "quests_get_my_achievements"
            }));
            Assert.That(response?.Tools[1].Title, Is.EqualTo("Turn my notifications on or off"));
            Assert.That(tools.LastUserToken, Is.EqualTo(fixture.Token));
        });
    }

    [Test]
    public async Task ListTools_WithTokenCookie_ForwardsUserToken()
    {
        var tools = new FakeMcpTools();
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Cookie", $"token={fixture.Token}");

        var response = await client.GetAsync("/api/assistant/tools");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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
            "/api/assistant/tools/quests_set_notifications_enabled",
            new { enabled = false });
        var result = await response.Content.ReadFromJsonAsync<AssistantToolCallResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result?.Tool, Is.EqualTo("quests_set_notifications_enabled"));
            Assert.That(result?.IsError, Is.False);
            Assert.That(result?.Result.GetProperty("notifyAll").GetBoolean(), Is.False);
            Assert.That(tools.LastCall, Is.EqualTo("quests_set_notifications_enabled"));
            Assert.That(tools.LastArguments?["enabled"].GetBoolean(), Is.False);
            Assert.That(tools.LastUserToken, Is.EqualTo(fixture.Token));
        });
    }

    [Test]
    public async Task CallTool_WithEmptyBody_SendsNoArguments()
    {
        var tools = new FakeMcpTools();
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/assistant/tools/quests_get_my_preferences", null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(tools.LastArguments, Is.Empty);
        });
    }

    [TestCase("courses_list_courses")]
    [TestCase("docs_search")]
    [TestCase("quests_DROP")]
    [TestCase("quests_")]
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
            "/api/assistant/tools/quests_get_my_preferences",
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
            "/api/assistant/tools/quests_update_my_preferences",
            new { email = new string('a', 4096) });

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
            Failure = new McpProtocolException("Unknown tool: 'quests_missing'", McpErrorCode.InvalidParams)
        };
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/tools/quests_missing", new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CallTool_WhenMcpUnreachable_ReturnsBadGateway()
    {
        var tools = new FakeMcpTools { Failure = new HttpRequestException("connection refused") };
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/tools/quests_get_my_preferences", new { });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
            Assert.That(body, Does.Contain("mcp_unavailable"));
            Assert.That(body, Does.Not.Contain("connection refused"));
        });
    }

    [Test]
    public async Task CallTool_WhenMcpDisabled_ReturnsServiceUnavailable()
    {
        var tools = new FakeMcpTools { Enabled = false };
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/tools/quests_get_my_preferences", new { });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(tools.CallCalls, Is.Zero);
        });
    }

    [Test]
    public async Task PostMessages_RelaysGarryToolEventsBeforeDeltas()
    {
        var toolEvent = new AssistantToolEvent(
            "quests_get_my_preferences",
            JsonSerializer.SerializeToElement(new { }),
            false,
            JsonSerializer.SerializeToElement(new { notifyAll = true }));
        using var fixture = new ToolApiFixture(new FakeMcpTools(), new ScriptedCompletion(
            ProviderStreamEvent.Tool(toolEvent),
            ProviderStreamEvent.Delta("Your notifications are on."),
            ProviderStreamEvent.Done()));
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            new AssistantMessageRequest(
                "Are my notifications on?",
                [],
                new AssistantRouteContext("quests-achievements-home")));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Does.Contain("event: tool"));
            Assert.That(body, Does.Contain("\"name\":\"quests_get_my_preferences\""));
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
                // An empty database falls back to default preferences, which is all the relay test needs.
                services
                    .AddHttpClient<AppDataClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler(HttpStatusCode.OK, "[]"));
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
                    new Claim(JwtRegisteredClaimNames.Name, "amber")
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
                new McpToolInfo("quests_get_my_preferences", "Get my notification preferences", "Gets preferences."),
                new McpToolInfo("quests_set_notifications_enabled", "Turn my notifications on or off", "Flips the master switch."),
                new McpToolInfo("quests_update_my_preferences", "Update my notification preferences", "Updates categories."),
                new McpToolInfo("quests_get_my_achievements", "Get my achievement progress", "Gets achievements.")
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
                JsonSerializer.SerializeToElement(new { notifyAll = false, notifyAchievements = true })));
        }
    }

    private sealed class ScriptedCompletion(params ProviderStreamEvent[] events) : IAssistantCompletionClient
    {
        public Task<IAssistantEventStream> StartCompletionAsync(
            IReadOnlyList<AssistantChatMessage> messages,
            CancellationToken cancellationToken) =>
            Task.FromResult<IAssistantEventStream>(new EventStream(events));

        private sealed class EventStream(ProviderStreamEvent[] events) : IAssistantEventStream
        {
            public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                foreach (var streamEvent in events)
                {
                    await Task.Yield();
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return streamEvent;
                }
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
