using System.Net;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class AppDataClientTests
{
    [Test]
    public async Task CreateNotificationAsync_SerializesContent()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Created, "{}");
        var client = CreateClient(handler);

        await client.CreateNotificationAsync(new NotificationInput(
            1,
            "lesson-completion",
            DateTimeOffset.UtcNow,
            "Course progress",
            "You made progress."));

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/notifications"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"email_subject\":\"Course progress\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"email_body\":\"You made progress.\""));
        });
    }

    [Test]
    public async Task GetAchievementsByTriggerAsync_FiltersAndOrdersTiers()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetAchievementsByTriggerAsync("lesson-completion");

        Assert.That(
            handler.LastRequestUri?.PathAndQuery,
            Is.EqualTo("/achievements?trigger=eq.lesson-completion&select=achievement_id,name,description,image,trigger,progress_needed&order=progress_needed.asc"));
    }

    [Test]
    public async Task UpsertUserAchievementsAsync_SendsAllProgressRows()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = CreateClient(handler);

        await client.UpsertUserAchievementsAsync([
            new UserAchievement(7, 1, 1),
            new UserAchievement(7, 2, 4)
        ]);

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(body.RootElement.GetArrayLength(), Is.EqualTo(2));
            Assert.That(handler.LastPreferValues, Does.Contain("resolution=merge-duplicates"));
        });
    }

    [Test]
    public async Task GetNotificationsAsync_FiltersUserAndOrdersNewestFirst()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetNotificationsAsync(7);

        Assert.That(handler.LastRequestUri?.PathAndQuery, Is.EqualTo(
            "/notifications?user_id=eq.7&select=notification_id,user_id,trigger,time,email_subject,email_body&order=time.desc,notification_id.desc"));
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