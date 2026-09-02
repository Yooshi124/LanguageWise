using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Services;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class AssistantPromptBuilderTests
{
    private static readonly AssistantPromptBuilder Builder = new();

    private static ValidatedAssistantRequest Request(
        IReadOnlyList<AssistantHistoryMessage>? history = null) =>
        new(
            "How do I edit it?",
            history ?? [],
            new AssistantRouteContext("forums", null, null));

    [Test]
    public void BuildMessages_PutsTheSystemPromptAndContextAheadOfTheConversation()
    {
        var messages = Builder.BuildMessages(Request(), "{\"page\":\"forums\"}");

        Assert.Multiple(() =>
        {
            Assert.That(messages[0].Role, Is.EqualTo("system"));
            Assert.That(messages[0].Content, Does.Contain("LanguageWise discussion forum"));
            Assert.That(messages[0].Content, Does.Contain("Garry"));
            Assert.That(messages[1].Role, Is.EqualTo("system"));
            Assert.That(messages[^1].Role, Is.EqualTo("user"));
            Assert.That(messages[^1].Content, Is.EqualTo("How do I edit it?"));
        });
    }

    /// <summary>
    /// The context is fenced so the model can tell server data from user text.
    /// Without the delimiters a question could impersonate the context itself.
    /// </summary>
    [Test]
    public void BuildMessages_FencesTheCanonicalContext()
    {
        var messages = Builder.BuildMessages(Request(), "{\"page\":\"forums\"}");

        Assert.Multiple(() =>
        {
            Assert.That(messages[1].Content, Does.Contain("<canonical_context>"));
            Assert.That(messages[1].Content, Does.Contain("{\"page\":\"forums\"}"));
            Assert.That(messages[1].Content, Does.Contain("</canonical_context>"));
        });
    }

    [Test]
    public void BuildMessages_ReplaysTheHistoryBetweenTheContextAndTheQuestion()
    {
        var history = new List<AssistantHistoryMessage>
        {
            new("user", "How do I create a post?"),
            new("assistant", "Select New post, then Publish.")
        };

        var messages = Builder.BuildMessages(Request(history), "{}");

        Assert.Multiple(() =>
        {
            Assert.That(messages, Has.Count.EqualTo(5));
            Assert.That(messages[2].Role, Is.EqualTo("user"));
            Assert.That(messages[2].Content, Is.EqualTo("How do I create a post?"));
            Assert.That(messages[3].Role, Is.EqualTo("assistant"));
        });
    }
}
