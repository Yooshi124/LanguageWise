using System.Text;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class AssistantSseResultTests
{
    [Test]
    public async Task ExecuteAsync_RelaysGarryToolEventsBeforeTheAnswer()
    {
        const string garryStream =
            "event: tool\ndata: {\"name\":\"chat_get_post\",\"arguments\":{\"postId\":7},\"isError\":false,\"result\":{\"postId\":7}}\n\n" +
            "event: delta\ndata: {\"content\":\"Here it is.\"}\n\n" +
            "event: done\ndata: {\"reason\":\"stop\"}\n\n";
        var completion = new AssistantCompletionStream(
            new HttpResponseMessage(),
            new MemoryStream(Encoding.UTF8.GetBytes(garryStream)),
            fromGarry: true);
        var context = new DefaultHttpContext();
        using var body = new MemoryStream();
        context.Response.Body = body;

        await new AssistantSseResult(completion, NullLogger<AssistantSseResult>.Instance).ExecuteAsync(context);

        var written = Encoding.UTF8.GetString(body.ToArray());
        Assert.That(written, Is.EqualTo(
            "event: tool\ndata: {\"name\":\"chat_get_post\",\"arguments\":{\"postId\":7},\"isError\":false,\"result\":{\"postId\":7}}\n\n" +
            "event: delta\ndata: {\"content\":\"Here it is.\"}\n\n" +
            "event: done\ndata: {\"reason\":\"stop\"}\n\n"));
    }
}
