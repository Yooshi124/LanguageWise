using System.Text.Json.Serialization;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

public sealed record AssistantMessageRequest(
    string? Message,
    IReadOnlyList<AssistantHistoryMessage>? History,
    AssistantRouteContext? Context);

public sealed record AssistantHistoryMessage(string? Role, string? Content);

public sealed record AssistantRouteContext(string? RouteName);

public sealed record AssistantDeltaEvent(string Content);

public sealed record AssistantDoneEvent(string Reason);

public sealed record AssistantErrorEvent(string Message, string Code);

public sealed record AssistantChatMessage(string Role, string Content);

public sealed record OpenRouterChatRequest(
    string Model,
    IReadOnlyList<AssistantChatMessage> Messages,
    bool Stream,
    [property: JsonPropertyName("max_tokens")] int MaxTokens);

public sealed record OllamaChatRequest(
    string Model,
    IReadOnlyList<AssistantChatMessage> Messages,
    bool Stream,
    bool Think);

public sealed record ValidatedAssistantRequest(
    string Message,
    IReadOnlyList<AssistantHistoryMessage> History,
    AssistantRouteContext Context);

public sealed record AssistantValidationResult(
    IReadOnlyDictionary<string, string[]> Errors,
    ValidatedAssistantRequest? Request);

public sealed record ProfilePreferences(
    string? Email,
    bool NotifyAll,
    bool NotifyCommunityContribution,
    bool NotifyPostEngagement,
    bool NotifyLessonCompletion,
    bool NotifyCourseCompletion,
    bool NotifyQuizResult,
    bool NotifyMinigameWin,
    bool NotifyLoginStreak,
    bool NotifyAchievements);

public sealed record ProfileNotification(
    long NotificationId,
    string Trigger,
    DateTimeOffset Time,
    string EmailSubject,
    string EmailBody);

public sealed record ProfileResponse(
    string Username,
    ProfilePreferences Preferences,
    IReadOnlyList<AchievementProgress> Achievements,
    IReadOnlyList<ProfileNotification> Notifications);