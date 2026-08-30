using System.Net;
using System.Text.Json;
using LanguageWise.ChatDiscussionService.Api.Clients;

namespace LanguageWise.ChatDiscussionService.Api.Tests;

public sealed class DiscussionClientTests
{
    private const string PostsJson =
        """
        [
          {
            "id": 1, "userId": 2, "authorName": "lachlan", "title": "Welcome", "content": "Say hello",
            "category": "global", "createdAt": "2026-02-12T09:00:00Z", "updatedAt": "2026-02-12T09:00:00Z",
            "commentCount": 4, "likeCount": 7, "likedByViewer": true, "matchedCommentExcerpt": null
          }
        ]
        """;

    [Test]
    public async Task GetPostsAsync_DeserialisesTheCountsAndTheViewerFlag()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, PostsJson);
        var client = CreateClient(handler);

        var posts = await client.GetPostsAsync(null, null, null, 20, 0, null);

        Assert.That(posts, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(posts[0].Category, Is.EqualTo("global"));
            Assert.That(posts[0].AuthorName, Is.EqualTo("lachlan"));
            Assert.That(posts[0].CommentCount, Is.EqualTo(4));
            Assert.That(posts[0].LikeCount, Is.EqualTo(7));
            Assert.That(posts[0].LikedByViewer, Is.True);
            Assert.That(posts[0].MatchedCommentExcerpt, Is.Null);
        });
    }

    [Test]
    public async Task GetPostsAsync_SendsEverySuppliedFilter()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetPostsAsync(3, "spanish", "verbs", 20, 40, 9);

        Assert.That(
            handler.LastRequestUri?.PathAndQuery,
            Is.EqualTo("/api/posts?userId=3&category=spanish&search=verbs&limit=20&offset=40&viewerId=9"));
    }

    [Test]
    public async Task GetPostsAsync_OmitsTheFiltersTheCallerLeftOut()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetPostsAsync(null, null, null, 20, 0, null);

        Assert.That(handler.LastRequestUri?.PathAndQuery, Is.EqualTo("/api/posts?limit=20&offset=0"));
    }

    [Test]
    public async Task GetPostsAsync_EscapesASearchTermContainingASpace()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetPostsAsync(null, null, "double consonants", 20, 0, null);

        Assert.That(handler.LastRequestUri?.Query, Does.Contain("search=double%20consonants"));
    }

    [Test]
    public async Task GetPostsAsync_EscapesACategoryContainingASpace()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetPostsAsync(null, "brazilian portuguese", null, 20, 0, null);

        Assert.That(handler.LastRequestUri?.Query, Does.Contain("category=brazilian%20portuguese"));
    }

    [Test]
    public async Task GetPostsAsync_ReturnsAnEmptyListWhenThereAreNoPosts()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        Assert.That(await client.GetPostsAsync(null, null, null, 20, 0, null), Is.Empty);
    }

    [Test]
    public async Task GetPostAsync_WhenThePostIsMissing_ReturnsNull()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        var client = CreateClient(handler);

        Assert.That(await client.GetPostAsync(1, null), Is.Null);
    }

    [Test]
    public async Task CreatePostAsync_SendsTheAuthorAlongsideTheContent()
    {
        using var handler = new StubHttpMessageHandler(
            HttpStatusCode.Created,
            """
            {
              "id": 5, "userId": 2, "authorName": "lachlan", "title": "T", "content": "C", "category": "global",
              "createdAt": "2026-02-12T09:00:00Z", "updatedAt": "2026-02-12T09:00:00Z"
            }
            """);
        var client = CreateClient(handler);

        await client.CreatePostAsync(2, "lachlan", "T", "C", "global");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/posts"));
            Assert.That(body.RootElement.GetProperty("userId").GetInt32(), Is.EqualTo(2));
            Assert.That(body.RootElement.GetProperty("authorName").GetString(), Is.EqualTo("lachlan"));
            Assert.That(body.RootElement.GetProperty("category").GetString(), Is.EqualTo("global"));
        });
    }

    [Test]
    public async Task CreateCommentAsync_SendsTheAuthorNameFromTheCaller()
    {
        using var handler = new StubHttpMessageHandler(
            HttpStatusCode.Created,
            """
            {
              "id": 5, "postId": 1, "userId": 2, "authorName": "lachlan", "content": "C",
              "createdAt": "2026-02-12T09:00:00Z", "updatedAt": "2026-02-12T09:00:00Z"
            }
            """);
        var client = CreateClient(handler);

        await client.CreateCommentAsync(1, 2, "lachlan", "C");

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.That(body.RootElement.GetProperty("authorName").GetString(), Is.EqualTo("lachlan"));
    }

    [Test]
    public async Task LikePostAsync_WhenTheLikeAlreadyExists_ReportsADuplicate()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Conflict, "{}");
        var client = CreateClient(handler);

        Assert.That(await client.LikePostAsync(1, 2), Is.EqualTo(LikeOutcome.Duplicate));
    }

    [Test]
    public async Task LikePostAsync_WhenThePostIsMissing_ReportsAMissingTarget()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound, "{}");
        var client = CreateClient(handler);

        Assert.That(await client.LikePostAsync(1, 2), Is.EqualTo(LikeOutcome.TargetNotFound));
    }

    [Test]
    public async Task LikeCommentAsync_WhenTheInsertSucceeds_ReportsCreated()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.Created, "{}");
        var client = CreateClient(handler);

        var outcome = await client.LikeCommentAsync(4, 2);

        Assert.Multiple(() =>
        {
            Assert.That(outcome, Is.EqualTo(LikeOutcome.Created));
            Assert.That(handler.LastRequestUri?.AbsolutePath, Is.EqualTo("/api/comments/4/likes"));
        });
    }

    [Test]
    public async Task UnlikePostAsync_IdentifiesTheLikeByPostAndUser()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent, "");
        var client = CreateClient(handler);

        await client.UnlikePostAsync(3, 2);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestMethod, Is.EqualTo(HttpMethod.Delete));
            Assert.That(handler.LastRequestUri?.PathAndQuery, Is.EqualTo("/api/posts/3/likes?userId=2"));
        });
    }

    [Test]
    public async Task GetCommentsAsync_RequestsTheCommentsNestedUnderThePost()
    {
        using var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.GetCommentsAsync(7, 100, 0, 2);

        Assert.That(
            handler.LastRequestUri?.PathAndQuery,
            Is.EqualTo("/api/posts/7/comments?limit=100&offset=0&viewerId=2"));
    }

    private static DiscussionClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://chat-discussion-service-db:8080/") });
}
