namespace LanguageWise.McpServer.Tools.QuizzesCourses;

public sealed record CourseListResult(IReadOnlyList<CourseItem> Courses);
public sealed record CourseItem(string Code, string Title, string Description);

public sealed record LessonVocabularyResult(string CourseCode, string LessonSlug, string LessonTitle, IReadOnlyList<VocabularyItem> Vocabulary);
public sealed record VocabularyItem(string Word, string Meaning);

public sealed record CourseProgressResult(string CourseCode, bool CourseCompleted, int LessonsCompleted, int LessonsTotal, IReadOnlyList<QuizProgressItem> Quizzes);
public sealed record QuizProgressItem(int QuizId, bool Completed, int? BestScore, int TotalQuestions);

public sealed record LessonListResult(string CourseCode, IReadOnlyList<LessonItem> Lessons);
public sealed record LessonItem(int LessonId, string Slug, string Title, int SortOrder);

public sealed record QuizListResult(string CourseCode, IReadOnlyList<QuizItem> Quizzes);
public sealed record QuizItem(int QuizId, string Title, string LessonSlug, string LessonTitle);

public sealed record FlashcardDeckResult(string CourseCode, string LessonSlug, string LessonTitle, IReadOnlyList<FlashcardItem> Cards);
public sealed record FlashcardItem(string Front, string Back);

public sealed record MyVocabularyResult(IReadOnlyList<CourseVocabularyItem> Courses, int TotalWords);
public sealed record CourseVocabularyItem(string CourseCode, string CourseTitle, IReadOnlyList<LessonVocabularyItem> Lessons);
public sealed record LessonVocabularyItem(string LessonSlug, string LessonTitle, IReadOnlyList<VocabularyItem> Vocabulary);

public sealed record MyMilestonesResult(IReadOnlyList<MilestoneItem> Milestones);
public sealed record MilestoneItem(
	string Kind,
	string? CourseCode,
	string? CourseTitle,
	string? LessonSlug,
	string? LessonTitle,
	string? QuizTitle,
	DateTimeOffset CompletedAt);

internal sealed record CourseDto(int Id, string Code, string Title, string Description);
internal sealed record LessonDetailDto(CourseDto? Course, string Slug, string Title, List<VocabularyDto>? Vocabulary);
internal sealed record VocabularyDto(string Word, string Meaning);
internal sealed record CourseProgressDto(bool CourseCompleted, List<LessonProgressDto>? Lessons, List<QuizProgressDto>? Quizzes);
internal sealed record LessonProgressDto(int LessonId, bool Completed);
internal sealed record QuizProgressDto(int QuizId, int LessonId, bool Completed, int? BestScore, int TotalQuestions);
internal sealed record LessonSummaryDto(int Id, string Slug, string Title, int SortOrder);
internal sealed record QuizSummaryDto(int Id, string Title, int LessonId, string LessonSlug, string LessonTitle, int LessonSortOrder);
internal sealed record FlashcardDeckDto(string LessonSlug, string LessonTitle, List<FlashcardDto>? Cards);
internal sealed record FlashcardDto(string FrontText, string BackText);
internal sealed record UserVocabularyDto(List<CourseVocabularyDto>? Courses);
internal sealed record CourseVocabularyDto(string Code, string Title, List<LessonVocabularyDto>? Lessons);
internal sealed record LessonVocabularyDto(string Slug, string Title, List<VocabularyDto>? Vocabulary);
internal sealed record MilestonePageDto(List<MilestoneDto>? Items);
internal sealed record MilestoneDto(int? CourseId, int? LessonId, int? QuizId, DateTimeOffset CompletedAt);
