using LanguageWise.MiniGamesService.Db.Data;

namespace LanguageWise.MiniGamesService.Db.Services;

/// <summary>Periodically purges games whose ExpiresAt has passed, so the table does not grow forever.</summary>
public sealed class ExpiredGameCleanupService(
    GameRepository gameRepository,
    ILogger<ExpiredGameCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                var deleted = gameRepository.DeleteExpired(DateTime.UtcNow);
                if (deleted > 0)
                {
                    logger.LogInformation("Purged {DeletedCount} expired game(s)", deleted);
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Expired game cleanup failed; will retry on the next interval");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
