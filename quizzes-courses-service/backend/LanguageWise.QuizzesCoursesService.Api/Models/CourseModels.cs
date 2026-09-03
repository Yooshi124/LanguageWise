using System.Text.Json;

namespace LanguageWise.QuizzesCoursesService.Api.Models;

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

public sealed record QuizSummary(
    int Id,
    string Title,
    int LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonSortOrder);

public sealed record QuizQuestion(
    int Id,
    int SortOrder,
    string Content,
    string Type,
    JsonElement QuestionData);

public sealed record QuizDetail(
    int Id,
    string Title,
    int LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonSortOrder,
    IReadOnlyList<QuizQuestion> Questions);

public sealed record QuizAttempt(int Id, int QuizId, DateTimeOffset StartedAt);

public sealed record QuizAnswerSubmission(int QuestionId, string Response);

public sealed record SubmitQuizAttemptRequest(IReadOnlyList<QuizAnswerSubmission> Answers);

public sealed record InternalStartQuizAttemptRequest(int UserId);

public sealed record InternalSubmitQuizAttemptRequest(
    int UserId,
    IReadOnlyList<QuizAnswerSubmission> Answers);

public sealed record QuizAnswerReview(
    int QuestionId,
    string StudentResponse,
    bool IsCorrect,
    string CorrectAnswer);

public sealed record QuizAttemptResult(
    int AttemptId,
    int QuizId,
    int Score,
    int TotalQuestions,
    bool Passed,
    DateTimeOffset CompletedAt,
    IReadOnlyList<QuizAnswerReview> Answers);

public sealed record FlashcardDeckSummary(
    int LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonSortOrder,
    int CardCount);

public sealed record Flashcard(int Id, string FrontText, string BackText);

public sealed record FlashcardDeck(
    int LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonSortOrder,
    IReadOnlyList<Flashcard> Cards);

public sealed record LessonProgress(int LessonId, bool Completed);

public sealed record QuizProgress(
    int QuizId,
    int LessonId,
    bool Completed,
    int? BestScore,
    int TotalQuestions);

public sealed record CourseProgress(
    bool CourseCompleted,
    bool CourseEligible,
    IReadOnlyList<LessonProgress> Lessons,
    IReadOnlyList<QuizProgress> Quizzes);

public sealed record LessonMilestone(
    int LessonId,
    string Slug,
    string Title,
    int SortOrder,
    bool Completed);

public sealed record StartedCourseProgress(
    string CourseCode,
    string CourseTitle,
    IReadOnlyList<LessonMilestone> Lessons);

public sealed record Milestone(
    int Id,
    int UserId,
    int? CourseId,
    int? LessonId,
    int? QuizId,
    DateTimeOffset CompletedAt);

public sealed record MilestonePage(
    IReadOnlyList<Milestone> Items,
    int? NextCursor);

/// <summary>Vocabulary from one milestone-completed lesson.</summary>
public sealed record LessonVocabulary(
    int LessonId,
    string Slug,
    string Title,
    IReadOnlyList<VocabularyWord> Vocabulary);

/// <summary>Vocabulary unlocked in one started course.</summary>
public sealed record CourseVocabulary(
    string Code,
    string Title,
    IReadOnlyList<LessonVocabulary> Lessons);

/// <summary>All vocabulary the user has unlocked across the courses they have started.</summary>
public sealed record UserVocabulary(IReadOnlyList<CourseVocabulary> Courses);
