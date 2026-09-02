using System.Net;
using System.Text;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using LanguageWise.ChatDiscussionService.Api.Options;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class OllamaAssistantClientTests
{
    private static readonly IReadOnlyList<AssistantChatMessage> Messages =
    [
        new("system", "You are the help assistant."),
        new("user", "How do I create a post?")
    ];

    private static OllamaAssistantClient Client(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://ollama:11434/") },
            // Fully qualified: 'Options' alone binds to the API's own Options namespace.
            Microsoft.Extensions.Options.Options.Create(new OllamaOptions
            {
                Model = "gemma4:e4b",
                MaxOutputTokens = 256,
                Temperature = 0.3,
                TopP = 0.9
            }));

    private static async Task<List<ProviderStreamEvent>> ReadAllAsync(AssistantCompletionStream stream)
    {
        var events = new List<ProviderStreamEvent>();

        await using (stream)
        {
            await foreach (var streamEvent in stream.ReadEventsAsync(CancellationToken.None))
            {
                events.Add(streamEvent);
            }
        }

        return events;
    }

    [Test]
    public async Task StartCompletionAsync_SendsTheModelAndMessagesAsAStreamingChatRequest()
    {
        var handler = new StubStreamHandler(Ndjson("Hello."));

        await using var stream = await Client(handler).StartCompletionAsync(Messages, CancellationToken.None);

        var body = JsonDocument.Parse(handler.LastRequestBody!).RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/chat"));
            Assert.That(body.GetProperty("model").GetString(), Is.EqualTo("gemma4:e4b"));
            Assert.That(body.GetProperty("stream").GetBoolean(), Is.True);
            Assert.That(body.GetProperty("messages").GetArrayLength(), Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Ollama's option names are snake_case, so a mapping slip here would be
    /// silently ignored by the model rather than reported.
    /// </summary>
    [Test]
    public async Task StartCompletionAsync_SendsTheModelOptionsUnderTheNamesOllamaExpects()
    {
        var handler = new StubStreamHandler(Ndjson("Hello."));

        await using var stream = await Client(handler).StartCompletionAsync(Messages, CancellationToken.None);

        var options = JsonDocument.Parse(handler.LastRequestBody!).RootElement.GetProperty("options");

        Assert.Multiple(() =>
        {
            Assert.That(options.GetProperty("num_predict").GetInt32(), Is.EqualTo(256));
            Assert.That(options.GetProperty("top_p").GetDouble(), Is.EqualTo(0.9));
            Assert.That(options.GetProperty("temperature").GetDouble(), Is.EqualTo(0.3));
        });
    }

    [Test]
    public async Task ReadEventsAsync_YieldsEachFragmentThenADoneEvent()
    {
        var stream = await Client(new StubStreamHandler(Ndjson("Select ", "New post.")))
            .StartCompletionAsync(Messages, CancellationToken.None);

        var events = await ReadAllAsync(stream);

        Assert.Multiple(() =>
        {
            Assert.That(
                events.Where(item => item.Type == "delta").Select(item => item.Content),
                Is.EqualTo(new[] { "Select ", "New post." }));
            Assert.That(events[^1].Type, Is.EqualTo("done"));
            Assert.That(events[^1].Reason, Is.EqualTo("stop"));
        });
    }

    /// <summary>
    /// A stream that simply stops has lost part of the answer. Reporting it means
    /// the browser can say so rather than presenting a truncated reply as final.
    /// </summary>
    [Test]
    public async Task ReadEventsAsync_WhenTheStreamEndsWithoutADoneChunk_Throws()
    {
        var stream = await Client(new StubStreamHandler(
                "{\"message\":{\"content\":\"Sel\"},\"done\":false}\n"))
            .StartCompletionAsync(Messages, CancellationToken.None);

        Assert.That(
            async () => await ReadAllAsync(stream),
            Throws.TypeOf<AssistantProviderStreamException>());
    }

    [Test]
    public async Task ReadEventsAsync_WithAnUnparseableChunk_Throws()
    {
        var stream = await Client(new StubStreamHandler("not json at all\n"))
            .StartCompletionAsync(Messages, CancellationToken.None);

        Assert.That(
            async () => await ReadAllAsync(stream),
            Throws.TypeOf<AssistantProviderStreamException>());
    }

    /// <summary>Ollama reports a mid-answer failure in the body, not the status code.</summary>
    [Test]
    public async Task ReadEventsAsync_WhenTheModelReportsAnErrorMidStream_Throws()
    {
        var stream = await Client(new StubStreamHandler(
                "{\"message\":{\"content\":\"Sel\"},\"done\":false}\n{\"error\":\"model runner has stopped\"}\n"))
            .StartCompletionAsync(Messages, CancellationToken.None);

        Assert.That(
            async () => await ReadAllAsync(stream),
            Throws.TypeOf<AssistantProviderStreamException>());
    }

    [Test]
    public void StartCompletionAsync_WhenTheModelIsNotInstalled_ThrowsWithTheStatus()
    {
        var handler = new StubStreamHandler(string.Empty, HttpStatusCode.NotFound);

        Assert.That(
            async () => await Client(handler).StartCompletionAsync(Messages, CancellationToken.None),
            Throws.TypeOf<AssistantProviderException>()
                .With.Property(nameof(AssistantProviderException.StatusCode))
                .EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public void StartCompletionAsync_WhenTheModelIsUnreachable_LetsTheFailureThrough()
    {
        Assert.That(
            async () => await Client(new UnreachableHandler())
                .StartCompletionAsync(Messages, CancellationToken.None),
            Throws.TypeOf<HttpRequestException>());
    }

    /// <summary>Ollama's wire format: one JSON object per line, the last one done.</summary>
    private static string Ndjson(params string[] fragments) =>
        string.Concat(fragments.Select(fragment =>
            $"{{\"message\":{{\"role\":\"assistant\",\"content\":{JsonSerializer.Serialize(fragment)}}},\"done\":false}}\n"))
        + "{\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done_reason\":\"stop\",\"done\":true}\n";

    /// <summary>Replays a canned chunk stream in place of a running model.</summary>
    private sealed class StubStreamHandler(
        string body,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        internal Uri? LastRequestUri { get; private set; }

        internal string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/x-ndjson"),
                RequestMessage = request
            };
        }
    }

    /// <summary>Stands in for the ollama container not running.</summary>
    private sealed class UnreachableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused.");
    }
}
