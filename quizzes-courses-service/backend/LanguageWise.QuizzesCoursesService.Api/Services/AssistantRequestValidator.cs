using System.Text.RegularExpressions;
using LanguageWise.QuizzesCoursesService.Api.Models;

namespace LanguageWise.QuizzesCoursesService.Api.Services;

public sealed partial class AssistantRequestValidator
{
    public const int MaximumMessageCharacters = 4000;
    public const int MaximumHistoryTurns = 12;
    public const int MaximumConversationCharacters = 12000;
    public const int MaximumHistoryMessageCharacters = MaximumConversationCharacters;

    private static readonly IReadOnlyDictionary<string, RouteRequirements> Routes =
        new Dictionary<string, RouteRequirements>(StringComparer.Ordinal)
        {
            ["home"] = RouteRequirements.None,
            ["quizzes"] = RouteRequirements.None,
            ["flashcards"] = RouteRequirements.None,
            ["course"] = RouteRequirements.Course,
            ["course-completion"] = RouteRequirements.Course,
            ["quiz-list"] = RouteRequirements.Course,
            ["flashcard-decks"] = RouteRequirements.Course,
            ["lesson"] = RouteRequirements.Lesson,
            ["flashcard-revision"] = RouteRequirements.Lesson
        };

    public AssistantValidationResult Validate(AssistantMessageRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var message = request.Message?.Trim() ?? string.Empty;
        var history = request.History ?? [];

        if (message.Length == 0)
        {
            errors["message"] = ["A message is required."];
        }
        else if (message.Length > MaximumMessageCharacters)
        {
            errors["message"] = [$"The message cannot exceed {MaximumMessageCharacters} characters."];
        }

        if (history.Count > MaximumHistoryTurns)
        {
            errors["history"] = [$"History cannot exceed {MaximumHistoryTurns} turns."];
        }

        var normalizedHistory = new List<AssistantHistoryMessage>(history.Count);
        var totalCharacters = message.Length;
        for (var index = 0; index < history.Count; index++)
        {
            var role = history[index].Role?.Trim().ToLowerInvariant() ?? string.Empty;
            var content = history[index].Content?.Trim() ?? string.Empty;
            var key = $"history[{index}]";

            if (role is not ("user" or "assistant"))
            {
                errors[key] = ["History roles must be either 'user' or 'assistant'."];
            }
            else if (content.Length == 0)
            {
                errors[key] = ["History content cannot be empty."];
            }
            else if (content.Length > MaximumHistoryMessageCharacters)
            {
                errors[key] =
                [
                    $"History content cannot exceed {MaximumHistoryMessageCharacters} characters."
                ];
            }

            normalizedHistory.Add(new AssistantHistoryMessage(role, content));
            totalCharacters += content.Length;
        }

        if (totalCharacters > MaximumConversationCharacters)
        {
            errors["history"] =
            [
                $"The message and history cannot exceed {MaximumConversationCharacters} characters."
            ];
        }

        var normalizedContext = ValidateContext(request.Context, errors);
        return errors.Count > 0
            ? new AssistantValidationResult(errors, null)
            : new AssistantValidationResult(
                errors,
                new ValidatedAssistantRequest(message, normalizedHistory, normalizedContext!));
    }

    private static AssistantRouteContext? ValidateContext(
        AssistantRouteContext? context,
        IDictionary<string, string[]> errors)
    {
        if (context is null)
        {
            errors["context"] = ["Route context is required."];
            return null;
        }

        var routeName = context.RouteName?.Trim().ToLowerInvariant() ?? string.Empty;
        var courseCode = NormalizeOptional(context.CourseCode);
        var lessonSlug = NormalizeOptional(context.LessonSlug);

        if (routeName == "quiz-runner")
        {
            errors["context.routeName"] = ["The assistant is not available in the quiz runner."];
            return null;
        }

        if (!Routes.TryGetValue(routeName, out var requirements))
        {
            errors["context.routeName"] = ["The route name is not supported."];
            return null;
        }

        if (courseCode is not null && !SafeRouteValue().IsMatch(courseCode))
        {
            errors["context.courseCode"] = ["The course code is invalid."];
        }

        if (lessonSlug is not null && !SafeRouteValue().IsMatch(lessonSlug))
        {
            errors["context.lessonSlug"] = ["The lesson slug is invalid."];
        }

        if (requirements >= RouteRequirements.Course && courseCode is null)
        {
            errors["context.courseCode"] = ["A course code is required for this route."];
        }
        else if (requirements == RouteRequirements.None && courseCode is not null)
        {
            errors["context.courseCode"] = ["A course code is not valid for this route."];
        }

        if (requirements == RouteRequirements.Lesson && lessonSlug is null)
        {
            errors["context.lessonSlug"] = ["A lesson slug is required for this route."];
        }
        else if (requirements != RouteRequirements.Lesson && lessonSlug is not null)
        {
            errors["context.lessonSlug"] = ["A lesson slug is not valid for this route."];
        }

        return new AssistantRouteContext(routeName, courseCode, lessonSlug);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,98}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeRouteValue();

    private enum RouteRequirements
    {
        None,
        Course,
        Lesson
    }
}

public sealed record AssistantValidationResult(
    IReadOnlyDictionary<string, string[]> Errors,
    ValidatedAssistantRequest? Request);

public sealed record ValidatedAssistantRequest(
    string Message,
    IReadOnlyList<AssistantHistoryMessage> History,
    AssistantRouteContext Context);
