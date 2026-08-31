using LanguageWise.QuizzesCoursesService.Db.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.QuizzesCoursesService.Db.Tests;

[TestFixture]
public sealed class DatabaseInitializerTests
{
    private static readonly string[] ExpectedSeedManifest =
    [
        "00-courses.sql",
        "10-de.sql",
        "20-fr.sql",
        "30-it.sql",
        "40-nl.sql",
        "50-es.sql",
        "60-pl.sql"
    ];

    [Test]
    public void Initialise_WhenExpectedSeedIsMissing_FailsWithExactManifestDetails()
    {
        using var sandbox = SqlSandbox.Create();
        File.Delete(Path.Combine(sandbox.SeedsDirectory, "30-it.sql"));

        var exception = Assert.Throws<InvalidOperationException>(sandbox.Initialise);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("Seed SQL manifest mismatch"));
            Assert.That(exception.Message, Does.Contain("Missing: 30-it.sql"));
            Assert.That(exception.Message, Does.Contain("Unexpected: (none)"));
            Assert.That(sandbox.TableExists("Courses"), Is.True);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM Courses;"), Is.Zero);
        });
    }

    [Test]
    public void Initialise_WhenUnexpectedSqlSeedExists_FailsWithExactManifestDetails()
    {
        using var sandbox = SqlSandbox.Create();
        File.WriteAllText(
            Path.Combine(sandbox.SeedsDirectory, "35-unexpected.sql"),
            "SELECT 1;");

        var exception = Assert.Throws<InvalidOperationException>(sandbox.Initialise);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("Seed SQL manifest mismatch"));
            Assert.That(exception.Message, Does.Contain("Missing: (none)"));
            Assert.That(exception.Message, Does.Contain("Unexpected: 35-unexpected.sql"));
            Assert.That(sandbox.TableExists("Courses"), Is.True);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM Courses;"), Is.Zero);
        });
    }

    [Test]
    public void Initialise_WhenMiddleSeedFails_RollsBackEverySeedScriptButRetainsSchema()
    {
        using var sandbox = SqlSandbox.Create();
        File.WriteAllText(
            Path.Combine(sandbox.SeedsDirectory, "30-it.sql"),
            """
            INSERT INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
            SELECT Id, 'rollback-probe', 'Rollback probe', 999, 'detectable row'
            FROM Courses
            WHERE Code = 'de';

            INSERT INTO MissingSeedTarget (Value) VALUES ('force rollback');
            """);

        var exception = Assert.Throws<SqliteException>(sandbox.Initialise);

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("MissingSeedTarget").IgnoreCase);
            Assert.That(sandbox.TableExists("Courses"), Is.True);
            Assert.That(sandbox.TableExists("Lessons"), Is.True);
            Assert.That(sandbox.TableExists("LessonVocabulary"), Is.True);
            Assert.That(sandbox.TableExists("Quizzes"), Is.True);
            Assert.That(sandbox.TableExists("QuizQuestions"), Is.True);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM Courses;"), Is.Zero);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM Lessons;"), Is.Zero);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM LessonVocabulary;"), Is.Zero);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM Quizzes;"), Is.Zero);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM QuizQuestions;"), Is.Zero);
            Assert.That(
                sandbox.Scalar("SELECT COUNT(*) FROM Lessons WHERE Slug = 'rollback-probe';"),
                Is.Zero);
            Assert.That(sandbox.RowCount("PRAGMA foreign_key_check;"), Is.Zero);
        });
    }

    [Test]
    public void Initialise_ExecutesTheManifestInDeterministicDependencyOrder()
    {
        using var sandbox = SqlSandbox.Create();

        sandbox.ReplaceSeed(
            "00-courses.sql",
            "INSERT INTO Courses (Code, Title, Description) VALUES ('de', 'German', 'ordered 1');");
        sandbox.ReplaceSeed(
            "10-de.sql",
            """
            INSERT INTO Courses (Code, Title, Description)
            SELECT 'fr', 'French', 'ordered 2'
            WHERE EXISTS (SELECT 1 FROM Courses WHERE Code = 'de' AND Description = 'ordered 1');
            """);
        sandbox.ReplaceSeed(
            "20-fr.sql",
            """
            INSERT INTO Courses (Code, Title, Description)
            SELECT 'it', 'Italian', 'ordered 3'
            WHERE EXISTS (SELECT 1 FROM Courses WHERE Code = 'fr' AND Description = 'ordered 2');
            """);
        sandbox.ReplaceSeed(
            "30-it.sql",
            """
            INSERT INTO Courses (Code, Title, Description)
            SELECT 'nl', 'Dutch', 'ordered 4'
            WHERE EXISTS (SELECT 1 FROM Courses WHERE Code = 'it' AND Description = 'ordered 3');
            """);
        sandbox.ReplaceSeed(
            "40-nl.sql",
            """
            INSERT INTO Courses (Code, Title, Description)
            SELECT 'es', 'Spanish', 'ordered 5'
            WHERE EXISTS (SELECT 1 FROM Courses WHERE Code = 'nl' AND Description = 'ordered 4');
            """);
        sandbox.ReplaceSeed(
            "50-es.sql",
            """
            INSERT INTO Courses (Code, Title, Description)
            SELECT 'pl', 'Polish', 'ordered 6'
            WHERE EXISTS (SELECT 1 FROM Courses WHERE Code = 'es' AND Description = 'ordered 5');
            """);
        sandbox.ReplaceSeed(
            "60-pl.sql",
            """
            INSERT INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown)
            SELECT Id, 'manifest-complete', 'Manifest complete', 1, 'ordered 7'
            FROM Courses
            WHERE Code = 'pl'
              AND Description = 'ordered 6'
              AND (SELECT COUNT(*) FROM Courses) = 6;
            """);

        sandbox.Initialise();

        Assert.Multiple(() =>
        {
            Assert.That(
                sandbox.Strings("SELECT Code FROM Courses ORDER BY Id;"),
                Is.EqualTo(new[] { "de", "fr", "it", "nl", "es", "pl" }));
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM Courses;"), Is.EqualTo(6));
            Assert.That(
                sandbox.Scalar("SELECT COUNT(*) FROM Lessons WHERE Slug = 'manifest-complete';"),
                Is.EqualTo(1));
        });
    }

    private sealed class SqlSandbox : IDisposable
    {
        private readonly string rootDirectory;

        private SqlSandbox(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
            SqlDirectory = Path.Combine(rootDirectory, "sql");
            SeedsDirectory = Path.Combine(SqlDirectory, "seeds");
            DatabasePath = Path.Combine(rootDirectory, "database.db");
            ConnectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                ForeignKeys = true
            }.ToString();
        }

        public string SqlDirectory { get; }

        public string SeedsDirectory { get; }

        public string DatabasePath { get; }

        public string ConnectionString { get; }

        public static SqlSandbox Create()
        {
            var sandbox = new SqlSandbox(Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"database-initializer-{Guid.NewGuid():N}"));

            try
            {
                Directory.CreateDirectory(sandbox.SeedsDirectory);

                var sourceSqlDirectory = GetSourceSqlDirectory();
                File.Copy(
                    Path.Combine(sourceSqlDirectory, "schema.sql"),
                    Path.Combine(sandbox.SqlDirectory, "schema.sql"));

                foreach (var seedFileName in ExpectedSeedManifest)
                {
                    File.Copy(
                        Path.Combine(sourceSqlDirectory, "seeds", seedFileName),
                        Path.Combine(sandbox.SeedsDirectory, seedFileName));
                }

                return sandbox;
            }
            catch
            {
                sandbox.Dispose();
                throw;
            }
        }

        public void Initialise() =>
            new DatabaseInitializer(
                ConnectionString,
                SqlDirectory,
                NullLogger<DatabaseInitializer>.Instance).Initialise();

        public void ReplaceSeed(string fileName, string sql) =>
            File.WriteAllText(Path.Combine(SeedsDirectory, fileName), sql);

        public bool TableExists(string tableName)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name);";
            command.Parameters.AddWithValue("$name", tableName);
            return Convert.ToInt64(command.ExecuteScalar()) == 1;
        }

        public long Scalar(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar());
        }

        public int RowCount(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();

            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        public IReadOnlyList<string> Strings(string sql)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            using var reader = command.ExecuteReader();

            var values = new List<string>();
            while (reader.Read())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }

        private SqliteConnection Open()
        {
            var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        private static string GetSourceSqlDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            {
                directory = directory.Parent;
            }

            return directory is null
                ? throw new DirectoryNotFoundException("Could not locate the LanguageWise repository root.")
                : Path.Combine(
                    directory.FullName,
                    "quizzes-courses-service",
                    "database",
                    "LanguageWise.QuizzesCoursesService.Db",
                    "sql");
        }
    }
}
