using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LanguageWise.McpServer.Tools.QuestsAchievements;

[McpServerToolType]
public sealed class QuestsAchievementsTools(DownstreamClient downstream)
{
	public const string ServiceName = "QuestsAchievements";
	private const string PreferencesPath = "api/preferences";

	[McpServerTool(Name = "quests_get_my_preferences", Title = "Get my notification preferences", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Gets the signed-in user's email notification preferences: whether notifications are on at all (the master switch), the notification email address, and whether each notification category is enabled.")]
	public async Task<NotificationPreferencesResult> GetMyPreferencesAsync(CancellationToken cancellationToken) =>
		ToResult(await GetPreferencesAsync(cancellationToken));

	[McpServerTool(Name = "quests_set_notifications_enabled", Title = "Turn my notifications on or off", ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Turns all of the signed-in user's email notifications on or off using the master switch. Individual category settings are kept. Only call this when the user has explicitly asked to turn notifications on or off.")]
	public async Task<NotificationPreferencesResult> SetNotificationsEnabledAsync(
		[Description("true to turn notifications on, false to turn them all off.")] bool enabled,
		CancellationToken cancellationToken)
	{
		var current = await GetPreferencesAsync(cancellationToken);
		return await SaveAsync(current with { NotifyAll = enabled }, cancellationToken);
	}

	[McpServerTool(Name = "quests_update_my_preferences", Title = "Change my notification categories", ReadOnly = false, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Enables or disables one or more of the signed-in user's notification categories. Omit a category to leave it unchanged. Does not change the master on/off switch or the email address. Only call this when the user has explicitly asked to change these settings.")]
	public async Task<NotificationPreferencesResult> UpdateMyPreferencesAsync(
		[Description("Notifications when the user contributes to the community, such as writing forum posts.")] bool? communityContribution = null,
		[Description("Notifications when others engage with the user's forum posts, such as likes and comments.")] bool? postEngagement = null,
		[Description("Notifications when the user completes a lesson.")] bool? lessonCompletion = null,
		[Description("Notifications when the user completes a course.")] bool? courseCompletion = null,
		[Description("Notifications with the user's quiz results.")] bool? quizResult = null,
		[Description("Notifications when the user wins a mini game.")] bool? minigameWin = null,
		[Description("Notifications about the user's daily login streak.")] bool? loginStreak = null,
		[Description("Notifications when the user earns a new achievement, even if that event's own category is off.")] bool? achievements = null,
		CancellationToken cancellationToken = default)
	{
		if (communityContribution is null && postEngagement is null && lessonCompletion is null && courseCompletion is null
			&& quizResult is null && minigameWin is null && loginStreak is null && achievements is null)
		{
			throw new McpException("Specify at least one notification category to change.");
		}

		var current = await GetPreferencesAsync(cancellationToken);
		return await SaveAsync(current with
		{
			NotifyCommunityContribution = communityContribution ?? current.NotifyCommunityContribution,
			NotifyPostEngagement = postEngagement ?? current.NotifyPostEngagement,
			NotifyLessonCompletion = lessonCompletion ?? current.NotifyLessonCompletion,
			NotifyCourseCompletion = courseCompletion ?? current.NotifyCourseCompletion,
			NotifyQuizResult = quizResult ?? current.NotifyQuizResult,
			NotifyMinigameWin = minigameWin ?? current.NotifyMinigameWin,
			NotifyLoginStreak = loginStreak ?? current.NotifyLoginStreak,
			NotifyAchievements = achievements ?? current.NotifyAchievements
		}, cancellationToken);
	}

	[McpServerTool(Name = "quests_get_my_achievements", Title = "Get my achievements progress", ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
	[Description("Lists every LanguageWise achievement with the signed-in user's progress towards it and whether it has been earned.")]
	public async Task<AchievementListResult> GetMyAchievementsAsync(CancellationToken cancellationToken)
	{
		var achievements = await downstream.GetAsync<List<AchievementStatusDto>>(ServiceName, "api/achievements", cancellationToken);
		return new AchievementListResult(achievements
			.Select(achievement => new AchievementItem(
				achievement.Name,
				achievement.Description,
				achievement.Progress,
				achievement.ProgressNeeded < 0 ? null : achievement.ProgressNeeded,
				achievement.ProgressNeeded >= 0 && achievement.Progress >= achievement.ProgressNeeded))
			.ToList());
	}

	private Task<PreferencesDto> GetPreferencesAsync(CancellationToken cancellationToken) =>
		downstream.GetAsync<PreferencesDto>(ServiceName, PreferencesPath, cancellationToken);

	private async Task<NotificationPreferencesResult> SaveAsync(PreferencesDto preferences, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(preferences.Email))
		{
			throw new McpException("Add a notification email address on the Achievements & Notifications page before changing notification settings.");
		}

		await downstream.PutAsync(ServiceName, PreferencesPath, preferences, cancellationToken);
		return ToResult(preferences);
	}

	private static NotificationPreferencesResult ToResult(PreferencesDto preferences) => new(
		preferences.NotifyAll,
		preferences.Email,
		preferences.NotifyCommunityContribution,
		preferences.NotifyPostEngagement,
		preferences.NotifyLessonCompletion,
		preferences.NotifyCourseCompletion,
		preferences.NotifyQuizResult,
		preferences.NotifyMinigameWin,
		preferences.NotifyLoginStreak,
		preferences.NotifyAchievements);
}
