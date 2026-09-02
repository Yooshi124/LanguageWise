using System.Text.Json.Serialization;

namespace LanguageWise.ChatDiscussionService.Api.Models;

// ---------------------------------------------------------------------------
// AI mode: a help assistant that answers questions about how the forum works.
//
// Answers are streamed back as server-sent events, and grounded in
// HelpKnowledgeBase rather than the model's own recollection, so the assistant
// describes the buttons this site actually has.
// ---------------------------------------------------------------------------

/// <summary>One earlier exchange, replayed so follow-up questions keep their context.</summary>
public sealed record AssistantHistoryMessage(string? Role, string? Content);

/// <summary>
/// Where the asker is standing. Sent by the client and validated against the
/// router's own route names, so the retrieved help topics can be biased towards
/// the page in front of them.
/// </summary>
public sealed record AssistantRouteContext(string? RouteName, string? ForumCode, int? PostId);

public sealed record AssistantMessageRequest(
    string? Message,
    IReadOnlyList<AssistantHistoryMessage>? History,
    AssistantRouteContext? Context);

/// <summary>A single help topic. Keywords widen retrieval beyond the words in the title.</summary>
public sealed record HelpArticle(string Id, string Title, string Body, IReadOnlyList<string> Keywords);

// The three server-sent event payloads. 'delta' arrives many times, then exactly
// one of 'done' or 'error' ends the stream.
public sealed record AssistantDeltaEvent(string Content);

public sealed record AssistantDoneEvent(string Reason);

public sealed record AssistantErrorEvent(string Message, string Code);

// The Ollama /api/chat wire shape. Its option names are snake_case and do not
// match the C# ones, so they are spelled out rather than left to the serializer.
public sealed record OllamaChatRequest(
    string Model,
    IReadOnlyList<AssistantChatMessage> Messages,
    bool Stream,
    bool Think,
    OllamaModelOptions Options);

public sealed record OllamaModelOptions(
    double Temperature,
    [property: JsonPropertyName("top_p")] double TopP,
    [property: JsonPropertyName("num_predict")] int NumPredict);

public sealed record AssistantChatMessage(string Role, string Content);
