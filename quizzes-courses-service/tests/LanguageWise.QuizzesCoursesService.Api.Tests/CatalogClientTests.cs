using System.Net;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

[TestFixture]
public class CatalogClientTests
{
    [Test]
    public async Task GetHealthAsync_DeserialisesDatabaseHealth()
    {
        const string json =
            """{"status":"healthy","service":"quizzes-courses-service-db","courses":6}""";
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        var response = await client.GetHealthAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Value?.Status, Is.EqualTo("healthy"));
            Assert.That(response.Value?.Courses, Is.EqualTo(6));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/health"));
        });
    }

    [Test]
    public async Task GetCoursesAsync_DeserialisesTheCourseCatalog()
    {
        const string json =
            """
            [
              { "id": 1, "code": "de", "title": "German", "description": "German course" },
              { "id": 2, "code": "fr", "title": "French", "description": "French course" }
            ]
            """;
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        var courses = await client.GetCoursesAsync();

        Assert.That(courses, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(courses[0].Code, Is.EqualTo("de"));
            Assert.That(courses[1].Title, Is.EqualTo("French"));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/courses"));
        });
    }

    [Test]
    public async Task GetLessonAsync_DeserialisesMarkdownAndVocabulary()
    {
        const string json =
            """
            {
              "id": 1,
              "course": { "id": 1, "code": "de", "title": "German", "description": "German course" },
              "slug": "welcome",
              "title": "Welcome",
              "sortOrder": 1,
              "contentMarkdown": "# Willkommen",
              "vocabulary": [{ "word": "Hallo", "meaning": "Hello" }]
            }
            """;
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        var lesson = await client.GetLessonAsync("de", "welcome");

        Assert.That(lesson, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(lesson!.ContentMarkdown, Is.EqualTo("# Willkommen"));
            Assert.That(lesson.Vocabulary.Single().Word, Is.EqualTo("Hallo"));
            Assert.That(
                handler.LastRequestUri?.AbsolutePath,
                Is.EqualTo("/api/courses/de/lessons/welcome"));
        });
    }

    [Test]
    public async Task GetCourseAsync_ReturnsNullForMissingCourse()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        var course = await client.GetCourseAsync("xx");

        Assert.That(course, Is.Null);
    }

    [Test]
    public void GetCoursesAsync_ThrowsWhenTheDatabaseServiceFails()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetCoursesAsync());
    }

    [Test]
    public async Task StartQuizAttemptAsync_SendsTheAuthenticatedUserToTheInternalService()
    {
        const string json = """{"id":12,"quizId":3,"startedAt":"2026-08-31T10:00:00Z"}""";
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Created, json);
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        var response = await client.StartQuizAttemptAsync(3, 42);
        using var body = JsonDocument.Parse(handler.LastRequestBody!);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSuccess, Is.True);
            Assert.That(response.Value?.Id, Is.EqualTo(12));
            Assert.That(handler.LastRequestMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/quizzes/3/attempts"));
            Assert.That(body.RootElement.GetProperty("userId").GetInt32(), Is.EqualTo(42));
        });
    }

    [Test]
    public async Task SubmitQuizAttemptAsync_SendsUserAndAnswers()
    {
        const string json =
            """
            {
              "attemptId": 12,
              "quizId": 3,
              "score": 1,
              "totalQuestions": 1,
              "passed": true,
              "completedAt": "2026-08-31T10:05:00Z",
              "answers": [
                {
                  "questionId": 9,
                  "studentResponse": "Hallo",
                  "isCorrect": true,
                  "correctAnswer": "Hallo"
                }
              ]
            }
            """;
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);
        var request = new SubmitQuizAttemptRequest([new QuizAnswerSubmission(9, "Hallo")]);

        var response = await client.SubmitQuizAttemptAsync(12, 42, request);
        using var body = JsonDocument.Parse(handler.LastRequestBody!);

        Assert.Multiple(() =>
        {
            Assert.That(response.Value?.Passed, Is.True);
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/quiz-attempts/12/submit"));
            Assert.That(body.RootElement.GetProperty("userId").GetInt32(), Is.EqualTo(42));
            Assert.That(
                body.RootElement.GetProperty("answers")[0].GetProperty("response").GetString(),
                Is.EqualTo("Hallo"));
        });
    }

    [Test]
    public async Task SetCourseMilestoneAsync_PreservesEligibilityConflict()
    {
        const string problem = """{"title":"Course requirements are incomplete."}""";
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Conflict, problem);
        using var httpClient = CreateHttpClient(handler);
        var client = new CatalogClient(httpClient);

        var response = await client.SetCourseMilestoneAsync("de", 42, completed: true);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(response.ErrorBody, Is.EqualTo(problem));
            Assert.That(handler.LastRequestMethod, Is.EqualTo(HttpMethod.Put));
            Assert.That(
                handler.LastRequestUri?.AbsolutePath,
                Is.EqualTo("/api/courses/de/milestones/42"));
        });
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://quizzes-courses-service-db:8080/") };
}
