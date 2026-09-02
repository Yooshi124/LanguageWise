using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Services;

public interface IAssistantContextService
{
    Task<AssistantContext> GetContextAsync(
        ValidatedAssistantRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// The retrieval half of AI mode: everything the model is allowed to treat as
/// fact, rendered as server-controlled JSON.
///
/// Grounded in <see cref="HelpKnowledgeBase"/> rather than live forum data on
/// purpose — the assistant explains how the forum works, and it deliberately
/// cannot read the thread the user happens to be looking at. The route context
/// only biases which help topics are retrieved, so standing on the edit page
/// surfaces the editing topic even when the question does not name it.
/// </summary>
public sealed class AssistantContextService(DiscussionClient client) : IAssistantContextService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Extra retrieval terms contributed by the page the question was asked from.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> RouteHints =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["forums"] = "forums forum sections",
            ["forum"] = "forum search posts",
            ["my-posts"] = "my posts own search",
            ["post-create"] = "create new post publish",
            ["post"] = "post comments likes",
            ["post-edit"] = "edit post update"
        };

    private const string NothingMatched =
        "I can only help with how the discussion forum works — creating, editing and deleting "
        + "posts, commenting, likes, searching a forum, and finding your own posts. "
        + "Try asking about one of those.";

    /// <summary>
    /// Retrieves once and renders the result both ways: as JSON for the model,
    /// and as prose for the fallback that stands in when the model is missing.
    /// </summary>
    public async Task<AssistantContext> GetContextAsync(
        ValidatedAssistantRequest request,
        CancellationToken cancellationToken)
    {
        // The page only refines a question that is already about the forum. Adding
        // its terms unconditionally would make every question match something,
        // including the ones the assistant should admit it cannot help with.
        var articles = HelpKnowledgeBase.Retrieve(request.Message);

        if (articles.Count > 0)
        {
            var hint = RouteHints.GetValueOrDefault(request.Context.RouteName!, string.Empty);
            articles = HelpKnowledgeBase.Retrieve($"{request.Message} {hint}");
        }

        return new AssistantContext(
            JsonSerializer.Serialize(
                new
                {
                    platform = "LanguageWise discussion forum",
                    page = request.Context.RouteName,
                    forum = request.Context.ForumCode,
                    forums = (await GetForumsAsync(cancellationToken))
                        .Select(forum => new { forum.Code, forum.Name }),
                    helpTopics = articles.Select(article => new
                    {
                        article.Title,
                        Body = article.Body.Trim()
                    })
                },
                JsonOptions),
            BuildFallbackAnswer(articles));
    }

    /// <summary>Failing to read the forums costs the list, not the reply.</summary>
    private async Task<IReadOnlyList<Forum>> GetForumsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetForumsAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// The retrieved help text as markdown, which the panel renders the same way
    /// it renders a real answer. Nothing matching is itself worth saying, and is
    /// the same thing the model would have been told to say.
    /// </summary>
    private static string BuildFallbackAnswer(IReadOnlyList<HelpArticle> articles) =>
        articles.Count == 0
            ? NothingMatched
            : string.Join(
                "\n\n",
                articles.Select(article => $"**{article.Title}**\n\n{article.Body.Trim()}"));
}

/// <summary>
/// What one question retrieved: the JSON the model is told to answer from, and
/// the prose to fall back to when there is no model to tell.
/// </summary>
public sealed record AssistantContext(string CanonicalContext, string FallbackAnswer);
