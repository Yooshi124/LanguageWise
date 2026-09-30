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
	public async Task Completion_ForwardsChatScopeAndConsumesInternalToolEvents()
	{
		var handler = new GarryHandler();
		var context = new DefaultHttpContext();
		context.Request.Headers.Authorization = "Bearer test-token";
		var client = new HttpClient(handler) { BaseAddress = new Uri("http://garry.test/") };
		var completionClient = new GarryCompletionClient(client, new HttpContextAccessor { HttpContext = context });

		await using var stream = await completionClient.StartCompletionAsync(
			[new AssistantChatMessage("system", "rules"), new AssistantChatMessage("system", "context"), new AssistantChatMessage("user", "Find posts")],
			CancellationToken.None);
		var events = new List<ProviderStreamEvent>();
		await foreach (var streamEvent in stream.ReadEventsAsync(CancellationToken.None))
		{
			events.Add(streamEvent);
		}

		using var payload = JsonDocument.Parse(handler.RequestContent!);
		Assert.Multiple(() =>
		{
			Assert.That(payload.RootElement.GetProperty("toolScope").GetString(), Is.EqualTo("chat"));
			Assert.That(handler.Authorization, Is.EqualTo("Bearer test-token"));
			Assert.That(events.Select(item => item.Type), Is.EqualTo(new[] { "delta", "done" }));
			Assert.That(events[0].Content, Is.EqualTo("Found a post."));
		});
	}

	private sealed class GarryHandler : HttpMessageHandler
	{
		public string? RequestContent { get; private set; }
		public string? Authorization { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			RequestContent = await request.Content!.ReadAsStringAsync(cancellationToken);
			Authorization = request.Headers.Authorization?.ToString();
			const string body =
				"event: tool\ndata: {\"name\":\"chat_list_forums\",\"arguments\":{},\"isError\":false,\"result\":{}}\n\n" +
				"event: delta\ndata: {\"content\":\"Found a post.\"}\n\n" +
				"event: done\ndata: {\"reason\":\"stop\"}\n\n";
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(body)))
			};
		}
	}
}