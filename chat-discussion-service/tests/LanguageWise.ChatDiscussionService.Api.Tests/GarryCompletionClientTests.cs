using System.Net;
using System.Text;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;
using Microsoft.AspNetCore.Http;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class GarryCompletionClientTests
{
    [Test]
    public async Task Completion_WhenMcpDisabled_SendsNoToolScope()
    {
        string? token = null;
        JsonElement? payload = null;
        using var client = new HttpClient(new Handler(async (request, cancellationToken) =>
        {
            token = request.Headers.Authorization?.Parameter;
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            payload = document.RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "event: delta\ndata: {\"content\":\"Hello\"}\n\n" +
                    "event: done\ndata: {\"reason\":\"stop\"}\n\n", Encoding.UTF8, "text/event-stream")
            };
        })) { BaseAddress = new Uri("http://garry/") };
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer signed-user-token";
        var adapter = new GarryCompletionClient(client, new HttpContextAccessor { HttpContext = context }, new FakeMcpTools(false));

        await using var stream = await adapter.StartCompletionAsync(
            [new AssistantChatMessage("system", "rules"),
                new AssistantChatMessage("system", "context"),
                new AssistantChatMessage("user", "How do I post?")], CancellationToken.None);
        var events = new List<ProviderStreamEvent>();
        await foreach (var streamEvent in stream.ReadEventsAsync(CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.EqualTo("signed-user-token"));
            Assert.That(payload?.GetProperty("toolScope").ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(events.Select(item => item.Type), Is.EqualTo(new[] { "delta", "done" }));
        });
    }

    [Test]
    public async Task Completion_WhenMcpEnabled_SendsChatScopeAndParsesToolEvents()
    {
        JsonElement? payload = null;
        using var client = new HttpClient(new Handler(async (request, cancellationToken) =>
        {
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            payload = document.RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "event: tool\ndata: {\"name\":\"chat_list_forums\",\"arguments\":{},\"isError\":false,\"result\":{\"forums\":[{\"code\":\"italian\",\"name\":\"Italian\"}]}}\n\n" +
                    "event: delta\ndata: {\"content\":\"Found a forum.\"}\n\n" +
                    "event: done\ndata: {\"reason\":\"stop\"}\n\n", Encoding.UTF8, "text/event-stream")
            };
        })) { BaseAddress = new Uri("http://garry/") };
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer signed-user-token";
        var adapter = new GarryCompletionClient(client, new HttpContextAccessor { HttpContext = context }, new FakeMcpTools(true));

        await using var stream = await adapter.StartCompletionAsync(
            [new AssistantChatMessage("system", "rules"),
                new AssistantChatMessage("system", "context"),
                new AssistantChatMessage("user", "Which forums are there?")], CancellationToken.None);
        var events = new List<ProviderStreamEvent>();
        await foreach (var streamEvent in stream.ReadEventsAsync(CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        Assert.Multiple(() =>
        {
            Assert.That(payload?.GetProperty("toolScope").GetString(), Is.EqualTo("chat"));
            Assert.That(events.Select(item => item.Type), Is.EqualTo(new[] { "tool", "delta", "done" }));
            Assert.That(events[0].ToolEvent?.Name, Is.EqualTo("chat_list_forums"));
            Assert.That(events[0].ToolEvent?.IsError, Is.False);
            Assert.That(events[0].ToolEvent?.Result?.GetProperty("forums")[0].GetProperty("code").GetString(), Is.EqualTo("italian"));
        });
    }

    [Test]
    public async Task Completion_WithMalformedToolEvent_ThrowsStreamException()
    {
        using var client = new HttpClient(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("event: tool\ndata: {\"isError\":false}\n\n", Encoding.UTF8, "text/event-stream")
        }))) { BaseAddress = new Uri("http://garry/") };
        var adapter = new GarryCompletionClient(client, new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, new FakeMcpTools(true));

        await using var stream = await adapter.StartCompletionAsync(
            [new AssistantChatMessage("system", "rules"),
                new AssistantChatMessage("system", "context"),
                new AssistantChatMessage("user", "Hi")], CancellationToken.None);

        Assert.ThrowsAsync<AssistantProviderStreamException>(async () =>
        {
            await foreach (var _ in stream.ReadEventsAsync(CancellationToken.None))
            {
            }
        });
    }

    private sealed class FakeMcpTools(bool enabled) : IMcpToolClient
    {
        public bool Enabled => enabled;

        public Task<IReadOnlyList<McpToolInfo>> ListToolsAsync(string? userToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<McpToolCallResult> CallToolAsync(
            string name,
            IReadOnlyDictionary<string, JsonElement> arguments,
            string? userToken,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
