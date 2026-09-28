using System.Net;
using System.Text;
using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;
using Microsoft.AspNetCore.Http;

namespace LanguageWise.QuizzesCoursesService.Api.Tests;

public sealed class GarryCompletionClientTests
{
    [Test]
    public async Task Completion_ForwardsVerifiedUserAndDomainMessages()
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
        context.Request.Headers.Cookie = "token=other-user-token";
        var adapter = new GarryCompletionClient(client, new HttpContextAccessor { HttpContext = context });

        await using var stream = await adapter.StartCompletionAsync(
            [new OpenRouterChatMessage("system", "course rules"),
                new OpenRouterChatMessage("system", "canonical catalog"),
                new OpenRouterChatMessage("assistant", "Earlier"),
                new OpenRouterChatMessage("user", "Explain this")], CancellationToken.None);
        var events = new List<ProviderStreamEvent>();
        await foreach (var streamEvent in stream.ReadEventsAsync(CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.EqualTo("signed-user-token"));
            Assert.That(payload?.GetProperty("domainRules").GetString(), Is.EqualTo("course rules"));
            Assert.That(payload?.GetProperty("canonicalContext").GetString(), Is.EqualTo("canonical catalog"));
            Assert.That(payload?.GetProperty("history").GetArrayLength(), Is.EqualTo(1));
            Assert.That(payload?.GetProperty("message").GetString(), Is.EqualTo("Explain this"));
            Assert.That(events.Select(item => item.Type), Is.EqualTo(new[] { "delta", "done" }));
        });
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}