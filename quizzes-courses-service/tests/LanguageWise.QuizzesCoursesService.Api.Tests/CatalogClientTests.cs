using System.Net;
using LanguageWise.QuizzesCoursesService.Api.Clients;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

[TestFixture]
public class CatalogClientTests
{
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

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://quizzes-courses-service-db:8080/") };
}
