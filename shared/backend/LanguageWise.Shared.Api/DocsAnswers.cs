using System.Text.RegularExpressions;
using LanguageWise.Shared.Api.Clients;

namespace LanguageWise.Shared.Api;

public sealed record DocsCitation(int Number, string Source, string Heading, double Relevance, string Text);

public sealed record DocsAnswer(string Answer, string Confidence, IReadOnlyList<DocsCitation> Citations);

/// <summary>Builds Garry's prompt for a docs answer and grounds the reply in the retrieved passages.</summary>
public static partial class DocsAnswers
{
    public const string InsufficientConfidence = "insufficient";
    public const int PassageCount = 4;
    private const string InsufficientSentinel = "INSUFFICIENT_CONTEXT";
    private const string InsufficientMessage =
        "I couldn't find anything in the LanguageWise docs that answers that, so I won't guess. "
        + "Try rephrasing, or ask about a specific page or feature.";

    public const string DomainRules = $"""
        You answer learners' questions about how LanguageWise works, using only the numbered
        documentation passages in the canonical context.
        - End every sentence that states a fact with the number of the passage it came from in square
          brackets, for example [1] or [2][3]. Only use numbers that appear in the context.
        - Do not use outside knowledge. Do not add a sources list or a confidence rating; the app shows them.
        - If the passages do not answer the question, reply with exactly {InsufficientSentinel} and nothing else.
        - Keep the answer under 150 words. Use Markdown lists where they help.
        """;

    public static DocsAnswer Insufficient() => new(InsufficientMessage, InsufficientConfidence, []);

    public static string BuildContext(IReadOnlyList<RagResult> passages) =>
        string.Join("\n\n", passages.Select((passage, index) =>
            $"[{index + 1}] {passage.Source} — {passage.Heading}\n{Body(passage.Text)}"));

    /// <summary>
    /// Drops citation markers that point at no passage, and lists the cited passages (or every
    /// supplied passage when the model cited none, since they were its only context).
    /// </summary>
    public static DocsAnswer Ground(string answer, string confidence, IReadOnlyList<RagResult> passages)
    {
        if (string.IsNullOrWhiteSpace(answer) || answer.Contains(InsufficientSentinel, StringComparison.Ordinal))
        {
            return Insufficient();
        }

        var cited = new SortedSet<int>();
        var text = CitationMarker().Replace(answer, match =>
        {
            var number = int.Parse(match.Groups[2].Value);
            if (number < 1 || number > passages.Count)
            {
                return "";
            }
            cited.Add(number);
            return match.Value;
        }).Trim();

        var citations = (cited.Count > 0 ? cited : Enumerable.Range(1, passages.Count))
            .Select(number =>
            {
                var passage = passages[number - 1];
                return new DocsCitation(number, passage.Source, passage.Heading, passage.Relevance, Body(passage.Text));
            })
            .ToList();
        return new DocsAnswer(text, confidence, citations);
    }

    // The RAG store prefixes each passage with "# source" and "## heading" lines.
    private static string Body(string text) => LeadingHeadings().Replace(text, "").Trim();

    [GeneratedRegex(@"([ \t]*)\[(\d{1,3})\]")]
    private static partial Regex CitationMarker();

    [GeneratedRegex(@"\A(\s*#{1,6} [^\n]*\n)+")]
    private static partial Regex LeadingHeadings();
}
