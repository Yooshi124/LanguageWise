using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api;

/// <summary>
/// The retrieval half of AI mode: a curated description of what this forum can
/// actually do, plus the keyword search that picks the topics worth handing to
/// the model. Kept free of HTTP and database dependencies so it unit tests directly.
///
/// The articles describe real controls in the frontend. When a control is renamed
/// or moved, its article here has to follow, otherwise the assistant will
/// confidently send people to a button that no longer exists.
/// </summary>
internal static class HelpKnowledgeBase
{
    /// <summary>How many articles are handed to the model as context for one question.</summary>
    internal const int RetrievedArticleLimit = 3;

    /// <summary>Words too common to tell two help topics apart.</summary>
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "any", "are", "but", "can", "did", "does", "for", "from", "get", "has", "have", "how",
        "its", "not", "out", "that", "the", "their", "them", "then", "there", "these", "they",
        "this", "use", "want", "was", "what", "when", "where", "which", "who", "why", "will", "with",
        "would", "you", "your"
    };

    internal static readonly IReadOnlyList<HelpArticle> Articles =
    [
        new(
            "signing-in",
            "Signing in and out",
            """
            Every page of the forum requires you to be signed in. If you are signed out, the forum
            sends you to the LanguageWise login page and returns you here afterwards.
            When you are signed in, the header shows "Logged in as" followed by your username, next
            to a "Log out" button.
            Reading, posting, commenting and liking all use that one LanguageWise account.
            """,
            ["signin", "login", "signout", "logout", "account", "password", "username", "sign", "log"]),

        new(
            "forums",
            "Forums and choosing where to post",
            """
            The forum is split into four sections: Global, Spanish, Italian and Japanese.
            The "Forums" tab lists all four; select one to read it.
            Global is for anything not tied to a single language. Every post belongs to exactly one
            of these forums, chosen with the "Forum" dropdown when you write or edit the post.
            """,
            ["forum", "forums", "category", "categories", "section", "channel", "board", "global",
             "spanish", "italian", "japanese", "language"]),

        new(
            "create-post",
            "Creating a new post",
            """
            Select the "New post" button on the right of the forum navigation bar. Fill in three
            fields: a Title, the Forum the post belongs to (Global, Spanish, Italian or Japanese),
            and the Content. Then select "Publish".
            All three are required, and the Publish button stays disabled until the title and content
            have something in them. Once published you are taken straight to your new post.
            """,
            ["create", "creating", "new", "write", "writing", "publish", "make", "start", "post",
             "posting", "thread", "topic"]),

        new(
            "edit-post",
            "Editing a post you wrote",
            """
            Open the post, then select the "Edit" button underneath it. Edit only appears on posts you
            wrote yourself, so if you cannot see it, the post belongs to someone else.
            Editing lets you change the title, the forum it sits in, and the content. Select "Save"
            to keep the changes or "Cancel" to discard them.
            Your own posts are all listed under the "My Posts" tab, which is the quickest way to find
            something you want to change.
            """,
            ["edit", "editing", "change", "update", "modify", "amend", "correct", "rewrite", "revise"]),

        new(
            "delete-post",
            "Deleting a post",
            """
            Open the post and select the "Delete" button underneath it, next to Edit. Only the author
            of a post can delete it.
            Deleting a post also removes every comment and like underneath it, and it cannot be undone.
            """,
            ["delete", "deleting", "remove", "removing", "erase", "discard"]),

        new(
            "comments",
            "Commenting on a post",
            """
            Open a post and use the "Add a comment" box at the bottom, then select "Post comment".
            Comments appear underneath the post with a "Load more comments" button when there are
            more than twenty.
            You can edit or delete your own comments using the "Edit" and "Delete" links on them.
            Those links only show on comments you wrote.
            """,
            ["comment", "comments", "commenting", "reply", "replies", "replying", "respond",
             "response", "discuss"]),

        new(
            "likes",
            "Liking posts and comments",
            """
            Every post and every comment has a heart button showing how many likes it has.
            Select it to like, and select it again to remove your like. The heart is filled in when
            you have liked something.
            You can like any post or comment once, including your own.
            """,
            ["like", "likes", "liking", "unlike", "heart", "upvote", "favourite", "favorite", "react"]),

        new(
            "my-posts",
            "Finding your own posts",
            """
            The "My Posts" tab lists every post you have written, newest first, across all four
            forums. It has its own search box that covers everything you posted.
            Use it to get back to a post you want to edit or delete.
            """,
            ["mine", "own", "history", "written", "authored", "myposts"]),

        new(
            "search",
            "Searching a forum",
            """
            Each forum page has a "Search this forum" box. It covers post titles, post content and
            the comments inside that forum, and it filters as you type.
            Search looks inside one forum at a time, so switch forums to search another one. To
            search only what you wrote, use the search box on the "My Posts" tab instead.
            """,
            ["search", "searching", "find", "finding", "filter", "lookup", "query"]),

        new(
            "ai-mode",
            "AI mode",
            """
            AI mode is this assistant. It answers questions about how the discussion forum works,
            such as how to create, edit or delete a post, how commenting and likes work, and where
            to find your own posts.
            It only knows about this forum's own features. It cannot read or write posts on your
            behalf, and it does not know what is in any particular thread.
            """,
            ["assistant", "chatbot", "bot", "help", "aimode", "support"])
    ];

    /// <summary>
    /// The articles most likely to answer the question, best first. Scores each
    /// article by how many distinct question words it matches, weighting a keyword
    /// hit above a body hit so "delete" reaches the delete article rather than the
    /// create article that merely mentions deleting.
    /// </summary>
    internal static IReadOnlyList<HelpArticle> Retrieve(string? question, int limit = RetrievedArticleLimit)
    {
        var terms = Tokenise(question);

        if (terms.Count == 0 || limit < 1)
        {
            return [];
        }

        return Articles
            .Select(article => (Article: article, Score: Score(article, terms)))
            .Where(scored => scored.Score > 0)
            .OrderByDescending(scored => scored.Score)
            .ThenBy(scored => scored.Article.Id, StringComparer.Ordinal)
            .Take(limit)
            .Select(scored => scored.Article)
            .ToList();
    }

    /// <summary>
    /// The retrieved articles rendered as the context block the model is told to
    /// answer from. Empty when nothing matched.
    /// </summary>
    internal static string BuildContext(IReadOnlyList<HelpArticle> articles) =>
        string.Join("\n\n", articles.Select(article => $"## {article.Title}\n{article.Body.Trim()}"));

    private static int Score(HelpArticle article, IReadOnlyCollection<string> terms)
    {
        var score = 0;

        foreach (var term in terms)
        {
            if (article.Keywords.Any(keyword => keyword.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                score += 3;
            }
            else if (article.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 2;
            }
            else if (article.Body.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }
        }

        return score;
    }

    /// <summary>Distinct, meaningful, lower-cased words of at least three characters.</summary>
    private static IReadOnlyCollection<string> Tokenise(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return [];
        }

        return question
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => new string(word.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant())
            .Where(word => word.Length >= 3 && !StopWords.Contains(word))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
