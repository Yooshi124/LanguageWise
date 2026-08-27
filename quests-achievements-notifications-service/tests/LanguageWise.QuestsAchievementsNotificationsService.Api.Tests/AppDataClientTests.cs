using System.Net;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class AppDataClientTests
{
    [Test]
    public async Task CreateNotificationAsync_WhenEventAlreadyExists_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Conflict, "{}");
        var client = CreateClient(handler);

        var created = await client.CreateNotificationAsync(new NotificationInput(
            "duplicate-event",
            1,
            "course-completion",
            DateTimeOffset.UtcNow,
            "learner@example.com"));

        Assert.That(created, Is.False);
    }

    [Test]
    public async Task CreateNotificationAsync_WhenInsertSucceeds_ReturnsTrue()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Created, "{}");
        var client = CreateClient(handler);

        var created = await client.CreateNotificationAsync(new NotificationInput(
            "new-event",
            1,
            "course-completion",
            DateTimeOffset.UtcNow,
            "learner@example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.True);
            Assert.That(handler.LastRequestMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/notifications"));
        });
    }

    [Test]
    public async Task UpsertPreferencesAsync_UsesUserIdConflictResolutionAndJsonBody()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);
        var preferences = new UserPreferences(7, "learner@example.com", true, true, false, true, false, true);

        await client.UpsertPreferencesAsync(preferences);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri?.PathAndQuery, Is.EqualTo("/user_preferences?on_conflict=user_id"));
            Assert.That(handler.LastPreferValues, Does.Contain("resolution=merge-duplicates"));
            Assert.That(body.RootElement.GetProperty("user_id").GetInt32(), Is.EqualTo(7));
            Assert.That(body.RootElement.GetProperty("email").GetString(), Is.EqualTo("learner@example.com"));
        });
    }

    private static AppDataClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://database/") });
}