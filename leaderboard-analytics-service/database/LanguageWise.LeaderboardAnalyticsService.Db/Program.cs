using LanguageWise.LeaderboardAnalyticsService.Db.Data;

var builder = WebApplication.CreateBuilder(args);
var databasePath = builder.Configuration["Database:Path"] ?? "data/leaderboard-analytics-service.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));
builder.Services.AddSingleton(new LanguageRankingRepository(connectionString));
builder.Services.AddSingleton(new DiscussionRankingRepository(connectionString));

var app = builder.Build();
app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", () => Results.Ok());

// ---------------------------------------------------------------------------
// Language Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/language-rankings", (
    LanguageRankingRepository repository,
    string? language = null,
    int limit = 50,
    int offset = 0) =>
    Results.Ok(repository.GetAll(language, limit, offset)));

app.MapGet("/api/language-rankings/{id:int}", (int id, LanguageRankingRepository repository) =>
    repository.GetById(id) is { } ranking ? Results.Ok(ranking) : Results.NotFound());

app.MapGet("/api/language-rankings/user/{userId:int}", (int userId, LanguageRankingRepository repository) =>
    Results.Ok(repository.GetByUserId(userId)));

// ---------------------------------------------------------------------------
// Discussion Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/discussion-rankings", (
    DiscussionRankingRepository repository,
    int limit = 50,
    int offset = 0) =>
    Results.Ok(repository.GetAll(limit, offset)));

app.MapGet("/api/discussion-rankings/{id:int}", (int id, DiscussionRankingRepository repository) =>
    repository.GetById(id) is { } ranking ? Results.Ok(ranking) : Results.NotFound());

app.MapGet("/api/discussion-rankings/user/{userId:int}", (int userId, DiscussionRankingRepository repository) =>
    repository.GetByUserId(userId) is { } ranking ? Results.Ok(ranking) : Results.NotFound());

app.Run();
