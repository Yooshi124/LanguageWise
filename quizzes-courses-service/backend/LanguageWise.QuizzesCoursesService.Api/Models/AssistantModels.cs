using System.Text.Json.Serialization;

namespace LanguageWise.QuizzesCoursesService.Api.Models;

public sealed record AssistantMessageRequest(
    string? Message,
    IReadOnlyList<AssistantHistoryMessage>? History,
    AssistantRouteContext? Context);

public sealed record AssistantHistoryMessage(string? Role, string? Content);

public sealed record AssistantRouteContext(
    string? RouteName,
    string? CourseCode,
    string? LessonSlug);

public sealed record AssistantDeltaEvent(string Content);

public sealed record AssistantDoneEvent(string Reason);

public sealed record AssistantErrorEvent(string Message, string Code);

public sealed record OpenRouterChatRequest(
    string Model,
    IReadOnlyList<OpenRouterChatMessage> Messages,
    bool Stream,
    [property: JsonPropertyName("max_tokens")] int MaxTokens);

public sealed record OpenRouterChatMessage(string Role, string Content);
