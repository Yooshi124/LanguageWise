using System.Net;
using System.Text;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;
using LanguageWise.QuizzesCoursesService.Api.Options;
using Microsoft.Extensions.Options;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

[TestFixture]
public sealed class OpenRouterAssistantClientTests
{
    [Test]
    public async Task StartCompletionAsync_ConstructsStreamingChatRequest()
    {
        const string sse = "data: [DONE]\n\n";
        using var handler = CreateSseHandler(sse);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await using var completion = await client.StartCompletionAsync(
            [new OpenRouterChatMessage("user", "Hallo")],
            CancellationToken.None);
        using var requestBody = JsonDocument.Parse(handler.LastRequestBody!);
        var root = requestBody.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/v1/chat/completions"));
            Assert.That(handler.LastAuthorizationScheme, Is.EqualTo("Bearer"));
            Assert.That(handler.LastAuthorizationParameter, Is.EqualTo("test-key"));
            Assert.That(handler.LastAcceptMediaTypes, Does.Contain("text/event-stream"));
            Assert.That(root.GetProperty("model").GetString(), Is.EqualTo("test-model"));
            Assert.That(root.GetProperty("stream").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("max_tokens").GetInt32(), Is.EqualTo(321));
            Assert.That(root.GetProperty("messages")[0].GetProperty("content").GetString(), Is.EqualTo("Hallo"));
        });
    }

    [Test]
    public async Task ReadEventsAsync_ParsesDeltasAndDoneAcrossSplitChunks()
    {
        const string sse =
            "data: {\"choices\":[{\"delta\":{\"content\":\"Hal\"},\"finish_reason\":null}]}\n\n" +
            "data: {\"choices\":[{\"delta\":{\"content\":\"lo\"},\"finish_reason\":null}]}\n\n" +
            "data: [DONE]\n\n";
        using var handler = CreateSseHandler(sse, chunkSize: 3);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);
        await using var completion = await client.StartCompletionAsync(
            [new OpenRouterChatMessage("user", "Hello")],
            CancellationToken.None);

        var events = await completion.ReadEventsAsync(CancellationToken.None).ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(events.Select(item => item.Type), Is.EqualTo(new[] { "delta", "delta", "done" }));
            Assert.That(events[0].Content, Is.EqualTo("Hal"));
            Assert.That(events[1].Content, Is.EqualTo("lo"));
        });
    }

    [Test]
    public void StartCompletionAsync_ThrowsSanitizedExceptionForProviderHttpError()
    {
        using var handler = new StubHttpMessageHandler(
            HttpStatusCode.Unauthorized,
            """{"error":{"message":"raw provider diagnostic"}}""");
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = Assert.ThrowsAsync<AssistantProviderException>(async () =>
            await client.StartCompletionAsync(
                [new OpenRouterChatMessage("user", "Hello")],
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(exception.Message, Does.Not.Contain("raw provider diagnostic"));
        });
    }

    [Test]
    public async Task StartCompletionAsync_RetriesOneRateLimitedRequest()
    {
        var requestCount = 0;
        using var handler = new StubHttpMessageHandler((request, _) =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                var rateLimited = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    RequestMessage = request
                };
                rateLimited.Headers.RetryAfter =
                    new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
                return Task.FromResult(rateLimited);
            }

            var stream = new MemoryStream(Encoding.UTF8.GetBytes("data: [DONE]\n\n"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
                RequestMessage = request
            });
        });
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        await using var completion = await client.StartCompletionAsync(
            [new OpenRouterChatMessage("user", "Hello")],
            CancellationToken.None);

        Assert.That(requestCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ReadEventsAsync_RejectsMalformedOrTruncatedStream()
    {
        const string sse = "data: {not-json}\n\n";
        using var handler = CreateSseHandler(sse);
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);
        await using var completion = await client.StartCompletionAsync(
            [new OpenRouterChatMessage("user", "Hello")],
            CancellationToken.None);

        Assert.ThrowsAsync<AssistantProviderStreamException>(async () =>
            await completion.ReadEventsAsync(CancellationToken.None).ToListAsync());
    }

    private static OpenRouterAssistantClient CreateClient(HttpClient httpClient) =>
        new(httpClient, Microsoft.Extensions.Options.Options.Create(new OpenRouterOptions
        {
            ApiKey = "test-key",
            BaseUrl = "https://openrouter.test/api/v1/",
            Model = "test-model",
            MaxOutputTokens = 321
        }));

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("https://openrouter.test/api/v1/") };

    private static StubHttpMessageHandler CreateSseHandler(string content, int? chunkSize = null) =>
        new((request, _) =>
        {
            Stream stream = chunkSize is null
                ? new MemoryStream(Encoding.UTF8.GetBytes(content))
                : new ChunkedReadStream(Encoding.UTF8.GetBytes(content), chunkSize.Value);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(stream),
                RequestMessage = request
            };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");
            return Task.FromResult(response);
        });

    private sealed class ChunkedReadStream(byte[] bytes, int chunkSize) : MemoryStream(bytes)
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            base.Read(buffer, offset, Math.Min(count, chunkSize));

        public override int Read(Span<byte> buffer) =>
            base.Read(buffer[..Math.Min(buffer.Length, chunkSize)]);

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, chunkSize)], cancellationToken);
    }
}
