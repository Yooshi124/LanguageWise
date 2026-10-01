using System.Net;
using System.Text;
using LanguageWise.ChatDiscussionService.Db.Clients;
using LanguageWise.ChatDiscussionService.Db.Data;
using LanguageWise.ChatDiscussionService.Db.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.ChatDiscussionService.Db.Tests;

[TestFixture]
public sealed class AuthorNameSyncTests
{
    private const int LachlanUserId = 2;

    private string databasePath = null!;
    private string connectionString = null!;
    private DiscussionRepository repository = null!;

    [SetUp]
    public void SetUp()
    {
        databasePath = TestDatabase.NewPath();
        connectionString = TestDatabase.ConnectionStringFor(databasePath);
        TestDatabase.Initialise(connectionString);
        repository = new DiscussionRepository(connectionString);
    }

    [TearDown]
    public void TearDown() => TestDatabase.Delete(databasePath);

    [Test]
    public void GetAuthorIds_ReturnsEachPostAndCommentAuthorOnce()
    {
        Assert.That(repository.GetAuthorIds(), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void SyncAuthorNames_WhenAUserWasRenamed_RenamesTheirPostsAndComments()
    {
        var renamed = repository.SyncAuthorNames([new DirectoryUser(LachlanUserId, "lachie")]);

        Assert.Multiple(() =>
        {
            Assert.That(renamed, Is.EqualTo(5));
            Assert.That(CountByAuthor("Posts", LachlanUserId, "lachie"), Is.EqualTo(3));
            Assert.That(CountByAuthor("Comments", LachlanUserId, "lachie"), Is.EqualTo(2));
            Assert.That(CountByAuthor("Posts", 1, "amber"), Is.EqualTo(1));
        });
    }

    [Test]
    public void SyncAuthorNames_WhenNamesAlreadyMatch_ChangesNothing()
    {
        var renamed = repository.SyncAuthorNames(
            [new DirectoryUser(1, "amber"), new DirectoryUser(LachlanUserId, "lachlan")]);

        Assert.That(renamed, Is.Zero);
    }

    [Test]
    public async Task SyncOnceAsync_AsksForEveryAuthorAndAppliesTheAnswer()
    {
        var handler = new StubHandler(_ => Json($"[{{\"id\":{LachlanUserId},\"username\":\"lachie\"}}]"));

        var renamed = await NewSync(handler).SyncOnceAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(handler.LastRequestUri?.PathAndQuery, Is.EqualTo("/api/users?ids=1&ids=2&ids=3&ids=4&ids=5"));
            Assert.That(renamed, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task SyncOnceAsync_WhenTheDirectoryIsUnreachable_LeavesNamesAlone()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("unreachable"));

        var renamed = await NewSync(handler).SyncOnceAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(renamed, Is.Zero);
            Assert.That(CountByAuthor("Posts", LachlanUserId, "lachlan"), Is.EqualTo(3));
        });
    }

    private AuthorNameSync NewSync(HttpMessageHandler handler) => new(
        repository,
        new StubHttpClientFactory(handler),
        TimeSpan.FromMinutes(5),
        NullLogger<AuthorNameSync>.Instance);

    private long CountByAuthor(string table, int userId, string authorName) =>
        TestDatabase.Count(
            connectionString,
            $"SELECT COUNT(*) FROM {table} WHERE UserId = {userId} AND AuthorName = '{authorName}';");

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.That(name, Is.EqualTo(nameof(UserDirectoryClient)));
            return new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://shared-db:8080/") };
        }
    }
}
