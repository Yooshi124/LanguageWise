using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

public sealed class AuthorizationTests
{
    [Test]
    public async Task Health_AllowsAnonymousRequests()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var endpoints = root.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("healthy"));
            Assert.That(
                root.GetProperty("dependencies").GetProperty("database").GetProperty("status").GetString(),
                Is.EqualTo("healthy"));
            Assert.That(endpoints, Has.Some.Matches<JsonElement>(endpoint =>
                endpoint.GetProperty("method").GetString() == "GET" &&
                endpoint.GetProperty("route").GetString() == "/health" &&
                endpoint.GetProperty("status").GetString() == "registered" &&
                !endpoint.GetProperty("authRequired").GetBoolean()));
            Assert.That(endpoints, Has.Some.Matches<JsonElement>(endpoint =>
                endpoint.GetProperty("method").GetString() == "POST" &&
                endpoint.GetProperty("route").GetString() == "/api/quizzes/{quizId:int}/attempts" &&
                endpoint.GetProperty("status").GetString() == "registered" &&
                endpoint.GetProperty("authRequired").GetBoolean()));
            Assert.That(endpoints, Has.Some.Matches<JsonElement>(endpoint =>
                endpoint.GetProperty("method").GetString() == "POST" &&
                endpoint.GetProperty("route").GetString() == "/api/assistant/messages" &&
                endpoint.GetProperty("status").GetString() == "registered" &&
                endpoint.GetProperty("authRequired").GetBoolean()));
        });
    }

    [Test]
    public async Task Health_WhenDatabaseIsUnhealthy_DoesNotLeakSqliteDiagnostics()
    {
        const string rawDiagnostic =
            "SQLite Error 1: 'no such table: Courses'. Data Source=/srv/private/catalog.db";
        const string databaseHealth =
            $$"""
            {
              "status": "unhealthy",
              "service": "quizzes-courses-service-db",
              "error": "{{rawDiagnostic}}"
            }
            """;
        using var fixture = new ApiFixture(HttpStatusCode.ServiceUnavailable, databaseHealth);
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");
        var responseBody = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseBody);
        var root = body.RootElement;
        var dependency = root.GetProperty("dependencies").GetProperty("database");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(root.GetProperty("status").GetString(), Is.EqualTo("unhealthy"));
            Assert.That(dependency.GetProperty("status").GetString(), Is.EqualTo("unhealthy"));
            Assert.That(dependency.GetProperty("type").GetString(), Is.EqualTo("http"));
            Assert.That(dependency.GetProperty("httpStatus").GetInt32(), Is.EqualTo(503));
            Assert.That(
                dependency.GetProperty("error").GetString(),
                Is.EqualTo("The database service is unhealthy."));
            Assert.That(responseBody, Does.Not.Contain(rawDiagnostic));
            Assert.That(responseBody, Does.Not.Contain("/srv/private/catalog.db"));
            Assert.That(root.GetProperty("endpoints").GetArrayLength(), Is.GreaterThan(1));
        });
    }

    [Test]
    public async Task Health_WhenDownstreamReturnsRawFailure_DoesNotRepublishResponseBody()
    {
        const string rawDownstreamBody = "proxy failure: internal-host=10.0.0.12; token=diagnostic-value";
        using var fixture = new ApiFixture(HttpStatusCode.BadGateway, rawDownstreamBody);
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");
        var responseBody = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(responseBody);
        var dependency = body.RootElement.GetProperty("dependencies").GetProperty("database");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(dependency.GetProperty("status").GetString(), Is.EqualTo("unhealthy"));
            Assert.That(dependency.GetProperty("type").GetString(), Is.EqualTo("http"));
            Assert.That(dependency.GetProperty("httpStatus").GetInt32(), Is.EqualTo(502));
            Assert.That(
                dependency.GetProperty("error").GetString(),
                Is.EqualTo("The database service is unhealthy."));
            Assert.That(responseBody, Does.Not.Contain(rawDownstreamBody));
            Assert.That(responseBody, Does.Not.Contain("diagnostic-value"));
        });
    }

    [Test]
    public async Task Courses_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/courses");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task LegacyMe_WithBearerToken_ReturnsNotFound()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.GetAsync("/api/me");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private sealed class ApiFixture : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"health-test-key-{Guid.NewGuid():N}.pem");
        private readonly HttpStatusCode databaseStatusCode;
        private readonly string databaseResponseBody;

        internal ApiFixture(
            HttpStatusCode databaseStatusCode = HttpStatusCode.OK,
            string databaseResponseBody =
                """{"status":"healthy","service":"quizzes-courses-service-db","courses":6}""")
        {
            this.databaseStatusCode = databaseStatusCode;
            this.databaseResponseBody = databaseResponseBody;
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        }

        internal string CreateToken(DateTime? expires = null)
        {
            var expiresAt = expires ?? DateTime.UtcNow.AddMinutes(5);
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, "7"),
                    new Claim(JwtRegisteredClaimNames.Name, "justin")
                ]),
                NotBefore = expiresAt.AddMinutes(-5),
                IssuedAt = expiresAt.AddMinutes(-5),
                Expires = expiresAt,
                SigningCredentials =
                    new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:VerificationKeyPath"] = publicKeyPath
                });
            });
            builder.ConfigureServices(services =>
            {
                services
                    .AddHttpClient<CatalogClient>()
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new StubHttpMessageHandler(databaseStatusCode, databaseResponseBody));
            });
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
}
