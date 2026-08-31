using LanguageWise.LeaderboardAnalyticsService.Api.Clients;

var builder = WebApplication.CreateBuilder(args);

var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6005";

builder.Services.AddHttpClient<LeaderboardClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

// ---------------------------------------------------------------------------
// Language Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/language-rankings", (
    LeaderboardClient client,
    CancellationToken cancellationToken,
    string? language = null,
    int limit = 50,
    int offset = 0) =>
    client.GetLanguageRankingsAsync(language, limit, offset, cancellationToken));

app.MapGet("/api/language-rankings/{id:int}", async (int id, LeaderboardClient client, CancellationToken cancellationToken) =>
    await client.GetLanguageRankingAsync(id, cancellationToken) is { } ranking
        ? Results.Ok(ranking)
        : Results.NotFound());

app.MapGet("/api/language-rankings/user/{userId:int}", (int userId, LeaderboardClient client, CancellationToken cancellationToken) =>
    client.GetLanguageRankingsByUserAsync(userId, cancellationToken));

// ---------------------------------------------------------------------------
// Discussion Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/discussion-rankings", (
    LeaderboardClient client,
    CancellationToken cancellationToken,
    int limit = 50,
    int offset = 0) =>
    client.GetDiscussionRankingsAsync(limit, offset, cancellationToken));

app.MapGet("/api/discussion-rankings/{id:int}", async (int id, LeaderboardClient client, CancellationToken cancellationToken) =>
    await client.GetDiscussionRankingAsync(id, cancellationToken) is { } ranking
        ? Results.Ok(ranking)
        : Results.NotFound());

app.MapGet("/api/discussion-rankings/user/{userId:int}", async (int userId, LeaderboardClient client, CancellationToken cancellationToken) =>
    await client.GetDiscussionRankingByUserAsync(userId, cancellationToken) is { } ranking
        ? Results.Ok(ranking)
        : Results.NotFound());

app.Run();
