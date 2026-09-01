using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.QuizzesCoursesService.Db.Data;

public sealed class LearningRepository(string connectionString)
{
    public const int PassingScore = 8;

    public IReadOnlyList<QuizSummary> GetQuizSummaries(string courseCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.Id, l.Slug, l.Title, l.SortOrder, q.Id, q.Title
            FROM Quizzes q
            INNER JOIN Lessons l ON l.Id = q.LessonId
            INNER JOIN Courses c ON c.Id = l.CourseId
            WHERE c.Code = $code
            ORDER BY l.SortOrder;
            """;
        command.Parameters.AddWithValue("$code", courseCode);

        var quizzes = new List<QuizSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            quizzes.Add(new QuizSummary(
                reader.GetInt32(4),
                reader.GetString(5),
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return quizzes;
    }

    public QuizDetail? GetQuiz(int quizId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT q.Id, q.Title, q.LessonId, l.Slug, l.Title, l.SortOrder
            FROM Quizzes q
            INNER JOIN Lessons l ON l.Id = q.LessonId
            WHERE q.Id = $quizId;
            """;
        command.Parameters.AddWithValue("$quizId", quizId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var detail = (
            Id: reader.GetInt32(0),
            Title: reader.GetString(1),
            LessonId: reader.GetInt32(2),
            LessonSlug: reader.GetString(3),
            LessonTitle: reader.GetString(4),
            LessonSortOrder: reader.GetInt32(5));
        reader.Close();

        command.CommandText =
            """
            SELECT Id, SortOrder, Content, Type, QuestionData
            FROM QuizQuestions
            WHERE QuizId = $quizId
            ORDER BY SortOrder;
            """;

        var questions = new List<QuizQuestion>();
        using var questionReader = command.ExecuteReader();
        while (questionReader.Read())
        {
            questions.Add(new QuizQuestion(
                questionReader.GetInt32(0),
                questionReader.GetInt32(1),
                questionReader.GetString(2),
                questionReader.GetString(3),
                ParseJson(questionReader.GetString(4))));
        }

        return new QuizDetail(
            detail.Id,
            detail.Title,
            detail.LessonId,
            detail.LessonSlug,
            detail.LessonTitle,
            detail.LessonSortOrder,
            questions);
    }

    public IReadOnlyList<FlashcardDeckSummary> GetFlashcardDecks(string courseCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.Id, l.Slug, l.Title, l.SortOrder, v.VocabularyJson
            FROM Lessons l
            INNER JOIN Courses c ON c.Id = l.CourseId
            INNER JOIN LessonVocabulary v ON v.LessonId = l.Id
            WHERE c.Code = $code
            ORDER BY l.SortOrder;
            """;
        command.Parameters.AddWithValue("$code", courseCode);

        var decks = new List<FlashcardDeckSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            decks.Add(new FlashcardDeckSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                DeserializeVocabulary(reader.GetString(4)).Count));
        }

        return decks;
    }

    public FlashcardDeck? GetFlashcardDeck(string courseCode, string lessonSlug)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.Id, l.Slug, l.Title, l.SortOrder, v.VocabularyJson
            FROM Lessons l
            INNER JOIN Courses c ON c.Id = l.CourseId
            INNER JOIN LessonVocabulary v ON v.LessonId = l.Id
            WHERE c.Code = $code AND l.Slug = $slug;
            """;
        command.Parameters.AddWithValue("$code", courseCode);
        command.Parameters.AddWithValue("$slug", lessonSlug);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var cards = DeserializeVocabulary(reader.GetString(4))
            .Select((word, index) => new Flashcard(index + 1, word.Word, word.Meaning))
            .ToArray();
        return new FlashcardDeck(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            cards);
    }

    public DomainResult<QuizAttempt> StartAttempt(int quizId, int userId)
    {
        if (userId <= 0)
        {
            return InvalidUser<QuizAttempt>();
        }

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM QuizQuestions WHERE QuizId = $quizId;";
        command.Parameters.AddWithValue("$quizId", quizId);
        var totalQuestions = Convert.ToInt32(command.ExecuteScalar());
        if (totalQuestions == 0)
        {
            return DomainResult<QuizAttempt>.Failure(
                DomainErrorKind.NotFound, "quiz_not_found", "Quiz was not found.");
        }

        var startedAt = DateTimeOffset.UtcNow;
        command.CommandText =
            """
            INSERT INTO QuizAttempts (UserId, QuizId, TotalQuestions, StartedAt)
            VALUES ($userId, $quizId, $totalQuestions, $startedAt);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$totalQuestions", totalQuestions);
        command.Parameters.AddWithValue("$startedAt", FormatTimestamp(startedAt));
        var attemptId = Convert.ToInt32(command.ExecuteScalar());

        return DomainResult<QuizAttempt>.Success(
            new QuizAttempt(attemptId, quizId, startedAt));
    }

    public DomainResult<QuizAttemptResult> SubmitAttempt(
        int attemptId,
        int userId,
        IReadOnlyList<QuizResponse>? responses)
    {
        if (userId <= 0)
        {
            return InvalidUser<QuizAttemptResult>();
        }

        if (responses is null)
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.Validation, "answers_required", "Answers are required.");
        }

        using var connection = Open();
        using var transaction = connection.BeginTransaction();

        var attempt = GetAttempt(connection, transaction, attemptId);
        if (attempt is null)
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.NotFound, "attempt_not_found", "Quiz attempt was not found.");
        }

        if (attempt.UserId != userId)
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.Conflict, "wrong_user", "Quiz attempt belongs to another user.");
        }

        if (attempt.CompletedAt is not null)
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.Conflict, "attempt_complete", "Quiz attempt has already been submitted.");
        }

        var duplicate = responses
            .GroupBy(response => response.QuestionId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.Validation, "duplicate_question",
                $"Question {duplicate.Key} was answered more than once.");
        }

        var questions = GetQuestionsForGrading(connection, transaction, attempt.QuizId);
        var questionIds = questions.Select(question => question.Id).ToHashSet();
        if (responses.Any(response => !questionIds.Contains(response.QuestionId)))
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.Validation, "wrong_quiz",
                "One or more answers do not belong to this quiz.");
        }

        if (responses.Count != questions.Count)
        {
            return DomainResult<QuizAttemptResult>.Failure(
                DomainErrorKind.Validation, "incomplete_answers",
                "Exactly one answer is required for every quiz question.");
        }

        var responsesByQuestion = responses.ToDictionary(response => response.QuestionId);
        var graded = new List<GradedAnswer>(questions.Count);
        foreach (var question in questions)
        {
            var response = responsesByQuestion[question.Id].Response;
            if (response is null)
            {
                return DomainResult<QuizAttemptResult>.Failure(
                    DomainErrorKind.Validation, "invalid_response", "Responses cannot be null.");
            }

            var grade = Grade(question, response);
            if (grade.Error is not null)
            {
                return DomainResult<QuizAttemptResult>.Failure(
                    DomainErrorKind.Validation, "invalid_response", grade.Error);
            }

            graded.Add(new GradedAnswer(question, response, grade.IsCorrect));
        }

        var completedAt = DateTimeOffset.UtcNow;
        foreach (var answer in graded)
        {
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText =
                """
                INSERT INTO QuizAnswers
                    (AttemptId, QuestionId, StudentResponse, IsCorrect, AnsweredAt)
                VALUES
                    ($attemptId, $questionId, $response, $isCorrect, $answeredAt);
                """;
            insert.Parameters.AddWithValue("$attemptId", attemptId);
            insert.Parameters.AddWithValue("$questionId", answer.Question.Id);
            insert.Parameters.AddWithValue("$response", answer.Response);
            insert.Parameters.AddWithValue("$isCorrect", answer.IsCorrect);
            insert.Parameters.AddWithValue("$answeredAt", FormatTimestamp(completedAt));
            insert.ExecuteNonQuery();
        }

        var score = graded.Count(answer => answer.IsCorrect);
        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText =
                """
                UPDATE QuizAttempts
                SET Score = $score, CompletedAt = $completedAt
                WHERE Id = $attemptId;
                """;
            update.Parameters.AddWithValue("$score", score);
            update.Parameters.AddWithValue("$completedAt", FormatTimestamp(completedAt));
            update.Parameters.AddWithValue("$attemptId", attemptId);
            update.ExecuteNonQuery();
        }

        if (score >= PassingScore)
        {
            using var milestone = connection.CreateCommand();
            milestone.Transaction = transaction;
            milestone.CommandText =
                """
                INSERT OR IGNORE INTO Milestones (UserId, QuizId, CompletedAt)
                VALUES ($userId, $quizId, $completedAt);
                """;
            milestone.Parameters.AddWithValue("$userId", userId);
            milestone.Parameters.AddWithValue("$quizId", attempt.QuizId);
            milestone.Parameters.AddWithValue("$completedAt", FormatTimestamp(completedAt));
            milestone.ExecuteNonQuery();
        }

        transaction.Commit();

        return DomainResult<QuizAttemptResult>.Success(new QuizAttemptResult(
            attempt.Id,
            attempt.QuizId,
            score,
            questions.Count,
            score >= PassingScore,
            completedAt,
            graded.Select(answer => new QuizAnswerReview(
                answer.Question.Id,
                answer.Response,
                answer.IsCorrect,
                FormatCorrectAnswer(answer.Question))).ToArray()));
    }

    public DomainResult<CourseProgress> GetCourseProgress(string courseCode, int userId)
    {
        if (userId <= 0)
        {
            return InvalidUser<CourseProgress>();
        }

        using var connection = Open();
        var course = GetCourseIdentity(connection, courseCode);
        if (course is null)
        {
            return DomainResult<CourseProgress>.Failure(
                DomainErrorKind.NotFound, "course_not_found", "Course was not found.");
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.Id, l.Slug, l.Title, l.SortOrder,
                   EXISTS(
                       SELECT 1 FROM Milestones lm
                       WHERE lm.UserId = $userId AND lm.LessonId = l.Id
                   ),
                   q.Id, q.Title,
                   CASE WHEN q.Id IS NULL THEN NULL ELSE EXISTS(
                       SELECT 1 FROM Milestones qm
                       WHERE qm.UserId = $userId AND qm.QuizId = q.Id
                   ) END,
                   (
                       SELECT MAX(a.Score)
                       FROM QuizAttempts a
                       WHERE a.UserId = $userId
                         AND a.QuizId = q.Id
                         AND a.CompletedAt IS NOT NULL
                   ),
                   (
                       SELECT COUNT(*)
                       FROM QuizQuestions qq
                       WHERE qq.QuizId = q.Id
                   )
            FROM Lessons l
            LEFT JOIN Quizzes q ON q.LessonId = l.Id
            WHERE l.CourseId = $courseId
            ORDER BY l.SortOrder;
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$courseId", course.Value.Id);

        var lessons = new List<LessonProgress>();
        var quizzes = new List<QuizProgress>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lessons.Add(new LessonProgress(
                reader.GetInt32(0),
                reader.GetBoolean(4)));

            if (!reader.IsDBNull(5))
            {
                quizzes.Add(new QuizProgress(
                    reader.GetInt32(5),
                    reader.GetInt32(0),
                    reader.GetBoolean(7),
                    reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    reader.GetInt32(9)));
            }
        }

        var eligible = lessons.Count > 0
            && lessons.All(lesson => lesson.Completed)
            && quizzes.All(quiz => quiz.Completed);
        var completed = MilestoneExists(connection, userId, "CourseId", course.Value.Id);

        return DomainResult<CourseProgress>.Success(new CourseProgress(
            completed,
            eligible,
            lessons,
            quizzes));
    }

    /// <summary>
    /// Progress for every course the user has started. A course counts as started once the user
    /// has any milestone (course, lesson, or quiz) or any quiz attempt in it. Each entry lists all
    /// of the course's lessons with the user's milestone state, so callers can unlock content as
    /// the user progresses.
    /// </summary>
    public DomainResult<IReadOnlyList<StartedCourseProgress>> GetStartedCoursesProgress(int userId)
    {
        if (userId <= 0)
        {
            return InvalidUser<IReadOnlyList<StartedCourseProgress>>();
        }

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT c.Code, c.Title, l.Id, l.Slug, l.Title, l.SortOrder,
                   EXISTS(
                       SELECT 1 FROM Milestones m
                       WHERE m.UserId = $userId AND m.LessonId = l.Id
                   )
            FROM Courses c
            INNER JOIN Lessons l ON l.CourseId = c.Id
            WHERE EXISTS(
                      SELECT 1 FROM Milestones m
                      WHERE m.UserId = $userId AND m.CourseId = c.Id)
               OR EXISTS(
                      SELECT 1 FROM Milestones m
                      INNER JOIN Lessons ml ON ml.Id = m.LessonId
                      WHERE m.UserId = $userId AND ml.CourseId = c.Id)
               OR EXISTS(
                      SELECT 1 FROM Milestones m
                      INNER JOIN Quizzes mq ON mq.Id = m.QuizId
                      INNER JOIN Lessons ql ON ql.Id = mq.LessonId
                      WHERE m.UserId = $userId AND ql.CourseId = c.Id)
               OR EXISTS(
                      SELECT 1 FROM QuizAttempts a
                      INNER JOIN Quizzes aq ON aq.Id = a.QuizId
                      INNER JOIN Lessons al ON al.Id = aq.LessonId
                      WHERE a.UserId = $userId AND al.CourseId = c.Id)
            ORDER BY c.Code, l.SortOrder;
            """;
        command.Parameters.AddWithValue("$userId", userId);

        var courses = new List<StartedCourseProgress>();
        var lessons = new List<LessonMilestone>();
        string? currentCode = null;
        string? currentTitle = null;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var code = reader.GetString(0);
            if (currentCode is not null && code != currentCode)
            {
                courses.Add(new StartedCourseProgress(currentCode, currentTitle!, lessons.ToArray()));
                lessons.Clear();
            }

            currentCode = code;
            currentTitle = reader.GetString(1);
            lessons.Add(new LessonMilestone(
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetBoolean(6)));
        }

        if (currentCode is not null)
        {
            courses.Add(new StartedCourseProgress(currentCode, currentTitle!, lessons.ToArray()));
        }

        return DomainResult<IReadOnlyList<StartedCourseProgress>>.Success(courses);
    }

    public DomainResult<MilestoneState> CompleteLesson(int lessonId, int userId)
    {
        if (userId <= 0)
        {
            return InvalidUser<MilestoneState>();
        }

        using var connection = Open();
        if (!EntityExists(connection, "Lessons", lessonId))
        {
            return DomainResult<MilestoneState>.Failure(
                DomainErrorKind.NotFound, "lesson_not_found", "Lesson was not found.");
        }

        InsertMilestone(connection, userId, "LessonId", lessonId);
        return DomainResult<MilestoneState>.Success(new MilestoneState(true));
    }

    public DomainResult<MilestoneState> UncompleteLesson(int lessonId, int userId)
    {
        if (userId <= 0)
        {
            return InvalidUser<MilestoneState>();
        }

        using var connection = Open();
        if (!EntityExists(connection, "Lessons", lessonId))
        {
            return DomainResult<MilestoneState>.Failure(
                DomainErrorKind.NotFound, "lesson_not_found", "Lesson was not found.");
        }

        DeleteMilestone(connection, userId, "LessonId", lessonId);
        return DomainResult<MilestoneState>.Success(new MilestoneState(false));
    }

    public DomainResult<MilestoneState> CompleteCourse(string courseCode, int userId)
    {
        var progress = GetCourseProgress(courseCode, userId);
        if (!progress.IsSuccess)
        {
            return DomainResult<MilestoneState>.Failure(
                progress.Error!.Kind, progress.Error.Code, progress.Error.Message);
        }

        if (!progress.Value!.CourseEligible)
        {
            return DomainResult<MilestoneState>.Failure(
                DomainErrorKind.Conflict,
                "course_prerequisites_incomplete",
                "Every lesson and every available quiz must be completed first.");
        }

        using var connection = Open();
        var course = GetCourseIdentity(connection, courseCode)!.Value;
        InsertMilestone(connection, userId, "CourseId", course.Id);
        return DomainResult<MilestoneState>.Success(new MilestoneState(true));
    }

    public DomainResult<MilestoneState> UncompleteCourse(string courseCode, int userId)
    {
        if (userId <= 0)
        {
            return InvalidUser<MilestoneState>();
        }

        using var connection = Open();
        var course = GetCourseIdentity(connection, courseCode);
        if (course is null)
        {
            return DomainResult<MilestoneState>.Failure(
                DomainErrorKind.NotFound, "course_not_found", "Course was not found.");
        }

        DeleteMilestone(connection, userId, "CourseId", course.Value.Id);
        return DomainResult<MilestoneState>.Success(new MilestoneState(false));
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static AttemptRow? GetAttempt(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int attemptId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, QuizId, UserId, StartedAt, CompletedAt
            FROM QuizAttempts
            WHERE Id = $attemptId;
            """;
        command.Parameters.AddWithValue("$attemptId", attemptId);
        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new AttemptRow(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetInt32(2),
                DateTimeOffset.Parse(reader.GetString(3)),
                reader.IsDBNull(4) ? null : DateTimeOffset.Parse(reader.GetString(4)))
            : null;
    }

    private static IReadOnlyList<QuestionRow> GetQuestionsForGrading(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int quizId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, SortOrder, Content, Type, QuestionData, CorrectAnswer
            FROM QuizQuestions
            WHERE QuizId = $quizId
            ORDER BY SortOrder;
            """;
        command.Parameters.AddWithValue("$quizId", quizId);

        var questions = new List<QuestionRow>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            questions.Add(new QuestionRow(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return questions;
    }

    private static GradeResult Grade(QuestionRow question, string response)
    {
        if (question.Type == "multiple_choice")
        {
            return new GradeResult(
                string.Equals(response, question.CorrectAnswer, StringComparison.Ordinal),
                null);
        }

        if (question.Type == "free_text")
        {
            return new GradeResult(
                string.Equals(
                    response.Trim(),
                    question.CorrectAnswer.Trim(),
                    StringComparison.OrdinalIgnoreCase),
                null);
        }

        string[]? correctTokens;
        try
        {
            correctTokens = JsonSerializer.Deserialize<string[]>(question.CorrectAnswer);
        }
        catch (JsonException)
        {
            return new GradeResult(false, "The stored word-ordering answer is invalid.");
        }

        if (correctTokens is null)
        {
            return new GradeResult(false, "The stored word-ordering answer is invalid.");
        }

        string[] responseTokens;
        if (response.TrimStart().StartsWith('['))
        {
            try
            {
                responseTokens = JsonSerializer.Deserialize<string[]>(response) ??
                    [];
            }
            catch (JsonException)
            {
                return new GradeResult(false, "Word-ordering responses contain invalid JSON.");
            }
        }
        else
        {
            responseTokens = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        return new GradeResult(
            responseTokens.SequenceEqual(correctTokens, StringComparer.Ordinal),
            null);
    }

    private static string FormatCorrectAnswer(QuestionRow question)
    {
        if (question.Type != "word_ordering")
        {
            return question.CorrectAnswer;
        }

        return JsonSerializer.Deserialize<string[]>(question.CorrectAnswer) is { } tokens
            ? string.Join(' ', tokens)
            : question.CorrectAnswer;
    }

    private static IReadOnlyList<VocabularyWord> DeserializeVocabulary(string json) =>
        JsonSerializer.Deserialize<VocabularyDocument>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Words ?? [];

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static (int Id, string Code)? GetCourseIdentity(
        SqliteConnection connection,
        string courseCode)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Code FROM Courses WHERE Code = $code;";
        command.Parameters.AddWithValue("$code", courseCode);
        using var reader = command.ExecuteReader();
        return reader.Read() ? (reader.GetInt32(0), reader.GetString(1)) : null;
    }

    private static bool EntityExists(SqliteConnection connection, string table, int id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT EXISTS(SELECT 1 FROM {table} WHERE Id = $id);";
        command.Parameters.AddWithValue("$id", id);
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    private static bool MilestoneExists(
        SqliteConnection connection,
        int userId,
        string targetColumn,
        int targetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT EXISTS(SELECT 1 FROM Milestones WHERE UserId = $userId AND {targetColumn} = $targetId);";
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$targetId", targetId);
        return Convert.ToInt64(command.ExecuteScalar()) == 1;
    }

    private static void InsertMilestone(
        SqliteConnection connection,
        int userId,
        string targetColumn,
        int targetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            $"""
             INSERT OR IGNORE INTO Milestones (UserId, {targetColumn}, CompletedAt)
             VALUES ($userId, $targetId, $completedAt);
             """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$targetId", targetId);
        command.Parameters.AddWithValue("$completedAt", FormatTimestamp(DateTimeOffset.UtcNow));
        command.ExecuteNonQuery();
    }

    private static void DeleteMilestone(
        SqliteConnection connection,
        int userId,
        string targetColumn,
        int targetId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            $"DELETE FROM Milestones WHERE UserId = $userId AND {targetColumn} = $targetId;";
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$targetId", targetId);
        command.ExecuteNonQuery();
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O");

    private static DomainResult<T> InvalidUser<T>() =>
        DomainResult<T>.Failure(
            DomainErrorKind.Validation, "invalid_user", "UserId must be greater than zero.");

    private sealed record VocabularyDocument(IReadOnlyList<VocabularyWord> Words);

    private sealed record AttemptRow(
        int Id,
        int QuizId,
        int UserId,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);

    private sealed record QuestionRow(
        int Id,
        int SortOrder,
        string Content,
        string Type,
        string QuestionData,
        string CorrectAnswer);

    private sealed record GradeResult(bool IsCorrect, string? Error);

    private sealed record GradedAnswer(QuestionRow Question, string Response, bool IsCorrect);
}
