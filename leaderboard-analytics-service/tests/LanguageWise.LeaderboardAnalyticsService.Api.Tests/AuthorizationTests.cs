using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LanguageWise.LeaderboardAnalyticsService.Api.Tests;

public sealed class AuthorizationTests
{
    [Test]
    public async Task Me_WithBearerToken_ReturnsIdentity()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.CreateToken());

        var response = await client.GetAsync("/api/me");
        var me = await response.Content.ReadFromJsonAsync<MeResponse>();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(me!.Id, Is.EqualTo(7));
            Assert.That(me.Username, Is.EqualTo("justin"));
        });
    }

    [Test]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private sealed record MeResponse(int Id, string Username);
}
