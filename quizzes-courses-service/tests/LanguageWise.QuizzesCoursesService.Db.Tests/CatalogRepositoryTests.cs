using LanguageWise.QuizzesCoursesService.Db.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.QuizzesCoursesService.Db.Tests;

[TestFixture]
public sealed class CatalogRepositoryTests
{
    private string databasePath = null!;
    private string connectionString = null!;

    [SetUp]
    public void SetUp()
    {
        databasePath = Path.Combine(Path.GetTempPath(), $"languagewise-{Guid.NewGuid():N}.db");
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString();

        var sqlDirectory = Path.Combine(
            FindRepositoryRoot(),
            "quizzes-courses-service",
            "database",
            "LanguageWise.QuizzesCoursesService.Db",
            "sql");
        var initializer = new DatabaseInitializer(
            connectionString,
            sqlDirectory,
            NullLogger<DatabaseInitializer>.Instance);
        initializer.Initialise();
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    [Test]
    public void Initialisation_SeedsAllSixSupportedCourses()
    {
        var repository = new CatalogRepository(connectionString);

        var courses = repository.GetCourses();

        Assert.That(courses.Select(course => course.Code), Is.EqualTo(new[] { "de", "fr", "it", "nl", "es", "pl" }));
    }

    [Test]
    public void Initialisation_IsIdempotent()
    {
        var sqlDirectory = Path.Combine(
            FindRepositoryRoot(),
            "quizzes-courses-service",
            "database",
            "LanguageWise.QuizzesCoursesService.Db",
            "sql");
        var initializer = new DatabaseInitializer(
            connectionString,
            sqlDirectory,
            NullLogger<DatabaseInitializer>.Instance);

        initializer.Initialise();

        Assert.That(new CatalogRepository(connectionString).CountCourses(), Is.EqualTo(6));
    }

    [Test]
    public void GetLessons_ReturnsLessonsInCourseOrder()
    {
        var lessons = new CatalogRepository(connectionString).GetLessons("de");

        Assert.That(lessons.Select(lesson => lesson.Slug), Is.EqualTo(new[] { "welcome", "greetings" }));
        Assert.That(lessons.Select(lesson => lesson.SortOrder), Is.Ordered);
    }

    [TestCase("de", "German", "Hallo")]
    [TestCase("pl", "Polish", "Cześć")]
    public void GetLesson_ReturnsMarkdownAndTypedVocabulary(
        string courseCode,
        string courseTitle,
        string firstVocabularyWord)
    {
        var lesson = new CatalogRepository(connectionString).GetLesson(courseCode, "welcome");

        Assert.That(lesson, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(lesson!.ContentMarkdown, Does.StartWith($"# Welcome to {courseTitle}"));
            Assert.That(lesson.Vocabulary, Has.Count.EqualTo(2));
            Assert.That(lesson.Vocabulary[0].Word, Is.EqualTo(firstVocabularyWord));
        });
    }

    [Test]
    public void Schema_RejectsUnsupportedCourseCodes()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO Courses (Code, Title, Description) VALUES ('xx', 'Invalid', 'Invalid');";

        Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
    }

    [Test]
    public void Schema_RejectsInvalidVocabularyJson()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE LessonVocabulary
            SET VocabularyJson = 'not-json'
            WHERE Id = (SELECT MIN(Id) FROM LessonVocabulary);
            """;

        Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
    }

    [Test]
    public void Schema_RequiresExactlyOneMilestoneTarget()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO Milestones (UserId, CourseId, LessonId, CompletedAt)
            VALUES (1, 1, 1, '2026-08-28T00:00:00Z');
            """;

        Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the LanguageWise repository root.");
    }
}
