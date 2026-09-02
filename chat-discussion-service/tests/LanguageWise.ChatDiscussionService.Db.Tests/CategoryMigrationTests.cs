using LanguageWise.ChatDiscussionService.Db.Data;
using LanguageWise.ChatDiscussionService.Db.Models;

namespace LanguageWise.ChatDiscussionService.Db.Tests;

/// <summary>Start-up against a volume that still carries Posts.Category.</summary>
[TestFixture]
public sealed class CategoryMigrationTests
{
    // SampleItems is populated because the initialiser treats an empty SampleItems
    // as a brand new volume and would seed over the top of the migrated rows.
    private const string LegacySchemaAndData =
        """
        CREATE TABLE SampleItems (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            Name        TEXT NOT NULL,
            Description TEXT NOT NULL,
            CreatedAt   TEXT NOT NULL
        );

        INSERT INTO SampleItems (Name, Description, CreatedAt)
        VALUES ('Introduce yourself', 'Say hello.', '2026-02-02T09:00:00Z');

        CREATE TABLE Posts (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId     INTEGER NOT NULL,
            AuthorName TEXT NOT NULL DEFAULT '',
            Title      TEXT NOT NULL,
            Content    TEXT NOT NULL,
            Category   TEXT NOT NULL,
            CreatedAt  TEXT NOT NULL,
            UpdatedAt  TEXT NOT NULL
        );

        INSERT INTO Posts (Id, UserId, AuthorName, Title, Content, Category, CreatedAt, UpdatedAt) VALUES
            (1, 2, 'lachlan', 'Welcome', 'Introduce yourself.', 'global',
                '2026-02-12T09:00:00Z', '2026-02-12T09:00:00Z'),
            (2, 2, 'lachlan', 'Vocabulary', 'What routine works?', 'es',
                '2026-02-12T10:00:00Z', '2026-02-12T10:00:00Z'),
            (3, 1, 'amber', 'Drills', 'Double consonants.', 'es',
                '2026-02-13T09:00:00Z', '2026-02-13T09:00:00Z');

        CREATE TABLE Comments (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            PostId     INTEGER NOT NULL,
            UserId     INTEGER NOT NULL,
            AuthorName TEXT NOT NULL DEFAULT '',
            Content    TEXT NOT NULL,
            CreatedAt  TEXT NOT NULL,
            UpdatedAt  TEXT NOT NULL,
            FOREIGN KEY (PostId) REFERENCES Posts (Id) ON DELETE CASCADE
        );

        INSERT INTO Comments (Id, PostId, UserId, AuthorName, Content, CreatedAt, UpdatedAt)
        VALUES (1, 2, 4, 'justin', 'Spaced repetition.', '2026-02-12T11:00:00Z', '2026-02-12T11:00:00Z');
        """;

    private string databasePath = null!;
    private string connectionString = null!;
    private DiscussionRepository repository = null!;

    [SetUp]
    public void SetUp()
    {
        databasePath = TestDatabase.NewPath();
        connectionString = TestDatabase.ConnectionStringFor(databasePath);

        TestDatabase.Execute(connectionString, LegacySchemaAndData);
        TestDatabase.Initialise(connectionString);

        repository = new DiscussionRepository(connectionString);
    }

    [TearDown]
    public void TearDown() => TestDatabase.Delete(databasePath);

    [Test]
    public void Migration_MintsOneForumPerDistinctCategory()
    {
        var forums = repository.GetForums();

        Assert.Multiple(() =>
        {
            Assert.That(forums.Select(forum => forum.Code), Is.EquivalentTo(new[] { "global", "es" }));
            Assert.That(forums.Select(forum => forum.CourseId), Is.All.Null);
        });
    }

    [Test]
    public void Migration_TitleCasesTheCategoryIntoAName()
    {
        Assert.Multiple(() =>
        {
            Assert.That(repository.GetForum("global")!.Name, Is.EqualTo("Global"));
            Assert.That(repository.GetForum("es")!.Name, Is.EqualTo("Es"));
        });
    }

    [Test]
    public void Migration_KeepsEveryPostInTheForumItsCategoryNamed()
    {
        var posts = repository.GetPosts(null, null, null, 20, 0, null);

        Assert.That(
            posts.Select(post => (post.Id, post.ForumCode)),
            Is.EquivalentTo(new[] { (1, "global"), (2, "es"), (3, "es") }));
    }

    // The swap drops and rebuilds Posts, which would cascade these away with keys on.
    [Test]
    public void Migration_KeepsTheCommentsHangingOffAMigratedPost()
    {
        var comments = repository.GetComments(2, 20, 0, null);

        Assert.That(comments.Select(comment => comment.Content), Is.EqualTo(new[] { "Spaced repetition." }));
    }

    [Test]
    public void Migration_DoesNotSeedOverTheVolumeItJustMigrated()
    {
        Assert.That(TestDatabase.Count(connectionString, "SELECT COUNT(*) FROM Posts;"), Is.EqualTo(3));
    }

    [Test]
    public void Migration_LeavesTheForumsReadyForTheFirstCourseSync()
    {
        var result = repository.SyncCourseForums([new CatalogCourse(5, "es", "Spanish")]);

        var spanish = repository.GetForum("es");

        Assert.Multiple(() =>
        {
            Assert.That(result.Added, Is.EqualTo(0));
            Assert.That(spanish!.CourseId, Is.EqualTo(5));
            Assert.That(spanish.Name, Is.EqualTo("Spanish"));
            Assert.That(repository.GetPost(2, null)!.ForumCode, Is.EqualTo("es"));
        });
    }

    [Test]
    public void Migration_IsNotRepeatedOnTheNextStartUp()
    {
        TestDatabase.Initialise(connectionString);

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetForums(), Has.Count.EqualTo(2));
            Assert.That(repository.GetPosts(null, null, null, 20, 0, null), Has.Count.EqualTo(3));
        });
    }
}
