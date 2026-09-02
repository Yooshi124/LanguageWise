using System.Text.Json.Serialization;

namespace LanguageWise.MiniGamesService.Api.Models;

public sealed record OpenRouterChatRequest(
    string Model,
    IReadOnlyList<OpenRouterChatMessage> Messages,
    bool Stream,
    [property: JsonPropertyName("max_tokens")] int MaxTokens,
    double? Temperature = null);

public sealed record OpenRouterChatMessage(string Role, string Content);
