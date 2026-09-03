using System.Text.Json;

namespace LanguageWise.QuizzesCoursesService.Db.Models;

public enum DomainErrorKind
{
    Validation,
    NotFound,
    Conflict
}

public sealed record DomainError(DomainErrorKind Kind, string Code, string Message);

public sealed record DomainResult<T>(T? Value, DomainError? Error)
{
    public bool IsSuccess => Error is null;

    public static DomainResult<T> Success(T value) => new(value, null);

    public static DomainResult<T> Failure(DomainErrorKind kind, string code, string message) =>
        new(default, new DomainError(kind, code, message));
}

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

public sealed record QuizAttempt(
    int Id,
    int QuizId,
    DateTimeOffset StartedAt);

public sealed record QuizResponse(int QuestionId, string Response);

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

public sealed record Flashcard(int Id, string FrontText, string BackText);

public sealed record FlashcardDeckSummary(
    int LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonSortOrder,
    int CardCount);

public sealed record FlashcardDeck(
    int LessonId,
    string LessonSlug,
    string LessonTitle,
    int LessonSortOrder,
    IReadOnlyList<Flashcard> Cards);

public sealed record QuizProgress(
    int QuizId,
    int LessonId,
    bool Completed,
    int? BestScore,
    int TotalQuestions);

public sealed record LessonProgress(
    int LessonId,
    bool Completed);

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

public sealed record MilestoneState(bool Completed, bool Changed);

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

public sealed record StartQuizAttemptRequest(int UserId);

public sealed record SubmitQuizAttemptRequest(int UserId, IReadOnlyList<QuizResponse> Answers);
