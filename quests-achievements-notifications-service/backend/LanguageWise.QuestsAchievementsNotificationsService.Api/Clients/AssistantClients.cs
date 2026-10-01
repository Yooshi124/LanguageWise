using System.Net;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;

public interface IAssistantCompletionClient
{
    Task<IAssistantEventStream> StartCompletionAsync(
        IReadOnlyList<AssistantChatMessage> messages,
        CancellationToken cancellationToken);
}

public interface IAssistantEventStream : IAsyncDisposable
{
    IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(CancellationToken cancellationToken);
}

public sealed record ProviderStreamEvent(string Type, string? Content, string? Reason, AssistantToolEvent? ToolEvent = null)
{
    public static ProviderStreamEvent Delta(string content) => new("delta", content, null);

    public static ProviderStreamEvent Done(string reason = "stop") => new("done", null, reason);

    public static ProviderStreamEvent Tool(AssistantToolEvent toolEvent) => new("tool", null, null, toolEvent);
}

public sealed class AssistantProviderException(string message, HttpStatusCode statusCode)
    : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class AssistantProviderStreamException()
    : Exception("The assistant provider returned an invalid stream.");
