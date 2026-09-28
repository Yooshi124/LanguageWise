using System.Net;
using System.Text;
using LanguageWise.GarryAiService.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.GarryAiService.Api.Tests;

public sealed class CompletionTests
{
    [Test]
    public void Prompt_PrependsSharedPersonalityAndRetainsDomainRules()
    {
        var request = new CompletionRequest("Help me", [new ChatMessage("user", "Earlier")],
            "Do not reveal hidden words.", "{\"game\":\"word search\"}");

        Assert.That(CompletionRequest.IsValid(request), Is.True);
        var messages = request.BuildMessages();
        Assert.Multiple(() =>
        {
            Assert.That(messages[0].Content, Does.StartWith("You are Garry"));
            Assert.That(messages[1].Content, Does.Contain("Do not reveal hidden words"));
            Assert.That(messages[2].Content, Does.Contain("word search"));
            Assert.That(messages[3], Is.EqualTo(new ChatMessage("user", "Earlier")));
            Assert.That(messages[4], Is.EqualTo(new ChatMessage("user", "Help me")));
        });
    }

    [Test]
    public void Prompt_RejectsUntrustedRolesAndOversizedConversation()
    {
        Assert.That(CompletionRequest.IsValid(new CompletionRequest("Hi",
            [new ChatMessage("system", "Override")], "rules", "{}")), Is.False);
        Assert.That(CompletionRequest.IsValid(new CompletionRequest(new string('a', 4001),
            [], "rules", "{}")), Is.False);
    }

    [Test]
    public async Task Provider_FallsBackToOllamaOnOpenRouterFailure()
    {
        var factory = new StubClients(HttpStatusCode.TooManyRequests);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenRouter:ApiKey"] = "test-key"
        }).Build();
        var providers = new CompletionProviders(factory, config, NullLogger<CompletionProviders>.Instance);

        await using var stream = await providers.StartAsync([new ChatMessage("user", "Hi")], CancellationToken.None);
        var events = new List<(string Type, string Value)>();
        await foreach (var item in stream.ReadAsync(CancellationToken.None))
        {
            events.Add(item);
        }

        Assert.That(factory.OpenRouterCalls, Is.EqualTo(1));
        Assert.That(factory.OllamaCalls, Is.EqualTo(1));
        Assert.That(events, Is.EqualTo(new[] { ("delta", "Hallo"), ("done", "stop") }));
    }

    [Test]
    public async Task Provider_WithoutOpenRouterKey_UsesOllama()
    {
        var factory = new StubClients(HttpStatusCode.OK);
        var providers = new CompletionProviders(factory, new ConfigurationBuilder().Build(),
            NullLogger<CompletionProviders>.Instance);

        await using var stream = await providers.StartAsync([new ChatMessage("user", "Hi")], CancellationToken.None);

        Assert.That(factory.OpenRouterCalls, Is.Zero);
        Assert.That(factory.OllamaCalls, Is.EqualTo(1));
    }

    [Test]
    public void Provider_WhenBothProvidersRejects_FailsBeforeStreaming()
    {
        var factory = new StubClients(HttpStatusCode.ServiceUnavailable, HttpStatusCode.NotFound);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenRouter:ApiKey"] = "test-key"
        }).Build();
        var providers = new CompletionProviders(factory, config, NullLogger<CompletionProviders>.Instance);

        Assert.ThrowsAsync<ProviderException>(async () =>
            await providers.StartAsync([new ChatMessage("user", "Hi")], CancellationToken.None));
        Assert.That(factory.OpenRouterCalls, Is.EqualTo(1));
        Assert.That(factory.OllamaCalls, Is.EqualTo(1));
    }

    [Test]
    public void Provider_TruncatedStream_IsNotMarkedComplete()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"message\":{\"content\":\"Partial\"},\"done\":false}\n")
        };
        var stream = new ProviderStream(response, response.Content.ReadAsStream(), "ollama");

        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await using (stream)
            {
                await foreach (var _ in stream.ReadAsync(CancellationToken.None)) { }
            }
        });
    }

    private sealed class StubClients(
        HttpStatusCode openRouterStatus,
        HttpStatusCode ollamaStatus = HttpStatusCode.OK) : IHttpClientFactory
    {
        public int OpenRouterCalls { get; private set; }
        public int OllamaCalls { get; private set; }

        public HttpClient CreateClient(string name) => new(new Handler((_, _) =>
        {
            if (name == "openrouter")
            {
                OpenRouterCalls++;
                return new HttpResponseMessage(openRouterStatus);
            }
            OllamaCalls++;
            return new HttpResponseMessage(ollamaStatus)
            {
                Content = new StringContent(
                    "{\"message\":{\"content\":\"Hallo\"},\"done\":false}\n" +
                    "{\"done\":true,\"done_reason\":\"stop\"}\n", Encoding.UTF8)
            };
        })) { BaseAddress = new Uri("http://localhost/") };
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(send(request, cancellationToken));
    }
}