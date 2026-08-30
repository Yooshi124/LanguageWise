namespace LanguageWise.QuizzesCoursesService.Db.Models;

public sealed record Course(int Id, string Code, string Title, string Description);

public sealed record LessonSummary(int Id, string Slug, string Title, int SortOrder);

public sealed record VocabularyWord(string Word, string Meaning);

public sealed record LessonDetail(
    int Id,
    Course Course,
    string Slug,
    string Title,
    int SortOrder,
    string ContentMarkdown,
    IReadOnlyList<VocabularyWord> Vocabulary);

public sealed record QuizSummary(int Id, int CourseId, string Title, bool IsAi);

public sealed record Flashcard(int Id, int CourseId, string FrontText, string BackText, bool IsAi);
