using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static LanguageWise.Shared.Api.Tests.AuthenticationTests;

namespace LanguageWise.Shared.Api.Tests;

public sealed class DocsSearchTests
{
    [Test]
    public async Task RagQuery_WithSession_ProxiesToGeneralRagEndpoint()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/query", new { query = "  how does the leaderboard work  ", nResults = 3 });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fixture.RagHandler.Path, Is.EqualTo("/query"));
            Assert.That(fixture.RagHandler.RequestBody, Does.Contain("\"query\":\"how does the leaderboard work\""));
            Assert.That(fixture.RagHandler.RequestBody, Does.Contain("\"n_results\":3"));
            Assert.That(body.RootElement.GetProperty("resultCount").GetInt32(), Is.EqualTo(1));
            Assert.That(body.RootElement.GetProperty("results")[0].GetProperty("heading").GetString(), Is.EqualTo("For users"));
        });
    }

    [Test]
    public async Task RagQuery_WithoutSession_ReturnsUnauthorizedWithoutCallingRag()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/rag/query", new { query = "mini games" });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(fixture.RagHandler.Path, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task RagQuery_WithBlankQuery_ReturnsValidationProblem(string query)
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/query", new { query });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(fixture.RagHandler.Path, Is.Null);
    }

    [Test]
    public async Task RagQuery_WithOverlongQuery_ReturnsValidationProblem()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/query", new { query = new string('a', 501) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task RagQuery_WhenRagServerFails_ReturnsBadGateway()
    {
        using var fixture = new ApiFixture();
        fixture.RagHandler.StatusCode = HttpStatusCode.InternalServerError;
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/query", new { query = "mini games" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
    }
}
