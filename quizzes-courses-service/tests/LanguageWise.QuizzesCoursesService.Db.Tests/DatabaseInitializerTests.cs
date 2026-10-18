using LanguageWise.QuizzesCoursesService.Db.Data;
using Npgsql;
using LanguageWise.Testing;
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
        "60-pl.sql",
        "70-seed-milestone-sample-data.sql"
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

        var exception = Assert.Throws<PostgresException>(sandbox.Initialise);

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
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM SeedHistory;"), Is.Zero);
            Assert.That(
                sandbox.Scalar("SELECT COUNT(*) FROM Lessons WHERE Slug = 'rollback-probe';"),
                Is.Zero);
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM pg_constraint WHERE connamespace = current_schema()::regnamespace AND contype = 'f' AND NOT convalidated;"), Is.Zero);
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

    [Test]
    public void Initialise_SeedsBusinessRuleConsistentSampleProgressIdempotently()
    {
        using var sandbox = SqlSandbox.Create();

        sandbox.Initialise();
        sandbox.Initialise();

        Assert.Multiple(() =>
        {
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM QuizAttempts;"), Is.EqualTo(29));
            Assert.That(
                sandbox.Scalar("SELECT COUNT(*) FROM QuizAttempts WHERE CompletedAt IS NULL;"),
                Is.EqualTo(1));
            Assert.That(sandbox.Scalar("SELECT COUNT(*) FROM QuizAnswers;"), Is.EqualTo(280));
            Assert.That(
                sandbox.Scalar(
                    """
                    SELECT COUNT(*)
                    FROM QuizAttempts attempt
                    WHERE attempt.CompletedAt IS NOT NULL
                      AND (
                          SELECT COUNT(*)
                          FROM QuizAnswers answer
                          WHERE answer.AttemptId = attempt.Id
                      ) <> attempt.TotalQuestions;
                    """),
                Is.Zero);
            Assert.That(
                sandbox.Scalar(
                    """
                    SELECT COUNT(*)
                    FROM Milestones milestone
                    WHERE milestone.QuizId IS NOT NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM QuizAttempts attempt
                          WHERE attempt.UserId = milestone.UserId
                            AND attempt.QuizId = milestone.QuizId
                            AND attempt.Score >= 8
                            AND attempt.CompletedAt IS NOT NULL
                      );
                    """),
                Is.Zero);
            Assert.That(
                sandbox.Strings(
                    """
                    SELECT course.Code
                    FROM Milestones milestone
                    JOIN Courses course ON course.Id = milestone.CourseId
                    WHERE milestone.UserId = 1
                    ORDER BY course.Code;
                    """),
                Is.EqualTo(new[] { "pl" }));
            Assert.That(
                sandbox.Scalar(
                    """
                    SELECT COUNT(*)
                    FROM Milestones courseMilestone
                    WHERE courseMilestone.CourseId IS NOT NULL
                      AND (
                          EXISTS (
                              SELECT 1
                              FROM Lessons lesson
                              WHERE lesson.CourseId = courseMilestone.CourseId
                                AND NOT EXISTS (
                                    SELECT 1
                                    FROM Milestones lessonMilestone
                                    WHERE lessonMilestone.UserId = courseMilestone.UserId
                                      AND lessonMilestone.LessonId = lesson.Id
                                )
                          )
                          OR EXISTS (
                              SELECT 1
                              FROM Quizzes quiz
                              JOIN Lessons lesson ON lesson.Id = quiz.LessonId
                              WHERE lesson.CourseId = courseMilestone.CourseId
                                AND NOT EXISTS (
                                    SELECT 1
                                    FROM Milestones quizMilestone
                                    WHERE quizMilestone.UserId = courseMilestone.UserId
                                      AND quizMilestone.QuizId = quiz.Id
                                )
                          )
                      );
                    """),
                Is.Zero);
        });
    }

    [Test]
    public void Initialise_ReplacesLegacyGermanOnlySampleProgress()
    {
        const string sampleSeed = "70-seed-milestone-sample-data.sql";
        using var sandbox = SqlSandbox.Create();

        sandbox.ReplaceSeed(
            sampleSeed,
            """
            INSERT INTO Milestones (UserId, LessonId, CompletedAt)
            SELECT 1, Lessons.Id, '2026-01-01T00:00:00.0000000+00:00'
            FROM Lessons
            JOIN Courses ON Courses.Id = Lessons.CourseId
            WHERE Courses.Code = 'de' ON CONFLICT DO NOTHING;
            """);
        sandbox.Initialise();

        Assert.That(
            sandbox.Scalar(
                """
                SELECT COUNT(*)
                FROM Milestones milestone
                JOIN Lessons lesson ON lesson.Id = milestone.LessonId
                JOIN Courses course ON course.Id = lesson.CourseId
                WHERE milestone.UserId = 1
                  AND course.Code = 'de';
                """),
            Is.EqualTo(20));

        sandbox.RestoreSeed(sampleSeed);
        sandbox.Scalar("DELETE FROM SeedHistory RETURNING 1;");
        sandbox.Initialise();

        Assert.Multiple(() =>
        {
            Assert.That(
                sandbox.Scalar(
                    """
                    SELECT COUNT(*)
                    FROM Milestones milestone
                    JOIN Lessons lesson ON lesson.Id = milestone.LessonId
                    JOIN Courses course ON course.Id = lesson.CourseId
                    WHERE milestone.UserId = 1
                      AND course.Code = 'de';
                    """),
                Is.EqualTo(6));
            Assert.That(
                sandbox.Scalar(
                    """
                    SELECT COUNT(*)
                    FROM Milestones
                    WHERE UserId = 1
                      AND CompletedAt = '2026-01-01T00:00:00.0000000+00:00';
                    """),
                Is.Zero);
        });
    }

    [Test]
    public void Initialise_SynchronizesEveryIdentityAfterExplicitSeedIdsAndNeverRewindsAllocation()
    {
        using var sandbox = SqlSandbox.Create();
        foreach (var fileName in ExpectedSeedManifest)
        {
            sandbox.ReplaceSeed(fileName, "SELECT 1;");
        }

        sandbox.ReplaceSeed("00-courses.sql",
            """
            INSERT INTO Courses (Id, Code, Title) VALUES (5000, 'de', 'German') ON CONFLICT DO NOTHING;
            INSERT INTO Lessons (Id, CourseId, Slug, Title, SortOrder, ContentMarkdown)
            VALUES (5000, 5000, 'seed', 'Seed', 1, '') ON CONFLICT DO NOTHING;
            INSERT INTO LessonVocabulary (Id, LessonId, VocabularyJson)
            VALUES (5000, 5000, '{"words":[]}') ON CONFLICT DO NOTHING;
            INSERT INTO Quizzes (Id, LessonId, Title) VALUES (5000, 5000, 'Seed quiz') ON CONFLICT DO NOTHING;
            INSERT INTO QuizQuestions (Id, QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
            VALUES (5000, 5000, 1, 'Seed question', 'free_text', '{}', 'answer') ON CONFLICT DO NOTHING;
            INSERT INTO QuizAttempts (Id, UserId, QuizId, TotalQuestions, StartedAt)
            VALUES (5000, 64, 5000, 1, '2026-10-18T00:00:00Z') ON CONFLICT DO NOTHING;
            INSERT INTO QuizAnswers (Id, AttemptId, QuestionId, StudentResponse, IsCorrect, AnsweredAt)
            VALUES (5000, 5000, 5000, 'answer', true, '2026-10-18T00:00:00Z') ON CONFLICT DO NOTHING;
            INSERT INTO Milestones (Id, UserId, LessonId, CompletedAt)
            VALUES (5000, 64, 5000, '2026-10-18T00:00:00Z') ON CONFLICT DO NOTHING;
            """);

        sandbox.Initialise();
        sandbox.Initialise();

        var insertions = new[]
        {
            "INSERT INTO Courses (Code, Title) VALUES ('fr', 'French') RETURNING Id;",
            "INSERT INTO Lessons (CourseId, Slug, Title, SortOrder, ContentMarkdown) VALUES (5000, 'new', 'New', 2, '') RETURNING Id;",
            "INSERT INTO LessonVocabulary (LessonId, VocabularyJson) SELECT Id, '{}' FROM Lessons WHERE Slug = 'new' RETURNING Id;",
            "INSERT INTO Quizzes (LessonId, Title) SELECT Id, 'New' FROM Lessons WHERE Slug = 'new' RETURNING Id;",
            "INSERT INTO QuizQuestions (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer) VALUES (5000, 2, 'New', 'free_text', '{}', 'answer') RETURNING Id;",
            "INSERT INTO QuizAttempts (UserId, QuizId, TotalQuestions, StartedAt) VALUES (64, 5000, 1, '2026-10-18T01:00:00Z') RETURNING Id;",
            "INSERT INTO QuizAnswers (AttemptId, QuestionId, StudentResponse, IsCorrect, AnsweredAt) SELECT 5000, Id, 'answer', true, '2026-10-18T01:00:00Z' FROM QuizQuestions WHERE SortOrder = 2 RETURNING Id;",
            "INSERT INTO Milestones (UserId, QuizId, CompletedAt) VALUES (64, 5000, '2026-10-18T01:00:00Z') RETURNING Id;"
        };

        foreach (var sql in insertions)
        {
            Assert.That(sandbox.Scalar(sql), Is.GreaterThan(5000));
        }

        var allocated = sandbox.Scalar("SELECT nextval(pg_get_serial_sequence('quizattempts', 'id'));");
        sandbox.Initialise();
        Assert.That(sandbox.Scalar("SELECT nextval(pg_get_serial_sequence('quizattempts', 'id'));"), Is.GreaterThan(allocated));
    }

    [Test]
    public async Task Initialise_WaitsForLiveInsertBeforeSynchronizingIdentities()
    {
        using var sandbox = SqlSandbox.Create();
        sandbox.Initialise();
        File.WriteAllText(Path.Combine(sandbox.SqlDirectory, "schema.sql"), "SELECT 1;");
        using var writer = new NpgsqlConnection(sandbox.ConnectionString);
        writer.Open();
        using var transaction = writer.BeginTransaction();
        using var insert = writer.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            "INSERT INTO QuizAttempts (UserId, QuizId, TotalQuestions, StartedAt) SELECT 99, Id, 1, '2026-10-18T00:00:00Z' FROM Quizzes ORDER BY Id LIMIT 1 RETURNING Id;";
        var insertedId = Convert.ToInt64(insert.ExecuteScalar());

        var initialization = Task.Run(sandbox.Initialise);
        bool blocked;
        try
        {
            blocked = SpinWait.SpinUntil(() => sandbox.Scalar(
                "SELECT COUNT(*) FROM pg_locks WHERE relation = 'quizattempts'::regclass AND mode = 'ShareRowExclusiveLock' AND NOT granted;") > 0,
                TimeSpan.FromSeconds(10));
        }
        finally
        {
            transaction.Commit();
            await initialization;
        }

        Assert.That(blocked, Is.True, "Initialization must wait for live inserts before resetting sequences.");
        Assert.That(sandbox.Scalar("SELECT nextval(pg_get_serial_sequence('quizattempts', 'id'));"), Is.GreaterThan(insertedId));
    }

    private sealed class SqlSandbox : IDisposable
    {
        private readonly string rootDirectory;
        private readonly PostgresTestDatabase database = new();

        private SqlSandbox(string rootDirectory)
        {
            this.rootDirectory = rootDirectory;
            SqlDirectory = Path.Combine(rootDirectory, "sql");
            SeedsDirectory = Path.Combine(SqlDirectory, "seeds");
            ConnectionString = database.ConnectionString;
        }

        public string SqlDirectory { get; }

        public string SeedsDirectory { get; }

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

        public void RestoreSeed(string fileName) =>
            File.Copy(
                Path.Combine(GetSourceSqlDirectory(), "seeds", fileName),
                Path.Combine(SeedsDirectory, fileName),
                overwrite: true);

        public bool TableExists(string tableName)
        {
            using var connection = Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = current_schema() AND table_name = lower(@name));";
            command.Parameters.AddWithValue("@name", tableName);
            return (bool)command.ExecuteScalar()!;
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
            database.Dispose();

            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }

        private NpgsqlConnection Open()
        {
            var connection = new NpgsqlConnection(ConnectionString);
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
