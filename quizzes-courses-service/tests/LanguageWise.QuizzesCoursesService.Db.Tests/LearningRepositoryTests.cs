using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Db.Data;
using LanguageWise.QuizzesCoursesService.Db.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.QuizzesCoursesService.Db.Tests;

[TestFixture]
public sealed class LearningRepositoryTests
{
    private static readonly string[] ExpectedCourseCodes = ["de", "fr", "it", "nl", "es", "pl"];

    private static readonly string[] ExpectedGermanLessonSlugs =
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
    private string sqlDirectory = null!;
    private CatalogRepository catalog = null!;
    private LearningRepository learning = null!;

    [SetUp]
    public void SetUp()
    {
        databasePath = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            $"languagewise-learning-{Guid.NewGuid():N}.db");
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString();
        sqlDirectory = Path.Combine(
            FindRepositoryRoot(),
            "quizzes-courses-service",
            "database",
            "LanguageWise.QuizzesCoursesService.Db",
            "sql");

        Initialise();
        catalog = new CatalogRepository(connectionString);
        learning = new LearningRepository(connectionString);
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
    public void FreshDatabase_SeedsEveryCourseQuizWithRequiredQuestionMixAndJsonShapes()
    {
        AssertQuizSeedIntegrity();

        var quizzes = learning.GetQuizSummaries("de");
        Assert.Multiple(() =>
        {
            Assert.That(
                quizzes.Select(quiz => quiz.LessonSlug),
                Is.EqualTo(ExpectedGermanLessonSlugs));
            Assert.That(
                quizzes.Select(quiz => quiz.Title),
                Is.EqualTo(quizzes.Select(quiz => $"{quiz.LessonTitle} Quiz")));
            Assert.That(TableExists("Flashcards"), Is.False);
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                GetQuiz("numbers").Questions.Any(question =>
                    question.Content.Contains("twenty-one", StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                GetQuiz("long-words").Questions.Any(question =>
                    question.QuestionData.ToString().Contains(
                        "Rindfleischetikettierungsüberwachungsaufgabenübertragungsgesetz",
                        StringComparison.Ordinal)),
                Is.True);
            Assert.That(
                GetQuiz("funny-unusual-words").Questions.Any(question =>
                    question.Content.Contains("catchy tune", StringComparison.Ordinal)),
                Is.True);
        });
    }

    [Test]
    public void Initialisation_IsIdempotentForEveryCourseQuizAndQuestion()
    {
        Initialise();

        AssertQuizSeedIntegrity();
    }

    [Test]
    public void FreshDatabase_StoresEveryWordOrderingAnswerAsAJsonStringArray()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT QuestionData, CorrectAnswer
            FROM QuizQuestions
            WHERE Type = 'word_ordering'
            ORDER BY QuizId, SortOrder;
            """;

        var answerCount = 0;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            using var questionData = JsonDocument.Parse(reader.GetString(0));
            using var answer = JsonDocument.Parse(reader.GetString(1));
            var tokens = questionData.RootElement
                .GetProperty("tokens")
                .EnumerateArray()
                .Select(token => token.GetString())
                .ToArray();
            var correctTokens = answer.RootElement
                .EnumerateArray()
                .Select(token => token.GetString())
                .ToArray();

            Assert.That(answer.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(
                answer.RootElement.EnumerateArray().All(token =>
                    token.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(token.GetString())),
                Is.True);
            Assert.That(correctTokens, Is.EquivalentTo(tokens));
            answerCount++;
        }

        Assert.That(answerCount, Is.EqualTo(360));
    }

    [Test]
    public void Schema_EnforcesOneQuizPerLessonAndDeterministicQuestionOrder()
    {
        var quiz = learning.GetQuizSummaries("de")[0];
        using var connection = Open();

        using var duplicateQuiz = connection.CreateCommand();
        duplicateQuiz.CommandText =
            "INSERT INTO Quizzes (LessonId, Title) VALUES ($lessonId, 'Duplicate');";
        duplicateQuiz.Parameters.AddWithValue("$lessonId", quiz.LessonId);

        using var duplicateOrder = connection.CreateCommand();
        duplicateOrder.CommandText =
            """
            INSERT INTO QuizQuestions
                (QuizId, SortOrder, Content, Type, QuestionData, CorrectAnswer)
            VALUES
                ($quizId, 1, 'Duplicate', 'free_text', '{}', 'answer');
            """;
        duplicateOrder.Parameters.AddWithValue("$quizId", quiz.Id);

        Assert.Multiple(() =>
        {
            Assert.Throws<SqliteException>(() => duplicateQuiz.ExecuteNonQuery());
            Assert.Throws<SqliteException>(() => duplicateOrder.ExecuteNonQuery());
        });
    }

    [Test]
    public void QuizDetail_HidesAnswersUntilCompletedReview()
    {
        var quiz = GetQuiz("greetings");
        var detailJson = JsonSerializer.Serialize(quiz);

        Assert.That(detailJson, Does.Not.Contain("CorrectAnswer"));

        var attempt = learning.StartAttempt(quiz.Id, 11).Value!;
        var review = learning.SubmitAttempt(attempt.Id, 11, CorrectAnswers(quiz)).Value!;
        var reviewJson = JsonSerializer.Serialize(review);

        Assert.Multiple(() =>
        {
            Assert.That(review.Score, Is.EqualTo(10));
            Assert.That(reviewJson, Does.Contain("CorrectAnswer"));
            Assert.That(review.Answers, Has.All.Matches<QuizAnswerReview>(
                answer => !string.IsNullOrWhiteSpace(answer.CorrectAnswer)));
        });
    }

    [Test]
    public void SubmitAttempt_GradesAllTypesAndFreeTextIsTrimmedCaseInsensitiveButAccentSensitive()
    {
        var greetings = GetQuiz("greetings");
        var greetingAnswers = CorrectAnswers(greetings).ToArray();
        ReplaceResponse(greetingAnswers, greetings, 5, "Guten Tag");
        ReplaceResponse(greetingAnswers, greetings, 8, "  nEiN ");
        ReplaceResponse(greetingAnswers, greetings, 9, "DANKE SCHÖN");

        var greetingAttempt = learning.StartAttempt(greetings.Id, 12).Value!;
        var greetingReview = learning.SubmitAttempt(
            greetingAttempt.Id,
            12,
            greetingAnswers).Value!;

        var politeness = GetQuiz("politeness");
        var politenessAnswers = CorrectAnswers(politeness).ToArray();
        ReplaceResponse(politenessAnswers, politeness, 9, "Konnen Sie helfen?");
        var politenessAttempt = learning.StartAttempt(politeness.Id, 12).Value!;
        var politenessReview = learning.SubmitAttempt(
            politenessAttempt.Id,
            12,
            politenessAnswers).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(greetingReview.Score, Is.EqualTo(10));
            Assert.That(
                greetingReview.Answers.Single(
                    answer => answer.QuestionId == greetings.Questions.Single(
                        question => question.SortOrder == 5).Id).IsCorrect,
                Is.True);
            Assert.That(politenessReview.Score, Is.EqualTo(9));
            Assert.That(
                politenessReview.Answers.Single(
                    answer => answer.QuestionId == politeness.Questions.Single(
                        question => question.SortOrder == 9).Id).IsCorrect,
                Is.False);
        });
    }

    [Test]
    public void SubmitAttempt_RejectsMalformedWordOrderingWithoutPersistingAnything()
    {
        var quiz = GetQuiz("greetings");
        var attempt = learning.StartAttempt(quiz.Id, 13).Value!;
        var answers = CorrectAnswers(quiz).ToArray();
        ReplaceResponse(answers, quiz, 5, "[\"Guten\",");

        var result = learning.SubmitAttempt(attempt.Id, 13, answers);

        Assert.Multiple(() =>
        {
            Assert.That(result.Error?.Code, Is.EqualTo("invalid_response"));
            Assert.That(
                Scalar("SELECT COUNT(*) FROM QuizAnswers WHERE AttemptId = $id;", attempt.Id),
                Is.Zero);
            Assert.That(
                Scalar("SELECT COUNT(*) FROM QuizAttempts WHERE Id = $id AND CompletedAt IS NOT NULL;", attempt.Id),
                Is.Zero);
        });
    }

    [Test]
    public void SubmitAttempt_ValidatesOwnershipCompletenessDuplicatesQuizAndCompletion()
    {
        var greetings = GetQuiz("greetings");
        var introductions = GetQuiz("introductions");
        var attempt = learning.StartAttempt(greetings.Id, 14).Value!;
        var correct = CorrectAnswers(greetings).ToArray();

        var wrongUser = learning.SubmitAttempt(attempt.Id, 99, correct);
        var incomplete = learning.SubmitAttempt(attempt.Id, 14, correct[..9]);
        var duplicateAnswers = correct.ToList();
        duplicateAnswers[9] = duplicateAnswers[0];
        var duplicate = learning.SubmitAttempt(attempt.Id, 14, duplicateAnswers);
        var wrongQuizAnswers = correct.ToArray();
        wrongQuizAnswers[9] = CorrectAnswers(introductions)[0];
        var wrongQuiz = learning.SubmitAttempt(attempt.Id, 14, wrongQuizAnswers);
        var submitted = learning.SubmitAttempt(attempt.Id, 14, correct);
        var submittedAgain = learning.SubmitAttempt(attempt.Id, 14, correct);

        Assert.Multiple(() =>
        {
            Assert.That(wrongUser.Error?.Code, Is.EqualTo("wrong_user"));
            Assert.That(incomplete.Error?.Code, Is.EqualTo("incomplete_answers"));
            Assert.That(duplicate.Error?.Code, Is.EqualTo("duplicate_question"));
            Assert.That(wrongQuiz.Error?.Code, Is.EqualTo("wrong_quiz"));
            Assert.That(submitted.IsSuccess, Is.True);
            Assert.That(submittedAgain.Error?.Code, Is.EqualTo("attempt_complete"));
            Assert.That(
                Scalar("SELECT COUNT(*) FROM QuizAnswers WHERE AttemptId = $id;", attempt.Id),
                Is.EqualTo(10));
        });
    }

    [Test]
    public void PassingThreshold_CreatesOneQuizMilestoneWhileRetainingEveryAttempt()
    {
        var quiz = GetQuiz("greetings");
        var firstAnswers = CorrectAnswers(quiz).ToArray();
        ReplaceResponse(firstAnswers, quiz, 1, "wrong");
        ReplaceResponse(firstAnswers, quiz, 2, "wrong");

        var first = learning.StartAttempt(quiz.Id, 15).Value!;
        var firstReview = learning.SubmitAttempt(first.Id, 15, firstAnswers).Value!;
        var second = learning.StartAttempt(quiz.Id, 15).Value!;
        var secondReview = learning.SubmitAttempt(second.Id, 15, CorrectAnswers(quiz)).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(firstReview.Score, Is.EqualTo(LearningRepository.PassingScore));
            Assert.That(firstReview.Passed, Is.True);
            Assert.That(secondReview.Score, Is.EqualTo(10));
            Assert.That(
                Scalar("SELECT COUNT(*) FROM QuizAttempts WHERE UserId = 15 AND QuizId = $id;", quiz.Id),
                Is.EqualTo(2));
            Assert.That(
                Scalar("SELECT COUNT(*) FROM Milestones WHERE UserId = 15 AND QuizId = $id;", quiz.Id),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void FailedThenPassingReattempt_LeavesFailureHistoryAndCreatesMilestoneOnlyOnPass()
    {
        var quiz = GetQuiz("greetings");
        var failingAnswers = CorrectAnswers(quiz).ToArray();
        ReplaceResponse(failingAnswers, quiz, 1, "wrong");
        ReplaceResponse(failingAnswers, quiz, 2, "wrong");
        ReplaceResponse(failingAnswers, quiz, 3, "wrong");

        var failedAttempt = learning.StartAttempt(quiz.Id, 16).Value!;
        var failedReview = learning.SubmitAttempt(failedAttempt.Id, 16, failingAnswers).Value!;
        var progressAfterFailure = learning.GetCourseProgress("de", 16).Value!;
        var milestoneAfterFailure = Scalar(
            "SELECT COUNT(*) FROM Milestones WHERE UserId = 16 AND QuizId = $id;",
            quiz.Id);
        var passedAttempt = learning.StartAttempt(quiz.Id, 16).Value!;
        var passedReview = learning.SubmitAttempt(
            passedAttempt.Id,
            16,
            CorrectAnswers(quiz)).Value!;
        var progressAfterPass = learning.GetCourseProgress("de", 16).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(failedReview.Score, Is.EqualTo(7));
            Assert.That(failedReview.Passed, Is.False);
            Assert.That(progressAfterFailure.Quizzes[0].Completed, Is.False);
            Assert.That(progressAfterFailure.Quizzes[0].BestScore, Is.EqualTo(7));
            Assert.That(milestoneAfterFailure, Is.Zero);
            Assert.That(passedReview.Passed, Is.True);
            Assert.That(progressAfterPass.Quizzes[0].Completed, Is.True);
            Assert.That(progressAfterPass.Quizzes[0].BestScore, Is.EqualTo(10));
            Assert.That(
                Scalar(
                    "SELECT COUNT(*) FROM QuizAttempts WHERE UserId = 16 AND CompletedAt IS NOT NULL;"),
                Is.EqualTo(2));
        });
    }

    [Test]
    public void FlashcardDecks_AreDerivedFromLessonVocabulary()
    {
        var decks = learning.GetFlashcardDecks("de");
        var greetings = learning.GetFlashcardDeck("de", "greetings");

        Assert.Multiple(() =>
        {
            Assert.That(decks, Has.Count.EqualTo(20));
            Assert.That(decks.Select(deck => deck.LessonSortOrder), Is.EqualTo(Enumerable.Range(1, 20)));
            Assert.That(greetings, Is.Not.Null);
            Assert.That(greetings!.Cards, Has.Count.EqualTo(5));
            Assert.That(greetings.Cards[0], Is.EqualTo(new Flashcard(1, "Hallo", "Hello")));
            Assert.That(learning.GetFlashcardDeck("de", "missing"), Is.Null);
            Assert.That(TableExists("Flashcards"), Is.False);
        });
    }

    [Test]
    public void CourseCompletion_RequiresEveryLessonAndAvailableQuizAndSupportsDeletion()
    {
        const int userId = 17;
        var lessons = catalog.GetLessons("de");

        var premature = learning.CompleteCourse("de", userId);
        foreach (var lesson in lessons)
        {
            Assert.That(learning.CompleteLesson(lesson.Id, userId).IsSuccess, Is.True);
        }

        var afterLessons = learning.GetCourseProgress("de", userId).Value!;
        var stillPremature = learning.CompleteCourse("de", userId);
        foreach (var summary in learning.GetQuizSummaries("de"))
        {
            var quiz = learning.GetQuiz(summary.Id)!;
            var attempt = learning.StartAttempt(quiz.Id, userId).Value!;
            learning.SubmitAttempt(attempt.Id, userId, CorrectAnswers(quiz));
        }

        var eligible = learning.GetCourseProgress("de", userId).Value!;
        var completed = learning.CompleteCourse("de", userId);
        learning.UncompleteLesson(lessons[0].Id, userId);
        var lessonRemoved = learning.GetCourseProgress("de", userId).Value!;
        learning.UncompleteCourse("de", userId);
        var courseRemoved = learning.GetCourseProgress("de", userId).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(premature.Error?.Code, Is.EqualTo("course_prerequisites_incomplete"));
            Assert.That(afterLessons.CourseEligible, Is.False);
            Assert.That(stillPremature.Error?.Code, Is.EqualTo("course_prerequisites_incomplete"));
            Assert.That(eligible.CourseEligible, Is.True);
            Assert.That(eligible.Quizzes.All(quiz => quiz.Completed), Is.True);
            Assert.That(completed.Value?.Completed, Is.True);
            Assert.That(lessonRemoved.CourseEligible, Is.False);
            Assert.That(lessonRemoved.Lessons[0].Completed, Is.False);
            Assert.That(courseRemoved.CourseCompleted, Is.False);
        });
    }

    [Test]
    public void Repository_ReturnsClearValidationAndNotFoundErrors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(learning.StartAttempt(99999, 1).Error?.Code, Is.EqualTo("quiz_not_found"));
            Assert.That(learning.StartAttempt(GetQuiz("greetings").Id, 0).Error?.Code, Is.EqualTo("invalid_user"));
            Assert.That(
                learning.SubmitAttempt(99999, 1, Array.Empty<QuizResponse>()).Error?.Code,
                Is.EqualTo("attempt_not_found"));
            Assert.That(learning.CompleteLesson(99999, 1).Error?.Code, Is.EqualTo("lesson_not_found"));
            Assert.That(learning.GetCourseProgress("xx", 1).Error?.Code, Is.EqualTo("course_not_found"));
            Assert.That(learning.UncompleteCourse("xx", 1).Error?.Code, Is.EqualTo("course_not_found"));
        });
    }

    private void Initialise() =>
        new DatabaseInitializer(
            connectionString,
            sqlDirectory,
            NullLogger<DatabaseInitializer>.Instance).Initialise();

    private void AssertQuizSeedIntegrity()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Scalar("SELECT COUNT(*) FROM Courses;"), Is.EqualTo(6));
            Assert.That(Scalar("SELECT COUNT(*) FROM Lessons;"), Is.EqualTo(120));
            Assert.That(Scalar("SELECT COUNT(*) FROM LessonVocabulary;"), Is.EqualTo(120));
            Assert.That(Scalar("SELECT COUNT(*) FROM Quizzes;"), Is.EqualTo(120));
            Assert.That(Scalar("SELECT COUNT(*) FROM QuizQuestions;"), Is.EqualTo(1200));
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM (
                        SELECT lesson.Id
                        FROM Lessons lesson
                        LEFT JOIN Quizzes quiz ON quiz.LessonId = lesson.Id
                        GROUP BY lesson.Id
                        HAVING COUNT(quiz.Id) <> 1
                    );
                    """),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM (
                        SELECT
                            course.Id
                        FROM Courses course
                        LEFT JOIN Lessons lesson ON lesson.CourseId = course.Id
                        LEFT JOIN LessonVocabulary vocabulary ON vocabulary.LessonId = lesson.Id
                        LEFT JOIN Quizzes quiz ON quiz.LessonId = lesson.Id
                        GROUP BY course.Id
                        HAVING COUNT(DISTINCT lesson.Id) <> 20
                            OR COUNT(DISTINCT vocabulary.Id) <> 20
                            OR COUNT(DISTINCT quiz.Id) <> 20
                    );
                    """),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM (
                        SELECT lesson.Id
                        FROM Lessons lesson
                        LEFT JOIN LessonVocabulary vocabulary ON vocabulary.LessonId = lesson.Id
                        LEFT JOIN Quizzes quiz ON quiz.LessonId = lesson.Id
                        GROUP BY lesson.Id
                        HAVING COUNT(DISTINCT vocabulary.Id) <> 1
                            OR COUNT(DISTINCT quiz.Id) <> 1
                    );
                    """),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM (
                        SELECT quiz.Id
                        FROM Quizzes quiz
                        LEFT JOIN QuizQuestions question ON question.QuizId = quiz.Id
                        GROUP BY quiz.Id
                        HAVING COUNT(question.Id) <> 10
                            OR COUNT(DISTINCT question.SortOrder) <> 10
                            OR MIN(question.SortOrder) <> 1
                            OR MAX(question.SortOrder) <> 10
                            OR SUM(question.Type = 'multiple_choice') <> 4
                            OR SUM(question.Type = 'word_ordering') <> 3
                            OR SUM(question.Type = 'free_text') <> 3
                    );
                    """),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM LessonVocabulary
                    WHERE json_valid(VocabularyJson) = 0
                       OR json_type(VocabularyJson) <> 'object'
                       OR COALESCE(json_type(VocabularyJson, '$.words'), '') <> 'array'
                       OR json_array_length(VocabularyJson, '$.words') NOT BETWEEN 5 AND 10;
                    """),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM QuizQuestions
                    WHERE json_valid(QuestionData) = 0
                       OR json_type(QuestionData) <> 'object';
                    """),
                Is.Zero);
            Assert.That(
                RowCount("PRAGMA foreign_key_check;"),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM QuizQuestions
                    WHERE Type NOT IN ('multiple_choice', 'word_ordering', 'free_text');
                    """),
                Is.Zero);
            Assert.That(
                Scalar(
                    """
                    SELECT COUNT(*)
                    FROM QuizQuestions question
                    WHERE question.Type = 'multiple_choice'
                      AND NOT EXISTS (
                          SELECT 1
                          FROM json_each(question.QuestionData, '$.options')
                          WHERE value = question.CorrectAnswer
                      );
                    """),
                Is.Zero);
        });

        foreach (var courseCode in ExpectedCourseCodes)
        {
            var summaries = learning.GetQuizSummaries(courseCode);
            Assert.That(summaries, Has.Count.EqualTo(20), courseCode);

            foreach (var summary in summaries)
            {
                var quiz = learning.GetQuiz(summary.Id);
                Assert.That(quiz, Is.Not.Null, $"{courseCode}/{summary.LessonSlug}");
                AssertQuizShape(courseCode, summary, quiz!);
            }
        }
    }

    private static void AssertQuizShape(
        string courseCode,
        QuizSummary summary,
        QuizDetail quiz)
    {
        var context = $"{courseCode}/{summary.LessonSlug}";

        Assert.Multiple(() =>
        {
            Assert.That(quiz.LessonId, Is.EqualTo(summary.LessonId), context);
            Assert.That(quiz.LessonSlug, Is.EqualTo(summary.LessonSlug), context);
            Assert.That(quiz.LessonSortOrder, Is.EqualTo(summary.LessonSortOrder), context);
            Assert.That(quiz.Questions, Has.Count.EqualTo(10), context);
            Assert.That(
                quiz.Questions.Select(question => question.SortOrder),
                Is.EqualTo(Enumerable.Range(1, 10)),
                context);
            Assert.That(
                quiz.Questions.Count(question => question.Type == "multiple_choice"),
                Is.EqualTo(4),
                context);
            Assert.That(
                quiz.Questions.Count(question => question.Type == "word_ordering"),
                Is.EqualTo(3),
                context);
            Assert.That(
                quiz.Questions.Count(question => question.Type == "free_text"),
                Is.EqualTo(3),
                context);
        });

        foreach (var question in quiz.Questions)
        {
            var questionContext = $"{context} question {question.SortOrder}";
            var properties = question.QuestionData.EnumerateObject().ToArray();

            switch (question.Type)
            {
                case "multiple_choice":
                    Assert.Multiple(() =>
                    {
                        Assert.That(question.QuestionData.ValueKind, Is.EqualTo(JsonValueKind.Object), questionContext);
                        Assert.That(properties.Select(property => property.Name), Is.EqualTo(["options"]), questionContext);
                        var options = question.QuestionData.GetProperty("options");
                        Assert.That(options.ValueKind, Is.EqualTo(JsonValueKind.Array), questionContext);
                        Assert.That(options.GetArrayLength(), Is.EqualTo(3), questionContext);
                        Assert.That(
                            options.EnumerateArray().All(option =>
                                option.ValueKind == JsonValueKind.String
                                && !string.IsNullOrWhiteSpace(option.GetString())),
                            Is.True,
                            questionContext);
                    });
                    break;

                case "word_ordering":
                    Assert.Multiple(() =>
                    {
                        Assert.That(question.QuestionData.ValueKind, Is.EqualTo(JsonValueKind.Object), questionContext);
                        Assert.That(properties.Select(property => property.Name), Is.EqualTo(["tokens"]), questionContext);
                        var tokens = question.QuestionData.GetProperty("tokens");
                        Assert.That(tokens.ValueKind, Is.EqualTo(JsonValueKind.Array), questionContext);
                        Assert.That(tokens.GetArrayLength(), Is.GreaterThanOrEqualTo(2), questionContext);
                        Assert.That(
                            tokens.EnumerateArray().All(token =>
                                token.ValueKind == JsonValueKind.String
                                && !string.IsNullOrWhiteSpace(token.GetString())),
                            Is.True,
                            questionContext);
                    });
                    break;

                case "free_text":
                    Assert.Multiple(() =>
                    {
                        Assert.That(question.QuestionData.ValueKind, Is.EqualTo(JsonValueKind.Object), questionContext);
                        Assert.That(properties, Is.Empty, questionContext);
                    });
                    break;

                default:
                    Assert.Fail($"Unsupported quiz type '{question.Type}' in {questionContext}.");
                    break;
            }
        }
    }

    private QuizDetail GetQuiz(string lessonSlug)
    {
        var summary = learning.GetQuizSummaries("de")
            .Single(quiz => quiz.LessonSlug == lessonSlug);
        return learning.GetQuiz(summary.Id)!;
    }

    private IReadOnlyList<QuizResponse> CorrectAnswers(QuizDetail quiz)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CorrectAnswer
            FROM QuizQuestions
            WHERE QuizId = $quizId
            ORDER BY SortOrder;
            """;
        command.Parameters.AddWithValue("$quizId", quiz.Id);

        var responses = new List<QuizResponse>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            responses.Add(new QuizResponse(reader.GetInt32(0), reader.GetString(1)));
        }

        return responses;
    }

    private static void ReplaceResponse(
        QuizResponse[] answers,
        QuizDetail quiz,
        int sortOrder,
        string response)
    {
        var questionId = quiz.Questions.Single(question => question.SortOrder == sortOrder).Id;
        var index = Array.FindIndex(answers, answer => answer.QuestionId == questionId);
        answers[index] = new QuizResponse(questionId, response);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private long Scalar(string sql, int? id = null)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        if (id is not null)
        {
            command.Parameters.AddWithValue("$id", id.Value);
        }

        return Convert.ToInt64(command.ExecuteScalar());
    }

    private int RowCount(string sql)
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

    private bool TableExists(string tableName)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT EXISTS(SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name);";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
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
