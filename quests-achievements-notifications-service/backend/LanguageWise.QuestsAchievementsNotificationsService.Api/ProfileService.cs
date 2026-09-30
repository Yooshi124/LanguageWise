using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;

namespace LanguageWise.QuestsAchievementsNotificationsService.Api;

public sealed class ProfileService(AppDataClient client)
{
    public async Task<ProfileResponse> GetAsync(
        int userId,
        string username,
        CancellationToken cancellationToken)
    {
        var preferences = await client.GetPreferencesAsync(userId, cancellationToken)
            ?? DefaultPreferences(userId);
        var achievements = await client.GetAchievementsAsync(cancellationToken);
        var progress = (await client.GetUserAchievementsAsync(userId, cancellationToken))
            .ToDictionary(item => item.AchievementId, item => item.Progress);
        var notifications = await client.GetNotificationsAsync(userId, cancellationToken);

        return new ProfileResponse(
            username,
            ToProfilePreferences(preferences),
            achievements.Select(achievement => new AchievementProgress(
                achievement.AchievementId,
                achievement.Name,
                achievement.Image,
                progress.GetValueOrDefault(achievement.AchievementId),
                achievement.ProgressNeeded)).ToList(),
            notifications.Select(notification => new ProfileNotification(
                notification.NotificationId,
                notification.Trigger,
                notification.Time,
                notification.EmailSubject,
                notification.EmailBody)).ToList());
    }

    public async Task<ProfilePreferences> GetPreferencesAsync(int userId, CancellationToken cancellationToken)
    {
        var preferences = await client.GetPreferencesAsync(userId, cancellationToken)
            ?? DefaultPreferences(userId);
        return ToProfilePreferences(preferences);
    }

    public async Task<IReadOnlyList<AchievementStatus>> GetAchievementStatusAsync(int userId, CancellationToken cancellationToken)
    {
        var achievements = await client.GetAchievementsAsync(cancellationToken);
        var progress = (await client.GetUserAchievementsAsync(userId, cancellationToken))
            .ToDictionary(item => item.AchievementId, item => item.Progress);
        return achievements.Select(achievement => new AchievementStatus(
            achievement.AchievementId,
            achievement.Name,
            achievement.Description,
            progress.GetValueOrDefault(achievement.AchievementId),
            achievement.ProgressNeeded)).ToList();
    }

    private static ProfilePreferences ToProfilePreferences(UserPreferences preferences) => new(
        preferences.Email,
        preferences.NotifyAll,
        preferences.NotifyCommunityContribution,
        preferences.NotifyPostEngagement,
        preferences.NotifyLessonCompletion,
        preferences.NotifyCourseCompletion,
        preferences.NotifyQuizResult,
        preferences.NotifyMinigameWin,
        preferences.NotifyLoginStreak,
        preferences.NotifyAchievements);

    private static UserPreferences DefaultPreferences(int userId) =>
        new(userId, null, true, true, true, true, true, true, true, true, true);
}