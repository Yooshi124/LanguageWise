using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class AuthorizationTests
{
    [Test]
    public async Task Health_AllowsAnonymousRequests()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetPosts_AllowsAnonymousRequests()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/posts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task CreatePost_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/posts",
            new { title = "Hello", content = "World", category = "global" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task CreatePost_WithBearerToken_ReturnsCreated()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PostAsJsonAsync(
            "/api/posts",
            new { title = "Hello", content = "World", category = "global" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task CreatePost_WithTokenCookie_ReturnsCreated()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", $"token={fixture.CreateToken()}");

        var response = await client.PostAsJsonAsync(
            "/api/posts",
            new { title = "Hello", content = "World", category = "global" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task CreatePost_IgnoresAnyUserIdSuppliedInTheBody()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        // The caller tries to post as somebody else.
        var response = await client.PostAsJsonAsync(
            "/api/posts",
            new { userId = FakeDiscussionDatabase.OtherUserId, title = "Hello", content = "World", category = "global" });

        using var forwarded = JsonDocument.Parse(fixture.Database.LastRequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(
                forwarded.RootElement.GetProperty("userId").GetInt32(),
                Is.EqualTo(FakeDiscussionDatabase.SignedInUserId));
        });
    }

    [Test]
    public async Task CreatePost_WithBlankTitle_ReturnsValidationProblem()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PostAsJsonAsync(
            "/api/posts",
            new { title = "   ", content = "World", category = "global" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PatchPost_WithMalformedJson_ReturnsBadRequest()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.PatchAsync("/api/posts/1", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task PatchPost_WhenCallerIsTheAuthor_ReturnsOk()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PatchAsJsonAsync("/api/posts/1", new { content = "Edited" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PatchPost_WhenCallerIsNotTheAuthor_ReturnsForbidden()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        // Post 2 belongs to another user.
        var response = await client.PatchAsJsonAsync("/api/posts/2", new { content = "Edited" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task PatchPost_WhenThePostDoesNotExist_ReturnsNotFound()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PatchAsJsonAsync("/api/posts/404", new { content = "Edited" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeletePost_WhenCallerIsNotTheAuthor_ReturnsForbidden()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.DeleteAsync("/api/posts/2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeletePost_WhenCallerIsTheAuthor_ReturnsNoContent()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.DeleteAsync("/api/posts/1");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task PatchComment_WhenCallerIsNotTheAuthor_ReturnsForbidden()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        // Comment 2 belongs to another user.
        var response = await client.PatchAsJsonAsync("/api/comments/2", new { content = "Edited" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task DeleteComment_WhenCallerIsNotTheAuthor_ReturnsForbidden()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.DeleteAsync("/api/comments/2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task LikePost_WhenTheUserHasAlreadyLikedIt_ReturnsConflict()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        // Post 9 reports the unique-index violation from the database service.
        var response = await client.PostAsync("/api/posts/9/likes", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task LikePost_WhenNotYetLiked_ReturnsCreated()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PostAsync("/api/posts/1/likes", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task LikePost_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsync("/api/posts/1/likes", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetPosts_WhenTheDatabaseServiceIsUnavailable_ReturnsServiceUnavailable()
    {
        using var fixture = new ApiFixture(new FailingHttpMessageHandler());
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/posts");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
    }

    [Test]
    public async Task GetForums_AllowsAnonymousRequestsAndListsEveryForum()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/forums");
        var codes = (await response.Content.ReadFromJsonAsync<List<ForumResponse>>())!
            .Select(forum => forum.Code);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(codes, Is.EquivalentTo(new[] { "global", "spanish", "italian", "japanese" }));
        });
    }

    [Test]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GetMe_WithBearerToken_ReturnsTheIdentityFromTheToken()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.GetAsync("/api/me");
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(me!.Id, Is.EqualTo(FakeDiscussionDatabase.SignedInUserId));
            Assert.That(me.Username, Is.EqualTo("lachlan"));
        });
    }

    [Test]
    public async Task GetPost_ReturnsTheFirstPageOfCommentsWithThePost()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/posts/1");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body.RootElement.GetProperty("comments").GetArrayLength(), Is.EqualTo(1));
            Assert.That(body.RootElement.GetProperty("commentsHasMore").GetBoolean(), Is.True);
        });
    }

    [Test]
    public async Task GetPosts_WithAForumThatDoesNotExist_ReturnsBadRequest()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/posts?category=klingon");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetPosts_ForwardsTheSearchTermToTheDatabaseService()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        await client.GetAsync("/api/posts?q=flashcards");

        Assert.That(fixture.Database.LastRequestUri?.Query, Does.Contain("search=flashcards"));
    }

    [Test]
    public async Task GetPosts_WithAnUnsupportedSort_ReturnsBadRequest()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/posts?sort=top");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreatePost_WithAForumThatDoesNotExist_ReturnsBadRequest()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.PostAsJsonAsync(
            "/api/posts",
            new { title = "Hello", content = "World", category = "klingon" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreatePost_SendsTheAuthorNameFromTheToken()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        await client.PostAsJsonAsync(
            "/api/posts",
            new { title = "Hello", content = "World", category = "global" });

        using var forwarded = JsonDocument.Parse(fixture.Database.LastRequestBody!);
        Assert.That(forwarded.RootElement.GetProperty("authorName").GetString(), Is.EqualTo("lachlan"));
    }

    private sealed record ForumResponse(string Code, string DisplayName, int SortOrder);

    private sealed record MeResponse(int Id, string Username);

    private sealed class ApiFixture : WebApplicationFactory<Program>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.GetTempFileName();
        private readonly HttpMessageHandler handler;

        internal ApiFixture(HttpMessageHandler? handler = null)
        {
            this.handler = handler ?? Database;
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        }

        internal FakeDiscussionDatabase Database { get; } = new();

        internal string CreateToken()
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
            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Auth:VerificationKeyPath", publicKeyPath);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DiscussionClient>();
                services.AddSingleton(new DiscussionClient(new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://chat-discussion-service-db:8080/")
                }));
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
