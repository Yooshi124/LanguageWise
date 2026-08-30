using System.Net;
using System.Text;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

/// <summary>
/// Stands in for the database microservice with just enough behaviour to drive the
/// backend's authorisation paths.
///
/// Post 1 and comment 1 belong to user 1 (the identity the tests sign in as);
/// post 2 and comment 2 belong to user 99, so they exercise the owner check.
/// Post 9 is liked already, so liking it reports a conflict.
/// </summary>
internal sealed class FakeDiscussionDatabase : HttpMessageHandler
{
    internal const int SignedInUserId = 1;
    internal const int OtherUserId = 99;

    public string? LastRequestBody { get; private set; }
    public Uri? LastRequestUri { get; private set; }
    public HttpMethod? LastRequestMethod { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequestUri = request.RequestUri;
        LastRequestMethod = request.Method;
        LastRequestBody = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var method = request.Method;

        // Likes
        if (path.EndsWith("/likes", StringComparison.Ordinal))
        {
            if (method == HttpMethod.Post)
            {
                return path.Contains("/posts/9/", StringComparison.Ordinal)
                    ? Empty(HttpStatusCode.Conflict)
                    : Empty(HttpStatusCode.Created);
            }

            if (method == HttpMethod.Delete)
            {
                return Empty(HttpStatusCode.NoContent);
            }

            return Json("[]");
        }

        // Comments addressed directly, used by the owner check
        if (path.StartsWith("/api/comments/", StringComparison.Ordinal))
        {
            var commentId = IdFrom(path, "/api/comments/");

            if (method == HttpMethod.Get)
            {
                return commentId is 1 or 2
                    ? Json(Comment(commentId.Value, commentId == 1 ? SignedInUserId : OtherUserId))
                    : Empty(HttpStatusCode.NotFound);
            }

            if (method == HttpMethod.Put)
            {
                return Json(Comment(commentId ?? 1, SignedInUserId));
            }

            if (method == HttpMethod.Delete)
            {
                return Empty(HttpStatusCode.NoContent);
            }
        }

        // Comments belonging to a post
        if (path.EndsWith("/comments", StringComparison.Ordinal))
        {
            return method == HttpMethod.Post
                ? Json(Comment(10, SignedInUserId), HttpStatusCode.Created)
                : Json($"[{Comment(1, SignedInUserId)}]");
        }

        // Posts
        if (path == "/api/posts")
        {
            return method == HttpMethod.Post
                ? Json(Post(10, SignedInUserId), HttpStatusCode.Created)
                : Json($"[{PostSummary(1, SignedInUserId)}]");
        }

        if (path.StartsWith("/api/posts/", StringComparison.Ordinal))
        {
            var postId = IdFrom(path, "/api/posts/");

            if (method == HttpMethod.Get)
            {
                return postId is 1 or 2 or 9
                    ? Json(PostSummary(postId.Value, postId == 1 ? SignedInUserId : OtherUserId))
                    : Empty(HttpStatusCode.NotFound);
            }

            if (method == HttpMethod.Put)
            {
                return Json(Post(postId ?? 1, SignedInUserId));
            }

            if (method == HttpMethod.Delete)
            {
                return Empty(HttpStatusCode.NoContent);
            }
        }

        return Empty(HttpStatusCode.NotFound);
    }

    private static int? IdFrom(string path, string prefix)
    {
        var remainder = path[prefix.Length..];
        var slash = remainder.IndexOf('/');
        var segment = slash < 0 ? remainder : remainder[..slash];
        return int.TryParse(segment, out var id) ? id : null;
    }

    private static string PostSummary(int id, int userId) =>
        $$"""
        {
          "id": {{id}}, "userId": {{userId}}, "authorName": "someone", "title": "A title", "content": "Some content",
          "category": "global", "createdAt": "2026-02-12T09:00:00Z", "updatedAt": "2026-02-12T09:00:00Z",
          "commentCount": 2, "likeCount": 3, "likedByViewer": false, "matchedCommentExcerpt": null
        }
        """;

    private static string Post(int id, int userId) =>
        $$"""
        {
          "id": {{id}}, "userId": {{userId}}, "authorName": "someone", "title": "A title", "content": "Some content",
          "category": "global", "createdAt": "2026-02-12T09:00:00Z", "updatedAt": "2026-02-12T09:00:00Z"
        }
        """;

    private static string Comment(int id, int userId) =>
        $$"""
        {
          "id": {{id}}, "postId": 1, "userId": {{userId}}, "authorName": "someone", "content": "A comment",
          "createdAt": "2026-02-12T09:30:00Z", "updatedAt": "2026-02-12T09:30:00Z",
          "likeCount": 0, "likedByViewer": false
        }
        """;

    private static HttpResponseMessage Json(string body, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Empty(HttpStatusCode statusCode) => new(statusCode);
}
