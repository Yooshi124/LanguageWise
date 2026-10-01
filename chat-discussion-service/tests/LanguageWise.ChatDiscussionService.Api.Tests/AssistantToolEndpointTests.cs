using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

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
            Assert.That(response?.Tools.Select(tool => tool.Name), Is.EqualTo(new[] { "chat_list_forums", "chat_search_posts" }));
            Assert.That(response?.Tools[0].Title, Is.EqualTo("List discussion forums"));
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
            "/api/assistant/tools/chat_search_posts",
            new { query = "greetings" });
        var result = await response.Content.ReadFromJsonAsync<AssistantToolCallResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result?.Tool, Is.EqualTo("chat_search_posts"));
            Assert.That(result?.IsError, Is.False);
            Assert.That(result?.Result.GetProperty("posts")[0].GetProperty("postId").GetInt32(), Is.EqualTo(7));
            Assert.That(tools.LastCall, Is.EqualTo("chat_search_posts"));
            Assert.That(tools.LastArguments?["query"].GetString(), Is.EqualTo("greetings"));
            Assert.That(tools.LastUserToken, Is.EqualTo(fixture.Token));
        });
    }

    [Test]
    public async Task CallTool_WithEmptyBody_SendsNoArguments()
    {
        var tools = new FakeMcpTools();
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/assistant/tools/chat_list_forums", null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(tools.LastArguments, Is.Empty);
        });
    }

    [TestCase("games_get_completion_stats")]
    [TestCase("chat_DROP")]
    [TestCase("chat_")]
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
            "/api/assistant/tools/chat_list_forums",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(tools.CallCalls, Is.Zero);
        });
    }

    [Test]
    public async Task CallTool_WhenMcpUnreachable_ReturnsBadGateway()
    {
        var tools = new FakeMcpTools { Failure = new HttpRequestException("connection refused") };
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/tools/chat_list_forums", new { });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
    }

    [Test]
    public async Task CallTool_WhenMcpDisabled_ReturnsServiceUnavailable()
    {
        var tools = new FakeMcpTools { Enabled = false };
        using var fixture = new ToolApiFixture(tools);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/tools/chat_list_forums", new { });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(tools.CallCalls, Is.Zero);
        });
    }

    private sealed class ToolApiFixture(IMcpToolClient tools) : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.GetTempFileName();
        private string? token;

        internal string Token => token ??= CreateToken();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
            builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMcpToolClient>();
                services.AddSingleton(tools);
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
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, FakeDiscussionDatabase.SignedInUserId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, "lachlan")
                ]),
                Expires = DateTime.UtcNow.AddMinutes(5),
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
                new McpToolInfo("chat_list_forums", "List discussion forums", "Lists forums."),
                new McpToolInfo("chat_search_posts", "Search discussion posts", "Searches posts.")
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
                JsonSerializer.SerializeToElement(new { posts = new[] { new { postId = 7, title = "Italian greetings" } } })));
        }
    }
}
