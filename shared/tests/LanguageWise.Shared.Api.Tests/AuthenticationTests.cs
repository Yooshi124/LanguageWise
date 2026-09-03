using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LanguageWise.Shared.Api.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.Shared.Api.Tests;

public sealed class AuthenticationTests
{
    [Test]
    public async Task CheckLogin_WithValidCookie_ReturnsAuthenticatedUser()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsync("/api/check-login", null);
        var user = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(user, Is.EqualTo(new AuthenticatedUserResponse(7, "justin")));
        });
    }

    [Test]
    public async Task CheckLogin_WithoutCookie_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/check-login", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CheckLogin_WithInvalidSignature_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var otherKey = RSA.Create(2048);
        using var client = fixture.CreateCookieClient(fixture.CreateToken(signingKey: otherKey));

        var response = await client.PostAsync("/api/check-login", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CheckLogin_WithExpiredToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(
            fixture.CreateToken(expires: DateTime.UtcNow.AddMinutes(-1)));

        var response = await client.PostAsync("/api/check-login", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CheckLogin_WithMissingSubject_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken(subject: null));

        var response = await client.PostAsync("/api/check-login", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase("not-a-number")]
    [TestCase("0")]
    [TestCase("-1")]
    public async Task CheckLogin_WithInvalidSubject_ReturnsUnauthorized(string subject)
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken(subject: subject));

        var response = await client.PostAsync("/api/check-login", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task CheckLogin_WithInvalidName_ReturnsUnauthorized(string? name)
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken(name: name));

        var response = await client.PostAsync("/api/check-login", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CheckLogin_WithTokenOnlyInRequestBody_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/check-login",
            new { token = fixture.CreateToken() });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CheckLogin_OnNewDay_ForwardsAbsoluteLoginStreak()
    {
        using var fixture = new ApiFixture(streakValue: 4);
        var token = fixture.CreateToken();
        using var client = fixture.CreateCookieClient(token);

        var response = await client.PostAsync("/api/check-login", null);

        using var body = JsonDocument.Parse(fixture.AchievementsHandler.RequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fixture.AchievementsHandler.Authorization, Is.EqualTo($"Bearer {token}"));
            Assert.That(body.RootElement.GetProperty("trigger").GetString(), Is.EqualTo("login-streak"));
            Assert.That(body.RootElement.GetProperty("recipientUserId").GetInt32(), Is.EqualTo(7));
            Assert.That(body.RootElement.GetProperty("recipientName").GetString(), Is.EqualTo("justin"));
            Assert.That(body.RootElement.GetProperty("value").GetInt32(), Is.EqualTo(4));
        });
    }

    [Test]
    public async Task CheckLogin_OnSameDay_DoesNotForwardLoginStreak()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsync("/api/check-login", null);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fixture.AchievementsHandler.RequestCount, Is.Zero);
        });
    }

    private sealed record AuthenticatedUserResponse(int Id, string Name);

    private sealed class ApiFixture : WebApplicationFactory<SharedApiAssemblyMarker>
    {
        private readonly RSA signingKey = RSA.Create(2048);
        private readonly string signingKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"shared-auth-test-key-{Guid.NewGuid():N}.pem");
        private readonly int? streakValue;

        internal ApiFixture(int? streakValue = null)
        {
            this.streakValue = streakValue;
            File.WriteAllText(signingKeyPath, signingKey.ExportRSAPrivateKeyPem());
        }

        internal RecordingAchievementsHandler AchievementsHandler { get; } = new();

        internal HttpClient CreateCookieClient(string token)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            client.DefaultRequestHeaders.Add("Cookie", $"token={token}");
            return client;
        }

        internal string CreateToken(
            DateTime? expires = null,
            string? subject = "7",
            string? name = "justin",
            RSA? signingKey = null)
        {
            var expiresAt = expires ?? DateTime.UtcNow.AddMinutes(5);
            var claims = new List<Claim>();
            if (subject is not null)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
            }
            if (name is not null)
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Name, name));
            }

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = expiresAt.AddMinutes(-5),
                IssuedAt = expiresAt.AddMinutes(-5),
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(
                    new RsaSecurityKey(signingKey ?? this.signingKey),
                    SecurityAlgorithms.RsaSha256)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Auth:SigningKeyPath", signingKeyPath);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:SigningKeyPath"] = signingKeyPath
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<UsersClient>();
                services.AddSingleton(new UsersClient(new HttpClient(new LoginStateHandler(streakValue))
                {
                    BaseAddress = new Uri("http://shared-database/")
                }));
                services.RemoveAll<AchievementsClient>();
                services.AddSingleton(new AchievementsClient(new HttpClient(AchievementsHandler)
                {
                    BaseAddress = new Uri("http://achievements/")
                }));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                signingKey.Dispose();
                File.Delete(signingKeyPath);
            }
        }
    }

    private sealed class LoginStateHandler(int? streakValue) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = streakValue is null
                ? new HttpResponseMessage(HttpStatusCode.NoContent)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { value = streakValue.Value })
                };
            return Task.FromResult(response);
        }
    }

    internal sealed class RecordingAchievementsHandler : HttpMessageHandler
    {
        internal int RequestCount { get; private set; }
        internal string? Authorization { get; private set; }
        internal string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Authorization = request.Headers.Authorization?.ToString();
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}