using LanguageWise.ChatDiscussionService.Db.Clients;

namespace LanguageWise.ChatDiscussionService.Db.Data;

public sealed class AuthorNameSync(
    DiscussionRepository repository,
    IHttpClientFactory httpClientFactory,
    TimeSpan interval,
    ILogger<AuthorNameSync> logger) : BackgroundService
{
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var timer = new PeriodicTimer(interval);
        do
        {
            await SyncOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task<int> SyncOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = new UserDirectoryClient(httpClientFactory.CreateClient(nameof(UserDirectoryClient)));
            var renamed = 0;

            foreach (var batch in repository.GetAuthorIds().Chunk(BatchSize))
            {
                renamed += repository.SyncAuthorNames(await client.GetUsersAsync(batch, cancellationToken));
            }

            if (renamed > 0)
            {
                logger.LogInformation("Refreshed the author name on {Count} posts and comments.", renamed);
            }

            return renamed;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Author names could not be refreshed; they are left as they are.");
            return 0;
        }
    }
}
