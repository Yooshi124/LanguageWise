using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Services;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class AssistantContextServiceTests
{
    private static readonly AssistantContextService Service = new();

    private static AssistantContext Retrieve(
        string message,
        string routeName = "forums",
        string? forumCode = null,
        int? postId = null) =>
        Service.GetContext(new ValidatedAssistantRequest(
            message,
            [],
            new AssistantRouteContext(routeName, forumCode, postId)));

    private static JsonElement Context(
        string message,
        string routeName = "forums",
        string? forumCode = null,
        int? postId = null) =>
        JsonDocument
            .Parse(Retrieve(message, routeName, forumCode, postId).CanonicalContext)
            .RootElement;

    private static IEnumerable<string> TopicTitles(JsonElement context) =>
        context.GetProperty("helpTopics").EnumerateArray()
            .Select(topic => topic.GetProperty("title").GetString()!);

    [Test]
    public void BuildCanonicalContext_RetrievesTheTopicsThatMatchTheQuestion()
    {
        var context = Context("How do I delete a post?");

        Assert.That(TopicTitles(context), Does.Contain("Deleting a post"));
    }

    /// <summary>
    /// The page is a retrieval hint in its own right, so standing on the edit
    /// screen surfaces the editing topic even when the question does not say so.
    /// </summary>
    [Test]
    public void BuildCanonicalContext_BiasesRetrievalTowardsThePageTheQuestionWasAskedFrom()
    {
        var context = Context("What can I change here?", "post-edit", postId: 7);

        Assert.That(TopicTitles(context), Does.Contain("Editing a post you wrote"));
    }

    [Test]
    public void BuildCanonicalContext_NamesThePageAndTheForumTheAskerIsIn()
    {
        var context = Context("What is this?", "forum", "spanish");

        Assert.Multiple(() =>
        {
            Assert.That(context.GetProperty("page").GetString(), Is.EqualTo("forum"));
            Assert.That(context.GetProperty("forum").GetString(), Is.EqualTo("spanish"));
        });
    }

    [Test]
    public void BuildCanonicalContext_ListsEveryForumSoTheModelNeverInventsOne()
    {
        var context = Context("Which forums are there?");

        var codes = context.GetProperty("forums").EnumerateArray()
            .Select(forum => forum.GetProperty("code").GetString());

        Assert.That(codes, Is.EquivalentTo(new[] { "global", "spanish", "italian", "japanese" }));
    }

    /// <summary>
    /// Nothing about the forum's actual content is in the context, because the
    /// assistant explains the site rather than reading it.
    /// </summary>
    [Test]
    public void BuildCanonicalContext_CarriesNoPostContentEvenOnAPostPage()
    {
        var context = Context("What does this post say?", "post", postId: 7);

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
    public void GetContext_BuildsAFallbackFromTheSameTopicsItGivesTheModel()
    {
        var context = Retrieve("How do I delete a post?");

        Assert.Multiple(() =>
        {
            Assert.That(context.FallbackAnswer, Does.Contain("Deleting a post"));
            Assert.That(context.FallbackAnswer, Does.Contain("Only the author"));
        });
    }

    [Test]
    public void GetContext_WhenNothingMatches_FallsBackToWhatTheAssistantCanHelpWith()
    {
        var context = Retrieve("zzzz");

        Assert.Multiple(() =>
        {
            Assert.That(TopicTitles(JsonDocument.Parse(context.CanonicalContext).RootElement), Is.Empty);
            Assert.That(context.FallbackAnswer, Does.Contain("I can only help with"));
        });
    }
}
