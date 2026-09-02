namespace LanguageWise.MiniGamesService.Api.Feature.Vocabulary;

/// <summary>
/// The course languages offered on the platform. AI mode lets the user play in any of these,
/// independent of which courses they have started; content mode intersects this with the
/// user's unlocked vocabulary.
/// </summary>
public static class SupportedLanguages
{
    public sealed record Language(string Code, string Title);

    public static readonly IReadOnlyList<Language> All =
    [
        new("de", "German"),
        new("es", "Spanish"),
        new("it", "Italian"),
        new("nl", "Dutch"),
        new("pl", "Polish"),
        new("fr", "French"),
    ];

    /// <summary>The display title for a code or title the caller supplied (falls back to the input).</summary>
    public static string ResolveTitle(string? codeOrTitle)
    {
        if (string.IsNullOrWhiteSpace(codeOrTitle))
        {
            return "English";
        }

        var match = All.FirstOrDefault(language =>
            string.Equals(language.Code, codeOrTitle, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language.Title, codeOrTitle, StringComparison.OrdinalIgnoreCase));
        return match?.Title ?? codeOrTitle;
    }
}
