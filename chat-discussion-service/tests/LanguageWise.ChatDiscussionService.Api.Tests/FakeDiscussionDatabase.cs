using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

/// <summary>
/// Stands in for the database microservice with just enough behaviour to drive the
/// backend's authorisation paths.
///
/// Post 1 and comment 1 belong to user 1 (the identity the tests sign in as);
/// post 2 and comment 2 belong to user 99, so they exercise the owner check.
/// Post 9 is liked already, so liking it reports a conflict.
///
/// Post 3 and comment 3 are the signed-in user's too, and each already holds as many
/// images as it may, which is what makes a further upload exceed the limit.
///
/// Images 1 and 2 hang off posts 1 and 2, images 3 and 4 off comments 1 and 2, so an
/// image inherits the ownership of whichever of the two it belongs to.
/// </summary>
internal sealed class FakeDiscussionDatabase : HttpMessageHandler
{
    internal const int SignedInUserId = 1;
    internal const int OtherUserId = 99;

    /// <summary>A post of the signed-in user's that already holds the maximum number of images.</summary>
    internal const int FullPostId = 3;

    /// <summary>A comment of the signed-in user's that already holds the maximum number of images.</summary>
    internal const int FullCommentId = 3;

    /// <summary>An image on comment 1, which the signed-in user wrote.</summary>
    internal const int OwnCommentImageId = 3;

    /// <summary>An image on comment 2, which somebody else wrote.</summary>
    internal const int OtherCommentImageId = 4;

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

        // Every image on the comments of one post, in a single read
        if (path.EndsWith("/comment-images", StringComparison.Ordinal))
        {
            return IdFrom(path, "/api/posts/") == 1
                ? Json($"[{Image(OwnCommentImageId, commentId: 1)}]")
                : Json("[]");
        }

        // Images belonging to a post or to a comment
        if (path.EndsWith("/images", StringComparison.Ordinal))
        {
            var onComment = path.StartsWith("/api/comments/", StringComparison.Ordinal);
            var owningId = IdFrom(path, onComment ? "/api/comments/" : "/api/posts/") ?? 0;

            if (method == HttpMethod.Post)
            {
                return Json(
                    onComment ? Image(20, commentId: owningId) : Image(20, owningId),
                    HttpStatusCode.Created);
            }

            var isFull = owningId == (onComment ? FullCommentId : FullPostId);
            var stored = isFull
                ? Enumerable.Range(1, ImageRules.MaxPerPost)
                    .Select(id => onComment ? Image(id, commentId: owningId) : Image(id, owningId))
                : Enumerable.Empty<string>();

            return Json($"[{string.Join(",", stored)}]");
        }

        // Images addressed directly, used by the owner check and by the byte proxy
        if (path.StartsWith("/api/images/", StringComparison.Ordinal))
        {
            var imageId = IdFrom(path, "/api/images/");

            if (imageId is not (1 or 2 or OwnCommentImageId or OtherCommentImageId))
            {
                return Empty(HttpStatusCode.NotFound);
            }

            if (path.EndsWith("/content", StringComparison.Ordinal))
            {
                return method == HttpMethod.Get ? PngBytes() : Empty(HttpStatusCode.NotFound);
            }

            if (method == HttpMethod.Get)
            {
                return Json(imageId is 1 or 2
                    ? Image(imageId.Value, imageId.Value)
                    : Image(imageId.Value, commentId: imageId == OwnCommentImageId ? 1 : 2));
            }

            if (method == HttpMethod.Delete)
            {
                return Empty(HttpStatusCode.NoContent);
            }
        }

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
                return commentId is 1 or 2 or 3
                    ? Json(Comment(commentId.Value, commentId is 1 or FullCommentId ? SignedInUserId : OtherUserId))
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
                return postId is 1 or 2 or 3 or 9
                    ? Json(PostSummary(postId.Value, postId is 1 or FullPostId ? SignedInUserId : OtherUserId))
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

    private static string Image(int id, int? postId = null, int? commentId = null) =>
        $$"""
        {
          "id": {{id}}, "postId": {{Number(postId)}}, "commentId": {{Number(commentId)}},
          "storageKey": "key{{id}}", "fileName": "picture.png", "contentType": "image/png",
          "sizeBytes": 1024, "uploadedAt": "2026-02-12T12:20:00Z"
        }
        """;

    private static string Number(int? value) => value?.ToString() ?? "null";

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

    private static HttpResponseMessage PngBytes()
    {
        var content = new ByteArrayContent(ImageBytes.Png());
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static HttpResponseMessage Empty(HttpStatusCode statusCode) => new(statusCode);
}
