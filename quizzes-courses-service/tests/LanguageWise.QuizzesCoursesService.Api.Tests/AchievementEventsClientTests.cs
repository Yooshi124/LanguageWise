using System.Net;
using LanguageWise.QuizzesCoursesService.Api.Clients;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

public sealed class AchievementEventsClientTests
{
    [Test]
    public async Task RecordLessonCompletionAsync_SendsAuthenticatedAbsoluteLessonCount()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new AchievementEventsClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://achievements/")
        });

        await client.RecordLessonCompletionAsync(7, "Amber", 5, "jwt-token", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/events"));
            Assert.That(handler.LastAuthorizationScheme, Is.EqualTo("Bearer"));
            Assert.That(handler.LastAuthorizationParameter, Is.EqualTo("jwt-token"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"trigger\":\"lesson-completion\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"recipientUserId\":7"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"recipientName\":\"Amber\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"value\":5"));
        });
    }

    [Test]
    public async Task RecordCourseCompletionAsync_SendsCourseCompletionTrigger()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new AchievementEventsClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://achievements/")
        });

        await client.RecordCourseCompletionAsync(7, "Amber", "jwt-token", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestBody, Does.Contain("\"trigger\":\"course-completion\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"subject\":\"Completed a LanguageWise course\""));
            Assert.That(handler.LastRequestBody, Does.Contain("\"value\":null"));
        });
    }

    [Test]
    public async Task RecordQuizResultAsync_SendsQuizScoreDescription()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        var client = new AchievementEventsClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://achievements/")
        });
        var result = new Models.QuizAttemptResult(1, 2, 8, 10, true, DateTimeOffset.UtcNow, []);

        await client.RecordQuizResultAsync(7, "Amber", result, "jwt-token", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestBody, Does.Contain("\"trigger\":\"quiz-result\""));
            Assert.That(handler.LastRequestBody, Does.Contain("score of 8 out of 10"));
            Assert.That(handler.LastRequestBody, Does.Contain("\"value\":null"));
        });
    }
}