using System.Net;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Services;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class AssistantContextServiceTests
{
    private const string ForumsJson =
        """
        [
          { "id": 1, "courseId": null, "code": "global", "name": "Global" },
          { "id": 2, "courseId": 11, "code": "spanish", "name": "Spanish" },
          { "id": 3, "courseId": 12, "code": "italian", "name": "Italian" }
        ]
        """;

    private static AssistantContextService CreateService(HttpMessageHandler handler) =>
        new(new DiscussionClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://chat-discussion-service-db:8080/")
        }));

    private static Task<AssistantContext> Retrieve(
        string message,
        string routeName = "forums",
        string? forumCode = null,
        int? postId = null) =>
        CreateService(new StubHttpMessageHandler(HttpStatusCode.OK, ForumsJson))
            .GetContextAsync(
                new ValidatedAssistantRequest(
                    message,
                    [],
                    new AssistantRouteContext(routeName, forumCode, postId)),
                CancellationToken.None);

    private static async Task<JsonElement> Context(
        string message,
        string routeName = "forums",
        string? forumCode = null,
        int? postId = null) =>
        JsonDocument
            .Parse((await Retrieve(message, routeName, forumCode, postId)).CanonicalContext)
            .RootElement;

    private static IEnumerable<string> TopicTitles(JsonElement context) =>
        context.GetProperty("helpTopics").EnumerateArray()
            .Select(topic => topic.GetProperty("title").GetString()!);

    [Test]
    public async Task BuildCanonicalContext_RetrievesTheTopicsThatMatchTheQuestion()
    {
        var context = await Context("How do I delete a post?");

        Assert.That(TopicTitles(context), Does.Contain("Deleting a post"));
    }

    // The page is a retrieval hint in its own right.
    [Test]
    public async Task BuildCanonicalContext_BiasesRetrievalTowardsThePageTheQuestionWasAskedFrom()
    {
        var context = await Context("What can I change here?", "post-edit", postId: 7);

        Assert.That(TopicTitles(context), Does.Contain("Editing a post you wrote"));
    }

    [Test]
    public async Task BuildCanonicalContext_NamesThePageAndTheForumTheAskerIsIn()
    {
        var context = await Context("What is this?", "forum", "spanish");

        Assert.Multiple(() =>
        {
            Assert.That(context.GetProperty("page").GetString(), Is.EqualTo("forum"));
            Assert.That(context.GetProperty("forum").GetString(), Is.EqualTo("spanish"));
        });
    }

    [Test]
    public async Task BuildCanonicalContext_ListsEveryForumSoTheModelNeverInventsOne()
    {
        var context = await Context("Which forums are there?");

        var codes = context.GetProperty("forums").EnumerateArray()
            .Select(forum => forum.GetProperty("code").GetString());

        Assert.That(codes, Is.EquivalentTo(new[] { "global", "spanish", "italian" }));
    }

    [Test]
    public async Task BuildCanonicalContext_WhenTheDatabaseWillNotAnswer_StillAnswersWithoutTheForumList()
    {
        var service = CreateService(new FailingHttpMessageHandler());

        var retrieved = await service.GetContextAsync(
            new ValidatedAssistantRequest(
                "How do I delete a post?",
                [],
                new AssistantRouteContext("forums", null, null)),
            CancellationToken.None);

        var context = JsonDocument.Parse(retrieved.CanonicalContext).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(context.GetProperty("forums").EnumerateArray(), Is.Empty);
            Assert.That(TopicTitles(context), Does.Contain("Deleting a post"));
        });
    }

    // The assistant explains the site rather than reading it.
    [Test]
    public async Task BuildCanonicalContext_CarriesNoPostContentEvenOnAPostPage()
    {
        var context = await Context("What does this post say?", "post", postId: 7);

        Assert.Multiple(() =>
        {
            Assert.That(context.TryGetProperty("post", out _), Is.False);
            Assert.That(context.TryGetProperty("comments", out _), Is.False);
        });
    }

    // -----------------------------------------------------------------------
    // The fallback answer, used when there is no model to hand the context to.
    // -----------------------------------------------------------------------

    [Test]
    public async Task GetContext_BuildsAFallbackFromTheSameTopicsItGivesTheModel()
    {
        var context = await Retrieve("How do I delete a post?");

        Assert.Multiple(() =>
        {
            Assert.That(context.FallbackAnswer, Does.Contain("Deleting a post"));
            Assert.That(context.FallbackAnswer, Does.Contain("Only the author"));
        });
    }

    [Test]
    public async Task GetContext_WhenNothingMatches_FallsBackToWhatTheAssistantCanHelpWith()
    {
        var context = await Retrieve("zzzz");

        Assert.Multiple(() =>
        {
            Assert.That(TopicTitles(JsonDocument.Parse(context.CanonicalContext).RootElement), Is.Empty);
            Assert.That(context.FallbackAnswer, Does.Contain("I can only help with"));
        });
    }
}
