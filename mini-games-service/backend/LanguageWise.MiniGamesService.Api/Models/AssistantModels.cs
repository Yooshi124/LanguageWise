namespace LanguageWise.MiniGamesService.Api.Models;

public sealed record AssistantMessageRequest(
    string? Message,
    IReadOnlyList<AssistantHistoryMessage>? History,
    AssistantRouteContext? Context);

public sealed record AssistantHistoryMessage(string? Role, string? Content);

public sealed record AssistantRouteContext(
    string? RouteName,
    string? CourseCode,
    string? Mode);

public sealed record AssistantDeltaEvent(string Content);

public sealed record AssistantDoneEvent(string Reason);

public sealed record AssistantErrorEvent(string Message, string Code);

public sealed record AssistantToolEvent(
    string Name,
    System.Text.Json.JsonElement? Arguments,
    bool IsError,
    System.Text.Json.JsonElement? Result);

public sealed record AssistantToolsResponse(IReadOnlyList<AssistantToolDescriptor> Tools);

public sealed record AssistantToolDescriptor(string Name, string Title, string Description);

public sealed record AssistantToolCallResponse(string Tool, bool IsError, System.Text.Json.JsonElement Result);
