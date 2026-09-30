using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace LanguageWise.MiniGamesService.Api.Tests;

// Exercises the HTTP layer (auth, routing, request/response shape) for the three gameplay
// endpoint groups. AI mode is used throughout so a fake IAiVocabularyProvider supplies
// deterministic words without needing course content or a live OpenRouter call.
[TestFixture]
public sealed class GameplayEndpointTests
{
    [Test]
    public async Task GuessTheWord_InitThenCorrectGuess_CompletesTheRound()
    {
        using var fixture = new GameplayApiFixture();
        using var client = fixture.CreateAuthenticatedClient();

        var initResponse = await client.PostAsync("/api/guess-the-word/init?mode=ai", null);
        var guessResponse = await client.PostAsJsonAsync("/api/guess-the-word/guess", new GuessTheWordGuessRequest("apple"));
        var result = await guessResponse.Content.ReadFromJsonAsync<GuessTheWordGuessResult>();

        Assert.Multiple(() =>
        {
            Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(guessResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result!.IsCorrect, Is.True);
        });
    }

    [Test]
    public async Task GuessTheWord_Guess_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var fixture = new GameplayApiFixture();
        using var client = fixture.CreateClient();

        var response = await client.PostAsJsonAsync("/api/guess-the-word/guess", new GuessTheWordGuessRequest("apple"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task GuessTheWord_Reset_ClearsTheActiveGame()
    {
        using var fixture = new GameplayApiFixture();
        using var client = fixture.CreateAuthenticatedClient();

        await client.PostAsync("/api/guess-the-word/init?mode=ai", null);
        var resetResponse = await client.PostAsync("/api/guess-the-word/reset", null);
        var stateResponse = await client.GetAsync("/api/guess-the-word");

        Assert.Multiple(() =>
        {
            Assert.That(resetResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(stateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task WordSearch_InitThenGuessTheFeaturedWord_ReturnsAMatch()
    {
        using var fixture = new GameplayApiFixture();
        using var client = fixture.CreateAuthenticatedClient();

        var initResponse = await client.PostAsync("/api/word-search/init?mode=ai", null);
        var state = await initResponse.Content.ReadFromJsonAsync<WordSearchState>();

        var guessResponse = await client.PostAsJsonAsync(
            "/api/word-search/guess",
            new WordSearchGuessRequest(state!.FeaturedWord, state.WordPaths[state.FeaturedWord]));
        var result = await guessResponse.Content.ReadFromJsonAsync<WordSearchMoveResult>();

        Assert.Multiple(() =>
        {
            Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(guessResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result!.IsValid, Is.True);
        });
    }

    [Test]
    public async Task Associations_InitThenSolveAGroup_ReturnsTheSolvedGroup()
    {
        using var fixture = new GameplayApiFixture();
        using var client = fixture.CreateAuthenticatedClient();

        var initResponse = await client.PostAsync("/api/associations/init?mode=ai", null);
        var guessResponse = await client.PostAsJsonAsync(
            "/api/associations/guess",
            new AssociationsGuessRequest(["APPLE", "MANGO", "PEACH", "GRAPE"]));
        var result = await guessResponse.Content.ReadFromJsonAsync<AssociationResult>();

        Assert.Multiple(() =>
        {
            Assert.That(initResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(guessResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result!.IsAssociation, Is.True);
        });
    }

    private sealed class GameplayApiFixture : WebApplicationFactory<MiniGamesApiAssemblyMarker>
    {
        private readonly RSA rsa = RSA.Create(2048);
        private readonly string publicKeyPath = Path.Combine(
            AppContext.BaseDirectory,
            $"gameplay-test-key-{Guid.NewGuid():N}.pem");
        private string? token;

        internal string Token => token ??= CreateToken();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
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
                // AI mode needs no course content, so swap in canned vocabulary.
                services.RemoveAll<IAiVocabularyProvider>();
                services.AddSingleton<IAiVocabularyProvider>(new FakeAiVocabularyProvider());

                // Persistence and course-content lookups are optional-fail-open in GameSessionManager,
                // so pointing them at a handler that always errors keeps these tests hermetic.
                services.RemoveAll<CourseVocabularyClient>();
                services.AddSingleton(new CourseVocabularyClient(
                    new HttpClient(new AlwaysUnavailableHandler()) { BaseAddress = new Uri("http://unused.invalid") },
                    new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions())));
                services.RemoveAll<GamesDatabaseClient>();
                services.AddSingleton(new GamesDatabaseClient(
                    new HttpClient(new AlwaysUnavailableHandler()) { BaseAddress = new Uri("http://unused.invalid") }));
                services.RemoveAll<AchievementEventsClient>();
                services.AddSingleton(new AchievementEventsClient(
                    new HttpClient(new AlwaysUnavailableHandler()) { BaseAddress = new Uri("http://unused.invalid") }));
            });
        }

        internal HttpClient CreateAuthenticatedClient()
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            return client;
        }

        private string CreateToken()
        {
            var now = DateTime.UtcNow;
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, "7")]),
                NotBefore = now.AddMinutes(-1),
                IssuedAt = now.AddMinutes(-1),
                Expires = now.AddMinutes(5),
                SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256)
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

    // Always returns service-unavailable; GameSessionManager persistence and content-mode
    // checks are designed to degrade gracefully when their backing services are unreachable.
    private sealed class AlwaysUnavailableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }

    private sealed class FakeAiVocabularyProvider : IAiVocabularyProvider
    {
        private static readonly VocabularyGroup[] AssociationGroups =
        [
            new("Fruit", [new WordEntry("APPLE", null), new WordEntry("MANGO", null), new WordEntry("PEACH", null), new WordEntry("GRAPE", null)]),
            new("Transport", [new WordEntry("TRAIN", null), new WordEntry("PLANE", null), new WordEntry("BOAT", null), new WordEntry("TRUCK", null)]),
            new("Weather", [new WordEntry("CLOUD", null), new WordEntry("RAINY", null), new WordEntry("STORM", null), new WordEntry("SUNNY", null)]),
            new("Kitchen", [new WordEntry("SPOON", null), new WordEntry("KNIFE", null), new WordEntry("PLATE", null), new WordEntry("OVEN", null)])
        ];

        private static readonly VocabularyGroup[] WordSearchGroups =
        [
            new("Space", new[] { "ASTRONAUT", "ECLIPSE", "GALAXY", "ROCKET", "COMET", "MARS", "MOON", "RING", "SUN" }
                .Select(word => new WordEntry(word, null)).ToArray())
        ];

        private static readonly VocabularyGroup[] GuessTheWordGroups =
        [
            new("Fruit", [new WordEntry("APPLE", null)])
        ];

        public Task<IReadOnlyList<VocabularyGroup>> GenerateGroupsAsync(
            string gameKind, string language, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<VocabularyGroup>>(gameKind switch
            {
                "guess_the_word" => GuessTheWordGroups,
                "word_search" => WordSearchGroups,
                "associations" => AssociationGroups,
                _ => []
            });
    }
}
