namespace LanguageWise.LeaderboardAnalyticsService.Api.Models;

public sealed record AssistantMessageRequest(
    string? Message,
    IReadOnlyList<AssistantHistoryMessage>? History,
    AssistantRouteContext? Context);

public sealed record AssistantHistoryMessage(string? Role, string? Content);

public sealed record AssistantRouteContext(string? RouteName);

public sealed record AssistantDeltaEvent(string Content);

public sealed record AssistantDoneEvent(string Reason);

public sealed record AssistantErrorEvent(string Message, string Code);

public sealed record AssistantChatMessage(string Role, string Content);

public sealed record ValidatedAssistantRequest(
    string Message,
    IReadOnlyList<AssistantHistoryMessage> History,
    AssistantRouteContext Context);

public sealed record AssistantValidationResult(
    IReadOnlyDictionary<string, string[]> Errors,
    ValidatedAssistantRequest? Request);

// The canonical analytics profile handed to Garry. Only server-controlled data
// is included so the model can ground answers about the learner.
public sealed record AnalyticsProfile(
    string Username,
    IReadOnlyList<LanguageRanking> Rankings,
    LessonsCompletedResponse LessonsCompleted);
