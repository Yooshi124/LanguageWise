using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Services;

public sealed class AssistantRequestValidator
{
    public const int MaximumMessageCharacters = 4000;
    public const int MaximumHistoryTurns = 12;
    public const int MaximumConversationCharacters = 12000;

    public AssistantValidationResult Validate(AssistantMessageRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (request is null)
        {
            errors["request"] = ["A request body is required."];
            return new AssistantValidationResult(errors, null);
        }

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

            normalizedHistory.Add(new AssistantHistoryMessage(role, content));
            totalCharacters += content.Length;
        }

        if (totalCharacters > MaximumConversationCharacters)
        {
            errors["history"] =
                [$"The message and history cannot exceed {MaximumConversationCharacters} characters."];
        }

        var routeName = request.Context?.RouteName?.Trim().ToLowerInvariant() ?? string.Empty;
        if (routeName != "quests-achievements-home")
        {
            errors["context.routeName"] = ["The route name is not supported."];
        }

        return errors.Count > 0
            ? new AssistantValidationResult(errors, null)
            : new AssistantValidationResult(
                errors,
                new ValidatedAssistantRequest(
                    message,
                    normalizedHistory,
                    new AssistantRouteContext(routeName)));
    }
}