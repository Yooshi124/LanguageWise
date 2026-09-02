namespace LanguageWise.ChatDiscussionService.Api.Options;

/// <summary>
/// Where AI mode's model lives and how it is asked to behave. The model runs in
/// the shared 'ollama' container, so there is no API key: an unreachable model
/// is the failure to plan for, not an unauthenticated one.
///
/// The address itself is not here: it comes from Services:Ollama, alongside the
/// other service addresses.
/// </summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Model { get; init; } = "gemma4:e4b";

    /// <summary>Ollama's num_predict: the ceiling on how long one answer runs.</summary>
    public int MaxOutputTokens { get; init; } = 512;

    /// <summary>Low, because the answer has to stay close to the supplied help topics.</summary>
    public double Temperature { get; init; } = 0.3;

    public double TopP { get; init; } = 0.9;
}
