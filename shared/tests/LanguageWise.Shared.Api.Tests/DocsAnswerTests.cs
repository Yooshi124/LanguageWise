using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static LanguageWise.Shared.Api.Tests.AuthenticationTests;

namespace LanguageWise.Shared.Api.Tests;

public sealed class DocsAnswerTests
{
    [Test]
    public async Task RagAnswer_WithSession_ReturnsCitedAnswerWithConfidence()
    {
        using var fixture = new ApiFixture();
        var token = fixture.CreateToken();
        using var client = fixture.CreateCookieClient(token);

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "  how does the leaderboard work  " });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var citations = root.GetProperty("citations");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fixture.RagHandler.Path, Is.EqualTo("/query"));
            Assert.That(fixture.RagHandler.RequestBody, Does.Contain("\"query\":\"how does the leaderboard work\""));
            Assert.That(fixture.GarryHandler.Authorization, Is.EqualTo($"Bearer {token}"));
            Assert.That(fixture.GarryHandler.RequestBody, Does.Contain("[2] leaderboard-analytics-service"));
            Assert.That(root.GetProperty("answer").GetString(), Is.EqualTo("Compare yourself with other students [1]. Made up."));
            Assert.That(root.GetProperty("confidence").GetString(), Is.EqualTo("high"));
            Assert.That(citations.GetArrayLength(), Is.EqualTo(1));
            Assert.That(citations[0].GetProperty("number").GetInt32(), Is.EqualTo(1));
            Assert.That(citations[0].GetProperty("heading").GetString(), Is.EqualTo("For users"));
            Assert.That(citations[0].GetProperty("text").GetString(), Is.EqualTo("Global analytics comparing you against other students."));
        });
    }

    [Test]
    public async Task RagAnswer_WhenModelCitesNothing_ListsEverySuppliedPassage()
    {
        using var fixture = new ApiFixture();
        fixture.GarryHandler.Answer = "Compare yourself with other students.";
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "leaderboard" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(body.RootElement.GetProperty("citations").GetArrayLength(), Is.EqualTo(2));
    }

    [Test]
    public async Task RagAnswer_WithInsufficientContext_DoesNotCallGarry()
    {
        using var fixture = new ApiFixture();
        fixture.RagHandler.Confidence = "insufficient";
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "how do I cook pasta" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fixture.GarryHandler.RequestCount, Is.Zero);
            Assert.That(body.RootElement.GetProperty("confidence").GetString(), Is.EqualTo("insufficient"));
            Assert.That(body.RootElement.GetProperty("citations").GetArrayLength(), Is.Zero);
        });
    }

    [Test]
    public async Task RagAnswer_WhenModelReportsInsufficientContext_ReturnsInsufficientAnswer()
    {
        using var fixture = new ApiFixture();
        fixture.GarryHandler.Answer = "INSUFFICIENT_CONTEXT";
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "leaderboard prizes" });
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Multiple(() =>
        {
            Assert.That(body.RootElement.GetProperty("confidence").GetString(), Is.EqualTo("insufficient"));
            Assert.That(body.RootElement.GetProperty("answer").GetString(), Does.Not.Contain("INSUFFICIENT_CONTEXT"));
            Assert.That(body.RootElement.GetProperty("citations").GetArrayLength(), Is.Zero);
        });
    }

    [Test]
    public async Task RagAnswer_WithoutSession_ReturnsUnauthorizedWithoutCallingRag()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "mini games" });

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(fixture.RagHandler.Path, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task RagAnswer_WithBlankQuery_ReturnsValidationProblem(string query)
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(fixture.RagHandler.Path, Is.Null);
    }

    [Test]
    public async Task RagAnswer_WithOverlongQuery_ReturnsValidationProblem()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = new string('a', 501) });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task RagAnswer_WhenRagServerFails_ReturnsBadGateway()
    {
        using var fixture = new ApiFixture();
        fixture.RagHandler.StatusCode = HttpStatusCode.InternalServerError;
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "mini games" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadGateway));
    }

    [TestCase(HttpStatusCode.ServiceUnavailable, HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests)]
    public async Task RagAnswer_WhenGarryFails_ReturnsProblem(HttpStatusCode garryStatus, HttpStatusCode expected)
    {
        using var fixture = new ApiFixture();
        fixture.GarryHandler.StatusCode = garryStatus;
        using var client = fixture.CreateCookieClient(fixture.CreateToken());

        var response = await client.PostAsJsonAsync("/api/rag/answer", new { query = "mini games" });

        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }
}
