using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.MiniGamesService.Api.Tests;

public sealed class AuthenticationTests
{
    [Test]
    public async Task GameState_WithoutCookie_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/guess-the-word");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GameState_WithInvalidSignature_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var otherKey = RSA.Create(2048);
        using var client = fixture.CreateCookieClient(fixture.CreateToken(otherKey));

        var response = await client.GetAsync("/api/guess-the-word");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [TestCase(null)]
    [TestCase("0")]
    [TestCase("not-a-number")]
    public async Task GameState_WithInvalidSubject_ReturnsForbidden(string? subject)
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken(subject: subject));

        var response = await client.GetAsync("/api/guess-the-word");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GameState_WithValidCookie_UsesAuthenticatedSubject()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.GetAsync("/api/guess-the-word");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GameState_WithCallerSuppliedUserId_RejectsIdentityOverride()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.GetAsync("/api/guess-the-word?userId=999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private sealed class ApiFixture : WebApplicationFactory<MiniGamesApiAssemblyMarker>
    {
        private readonly RSA signingKey = RSA.Create(2048);
        private readonly string signingKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"mini-games-auth-test-key-{Guid.NewGuid():N}.pem");

        internal ApiFixture()
        {
            File.WriteAllText(signingKeyPath, signingKey.ExportRSAPublicKeyPem());
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

        internal string CreateToken(RSA? signingKey = null, string? subject = "7")
        {
            var claims = subject is null
                ? []
                : new[] { new Claim(JwtRegisteredClaimNames.Sub, subject) };
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = DateTime.UtcNow.AddMinutes(-1),
                IssuedAt = DateTime.UtcNow.AddMinutes(-1),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(
                    new RsaSecurityKey(signingKey ?? this.signingKey),
                    SecurityAlgorithms.RsaSha256)
            };
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(descriptor));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Auth:VerificationKeyPath", signingKeyPath);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:VerificationKeyPath"] = signingKeyPath
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