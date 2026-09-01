using System.Net;
using System.Text;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;
using LanguageWise.QuizzesCoursesService.Api.Services;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

[TestFixture]
public sealed class AssistantContextServiceTests
{
    [Test]
    public async Task GetContextAsync_ForLesson_IncludesCanonicalMarkdownAndVocabulary()
    {
        const string lessonJson =
            """
            {
              "id": 1,
              "course": { "id": 1, "code": "de", "title": "German", "description": "Course" },
              "slug": "welcome",
              "title": "Welcome",
              "sortOrder": 1,
              "contentMarkdown": "# Willkommen",
              "vocabulary": [{ "word": "Hallo", "meaning": "Hello" }]
            }
            """;
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, lessonJson);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://catalog/")
        };
        var service = new AssistantContextService(new CatalogClient(httpClient));

        var result = await service.GetContextAsync(
            new AssistantRouteContext("lesson", "de", "welcome"),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFound, Is.True);
            Assert.That(result.CanonicalContext, Does.Contain("# Willkommen"));
            Assert.That(result.CanonicalContext, Does.Contain("Hallo"));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/courses/de/lessons/welcome"));
        });
    }

    [Test]
    public async Task GetContextAsync_ForQuizList_IncludesCanonicalQuizSummaries()
    {
        using var handler = new StubHttpMessageHandler((request, _) =>
        {
            var json = request.RequestUri?.AbsolutePath switch
            {
                "/api/courses/de" =>
                    """{"id":1,"code":"de","title":"German","description":"Course"}""",
                "/api/courses/de/quizzes" =>
                    """
                    [
                      {
                        "id": 4,
                        "title": "Welcome quiz",
                        "lessonId": 1,
                        "lessonSlug": "welcome",
                        "lessonTitle": "Welcome",
                        "lessonSortOrder": 1
                      }
                    ]
                    """,
                _ => throw new InvalidOperationException("Unexpected catalog request.")
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://catalog/")
        };
        var service = new AssistantContextService(new CatalogClient(httpClient));

        var result = await service.GetContextAsync(
            new AssistantRouteContext("quiz-list", "de", null),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsFound, Is.True);
            Assert.That(result.CanonicalContext, Does.Contain("Welcome quiz"));
            Assert.That(result.CanonicalContext, Does.Contain("\"quizzes\""));
        });
    }
}
