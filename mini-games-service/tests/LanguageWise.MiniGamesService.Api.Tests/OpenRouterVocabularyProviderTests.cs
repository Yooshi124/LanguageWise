using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.MiniGamesService.Api.Tests;

public sealed class OpenRouterVocabularyProviderTests
{
    private const string WordSearchJson = """
        {"groups":[{"title":"Animals","words":[
          {"word":"Hund","definition":"dog"},
          {"word":"Katze","definition":"cat"},
          {"word":"Vogel","definition":"bird"},
          {"word":"Pferd","definition":"horse"}
        ]}]}
        """;

    [Test]
    public async Task GenerateGroupsAsync_ParsesThemedWordsWithDefinitions()
    {
        var provider = CreateProvider(WordSearchJson);

        var groups = await provider.GenerateGroupsAsync("word_search", "German");

        var group = groups.Single();
        Assert.Multiple(() =>
        {
            Assert.That(group.Title, Is.EqualTo("Animals"));
            Assert.That(group.Words.Select(entry => entry.Word), Is.EquivalentTo(new[] { "HUND", "KATZE", "VOGEL", "PFERD" }));
            Assert.That(group.Words.First(entry => entry.Word == "HUND").Definition, Is.EqualTo("dog"));
        });
    }

    [Test]
    public async Task GenerateGroupsAsync_NormalisesWordsThroughThePlayableFilter()
    {
        // "ab" is too short, "super long phrase" is a phrase whose tokens are dropped as too long/shared.
        const string json = """
            {"groups":[{"title":"Mixed","words":[
              {"word":"ab","definition":"from"},
              {"word":"Haus","definition":"house"},
              {"word":"Baum","definition":"tree"},
              {"word":"Garten","definition":"garden"},
              {"word":"Stuhl","definition":"chair"}
            ]}]}
            """;
        var provider = CreateProvider(json);

        var groups = await provider.GenerateGroupsAsync("word_search", "German");

        Assert.That(groups.Single().Words.Select(entry => entry.Word), Is.EquivalentTo(new[] { "HAUS", "BAUM", "GARTEN", "STUHL" }));
    }

    [Test]
    public async Task GenerateGroupsAsync_ExtractsJsonFromProse()
    {
        var provider = CreateProvider($"Here is your list:\n{WordSearchJson}\nHope that helps!");

        var groups = await provider.GenerateGroupsAsync("word_search", "German");

        Assert.That(groups.Single().Words, Is.Not.Empty);
    }

    [Test]
    public async Task GenerateGroupsAsync_RetriesOnceOnInvalidJson()
    {
        var client = new StubVocabularyCompletionClient("not json at all", WordSearchJson);
        var provider = new OpenRouterVocabularyProvider(client, NullLogger<OpenRouterVocabularyProvider>.Instance);

        var groups = await provider.GenerateGroupsAsync("word_search", "German");

        Assert.Multiple(() =>
        {
            Assert.That(groups.Single().Words, Is.Not.Empty);
            Assert.That(client.CallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void GenerateGroupsAsync_ThrowsAfterRepeatedInvalidJson()
    {
        var client = new StubVocabularyCompletionClient("garbage", "still garbage");
        var provider = new OpenRouterVocabularyProvider(client, NullLogger<OpenRouterVocabularyProvider>.Instance);

        Assert.ThrowsAsync<AiVocabularyUnavailableException>(
            () => provider.GenerateGroupsAsync("word_search", "German"));
    }

    [Test]
    public void GenerateGroupsAsync_WhenProviderFails_ThrowsAiUnavailable()
    {
        var client = new StubVocabularyCompletionClient(new HttpRequestException("down"));
        var provider = new OpenRouterVocabularyProvider(client, NullLogger<OpenRouterVocabularyProvider>.Instance);

        Assert.ThrowsAsync<AiVocabularyUnavailableException>(
            () => provider.GenerateGroupsAsync("word_search", "German"));
    }

    [Test]
    public async Task GenerateGroupsAsync_UsesPerGameTokenBudget()
    {
        var client = new StubVocabularyCompletionClient("""
            {"groups":[{"title":"Five-letter word","words":[{"word":"Hunde","definition":"dogs"}]}]}
            """);
        var provider = new OpenRouterVocabularyProvider(client, NullLogger<OpenRouterVocabularyProvider>.Instance);

        await provider.GenerateGroupsAsync("guess_the_word", "German");

        // Guess the Word needs only a single word, so it gets the smallest (fastest) budget.
        Assert.That(client.LastMaxTokens, Is.EqualTo(96));
    }

    [Test]
    public async Task GenerateGroupsAsync_PromptContainsTheResolvedLanguageName()
    {
        var client = new StubVocabularyCompletionClient(WordSearchJson);
        var provider = new OpenRouterVocabularyProvider(client, NullLogger<OpenRouterVocabularyProvider>.Instance);

        await provider.GenerateGroupsAsync("word_search", "German");

        Assert.That(client.LastMessages.Last().Content, Does.Contain("German"));
    }

    [Test]
    public void GenerateGroupsAsync_UnknownGameKind_Throws()
    {
        var provider = CreateProvider(WordSearchJson);

        Assert.ThrowsAsync<ArgumentException>(
            () => provider.GenerateGroupsAsync("unknown_game", "German"));
    }

    private static OpenRouterVocabularyProvider CreateProvider(string response) =>
        new(new StubVocabularyCompletionClient(response), NullLogger<OpenRouterVocabularyProvider>.Instance);

    /// <summary>Returns queued responses (or throws) in order, so retry behaviour can be asserted.</summary>
    private sealed class StubVocabularyCompletionClient : IVocabularyCompletionClient
    {
        private readonly Queue<object> outcomes;

        public StubVocabularyCompletionClient(params object[] outcomes) =>
            this.outcomes = new Queue<object>(outcomes);

        public int CallCount { get; private set; }

        public int LastMaxTokens { get; private set; }

        public IReadOnlyList<OpenRouterChatMessage> LastMessages { get; private set; } = [];

        public Task<string> CompleteAsync(IReadOnlyList<OpenRouterChatMessage> messages, int maxTokens, double? temperature = null, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastMaxTokens = maxTokens;
            LastMessages = messages;
            var outcome = outcomes.Count > 0 ? outcomes.Dequeue() : outcomes.Peek();
            if (outcome is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((string)outcome);
        }
    }
}
