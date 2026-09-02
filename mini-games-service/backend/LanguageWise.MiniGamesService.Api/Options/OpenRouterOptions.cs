namespace LanguageWise.MiniGamesService.Api.Options;

public sealed class OpenRouterOptions
{
    public const string SectionName = "OpenRouter";

    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1/";

    public string Model { get; init; } = "google/gemma-4-26b-a4b-it";

    public int MaxOutputTokens { get; init; } = 2048;
}
