using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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

    private sealed record AuthenticatedUserResponse(int Id, string Name);

    private sealed class ApiFixture : WebApplicationFactory<SharedApiAssemblyMarker>
    {
        private readonly RSA signingKey = RSA.Create(2048);
        private readonly string signingKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"shared-auth-test-key-{Guid.NewGuid():N}.pem");

        internal ApiFixture()
        {
            File.WriteAllText(signingKeyPath, signingKey.ExportRSAPrivateKeyPem());
        }

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
}