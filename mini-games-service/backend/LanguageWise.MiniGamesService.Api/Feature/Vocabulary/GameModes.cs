namespace LanguageWise.MiniGamesService.Api.Feature.Vocabulary;

/// <summary>Vocabulary sources a game round can be started from.</summary>
public static class GameModes
{
    /// <summary>Words from the user's completed course lessons (quizzes-courses service).</summary>
    public const string Content = "content";

    /// <summary>Words generated on demand by the AI provider (OpenRouter).</summary>
    public const string Ai = "ai";

    /// <summary>True when the value names a known mode.</summary>
    public static bool IsValid(string? mode) =>
        string.Equals(mode, Content, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mode, Ai, StringComparison.OrdinalIgnoreCase);
}
