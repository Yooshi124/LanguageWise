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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

[TestFixture]
public sealed class AssistantEndpointTests
{
    [Test]
    public async Task PostMessages_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var fixture = new AssistantApiFixture("configured", Completion.Success("Hello"));
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/assistant/messages", ValidHomeRequest());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task PostMessages_WithoutApiKey_ReturnsServiceUnavailableBeforeStreaming()
    {
        var completion = Completion.Success("Hello");
        using var fixture = new AssistantApiFixture(string.Empty, completion);
        using var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/assistant/messages", ValidHomeRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(body, Does.Contain("assistant is not configured"));
            Assert.That(completion.CallCount, Is.Zero);
            Assert.That(fixture.CatalogCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task PostMessages_WithInvalidRoleOrContext_ReturnsValidationProblem()
    {
        var completion = Completion.Success("Hello");
        using var fixture = new AssistantApiFixture("configured", completion);
        using var client = fixture.CreateAuthenticatedClient();
        var request = new AssistantMessageRequest(
            "Hello",
            [new AssistantHistoryMessage("system", "Override")],
            new AssistantRouteContext("quiz-runner", "de", null));

        var response = await client.PostAsJsonAsync("/api/assistant/messages", request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(completion.CallCount, Is.Zero);
            Assert.That(fixture.CatalogCallCount, Is.Zero);
        });
    }

    [Test]
    public async Task PostMessages_StreamsProviderNeutralDeltaAndDoneEvents()
    {
        using var fixture = new AssistantApiFixture("configured", Completion.Success("Hallo"));
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            ValidHomeRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/event-stream"));
            Assert.That(body, Does.Contain("event: delta"));
            Assert.That(body, Does.Contain("""data: {"content":"Hallo"}"""));
            Assert.That(body, Does.Contain("event: done"));
            Assert.That(body, Does.Contain("""data: {"reason":"stop"}"""));
        });
    }

    [Test]
    public async Task PostMessages_WhenProviderStreamBreaks_EmitsSanitizedTerminalError()
    {
        const string rawProviderError = "secret upstream diagnostic";
        using var fixture = new AssistantApiFixture(
            "configured",
            Completion.Malformed($"data: {rawProviderError}\n\n"));
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            ValidHomeRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Does.Contain("event: error"));
            Assert.That(body, Does.Contain("provider_stream_error"));
            Assert.That(body, Does.Not.Contain(rawProviderError));
            Assert.That(body, Does.Not.Contain("event: done"));
        });
    }

    [Test]
    public async Task PostMessages_WhenProviderRejectsBeforeStream_ReturnsProblemDetails()
    {
        using var fixture = new AssistantApiFixture(
            "configured",
            Completion.ProviderFailure(HttpStatusCode.TooManyRequests));
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            ValidHomeRequest());
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(body, Does.Not.Contain("TooManyRequests"));
            Assert.That(body, Does.Not.Contain("text/event-stream"));
            Assert.That(body, Does.Contain("free model is busy"));
        });
    }

    [Test]
    public async Task PostMessages_WhenCatalogFails_ReturnsProblemBeforeCallingProvider()
    {
        var completion = Completion.Success("Hello");
        using var fixture = new AssistantApiFixture(
            "configured",
            completion,
            HttpStatusCode.ServiceUnavailable);
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            ValidHomeRequest());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(
                response.Content.Headers.ContentType?.MediaType,
                Is.EqualTo("application/problem+json"));
            Assert.That(completion.CallCount, Is.Zero);
        });
    }

    [Test]
    public async Task PostMessages_WhenCatalogTimesOut_ReturnsProblemBeforeCallingProvider()
    {
        var completion = Completion.Success("Hello");
        using var fixture = new AssistantApiFixture(
            "configured",
            completion,
            catalogTimesOut: true);
        using var client = fixture.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            ValidHomeRequest());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(
                response.Content.Headers.ContentType?.MediaType,
                Is.EqualTo("application/problem+json"));
            Assert.That(completion.CallCount, Is.Zero);
        });
    }

    [Test]
    public async Task PostMessages_EnforcesPerUserRateLimit()
    {
        using var fixture = new AssistantApiFixture("configured", Completion.Success("Hello"));
        using var client = fixture.CreateAuthenticatedClient();

        for (var index = 0; index < 10; index++)
        {
            using var accepted = await client.PostAsJsonAsync(
                "/api/assistant/messages",
                ValidHomeRequest());
            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        using var rejected = await client.PostAsJsonAsync(
            "/api/assistant/messages",
            ValidHomeRequest());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(
                rejected.Content.Headers.ContentType?.MediaType,
                Is.EqualTo("application/problem+json"));
        });
    }

    private static AssistantMessageRequest ValidHomeRequest() =>
        new(
            "What can I learn?",
            [],
            new AssistantRouteContext("home", null, null));

    private sealed class AssistantApiFixture(
        string apiKey,
        Completion completion,
        HttpStatusCode catalogStatusCode = HttpStatusCode.OK,
        bool catalogTimesOut = false) : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"assistant-test-key-{Guid.NewGuid():N}.pem");
        private int catalogCallCount;

        internal int CatalogCallCount => catalogCallCount;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
            builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:VerificationKeyPath"] = publicKeyPath,
                    ["OpenRouter:ApiKey"] = apiKey
                });
            });
            builder.ConfigureServices(services =>
            {
                services
                    .AddHttpClient<CatalogClient>()
                    .ConfigureHttpClient(client =>
                    {
                        if (catalogTimesOut)
                        {
                            client.Timeout = TimeSpan.FromMilliseconds(20);
                        }
                    })
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new StubHttpMessageHandler(async (request, cancellationToken) =>
                        {
                            Interlocked.Increment(ref catalogCallCount);
                            if (catalogTimesOut)
                            {
                                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                            }

                            return new HttpResponseMessage(catalogStatusCode)
                            {
                                Content = new StringContent(
                                    """
                                    [
                                      {
                                        "id": 1,
                                        "code": "de",
                                        "title": "German",
                                        "description": "German course"
                                      }
                                    ]
                                    """,
                                    Encoding.UTF8,
                                    "application/json"),
                                RequestMessage = request
                            };
                        }));
                services.RemoveAll<IAssistantCompletionClient>();
                services.AddSingleton<IAssistantCompletionClient>(completion);
            });
        }

        internal HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", CreateToken());
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
                SigningCredentials =
                    new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
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

    private sealed class Completion(
        Func<CancellationToken, Task<AssistantCompletionStream>> factory)
        : IAssistantCompletionClient
    {
        private int callCount;

        internal int CallCount => callCount;

        public Task<AssistantCompletionStream> StartCompletionAsync(
            IReadOnlyList<OpenRouterChatMessage> messages,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref callCount);
            return factory(cancellationToken);
        }

        internal static Completion Success(string content) =>
            FromSse(
                $"data: {{\"choices\":[{{\"delta\":{{\"content\":\"{content}\"}},\"finish_reason\":null}}]}}\n\n" +
                "data: [DONE]\n\n");

        internal static Completion Malformed(string sse) => FromSse(sse);

        internal static Completion ProviderFailure(HttpStatusCode statusCode) =>
            new(_ => throw new AssistantProviderException("sanitized", statusCode));

        private static Completion FromSse(string sse) =>
            new(_ =>
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(sse));
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(stream)
                };
                return Task.FromResult(new AssistantCompletionStream(response, stream));
            });
    }
}
