using System.Text.Json.Serialization;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

public sealed record Achievement(
    [property: JsonPropertyName("achievement_id")] int AchievementId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("trigger")] string Trigger,
    [property: JsonPropertyName("progress_needed")] int ProgressNeeded);

public sealed record UserAchievement(
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("achievement_id")] int AchievementId,
    [property: JsonPropertyName("progress")] int Progress);

public sealed record UserPreferences(
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("notify_all")] bool NotifyAll,
    [property: JsonPropertyName("notify_post_engagement")] bool NotifyPostEngagement,
    [property: JsonPropertyName("notify_course_completion")] bool NotifyCourseCompletion,
    [property: JsonPropertyName("notify_quiz_results")] bool NotifyQuizResults,
    [property: JsonPropertyName("notify_streaks")] bool NotifyStreaks,
    [property: JsonPropertyName("notify_achievements")] bool NotifyAchievements);

public sealed record NotificationInput(
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("trigger")] string Trigger,
    [property: JsonPropertyName("time")] DateTimeOffset Time,
    [property: JsonPropertyName("email_subject")] string EmailSubject,
    [property: JsonPropertyName("email_body")] string EmailBody);

public sealed record Notification(
    [property: JsonPropertyName("notification_id")] long NotificationId,
    [property: JsonPropertyName("user_id")] int UserId,
    [property: JsonPropertyName("trigger")] string Trigger,
    [property: JsonPropertyName("time")] DateTimeOffset Time,
    [property: JsonPropertyName("email_subject")] string EmailSubject,
    [property: JsonPropertyName("email_body")] string EmailBody);

public sealed record AchievementUpdate(
    int AchievementId,
    string Name,
    string Description,
    int Progress,
    int ProgressNeeded,
    bool NewlyAttained);

public sealed record PreferenceUpdateRequest(
    string Email,
    bool NotifyAll,
    bool NotifyPostEngagement,
    bool NotifyCourseCompletion,
    bool NotifyQuizResults,
    bool NotifyStreaks,
    bool NotifyAchievements);

public sealed record EventRequest(
    string Trigger,
    string Subject,
    int RecipientUserId,
    string RecipientName,
    int? Value = null);

public sealed record AchievementProgress(
    int AchievementId,
    string Name,
    string Image,
    int Progress,
    int ProgressNeeded);