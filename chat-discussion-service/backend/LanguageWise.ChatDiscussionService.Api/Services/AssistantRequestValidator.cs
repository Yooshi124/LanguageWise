using LanguageWise.ChatDiscussionService.Api.Models;

namespace LanguageWise.ChatDiscussionService.Api.Services;

/// <summary>
/// Validation and normalisation for AI mode requests, with no HTTP or model
/// dependencies, so it can be unit tested directly rather than through the
/// request pipeline. Sibling of <see cref="DiscussionRules"/>.
///
/// The whole conversation is re-sent on every request and the client controls
/// its length, so the caps here are what stops one browser tab from posting a
/// megabyte of history to a metered model.
/// </summary>
public sealed class AssistantRequestValidator
{
    public const int MaximumMessageCharacters = 4000;
    public const int MaximumHistoryTurns = 12;
    public const int MaximumConversationCharacters = 12000;
    public const int MaximumHistoryMessageCharacters = MaximumConversationCharacters;

    /// <summary>
    /// The router's route names, and what each one has to carry with it. An
    /// unknown name is rejected rather than ignored: it is the only signal that
    /// the client and this allowlist have drifted apart.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, RouteRequirements> Routes =
        new Dictionary<string, RouteRequirements>(StringComparer.Ordinal)
        {
            ["forums"] = RouteRequirements.None,
            ["my-posts"] = RouteRequirements.None,
            ["post-create"] = RouteRequirements.None,
            ["forum"] = RouteRequirements.Forum,
            ["post"] = RouteRequirements.Post,
            ["post-edit"] = RouteRequirements.Post
        };

    public AssistantValidationResult Validate(AssistantMessageRequest? request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (request is null)
        {
            errors["body"] = ["A request body is required."];
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
            errors["message"] = [$"A message cannot be longer than {MaximumMessageCharacters} characters."];
        }

        if (history.Count > MaximumHistoryTurns)
        {
            errors["history"] = [$"At most {MaximumHistoryTurns} earlier turns can be supplied."];
        }

        var normalisedHistory = new List<AssistantHistoryMessage>(history.Count);
        var totalCharacters = message.Length;

        for (var index = 0; index < history.Count; index++)
        {
            var role = history[index].Role?.Trim().ToLowerInvariant() ?? string.Empty;
            var content = history[index].Content?.Trim() ?? string.Empty;
            var key = $"history[{index}]";

            // Roles are checked rather than coerced, so a client cannot smuggle in
            // a second system prompt and have it silently accepted as a user turn.
            if (role is not (UserRole or AssistantRole))
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
                    $"History content cannot be longer than {MaximumHistoryMessageCharacters} characters."
                ];
            }

            normalisedHistory.Add(new AssistantHistoryMessage(role, content));
            totalCharacters += content.Length;
        }

        if (totalCharacters > MaximumConversationCharacters)
        {
            errors["history"] =
            [
                $"The message and history together cannot exceed {MaximumConversationCharacters} characters."
            ];
        }

        var normalisedContext = ValidateContext(request.Context, errors);

        return errors.Count > 0
            ? new AssistantValidationResult(errors, null)
            : new AssistantValidationResult(
                errors,
                new ValidatedAssistantRequest(message, normalisedHistory, normalisedContext!));
    }

    internal const string UserRole = "user";
    internal const string AssistantRole = "assistant";

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

        if (!Routes.TryGetValue(routeName, out var requirements))
        {
            errors["context.routeName"] = ["The route name is not supported."];
            return null;
        }

        var forumCode = string.IsNullOrWhiteSpace(context.ForumCode)
            ? null
            : context.ForumCode.Trim().ToLowerInvariant();

        if (forumCode is not null && !DiscussionRules.IsKnownForum(forumCode))
        {
            errors["context.forumCode"] = [DiscussionRules.UnknownForumMessage];
        }

        if (requirements == RouteRequirements.Forum && forumCode is null)
        {
            errors["context.forumCode"] = ["A forum code is required for this route."];
        }
        else if (requirements != RouteRequirements.Forum && forumCode is not null)
        {
            errors["context.forumCode"] = ["A forum code is not valid for this route."];
        }

        if (context.PostId is <= 0)
        {
            errors["context.postId"] = ["The post ID is invalid."];
        }

        if (requirements == RouteRequirements.Post && context.PostId is null)
        {
            errors["context.postId"] = ["A post ID is required for this route."];
        }
        else if (requirements != RouteRequirements.Post && context.PostId is not null)
        {
            errors["context.postId"] = ["A post ID is not valid for this route."];
        }

        return new AssistantRouteContext(routeName, forumCode, context.PostId);
    }

    private enum RouteRequirements
    {
        None,
        Forum,
        Post
    }
}

public sealed record AssistantValidationResult(
    IReadOnlyDictionary<string, string[]> Errors,
    ValidatedAssistantRequest? Request);

/// <summary>A request that has been through the validator, so nothing on it is null.</summary>
public sealed record ValidatedAssistantRequest(
    string Message,
    IReadOnlyList<AssistantHistoryMessage> History,
    AssistantRouteContext Context);
