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

public sealed class AccountTests
{
    [Test]
    public async Task CreateAccount_WithValidDetails_ReturnsCreatedAccount()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { username = " newbie ", password = "long-enough" });
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(account, Is.EqualTo(new AccountResponse(12, "newbie")));
            Assert.That(fixture.Database.LastBody!.RootElement.GetProperty("username").GetString(), Is.EqualTo("newbie"));
        });
    }

    [Test]
    public async Task CreateAccount_WithTakenUsername_ReturnsConflict()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { username = "taken", password = "long-enough" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [TestCase("ab", "long-enough")]
    [TestCase("has space", "long-enough")]
    [TestCase("newbie", "short")]
    [TestCase("newbie", null)]
    public async Task CreateAccount_WithInvalidDetails_ReturnsBadRequestWithoutCallingDatabase(string username, string? password)
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { username, password });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(fixture.Database.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task UpdateAccount_WithoutCookie_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/users/7", ValidUpdate());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(fixture.Database.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task UpdateAccount_ForAnotherUser_ReturnsForbiddenWithoutCallingDatabase()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PatchAsJsonAsync("/api/users/8", ValidUpdate());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(fixture.Database.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task UpdateAccount_WithWrongCurrentPassword_ReturnsBadRequestWithoutUpdating()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PatchAsJsonAsync(
            "/api/users/7",
            new { username = "justin2", currentPassword = "wrong" });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(fixture.Database.Requests, Is.EqualTo(new[] { "POST /api/users/verify" }));
        });
    }

    [Test]
    public async Task UpdateAccount_WithNoChanges_ReturnsBadRequest()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PatchAsJsonAsync("/api/users/7", new { currentPassword = ApiFixture.CurrentPassword });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(fixture.Database.Requests, Is.Empty);
        });
    }

    [Test]
    public async Task UpdateAccount_WithNewUsername_ReissuesSessionWithNewName()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PatchAsJsonAsync("/api/users/7", ValidUpdate());
        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        var cookie = response.Headers.GetValues("Set-Cookie").Single();
        var token = cookie.Split(';')[0]["token=".Length..];
        var name = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Name).Value;

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(account, Is.EqualTo(new AccountResponse(7, "justin2")));
            Assert.That(name, Is.EqualTo("justin2"));
            Assert.That(cookie, Does.Contain("httponly").IgnoreCase);
            Assert.That(fixture.Database.Requests, Is.EqualTo(new[] { "POST /api/users/verify", "PATCH /api/users/7" }));
        });
    }

    [Test]
    public async Task UpdateAccount_WithTakenUsername_ReturnsConflict()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PatchAsJsonAsync(
            "/api/users/7",
            new { username = "taken", currentPassword = ApiFixture.CurrentPassword });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(response.Headers.Contains("Set-Cookie"), Is.False);
        });
    }

    private static object ValidUpdate() => new
    {
        username = "justin2",
        newPassword = "new-password",
        currentPassword = ApiFixture.CurrentPassword
    };

    private sealed record AccountResponse(int Id, string Name);

    private sealed class ApiFixture : WebApplicationFactory<SharedApiAssemblyMarker>
    {
        internal const string CurrentPassword = "current-pass";

        private readonly RSA signingKey = RSA.Create(2048);
        private readonly string signingKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"shared-account-test-key-{Guid.NewGuid():N}.pem");

        internal ApiFixture()
        {
            File.WriteAllText(signingKeyPath, signingKey.ExportRSAPrivateKeyPem());
        }

        internal FakeDatabaseHandler Database { get; } = new();

        internal HttpClient CreateCookieClient(string token)
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
            client.DefaultRequestHeaders.Add("Cookie", $"token={token}");
            return client;
        }

        internal string CreateToken()
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, "7"),
                    new Claim(JwtRegisteredClaimNames.Name, "justin")
                ]),
                Expires = DateTime.UtcNow.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(signingKey), SecurityAlgorithms.RsaSha256)
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
                services.AddSingleton(new UsersClient(new HttpClient(Database)
                {
                    BaseAddress = new Uri("http://shared-database/")
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

    private sealed class FakeDatabaseHandler : HttpMessageHandler
    {
        internal List<string> Requests { get; } = [];
        internal JsonDocument? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var route = $"{request.Method} {request.RequestUri!.AbsolutePath}";
            Requests.Add(route);
            LastBody = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var body = LastBody.RootElement;
            var username = body.TryGetProperty("username", out var value) ? value.GetString() : null;

            return route switch
            {
                "POST /api/users/verify" => body.GetProperty("password").GetString() == ApiFixture.CurrentPassword
                    ? Json(HttpStatusCode.OK, new { authenticated = true, userId = 7 })
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized),
                "POST /api/users" or "PATCH /api/users/7" when username == "taken" => new HttpResponseMessage(HttpStatusCode.Conflict),
                "POST /api/users" => Json(HttpStatusCode.Created, new { id = 12, username }),
                "PATCH /api/users/7" => Json(HttpStatusCode.OK, new { id = 7, username = username ?? "justin" }),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        }

        private static HttpResponseMessage Json(HttpStatusCode status, object content) =>
            new(status) { Content = JsonContent.Create(content) };
    }
}
