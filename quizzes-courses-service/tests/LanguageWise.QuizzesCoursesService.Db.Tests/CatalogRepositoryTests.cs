using LanguageWise.QuizzesCoursesService.Db.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;

namespace LanguageWise.QuizzesCoursesService.Db.Tests;

[TestFixture]
public sealed class CatalogRepositoryTests
{
    private static readonly string[] ExpectedLessonSlugs =
    [
        "greetings",
        "introductions",
        "politeness",
        "numbers",
        "family",
        "food",
        "drinks",
        "home",
        "travel",
        "directions",
        "time-calendar",
        "weather",
        "shopping",
        "work-school",
        "body-health",
        "emotions",
        "hobbies",
        "nature-animals",
        "long-words",
        "funny-unusual-words"
    ];

    private string databasePath = null!;
    private string connectionString = null!;

    [SetUp]
    public void SetUp()
    {
        databasePath = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            $"languagewise-catalog-{Guid.NewGuid():N}.db");
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
    public void GetLessons_ReturnsTwentyAlignedLessonsInCourseOrder()
    {
        var repository = new CatalogRepository(connectionString);
        var lessonsByCourse = repository.GetCourses()
            .ToDictionary(course => course.Code, course => repository.GetLessons(course.Code));

        Assert.Multiple(() =>
        {
            foreach (var lessons in lessonsByCourse.Values)
            {
                Assert.That(lessons, Has.Count.EqualTo(20));
                Assert.That(lessons.Select(lesson => lesson.Slug), Is.EqualTo(ExpectedLessonSlugs));
                Assert.That(lessons.Select(lesson => lesson.SortOrder), Is.EqualTo(Enumerable.Range(1, 20)));
            }
        });
    }

    [TestCase("de", "Hallo")]
    [TestCase("pl", "Cześć")]
    public void GetLesson_ReturnsMarkdownAndTypedVocabulary(
        string courseCode,
        string firstVocabularyWord)
    {
        var lesson = new CatalogRepository(connectionString).GetLesson(courseCode, "greetings");

        Assert.That(lesson, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(lesson!.ContentMarkdown, Does.StartWith("## Learn in context"));
            Assert.That(lesson.ContentMarkdown, Does.Not.StartWith("# "));
            Assert.That(lesson.Vocabulary.Count, Is.InRange(5, 10));
            Assert.That(lesson.Vocabulary[0].Word, Is.EqualTo(firstVocabularyWord));
        });
    }

    [Test]
    public void Initialisation_SeedsStructuredLanguageSpecificContentForEveryLesson()
    {
        var repository = new CatalogRepository(connectionString);
        var workedUsageLessons = new HashSet<string>
        {
            "numbers",
            "nature-animals",
            "long-words",
            "funny-unusual-words"
        };

        foreach (var course in repository.GetCourses())
        {
            foreach (var lesson in repository.GetLessons(course.Code))
            {
                var detail = repository.GetLesson(course.Code, lesson.Slug)!;

                Assert.Multiple(() =>
                {
                    Assert.That(detail.ContentMarkdown, Does.StartWith("## Learn in context"));
                    Assert.That(detail.ContentMarkdown, Does.Not.StartWith("# "));
                    Assert.That(detail.ContentMarkdown, Does.Contain("| Target language | English |"));
                    Assert.That(
                        detail.Vocabulary.Count(word => ContentUsesVocabularyWord(detail.ContentMarkdown, word.Word)),
                        Is.GreaterThanOrEqualTo(1),
                        $"{course.Code}/{lesson.Slug} should use vocabulary in its target-language examples.");
                    Assert.That(detail.ContentMarkdown, Does.Contain("## Language note"));
                    Assert.That(detail.ContentMarkdown, Does.Not.Contain("Practise the vocabulary aloud"));

                    if (workedUsageLessons.Contains(lesson.Slug))
                    {
                        Assert.That(detail.ContentMarkdown, Does.Contain("## Worked usage"));
                        Assert.That(detail.ContentMarkdown, Does.Contain("- **"));
                        Assert.That(detail.ContentMarkdown, Does.Contain("— *"));
                    }
                    else
                    {
                        Assert.That(detail.ContentMarkdown, Does.Contain("## Mini dialogue"));
                        Assert.That(detail.ContentMarkdown, Does.Contain("> **A:**"));
                        Assert.That(detail.ContentMarkdown, Does.Contain("> *"));
                    }
                });
            }
        }
    }

    [Test]
    public void Initialisation_MiniDialoguesAreConversationalAndNotCopiedFromTheExampleTable()
    {
        var repository = new CatalogRepository(connectionString);
        var workedUsageLessons = new HashSet<string>
        {
            "numbers",
            "nature-animals",
            "long-words",
            "funny-unusual-words"
        };

        foreach (var course in repository.GetCourses())
        {
            foreach (var lesson in repository.GetLessons(course.Code))
            {
                if (workedUsageLessons.Contains(lesson.Slug))
                {
                    continue;
                }

                var detail = repository.GetLesson(course.Code, lesson.Slug)!;
                var label = $"{course.Code}/{lesson.Slug}";

                var tableSentences = ExtractTableTargetSentences(detail.ContentMarkdown);
                var dialogueTurns = ExtractDialogueTurns(detail.ContentMarkdown);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        dialogueTurns,
                        Has.Count.GreaterThanOrEqualTo(2),
                        $"{label} should have at least two dialogue turns.");
                    Assert.That(
                        dialogueTurns.Select(turn => turn.Speaker).Distinct().Count(),
                        Is.GreaterThanOrEqualTo(2),
                        $"{label} should have a partner (B) that responds, not a single speaker.");
                    Assert.That(
                        dialogueTurns.Any(turn => turn.Speaker == "A"),
                        Is.True,
                        $"{label} dialogue should open with speaker A.");
                    Assert.That(
                        dialogueTurns.Any(turn => turn.Speaker == "B"),
                        Is.True,
                        $"{label} dialogue should include a responding speaker B.");
                    Assert.That(
                        dialogueTurns.All(turn => !string.IsNullOrWhiteSpace(turn.Translation)),
                        Is.True,
                        $"{label} every dialogue turn should have an English translation line.");

                    // Guards against the previous defect where the two example-table
                    // sentences were simply copied verbatim into speakers A and B.
                    foreach (var turn in dialogueTurns)
                    {
                        Assert.That(
                            tableSentences,
                            Does.Not.Contain(Normalise(turn.Target)),
                            $"{label} dialogue turn '{turn.Target}' must not be copied verbatim from the example table.");
                    }
                });
            }
        }
    }

    [Test]
    public void Initialisation_SeedsFiveToTenVocabularyItemsForEveryLesson()
    {
        var repository = new CatalogRepository(connectionString);

        foreach (var course in repository.GetCourses())
        {
            foreach (var lesson in repository.GetLessons(course.Code))
            {
                var detail = repository.GetLesson(course.Code, lesson.Slug);

                Assert.That(
                    detail!.Vocabulary.Count,
                    Is.InRange(5, 10),
                    $"{course.Code}/{lesson.Slug} should have between five and ten vocabulary items.");
            }
        }
    }

    [TestCase("de", "Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz", "Ohrwurm")]
    [TestCase("fr", "anticonstitutionnellement", "pamplemousse")]
    [TestCase("it", "precipitevolissimevolmente", "pantofolaio")]
    [TestCase("nl", "meervoudigepersoonlijkheidsstoornis", "gezellig")]
    [TestCase("es", "electroencefalografista", "sobremesa")]
    [TestCase("pl", "konstantynopolitańczykowianeczka", "chrząszcz")]
    public void Initialisation_SeedsDedicatedLongAndFunnyWordLessons(
        string courseCode,
        string longWord,
        string funnyWord)
    {
        var repository = new CatalogRepository(connectionString);
        var longWords = repository.GetLesson(courseCode, "long-words");
        var funnyWords = repository.GetLesson(courseCode, "funny-unusual-words");

        Assert.Multiple(() =>
        {
            Assert.That(longWords, Is.Not.Null);
            Assert.That(funnyWords, Is.Not.Null);
            Assert.That(longWords!.Vocabulary.Count, Is.InRange(5, 10));
            Assert.That(funnyWords!.Vocabulary.Count, Is.InRange(5, 10));
            Assert.That(longWords.Vocabulary.Select(word => word.Word), Does.Contain(longWord));
            Assert.That(funnyWords.Vocabulary.Select(word => word.Word), Does.Contain(funnyWord));
            Assert.That(longWords.ContentMarkdown, Does.Contain(longWord).IgnoreCase);
            Assert.That(funnyWords.ContentMarkdown, Does.Contain(funnyWord).IgnoreCase);
        });

        if (courseCode == "de")
        {
            Assert.That(
                longWords!.Vocabulary.Single(word => word.Word == longWord).Meaning,
                Is.EqualTo("law delegating duties for supervising beef labeling"));
            Assert.That(longWords.ContentMarkdown, Does.Contain("Rindfleisch + Etikettierung"));
        }
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

    private static bool ContentUsesVocabularyWord(string contentMarkdown, string vocabularyWord)
    {
        var distinctiveWord = vocabularyWord
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(part => part.Any(char.IsLetter));

        return distinctiveWord is not null
            && contentMarkdown.Contains(distinctiveWord, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalise(string value)
        => Regex.Replace(value, "\\s+", " ").Trim();

    private static HashSet<string> ExtractTableTargetSentences(string contentMarkdown)
    {
        var sentences = new HashSet<string>();
        foreach (var rawLine in contentMarkdown.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith('|') || line.Contains("---") || line.Contains("Target language"))
            {
                continue;
            }

            var cells = line.Trim('|').Split('|');
            if (cells.Length > 0)
            {
                var target = Normalise(cells[0]);
                if (target.Length > 0)
                {
                    sentences.Add(target);
                }
            }
        }

        return sentences;
    }

    private static IReadOnlyList<DialogueTurn> ExtractDialogueTurns(string contentMarkdown)
    {
        var start = contentMarkdown.IndexOf("## Mini dialogue", StringComparison.Ordinal);
        if (start < 0)
        {
            return Array.Empty<DialogueTurn>();
        }

        var end = contentMarkdown.IndexOf("## Language note", start, StringComparison.Ordinal);
        var section = end < 0 ? contentMarkdown[start..] : contentMarkdown[start..end];

        var turns = new List<DialogueTurn>();
        var lines = section.Split('\n');
        var speakerPattern = new Regex(@"^>\s*\*\*([A-Z]):\*\*\s*(.+)$");
        for (var index = 0; index < lines.Length; index++)
        {
            var match = speakerPattern.Match(lines[index].Trim());
            if (!match.Success)
            {
                continue;
            }

            var speaker = match.Groups[1].Value;
            var target = match.Groups[2].Value.Trim();

            var translation = string.Empty;
            for (var lookahead = index + 1; lookahead < lines.Length; lookahead++)
            {
                var next = lines[lookahead].Trim();
                if (next.StartsWith("> *") && next.EndsWith('*'))
                {
                    translation = next.Trim('>', ' ', '*');
                    break;
                }

                if (next.Length == 0 || next == ">")
                {
                    continue;
                }

                break;
            }

            turns.Add(new DialogueTurn(speaker, target, translation));
        }

        return turns;
    }

    private sealed record DialogueTurn(string Speaker, string Target, string Translation);


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
