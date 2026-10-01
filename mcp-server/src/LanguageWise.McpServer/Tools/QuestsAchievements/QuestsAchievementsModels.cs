namespace LanguageWise.McpServer.Tools.QuestsAchievements;

public sealed record NotificationPreferencesResult(
	bool NotificationsEnabled,
	string? Email,
	bool CommunityContribution,
	bool PostEngagement,
	bool LessonCompletion,
	bool CourseCompletion,
	bool QuizResult,
	bool MinigameWin,
	bool LoginStreak,
	bool Achievements);

public sealed record AchievementListResult(IReadOnlyList<AchievementItem> Achievements);

/// <param name="ProgressNeeded">Null for record-style achievements (such as longest login streak) that have no target.</param>
public sealed record AchievementItem(string Name, string Description, int Progress, int? ProgressNeeded, bool Earned);

internal sealed record PreferencesDto(
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

internal sealed record AchievementStatusDto(
	int AchievementId,
	string Name,
	string Description,
	int Progress,
	int ProgressNeeded);
