using System.Net;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class OllamaEmailGeneratorTests
{
    private static readonly EmailContext Context = new(
        "course_completion",
        "course-12",
        "Course Champion",
        5,
        5,
        true);

    [Test]
    public async Task GenerateAsync_ReturnsStructuredOllamaContent()
    {
        const string response = """
            {"message":{"content":"{\"subject\":\"Course complete!\",\"body\":\"You unlocked Course Champion.\"}"}}
            """;
        var generator = CreateGenerator(HttpStatusCode.OK, response);

        var result = await generator.GenerateAsync(Context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Subject, Is.EqualTo("Course complete!"));
            Assert.That(result.Body, Is.EqualTo("You unlocked Course Champion."));
            Assert.That(result.UsedFallback, Is.False);
        });
    }

    [Test]
    public async Task GenerateAsync_WhenOllamaFails_ReturnsFallbackContent()
    {
        var generator = CreateGenerator(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await generator.GenerateAsync(Context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Subject, Is.EqualTo("Achievement unlocked: Course Champion"));
            Assert.That(result.Body, Does.Contain("5 of 5"));
            Assert.That(result.UsedFallback, Is.True);
        });
    }

    private static OllamaEmailGenerator CreateGenerator(HttpStatusCode statusCode, string responseBody)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(statusCode, responseBody))
        {
            BaseAddress = new Uri("http://ollama/")
        };

        return new OllamaEmailGenerator(
            httpClient,
            Options.Create(new OllamaOptions()),
            NullLogger<OllamaEmailGenerator>.Instance);
    }
}