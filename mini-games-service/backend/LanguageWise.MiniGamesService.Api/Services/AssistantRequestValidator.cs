using System.Text.RegularExpressions;
using LanguageWise.MiniGamesService.Api.Models;

namespace LanguageWise.MiniGamesService.Api.Services;

public sealed partial class AssistantRequestValidator
{
    public const int MaximumMessageCharacters = 4000;
    public const int MaximumHistoryTurns = 12;
    public const int MaximumConversationCharacters = 12000;
    public const int MaximumHistoryMessageCharacters = MaximumConversationCharacters;

    private static readonly IReadOnlySet<string> Routes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "home",
            "guess-the-word",
            "word-search",
            "associations"
        };

    private static readonly IReadOnlySet<string> Modes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "content",
            "ai"
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
        var mode = NormalizeOptional(context.Mode);

        if (!Routes.Contains(routeName))
        {
            errors["context.routeName"] = ["The route name is not supported."];
        }

        if (courseCode is not null && !SafeRouteValue().IsMatch(courseCode))
        {
            errors["context.courseCode"] = ["The course code is invalid."];
        }

        if (mode is not null && !Modes.Contains(mode))
        {
            errors["context.mode"] = ["The vocabulary mode is invalid."];
        }

        return errors.Count > 0
            ? null
            : new AssistantRouteContext(routeName, courseCode, mode);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,98}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeRouteValue();
}

public sealed record AssistantValidationResult(
    IReadOnlyDictionary<string, string[]> Errors,
    ValidatedAssistantRequest? Request);

public sealed record ValidatedAssistantRequest(
    string Message,
    IReadOnlyList<AssistantHistoryMessage> History,
    AssistantRouteContext Context);
