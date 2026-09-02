using System.Runtime.CompilerServices;
using LanguageWise.ChatDiscussionService.Api.Clients;

namespace LanguageWise.ChatDiscussionService.Api.Services;

/// <summary>
/// AI mode without a model behind it. The help topics that matched the question
/// are already a usable answer, so when Ollama cannot be reached they are served
/// verbatim instead of the request failing.
///
/// It is an <see cref="IAssistantEventStream"/> so it leaves through the same SSE
/// relay a real answer does — the browser sees one delta and a done event, with
/// the reason set to 'fallback' so it can say where the answer came from.
/// </summary>
public sealed class HelpTextEventStream(string helpText) : IAssistantEventStream
{
    public async IAsyncEnumerable<ProviderStreamEvent> ReadEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        yield return ProviderStreamEvent.Delta(helpText);
        yield return ProviderStreamEvent.Done(AssistantCompletionStream.FallbackReason);

        await Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
