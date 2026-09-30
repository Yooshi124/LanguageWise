using LanguageWise.MiniGamesService.Api.Models;

namespace LanguageWise.MiniGamesService.Api.Services;

public static class StreakCalculator
{
    // Consecutive days (ending today or yesterday, UTC) with at least one completed round, so a
    // missed day doesn't zero the streak until a full day has passed with nothing played.
    public static int CalculateStreak(IEnumerable<GameAttemptResponse> completedAttempts)
    {
        var playedDates = completedAttempts
            .Select(attempt => attempt.CompletedAt)
            .Where(completedAt => !string.IsNullOrEmpty(completedAt))
            .Select(completedAt => DateTime.Parse(completedAt!, null, System.Globalization.DateTimeStyles.RoundtripKind).Date)
            .ToHashSet();

        if (playedDates.Count == 0)
        {
            return 0;
        }

        var today = DateTime.UtcNow.Date;
        var cursor = playedDates.Contains(today) ? today : today.AddDays(-1);
        if (!playedDates.Contains(cursor))
        {
            return 0;
        }

        var streak = 0;
        while (playedDates.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
