using System.Text.Json;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Services;

public interface IAssistantContextService
{
    Task<AssistantContextResult> GetContextAsync(
        AssistantRouteContext routeContext,
        CancellationToken cancellationToken);
}

public sealed class AssistantContextService(CatalogClient catalogClient) : IAssistantContextService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AssistantContextResult> GetContextAsync(
        AssistantRouteContext routeContext,
        CancellationToken cancellationToken)
    {
        return routeContext.RouteName switch
        {
            "home" or "quizzes" or "flashcards" =>
                AssistantContextResult.Success(JsonSerializer.Serialize(new
                {
                    page = routeContext.RouteName,
                    platform = "LanguageWise",
                    courses = await catalogClient.GetCoursesAsync(cancellationToken)
                }, JsonOptions)),
            "lesson" or "flashcard-revision" =>
                await GetLessonContextAsync(routeContext, cancellationToken),
            "quiz-list" => await GetQuizListContextAsync(routeContext, cancellationToken),
            _ => await GetCourseContextAsync(routeContext, cancellationToken)
        };
    }

    private async Task<AssistantContextResult> GetCourseContextAsync(
        AssistantRouteContext routeContext,
        CancellationToken cancellationToken)
    {
        var course = await catalogClient.GetCourseAsync(routeContext.CourseCode!, cancellationToken);
        if (course is null)
        {
            return AssistantContextResult.NotFound("The requested course was not found.");
        }

        var lessons = await catalogClient.GetLessonsAsync(
            routeContext.CourseCode!,
            cancellationToken);
        return lessons is null
            ? AssistantContextResult.NotFound("The requested course was not found.")
            : AssistantContextResult.Success(JsonSerializer.Serialize(new
            {
                page = routeContext.RouteName,
                course,
                lessons
            }, JsonOptions));
    }

    private async Task<AssistantContextResult> GetLessonContextAsync(
        AssistantRouteContext routeContext,
        CancellationToken cancellationToken)
    {
        var lesson = await catalogClient.GetLessonAsync(
            routeContext.CourseCode!,
            routeContext.LessonSlug!,
            cancellationToken);
        return lesson is null
            ? AssistantContextResult.NotFound("The requested lesson was not found.")
            : AssistantContextResult.Success(JsonSerializer.Serialize(new
            {
                page = routeContext.RouteName,
                lesson
            }, JsonOptions));
    }

    private async Task<AssistantContextResult> GetQuizListContextAsync(
        AssistantRouteContext routeContext,
        CancellationToken cancellationToken)
    {
        var course = await catalogClient.GetCourseAsync(routeContext.CourseCode!, cancellationToken);
        if (course is null)
        {
            return AssistantContextResult.NotFound("The requested course was not found.");
        }

        var quizzes = await catalogClient.GetQuizzesAsync(
            routeContext.CourseCode!,
            cancellationToken);
        return quizzes is null
            ? AssistantContextResult.NotFound("The requested course was not found.")
            : AssistantContextResult.Success(JsonSerializer.Serialize(new
            {
                page = routeContext.RouteName,
                course,
                quizzes
            }, JsonOptions));
    }
}

public sealed record AssistantContextResult(string? CanonicalContext, string? NotFoundMessage)
{
    public bool IsFound => CanonicalContext is not null;

    public static AssistantContextResult Success(string canonicalContext) =>
        new(canonicalContext, null);

    public static AssistantContextResult NotFound(string message) =>
        new(null, message);
}
