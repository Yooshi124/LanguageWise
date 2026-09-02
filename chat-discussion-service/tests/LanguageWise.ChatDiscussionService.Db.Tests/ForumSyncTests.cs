using LanguageWise.ChatDiscussionService.Db.Data;
using LanguageWise.ChatDiscussionService.Db.Models;

namespace LanguageWise.ChatDiscussionService.Db.Tests;

[TestFixture]
public sealed class ForumSyncTests
{
    /// <summary>The catalogue as the quizzes and courses service seeds it.</summary>
    private static readonly CatalogCourse[] SeededCatalogue =
    [
        new CatalogCourse(1, "de", "German"),
        new CatalogCourse(2, "fr", "French"),
        new CatalogCourse(3, "it", "Italian"),
        new CatalogCourse(4, "nl", "Dutch"),
        new CatalogCourse(5, "es", "Spanish"),
        new CatalogCourse(6, "pl", "Polish")
    ];

    private const int SpanishForumId = 6;
    private const int SpanishCourseId = 5;

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
    public void Seeding_MirrorsTheCourseCatalogue()
    {
        var forums = repository.GetForums();

        Assert.Multiple(() =>
        {
            Assert.That(
                forums.Select(forum => (forum.Code, forum.CourseId)),
                Is.SupersetOf(SeededCatalogue.Select(course => (course.Code, (int?)course.Id))));
            Assert.That(repository.GetForum("global")!.CourseId, Is.Null);
        });
    }

    [Test]
    public void SyncCourseForums_AgainstTheSeededCatalogue_ChangesNothing()
    {
        var result = repository.SyncCourseForums(SeededCatalogue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Added, Is.EqualTo(0));
            Assert.That(result.Renamed, Is.EqualTo(0));
            Assert.That(repository.GetForums(), Has.Count.EqualTo(7));
        });
    }

    [Test]
    public void SyncCourseForums_WhenACourseIsRenamed_RenamesTheForumItOwns()
    {
        var result = repository.SyncCourseForums([new CatalogCourse(SpanishCourseId, "es", "Spanish A1")]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Renamed, Is.EqualTo(1));
            Assert.That(result.Added, Is.EqualTo(0));
            Assert.That(repository.GetForum("es")!.Name, Is.EqualTo("Spanish A1"));
            Assert.That(repository.GetForum("es")!.Id, Is.EqualTo(SpanishForumId));
        });
    }

    [Test]
    public void SyncCourseForums_RenamingAForum_KeepsThePostsInIt()
    {
        repository.SyncCourseForums([new CatalogCourse(SpanishCourseId, "es", "Spanish A1")]);

        var post = repository.GetPost(2, null);

        Assert.Multiple(() =>
        {
            Assert.That(post!.ForumId, Is.EqualTo(SpanishForumId));
            Assert.That(post.ForumName, Is.EqualTo("Spanish A1"));
        });
    }

    [Test]
    public void SyncCourseForums_WhenACourseChangesItsCode_KeepsTheForumCodeItWasGiven()
    {
        repository.SyncCourseForums([new CatalogCourse(SpanishCourseId, "spanish", "Spanish A1")]);

        Assert.Multiple(() =>
        {
            Assert.That(repository.GetForum("es")!.Name, Is.EqualTo("Spanish A1"));
            Assert.That(repository.GetForum("spanish"), Is.Null);
            Assert.That(repository.GetForums(), Has.Count.EqualTo(7));
        });
    }

    // The path a migrated volume takes: its forums carry no CourseId.
    [Test]
    public void SyncCourseForums_AdoptsAForumThatHasNoCourseYet()
    {
        TestDatabase.Execute(
            connectionString,
            "INSERT INTO Forums (CourseId, Code, Name) VALUES (NULL, 'ja', 'Ja');");

        var result = repository.SyncCourseForums([new CatalogCourse(99, "ja", "Japanese")]);

        var japanese = repository.GetForum("ja");

        Assert.Multiple(() =>
        {
            Assert.That(result.Added, Is.EqualTo(0));
            Assert.That(japanese!.CourseId, Is.EqualTo(99));
            Assert.That(japanese.Name, Is.EqualTo("Japanese"));
            Assert.That(repository.GetForums(), Has.Count.EqualTo(8));
        });
    }

    [Test]
    public void SyncCourseForums_WithACourseNoForumMatches_AddsOne()
    {
        var result = repository.SyncCourseForums([new CatalogCourse(99, "ja", "Japanese")]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Added, Is.EqualTo(1));
            Assert.That(repository.GetForum("ja")!.CourseId, Is.EqualTo(99));
        });
    }

    [Test]
    public void SyncCourseForums_NeverDeletesAForumTheCatalogueNoLongerLists()
    {
        repository.SyncCourseForums([new CatalogCourse(SpanishCourseId, "es", "Spanish")]);

        Assert.That(
            repository.GetForums().Select(forum => forum.Code),
            Is.EquivalentTo(new[] { "global", "de", "fr", "it", "nl", "es", "pl" }));
    }

    // The volume the pre-release seed left behind: its categories were spelled out in
    // full, so the migration minted "italian" beside the course's own "it" and the
    // forum list carried Italian twice.
    [Test]
    public void SyncCourseForums_FoldsALegacyLanguageForumIntoTheCourseThatOwnsIt()
    {
        TestDatabase.Execute(
            connectionString,
            "INSERT INTO Forums (CourseId, Code, Name) VALUES (NULL, 'italian', 'Italian');");

        var result = repository.SyncCourseForums(SeededCatalogue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Merged, Is.EqualTo(1));
            Assert.That(repository.GetForum("italian"), Is.Null);
            Assert.That(
                repository.GetForums().Select(forum => forum.Code),
                Is.EquivalentTo(new[] { "global", "de", "fr", "it", "nl", "es", "pl" }));
        });
    }

    [Test]
    public void SyncCourseForums_MergingALegacyForum_MovesItsPostsIntoTheCourseForum()
    {
        TestDatabase.Execute(
            connectionString,
            """
            INSERT INTO Forums (CourseId, Code, Name) VALUES (NULL, 'italian', 'Italian');

            INSERT INTO Posts (Id, UserId, AuthorName, Title, Content, ForumId, CreatedAt, UpdatedAt)
            SELECT 900, 1, 'amber', 'Legacy post', 'Written before the catalogue sync.', Id,
                   '2026-02-14T09:00:00Z', '2026-02-14T09:00:00Z'
              FROM Forums WHERE Code = 'italian';
            """);

        repository.SyncCourseForums(SeededCatalogue);

        var post = repository.GetPost(900, null);

        Assert.Multiple(() =>
        {
            Assert.That(post!.ForumCode, Is.EqualTo("it"));
            Assert.That(post.ForumName, Is.EqualTo("Italian"));
        });
    }

    // Japanese was a legacy category no course ever backed, so there is nothing to
    // fold it into and it keeps the posts it holds.
    [Test]
    public void SyncCourseForums_LeavesALegacyForumNoCourseClaims()
    {
        TestDatabase.Execute(
            connectionString,
            "INSERT INTO Forums (CourseId, Code, Name) VALUES (NULL, 'japanese', 'Japanese');");

        var result = repository.SyncCourseForums(SeededCatalogue);

        Assert.Multiple(() =>
        {
            Assert.That(result.Merged, Is.EqualTo(0));
            Assert.That(repository.GetForum("japanese")!.CourseId, Is.Null);
        });
    }

    // Global survives only because no course carries its code; it is not protected.
    [Test]
    public void SyncCourseForums_AcrossTheWholeCatalogue_LeavesGlobalAlone()
    {
        repository.SyncCourseForums(SeededCatalogue);

        var global = repository.GetForum("global");

        Assert.Multiple(() =>
        {
            Assert.That(global!.Name, Is.EqualTo("Global"));
            Assert.That(global.CourseId, Is.Null);
        });
    }
}
