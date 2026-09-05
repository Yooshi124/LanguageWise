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
            new ProfilePreferences(
                preferences.Email,
                preferences.NotifyAll,
                preferences.NotifyCommunityContribution,
                preferences.NotifyPostEngagement,
                preferences.NotifyLessonCompletion,
                preferences.NotifyCourseCompletion,
                preferences.NotifyQuizResult,
                preferences.NotifyMinigameWin,
                preferences.NotifyLoginStreak,
                preferences.NotifyAchievements),
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

    private static UserPreferences DefaultPreferences(int userId) =>
        new(userId, null, true, true, true, true, true, true, true, true, true);
}