using System.Net;
using System.Text;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Tests;

public sealed class AssistantClientTests
{
    [Test]
    public async Task StartCompletion_WhenOpenRouterRejectsRequest_FallsBackToOllama()
    {
        var openRouterHandler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadGateway));
        var ollamaHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"message\":{\"content\":\"Local answer\"},\"done\":false}\n" +
                "{\"message\":{\"content\":\"\"},\"done\":true,\"done_reason\":\"stop\"}\n",
                Encoding.UTF8,
                "application/x-ndjson")
        });
        var openRouterOptions = Options.Create(new OpenRouterOptions
        {
            ApiKey = "configured"
        });
        var ollamaOptions = Options.Create(new OllamaOptions());
        var client = new FallbackAssistantCompletionClient(
            new OpenRouterAssistantClient(
                new HttpClient(openRouterHandler) { BaseAddress = new Uri("https://openrouter.test/") },
                openRouterOptions),
            new OllamaAssistantClient(
                new HttpClient(ollamaHandler) { BaseAddress = new Uri("http://ollama.test/") },
                ollamaOptions),
            openRouterOptions,
            NullLogger<FallbackAssistantCompletionClient>.Instance);

        await using var completion = await client.StartCompletionAsync(
            [new AssistantChatMessage("user", "Hello")],
            CancellationToken.None);
        var events = new List<ProviderStreamEvent>();
        await foreach (var streamEvent in completion.ReadEventsAsync(CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        Assert.Multiple(() =>
        {
            Assert.That(openRouterHandler.CallCount, Is.EqualTo(1));
            Assert.That(ollamaHandler.CallCount, Is.EqualTo(1));
            Assert.That(openRouterHandler.LastBody, Does.Contain("\"max_tokens\":1024"));
            Assert.That(openRouterHandler.LastBody, Does.Contain(
                $"\"model\":\"{OpenRouterAssistantClient.Model}\""));
            Assert.That(ollamaHandler.LastBody, Does.Not.Contain("\"options\""));
            Assert.That(ollamaHandler.LastBody, Does.Not.Contain("\"num_predict\""));
            Assert.That(events, Does.Contain(ProviderStreamEvent.Delta("Local answer")));
            Assert.That(events, Does.Contain(ProviderStreamEvent.Done()));
        });
    }

    [Test]
    public async Task StartCompletion_WithoutOpenRouterKey_UsesOnlyOllama()
    {
        var openRouterHandler = new RecordingHandler(_ =>
            throw new InvalidOperationException("OpenRouter should not be called."));
        var ollamaHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"message\":{\"content\":\"Local answer\"},\"done\":true}\n",
                Encoding.UTF8,
                "application/x-ndjson")
        });
        var openRouterOptions = Options.Create(new OpenRouterOptions());
        var client = new FallbackAssistantCompletionClient(
            new OpenRouterAssistantClient(
                new HttpClient(openRouterHandler) { BaseAddress = new Uri("https://openrouter.test/") },
                openRouterOptions),
            new OllamaAssistantClient(
                new HttpClient(ollamaHandler) { BaseAddress = new Uri("http://ollama.test/") },
                Options.Create(new OllamaOptions())),
            openRouterOptions,
            NullLogger<FallbackAssistantCompletionClient>.Instance);

        await using var completion = await client.StartCompletionAsync(
            [new AssistantChatMessage("user", "Hello")],
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(openRouterHandler.CallCount, Is.Zero);
            Assert.That(ollamaHandler.CallCount, Is.EqualTo(1));
        });
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        internal int CallCount { get; private set; }
        internal string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var response = responseFactory(request);
            response.RequestMessage = request;
            return response;
        }
    }
}