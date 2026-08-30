using System.Net;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class OllamaEmailGeneratorTests
{
    private static readonly EmailContext Context = new(
        "course-completion",
        "course-12",
        [
            new AchievementUpdate(1, "First Course", 1, 1, false),
            new AchievementUpdate(2, "Course Explorer", 5, 5, true),
            new AchievementUpdate(3, "Course Champion", 5, 10, false)
        ]);

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
            Assert.That(result.Subject, Is.EqualTo("Achievement unlocked: Course Explorer"));
            Assert.That(result.Body, Does.Contain("5 of 5"));
            Assert.That(result.Body, Does.Contain("Course Champion: 5 of 10"));
            Assert.That(result.UsedFallback, Is.True);
        });
    }

    [Test]
    public async Task GenerateAsync_WhenNoTierIsNewlyAttained_FallbackSummarizesAllProgress()
    {
        var context = new EmailContext(
            "streak",
            "streak-2",
            [
                new AchievementUpdate(9, "Three Day Streak", 2, 3, false),
                new AchievementUpdate(10, "Seven Day Streak", 2, 7, false)
            ]);
        var generator = CreateGenerator(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await generator.GenerateAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Subject, Is.EqualTo("Your LanguageWise progress"));
            Assert.That(result.Body, Does.Contain("Three Day Streak: 2 of 3"));
            Assert.That(result.Body, Does.Contain("Seven Day Streak: 2 of 7"));
            Assert.That(result.UsedFallback, Is.True);
        });
    }

    [Test]
    public async Task GenerateAsync_DisablesThinkingAndBoundsGeneratedTokens()
    {
        const string response = """
            {"message":{"content":"{\"subject\":\"Progress\",\"body\":\"Keep going.\"}"}}
            """;
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ollama/") };
        var generator = new OllamaEmailGenerator(
            httpClient,
            Options.Create(new OllamaOptions()),
            NullLogger<OllamaEmailGenerator>.Instance);

        await generator.GenerateAsync(Context);

        using var request = System.Text.Json.JsonDocument.Parse(handler.LastRequestBody!);
        var messages = request.RootElement.GetProperty("messages");
        var instructions = messages[0].GetProperty("content").GetString();
        var eventDetails = messages[1].GetProperty("content").GetString();
        Assert.Multiple(() =>
        {
            Assert.That(request.RootElement.GetProperty("think").GetBoolean(), Is.False);
            Assert.That(request.RootElement.GetProperty("options").GetProperty("num_predict").GetInt32(), Is.EqualTo(192));
            Assert.That(instructions, Does.Contain("Mention every affected achievement"));
            Assert.That(instructions, Does.Contain("highlight any marked as newly attained"));
            Assert.That(eventDetails, Does.Contain("First Course: 1/1; newly attained: False"));
            Assert.That(eventDetails, Does.Contain("Course Explorer: 5/5; newly attained: True"));
            Assert.That(eventDetails, Does.Contain("Course Champion: 5/10; newly attained: False"));
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