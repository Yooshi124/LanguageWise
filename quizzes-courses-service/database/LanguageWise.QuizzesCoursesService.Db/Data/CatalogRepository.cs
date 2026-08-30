using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.QuizzesCoursesService.Db.Data;

public sealed class CatalogRepository(string connectionString)
{
    public IReadOnlyList<Course> GetCourses()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Code, Title, Description FROM Courses ORDER BY Id;";

        var courses = new List<Course>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            courses.Add(MapCourse(reader));
        }

        return courses;
    }

    public Course? GetCourse(string code)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Code, Title, Description FROM Courses WHERE Code = $code;";
        command.Parameters.AddWithValue("$code", code);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapCourse(reader) : null;
    }

    public IReadOnlyList<LessonSummary> GetLessons(string courseCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.Id, l.Slug, l.Title, l.SortOrder
            FROM Lessons l
            INNER JOIN Courses c ON c.Id = l.CourseId
            WHERE c.Code = $code
            ORDER BY l.SortOrder;
            """;
        command.Parameters.AddWithValue("$code", courseCode);

        var lessons = new List<LessonSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lessons.Add(new LessonSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3)));
        }

        return lessons;
    }

    public LessonDetail? GetLesson(string courseCode, string lessonSlug)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT l.Id, l.Slug, l.Title, l.SortOrder, l.ContentMarkdown,
                   c.Id, c.Code, c.Title, c.Description,
                   COALESCE(v.VocabularyJson, '{"words":[]}')
            FROM Lessons l
            INNER JOIN Courses c ON c.Id = l.CourseId
            LEFT JOIN LessonVocabulary v ON v.LessonId = l.Id
            WHERE c.Code = $code AND l.Slug = $slug;
            """;
        command.Parameters.AddWithValue("$code", courseCode);
        command.Parameters.AddWithValue("$slug", lessonSlug);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var vocabulary = JsonSerializer.Deserialize<VocabularyDocument>(
            reader.GetString(9),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Words ?? [];

        return new LessonDetail(
            reader.GetInt32(0),
            new Course(reader.GetInt32(5), reader.GetString(6), reader.GetString(7), reader.GetString(8)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetString(4),
            vocabulary);
    }

    public IReadOnlyList<QuizSummary> GetQuizzes(string courseCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT q.Id, q.CourseId, q.Title, q.IsAi
            FROM Quizzes q
            INNER JOIN Courses c ON c.Id = q.CourseId
            WHERE c.Code = $code
            ORDER BY q.Id;
            """;
        command.Parameters.AddWithValue("$code", courseCode);

        var quizzes = new List<QuizSummary>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            quizzes.Add(new QuizSummary(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetBoolean(3)));
        }

        return quizzes;
    }

    public IReadOnlyList<Flashcard> GetFlashcards(string courseCode)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT f.Id, f.CourseId, f.FrontText, f.BackText, f.IsAi
            FROM Flashcards f
            INNER JOIN Courses c ON c.Id = f.CourseId
            WHERE c.Code = $code
            ORDER BY f.Id;
            """;
        command.Parameters.AddWithValue("$code", courseCode);

        var flashcards = new List<Flashcard>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            flashcards.Add(new Flashcard(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4)));
        }

        return flashcards;
    }

    public long CountCourses()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Courses;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static Course MapCourse(SqliteDataReader reader) =>
        new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetString(3));

    private sealed record VocabularyDocument(IReadOnlyList<VocabularyWord> Words);
}
