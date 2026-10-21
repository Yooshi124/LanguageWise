using LanguageWise.ChatDiscussionService.Db.Data;
using LanguageWise.ChatDiscussionService.Db.Models;
using LanguageWise.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace LanguageWise.ChatDiscussionService.Db.Tests;

[TestFixture]
public sealed class PostgresMigrationTests
{
    private PostgresTestDatabase database = null!;
    private DiscussionRepository repository = null!;

    [SetUp]
    public void SetUp()
    {
        database = new PostgresTestDatabase();
        TestDatabase.Initialise(database.ConnectionString);
        repository = new DiscussionRepository(database.ConnectionString);
    }

    [TearDown]
    public void TearDown() => database?.Dispose();

    [Test]
    public void Database_UsesPostgres18AndIntegerIdentityColumns()
    {
        using var connection = new NpgsqlConnection(database.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_schema = current_schema() AND column_name = 'id'
              AND data_type = 'integer' AND is_identity = 'YES';
            """;

        Assert.Multiple(() =>
        {
            Assert.That(connection.PostgreSqlVersion.Major, Is.EqualTo(18));
            Assert.That(command.ExecuteScalar(), Is.EqualTo(5L));
        });
    }

    [Test]
    public void Initialise_Repeatedly_PreservesSeedCountsAndUserChanges()
    {
        repository.SyncCourseForums([new CatalogCourse(5, "es", "Spanish A1")]);
        repository.SyncAuthorNames([new DirectoryUser(2, "lachie")]);
        repository.UpdatePost(1, new PostUpdate("Edited welcome", "New content", "global"));
        repository.UpdateComment(1, new CommentUpdate("Edited comment"));
        repository.DeletePost(3);
        repository.UnlikePost(1, 3);
        var image = repository.CreatePostImage(1, new ImageInput("preserved", "image.png", "image/png", 10));

        TestDatabase.Initialise(database.ConnectionString);
        TestDatabase.Initialise(database.ConnectionString);

        Assert.Multiple(() =>
        {
            Assert.That(repository.CountForums(), Is.EqualTo(7));
            Assert.That(repository.CountPosts(), Is.EqualTo(5));
            Assert.That(repository.GetForum("es")!.Name, Is.EqualTo("Spanish A1"));
            Assert.That(repository.GetPost(1, null)!.Title, Is.EqualTo("Edited welcome"));
            Assert.That(repository.GetPost(1, null)!.AuthorName, Is.EqualTo("lachie"));
            Assert.That(repository.GetComment(1)!.Content, Is.EqualTo("Edited comment"));
            Assert.That(repository.GetPost(3, null), Is.Null);
            Assert.That(repository.GetPostLikes(1).Select(like => like.UserId), Does.Not.Contain(3));
            Assert.That(repository.GetImage(image!.Id), Is.EqualTo(image));
        });
    }

    [Test]
    public void GeneratedIds_AfterExplicitSeedsAndRepeatInitialisation_DoNotCollideOrRewind()
    {
        var post = NewPost();
        var comment = repository.CreateComment(post.Id, new CommentInput(99, "author", "comment"))!;
        repository.LikePost(post.Id, 99);
        var like = repository.GetPostLikes(post.Id).Single();
        var image = repository.CreatePostImage(post.Id, new ImageInput("new-image", "image.png", "image/png", 10))!;
        repository.SyncCourseForums([new CatalogCourse(99, "ja", "Japanese")]);
        var forum = repository.GetForum("ja")!;
        repository.DeletePost(post.Id);
        TestDatabase.Initialise(database.ConnectionString);
        var nextPost = NewPost();
        var nextComment = repository.CreateComment(nextPost.Id, new CommentInput(99, "author", "next"))!;
        repository.LikePost(nextPost.Id, 99);
        var nextImage = repository.CreatePostImage(nextPost.Id, new ImageInput("next-image", "image.png", "image/png", 10))!;
        repository.SyncCourseForums([new CatalogCourse(100, "ko", "Korean")]);

        Assert.Multiple(() =>
        {
            Assert.That(post.Id, Is.GreaterThan(6));
            Assert.That(comment.Id, Is.GreaterThan(5));
            Assert.That(like.Id, Is.GreaterThan(7));
            Assert.That(image.Id, Is.Positive);
            Assert.That(forum.Id, Is.GreaterThan(7));
            Assert.That(nextPost.Id, Is.GreaterThan(post.Id));
            Assert.That(nextComment.Id, Is.GreaterThan(comment.Id));
            Assert.That(repository.GetPostLikes(nextPost.Id).Single().Id, Is.GreaterThan(like.Id));
            Assert.That(nextImage.Id, Is.GreaterThan(image.Id));
            Assert.That(repository.GetForum("ko")!.Id, Is.GreaterThan(forum.Id));
        });
    }

    [Test]
    public void Initialise_WhenSeedFails_RollsBackSchemaAndData()
    {
        using var emptyDatabase = new PostgresTestDatabase();
        var sqlDirectory = Path.Combine(Path.GetTempPath(), $"chat-sql-{Guid.NewGuid():N}");
        Directory.CreateDirectory(sqlDirectory);
        try
        {
            File.Copy(Path.Combine(AppContext.BaseDirectory, "sql", "schema.sql"), Path.Combine(sqlDirectory, "schema.sql"));
            File.WriteAllText(Path.Combine(sqlDirectory, "seed.sql"),
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "seed.sql")) +
                "\nSELECT * FROM missing_seed_table;");
            var initializer = new DatabaseInitializer(emptyDatabase.ConnectionString, sqlDirectory,
                NullLogger<DatabaseInitializer>.Instance);

            Assert.Throws<PostgresException>(() => initializer.Initialise());
            Assert.That(TestDatabase.Count(emptyDatabase.ConnectionString,
                "SELECT COUNT(*) FROM pg_tables WHERE schemaname = current_schema();"), Is.Zero);
            TestDatabase.Initialise(emptyDatabase.ConnectionString);
            Assert.That(new DiscussionRepository(emptyDatabase.ConnectionString).CountPosts(), Is.EqualTo(6));
        }
        finally
        {
            Directory.Delete(sqlDirectory, recursive: true);
        }
    }

    [Test]
    public async Task Initialise_ConcurrentFirstStarts_SeedsOnce()
    {
        using var emptyDatabase = new PostgresTestDatabase();
        await Task.WhenAll(Enumerable.Range(0, 3).Select(_ =>
            Task.Run(() => TestDatabase.Initialise(emptyDatabase.ConnectionString))));

        var seeded = new DiscussionRepository(emptyDatabase.ConnectionString);
        Assert.Multiple(() =>
        {
            Assert.That(seeded.CountForums(), Is.EqualTo(7));
            Assert.That(seeded.CountPosts(), Is.EqualTo(6));
            Assert.That(seeded.GetComments(2, 100, 0, null), Has.Count.EqualTo(2));
            Assert.That(seeded.GetPostLikes(1), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void GetPosts_NullFiltersAndViewer_MapsCountsDatesAndBooleans()
    {
        var posts = repository.GetPosts(null, null, null, 20, 0, null);
        var welcome = repository.GetPost(1, 3)!;
        var comment = repository.GetComments(1, 100, 0, 2).Single();

        Assert.Multiple(() =>
        {
            Assert.That(posts.Select(post => post.Id), Is.EqualTo(new[] { 6, 5, 4, 3, 2, 1 }));
            Assert.That(posts.All(post => !post.LikedByViewer && post.MatchedCommentExcerpt is null), Is.True);
            Assert.That(welcome.CommentCount, Is.EqualTo(1));
            Assert.That(welcome.LikeCount, Is.EqualTo(2));
            Assert.That(welcome.LikedByViewer, Is.True);
            Assert.That(welcome.CreatedAt, Is.EqualTo(new DateTime(2026, 2, 12, 9, 0, 0, DateTimeKind.Utc)));
            Assert.That(welcome.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(comment.LikeCount, Is.EqualTo(1));
            Assert.That(comment.LikedByViewer, Is.True);
            Assert.That(repository.GetComments(1, 100, 0, null).Single().LikedByViewer, Is.False);
        });
    }

    [Test]
    public void GetPosts_FiltersAndPaginatesWithoutChangingOrder()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.GetPosts(2, "es", "  SUBJUNCTIVE  ", 20, 0, 1).Single().Id, Is.EqualTo(6));
            Assert.That(repository.GetPosts(null, null, "  ", 2, 1, null).Select(post => post.Id),
                Is.EqualTo(new[] { 5, 4 }));
            Assert.That(repository.GetPosts(999, null, null, 20, 0, null), Is.Empty);
            Assert.That(repository.GetPosts(null, "GLOBAL", null, 20, 0, null), Is.Empty);
            Assert.That(repository.GetForum("GLOBAL"), Is.Null);
        });
    }

    [TestCase("MiXeD", "mixed", true)]
    [TestCase("caf\u00e9", "CAF\u00c9", false)]
    [TestCase("100% exact", "%", true)]
    [TestCase("100 percent", "%", false)]
    [TestCase("under_score", "_", true)]
    [TestCase("under score", "_", false)]
    [TestCase("path\\file", "\\", true)]
    [TestCase("path/file", "\\", false)]
    public void Search_PreservesAsciiCaseAndLiteralWildcardSemantics(string content, string search, bool matches)
    {
        var post = NewPost(content);

        Assert.That(repository.GetPosts(99, null, search, 20, 0, null).Select(result => result.Id),
            matches ? Does.Contain(post.Id) : Does.Not.Contain(post.Id));
    }

    [Test]
    public void Search_MatchesCommentsAndReturnsEarliestMatchingExcerpt()
    {
        var post = NewPost();
        repository.CreateComment(post.Id, new CommentInput(99, "author", "first NEEDLE"));
        repository.CreateComment(post.Id, new CommentInput(99, "author", "second needle"));

        var result = repository.GetPosts(null, null, "needle", 20, 0, null).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Id, Is.EqualTo(post.Id));
            Assert.That(result.CommentCount, Is.EqualTo(2));
            Assert.That(result.MatchedCommentExcerpt, Is.EqualTo("first NEEDLE"));
        });
    }

    [Test]
    public void PostsAndComments_CreateUpdateAndDelete_PreserveAuthorshipAndUtcDates()
    {
        var post = NewPost();
        var updated = repository.UpdatePost(post.Id, new PostUpdate(" edited ", " changed ", "es"))!;
        var comment = repository.CreateComment(post.Id, new CommentInput(99, " author ", " reply "))!;
        var editedComment = repository.UpdateComment(comment.Id, new CommentUpdate(" edited reply "))!;

        Assert.Multiple(() =>
        {
            Assert.That(updated.Title, Is.EqualTo("edited"));
            Assert.That(updated.Content, Is.EqualTo("changed"));
            Assert.That(updated.ForumCode, Is.EqualTo("es"));
            Assert.That(updated.UserId, Is.EqualTo(99));
            Assert.That(updated.AuthorName, Is.EqualTo("author"));
            Assert.That(updated.CreatedAt, Is.EqualTo(post.CreatedAt));
            Assert.That(updated.UpdatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(editedComment.Content, Is.EqualTo("edited reply"));
            Assert.That(editedComment.AuthorName, Is.EqualTo("author"));
            Assert.That(editedComment.CreatedAt, Is.EqualTo(comment.CreatedAt));
            Assert.That(editedComment.UpdatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
        Assert.That(repository.DeleteComment(comment.Id), Is.True);
        Assert.That(repository.GetComment(comment.Id), Is.Null);
        Assert.That(repository.DeletePost(post.Id), Is.True);
        Assert.That(repository.GetPost(post.Id, null), Is.Null);
    }

    [Test]
    public void Likes_MapUniqueAndForeignKeyViolationsToExistingOutcomes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.LikePost(1, 3), Is.EqualTo(LikeOutcome.Duplicate));
            Assert.That(repository.LikeComment(1, 2), Is.EqualTo(LikeOutcome.Duplicate));
            Assert.That(repository.LikePost(int.MaxValue, 99), Is.EqualTo(LikeOutcome.TargetNotFound));
            Assert.That(repository.LikeComment(int.MaxValue, 99), Is.EqualTo(LikeOutcome.TargetNotFound));
            Assert.That(repository.LikePost(1, 99), Is.EqualTo(LikeOutcome.Created));
            Assert.That(repository.LikeComment(1, 99), Is.EqualTo(LikeOutcome.Created));
            Assert.That(repository.GetPostLikes(1).Single(like => like.UserId == 99).CommentId, Is.Null);
            Assert.That(repository.GetCommentLikes(1).Single(like => like.UserId == 99).PostId, Is.Null);
            Assert.That(repository.UnlikePost(1, 99), Is.True);
            Assert.That(repository.UnlikeComment(1, 99), Is.True);
            Assert.That(repository.UnlikePost(1, 99), Is.False);
        });
    }

    [Test]
    public void MissingTargets_ReturnNullOrFalseRatherThanThrowing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.GetPost(int.MaxValue, null), Is.Null);
            Assert.That(repository.UpdatePost(int.MaxValue, new PostUpdate("title", "content", "global")), Is.Null);
            Assert.That(repository.GetComment(int.MaxValue), Is.Null);
            Assert.That(repository.UpdateComment(int.MaxValue, new CommentUpdate("content")), Is.Null);
            Assert.That(repository.CreateComment(int.MaxValue, new CommentInput(99, "author", "content")), Is.Null);
            Assert.That(repository.CreatePostImage(int.MaxValue, new ImageInput("missing-post", "a", "b", 1)), Is.Null);
            Assert.That(repository.CreateCommentImage(int.MaxValue, new ImageInput("missing-comment", "a", "b", 1)), Is.Null);
            Assert.That(repository.GetImage(int.MaxValue), Is.Null);
            Assert.That(repository.DeletePost(int.MaxValue), Is.False);
            Assert.That(repository.DeleteComment(int.MaxValue), Is.False);
            Assert.That(repository.DeleteImage(int.MaxValue), Is.False);
        });
    }

    [Test]
    public void Images_MapBigintSizesAndNullableTargets_AndCascadeWithPost()
    {
        var post = NewPost();
        var comment = repository.CreateComment(post.Id, new CommentInput(99, "author", "content"))!;
        var postImage = repository.CreatePostImage(post.Id,
            new ImageInput("post-image", " post.png ", " image/png ", (long)int.MaxValue + 1))!;
        var commentImage = repository.CreateCommentImage(comment.Id,
            new ImageInput("comment-image", "comment.png", "image/png", 10))!;
        repository.LikePost(post.Id, 99);
        repository.LikeComment(comment.Id, 99);

        Assert.Multiple(() =>
        {
            Assert.That(postImage.SizeBytes, Is.EqualTo((long)int.MaxValue + 1));
            Assert.That(postImage.CommentId, Is.Null);
            Assert.That(postImage.FileName, Is.EqualTo("post.png"));
            Assert.That(postImage.ContentType, Is.EqualTo("image/png"));
            Assert.That(postImage.UploadedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(commentImage.PostId, Is.Null);
            Assert.That(repository.GetImage(postImage.Id), Is.EqualTo(postImage));
            Assert.That(repository.GetPostImages(post.Id), Is.EqualTo(new[] { postImage }));
            Assert.That(repository.GetCommentImages(comment.Id), Is.EqualTo(new[] { commentImage }));
            Assert.That(repository.GetImagesForPostComments(post.Id), Is.EqualTo(new[] { commentImage }));
            Assert.That(repository.GetPostStorageKeys(post.Id), Is.EquivalentTo(new[] { "post-image", "comment-image" }));
            Assert.That(repository.GetCommentStorageKeys(comment.Id), Is.EqualTo(new[] { "comment-image" }));
        });

        repository.DeletePost(post.Id);

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetComment(comment.Id), Is.Null);
            Assert.That(repository.GetImage(postImage.Id), Is.Null);
            Assert.That(repository.GetImage(commentImage.Id), Is.Null);
            Assert.That(repository.GetPostLikes(post.Id), Is.Empty);
            Assert.That(repository.GetCommentLikes(comment.Id), Is.Empty);
        });
    }

    [Test]
    public void Forums_OrderGlobalFirstThenAsciiCaseInsensitiveNames()
    {
        repository.SyncCourseForums([
            new CatalogCourse(99, "upper", "apple"),
            new CatalogCourse(100, "lower", "Banana")]);

        var forums = repository.GetForums();
        Assert.That(forums.Take(3).Select(forum => forum.Code), Is.EqualTo(new[] { "global", "upper", "lower" }));
    }

    private Post NewPost(string content = "body") =>
        repository.CreatePost(new PostInput(99, " author ", " new post ", content, "global"));
}