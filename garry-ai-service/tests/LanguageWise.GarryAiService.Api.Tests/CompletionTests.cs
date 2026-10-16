using System.Net;
using System.Text;
using LanguageWise.GarryAiService.Api;
using Microsoft.Extensions.Configuration;

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
    public void Provider_WhenOpenRouterRejects_DoesNotTryAnotherProvider()
    {
        var factory = new StubClients(HttpStatusCode.TooManyRequests);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenRouter:ApiKey"] = "test-key"
        }).Build();
        var providers = new CompletionProviders(factory, config);

        Assert.ThrowsAsync<ProviderException>(async () =>
            await providers.StartAsync([new ChatMessage("user", "Hi")], CancellationToken.None));
        Assert.That(factory.OpenRouterCalls, Is.EqualTo(1));
    }

    [Test]
    public void Provider_WithoutOpenRouterKey_RejectsWithoutContactingProvider()
    {
        var factory = new StubClients(HttpStatusCode.OK);
        var providers = new CompletionProviders(factory, new ConfigurationBuilder().Build());

        Assert.ThrowsAsync<ProviderException>(async () =>
            await providers.StartAsync([new ChatMessage("user", "Hi")], CancellationToken.None));

        Assert.That(factory.OpenRouterCalls, Is.Zero);
    }

    [Test]
    public void Provider_TruncatedStream_IsNotMarkedComplete()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("data: {\"choices\":[{\"delta\":{\"content\":\"Partial\"}}]}\n\n")
        };
        var stream = new ProviderStream(response, response.Content.ReadAsStream());

        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await using (stream)
            {
                await foreach (var _ in stream.ReadAsync(CancellationToken.None)) { }
            }
        });
    }

    private sealed class StubClients(HttpStatusCode openRouterStatus) : IHttpClientFactory
    {
        public int OpenRouterCalls { get; private set; }

        public HttpClient CreateClient(string name) => new(new Handler((_, _) =>
        {
            Assert.That(name, Is.EqualTo("openrouter"));
            OpenRouterCalls++;
            return new HttpResponseMessage(openRouterStatus)
            {
                Content = new StringContent(
                    "data: {\"choices\":[{\"delta\":{\"content\":\"Hallo\"}}]}\n\n" +
                    "data: {\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n", Encoding.UTF8)
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