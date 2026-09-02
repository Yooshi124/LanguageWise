using System.Text;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

/// <summary>
/// A stand-in for Ollama, so the endpoint tests never wait on a language model.
/// It replays a canned model stream, which means the endpoint's own server-sent
/// event relay is still exercised end to end.
///
/// The model request itself is covered by <see cref="OllamaAssistantClientTests"/>.
/// </summary>
internal sealed class StubAssistantCompletionClient : IAssistantCompletionClient
{
    /// <summary>The two fragments <see cref="DefaultStream"/> streams, joined up.</summary>
    internal const string Answer = "Select the New post button, then Publish.";

    /// <summary>Ollama's wire format: newline-delimited JSON, the last one done.</summary>
    private static readonly string DefaultStream = string.Concat(
        "{\"message\":{\"role\":\"assistant\",\"content\":\"Select the New post button, \"},\"done\":false}\n",
        "{\"message\":{\"role\":\"assistant\",\"content\":\"then Publish.\"},\"done\":false}\n",
        "{\"message\":{\"role\":\"assistant\",\"content\":\"\"},\"done_reason\":\"stop\",\"done\":true}\n");

    private readonly string providerStream;

    internal StubAssistantCompletionClient(string? providerStream = null) =>
        this.providerStream = providerStream ?? DefaultStream;

    internal IReadOnlyList<AssistantChatMessage> LastMessages { get; private set; } = [];

    internal int CallCount { get; private set; }

    public Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken)
    {
        LastMessages = messages;
        CallCount++;

        return Task.FromResult(new AssistantCompletionStream(
            new HttpResponseMessage(),
            new MemoryStream(Encoding.UTF8.GetBytes(providerStream))));
    }
}

/// <summary>Refuses every request, so the endpoint's model failure handling can be exercised.</summary>
internal sealed class FailingAssistantCompletionClient(System.Net.HttpStatusCode statusCode)
    : IAssistantCompletionClient
{
    public Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken) =>
        throw new AssistantProviderException("The model said no.", statusCode);
}

/// <summary>Stands in for Ollama not running at all.</summary>
internal sealed class UnreachableAssistantCompletionClient : IAssistantCompletionClient
{
    public Task<AssistantCompletionStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken) =>
        throw new HttpRequestException("The assistant model is not running.");
}
