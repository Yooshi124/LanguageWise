using System.Text.Json;

namespace LanguageWise.GarryAiService.Api;

public sealed class GarryAgentPipeline(
    CompletionProviders providers,
    GarryToolLoop toolLoop,
    ILogger<GarryAgentPipeline> logger)
{
    public async Task<string> RunAsync(
        CompletionRequest request,
        string? userToken,
        AdminSwitchState switches,
        Func<ToolEvent, Task> onTool,
        CancellationToken cancellationToken)
    {
        var planner = new GarryAgent(request, GarryAgent.PlannerPrompt);
        var worker = new GarryAgent(request, GarryAgent.WorkerPrompt);
        var reviewer = new GarryAgent(request, GarryAgent.ReviewerPrompt);

        var plan = await planner.CompleteAsync(providers, cancellationToken);
        worker.Receive("Planner task plan", plan);

        WorkerCompletion? researched = null;
        if (switches.Mcp && request.ToolScope is not null && toolLoop.IsAvailable)
        {
            try
            {
                researched = await toolLoop.RunAsync(request, worker.Messages, userToken, onTool, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or ProviderException
                or JsonException or InvalidDataException or IOException or ModelContextProtocol.McpException or TimeoutException
                || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                logger.LogWarning("Worker tool loop failed; continuing worker without tools: {ErrorType}", exception.GetType().Name);
                worker.Receive("Tool availability", "Live tools are unavailable. Do not claim to have verified live data.");
            }
        }

        var draft = researched?.Content ?? await worker.CompleteAsync(providers, cancellationToken);
        reviewer.Receive("Planner task plan", plan);
        reviewer.Receive("Worker draft", draft);
        if (researched is not null)
        {
            foreach (var evidence in researched.Evidence)
            {
                reviewer.Receive("Worker tool evidence", evidence.Content);
            }
        }
        var answer = await reviewer.CompleteAsync(providers, cancellationToken);
        return researched?.Citations.AppendTo(answer) ?? answer;
    }
}
