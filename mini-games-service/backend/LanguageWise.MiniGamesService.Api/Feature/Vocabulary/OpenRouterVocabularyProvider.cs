using System.Text.Json;
using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Models;

namespace LanguageWise.MiniGamesService.Api.Feature.Vocabulary;

/// <summary>Thrown when the AI provider cannot produce a usable word list for a game.</summary>
public sealed class AiVocabularyUnavailableException(string message)
    : Exception(message);

/// <summary>Generates themed, beginner-level vocabulary word lists via the AI provider.</summary>
public interface IAiVocabularyProvider
{
    /// <summary>
    /// Vocabulary groups generated for the given game kind ("guess_the_word", "word_search" or
    /// "associations") in the requested language. Words are beginner level and carry definitions.
    /// </summary>
    Task<IReadOnlyList<VocabularyGroup>> GenerateGroupsAsync(
        string gameKind,
        string language,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// AI-backed vocabulary source. Prompts are shaped per game (single 5-letter word list for
/// Guess the Word, one theme for Word Search, explicit 4×4 categories for Associations) and the
/// model is asked for strict JSON, which is parsed defensively with one retry.
/// </summary>
public sealed class OpenRouterVocabularyProvider(
    IVocabularyCompletionClient completionClient,
    ILogger<OpenRouterVocabularyProvider> logger) : IAiVocabularyProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<VocabularyGroup>> GenerateGroupsAsync(
        string gameKind,
        string language,
        CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(gameKind, language);
        var maxTokens = MaxTokensFor(gameKind);
        // A higher temperature makes consecutive rounds pick different words instead of the model's
        // single most-likely answer (e.g. always "Apfel" for German Guess the Word).
        var temperature = TemperatureFor(gameKind);

        for (var attempt = 0; ; attempt++)
        {
            string content;
            try
            {
                content = await completionClient.CompleteAsync(messages, maxTokens, temperature, cancellationToken);
            }
            catch (Exception exception) when (exception is VocabularyProviderException or HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(exception, "AI vocabulary generation failed for {GameKind} ({Language})", gameKind, language);
                throw new AiVocabularyUnavailableException("AI word generation is unavailable right now.");
            }

            var groups = TryParseGroups(content, gameKind);
            if (groups is not null)
            {
                logger.LogInformation(
                    "AI generated {GroupCount} vocabulary group(s) for {GameKind} ({Language})",
                    groups.Count, gameKind, language);
                return groups;
            }

            if (attempt >= 1)
            {
                logger.LogWarning("AI returned unusable vocabulary JSON for {GameKind} ({Language}): {Content}", gameKind, language, content);
                throw new AiVocabularyUnavailableException("AI word generation returned an unusable word list.");
            }

            logger.LogInformation("AI vocabulary response for {GameKind} was not valid JSON; retrying once", gameKind);
            messages =
            [
                .. messages,
                new OpenRouterChatMessage("assistant", content),
                new OpenRouterChatMessage("user", "That was not valid JSON. Reply with only the JSON object, no other text.")
            ];
        }
    }

    private static IReadOnlyList<OpenRouterChatMessage> BuildMessages(string gameKind, string language) =>
    [
        new OpenRouterChatMessage("system",
            "You output vocabulary for beginner (A1-A2) language learners as compact JSON only — no markdown, no commentary, no extra keys. " +
            "Schema: {\"groups\":[{\"title\":string,\"words\":[{\"word\":string,\"definition\":string}]}]}. " +
            "Rules: every word is a single letters-only word in the target language (no spaces/phrases); " +
            "every definition is a concise English gloss of at most 10 words; " +
            "emit exactly the word counts the user asks for — nothing more, nothing less."),
        new OpenRouterChatMessage("user", BuildUserPrompt(gameKind, language))
    ];

    /// <summary>
    /// Token budget per game. Smaller lists generate far faster, so keep these tight: a single
    /// five-letter word needs very few tokens, while associations needs the largest list.
    /// </summary>
    private static int MaxTokensFor(string gameKind) => gameKind switch
    {
        "guess_the_word" => 96,
        "word_search" => 750,
        "associations" => 1200,
        _ => 1024
    };

    /// <summary>
    /// Sampling temperature per game. Guess the Word picks one word, so it benefits most from
    /// randomness to avoid repeating the model's favourite answer across rounds; the list games
    /// already get variety from producing many words.
    /// </summary>
    private static double TemperatureFor(string gameKind) => gameKind switch
    {
        "guess_the_word" => 1.0,
        _ => 0.7
    };

    /// <summary>A random broad theme so Guess the Word solutions vary between rounds.</summary>
    private static string RandomSolutionTheme() => SolutionThemes[Random.Shared.Next(SolutionThemes.Length)];

    private static readonly string[] SolutionThemes =
    [
        "animals", "food and drink", "the home", "clothing", "nature", "the body",
        "family and people", "colours", "travel and places", "everyday objects",
        "school and work", "weather", "sports and hobbies", "time and days"
    ];

    private static string BuildUserPrompt(string gameKind, string language) => gameKind switch
    {
        // One hidden 5-letter answer + its gloss; the title is fixed and minimal so no tokens are
        // spent on text the game never shows. A random theme steers the model away from repeating
        // its single favourite word every round.
        "guess_the_word" =>
            $"Output one group with title \"word\" containing exactly 1 word: a common beginner-level {language} word on the theme of " +
            $"{RandomSolutionTheme()}, written in {language} (not English), exactly 5 letters, single word. " +
            "Add an English definition of at most 10 words. No other words.",
        "word_search" =>
            $"Choose one beginner-friendly theme (e.g. animals, food, travel). Output one group whose title is that theme, " +
            $"containing exactly 10 beginner-level {language} words written in {language} (not English), each a single letters-only word of 3-12 letters. " +
            "Add an English definition of at most 10 words per word.",
        "associations" =>
            $"Output exactly 4 groups. Each group: title = a beginner category (e.g. \"Colours\", \"Animals\"), " +
            $"and exactly 4 beginner-level {language} words written in {language} (not English) that belong to it, each a single letters-only word. " +
            "The 4 categories must be distinct and all 16 words different. Add an English definition of at most 10 words per word.",
        _ => throw new ArgumentException($"Unknown game kind '{gameKind}'.", nameof(gameKind))
    };

    /// <summary>
    /// Parse the model's JSON into playable groups. Returns null when the payload cannot be
    /// parsed or contains no usable words. Words are normalised through the playable-words
    /// filter; the solution list for Guess the Word is additionally constrained to 5 letters.
    /// </summary>
    private List<VocabularyGroup>? TryParseGroups(string content, string gameKind)
    {
        GeneratedWordList? generated;
        try
        {
            // Models sometimes wrap JSON in prose or a code fence; take the outermost object.
            var start = content.IndexOf('{');
            var end = content.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            generated = JsonSerializer.Deserialize<GeneratedWordList>(content[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (generated?.Groups is null || generated.Groups.Count == 0)
        {
            return null;
        }

        var groups = new List<VocabularyGroup>();
        foreach (var group in generated.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.Title) || group.Words is null)
            {
                continue;
            }

            var entries = group.Words
                .Where(word => !string.IsNullOrWhiteSpace(word.WordText))
                .Select(word => (word: PlayableWords.Extract([word.WordText]).FirstOrDefault(), word.Definition))
                .Where(pair => pair.word is not null)
                .Select(pair => new WordEntry(pair.word!, string.IsNullOrWhiteSpace(pair.Definition) ? null : pair.Definition.Trim()))
                .DistinctBy(entry => entry.Word)
                .ToList();

            if (entries.Count > 0)
            {
                groups.Add(new VocabularyGroup(group.Title.Trim(), entries));
            }
        }

        return groups.Count > 0 ? groups : null;
    }

    private sealed record GeneratedWordList(IReadOnlyList<GeneratedGroup> Groups);

    private sealed record GeneratedGroup(string Title, IReadOnlyList<GeneratedWord> Words);

    private sealed record GeneratedWord(
        [property: System.Text.Json.Serialization.JsonPropertyName("word")] string WordText,
        string Definition);
}
