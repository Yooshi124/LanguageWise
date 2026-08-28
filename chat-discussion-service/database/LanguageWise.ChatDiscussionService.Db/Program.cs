using LanguageWise.ChatDiscussionService.Db.Data;

const string serviceName = "chat-discussion-service-db";

var builder = WebApplication.CreateBuilder(args);
var databasePath = builder.Configuration["Database:Path"] ?? "data/chat-discussion-service.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

builder.Services.AddSingleton(new SampleItemRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));
builder.Services.AddSingleton(new DiscussionRepository(connectionString));

var app = builder.Build();
app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (SampleItemRepository repository) =>
{
    try
    {
        return Results.Ok(new { status = "healthy", service = serviceName, items = repository.Count() });
    }
    catch (Exception exception)
    {
        return Results.Json(
            new { status = "unhealthy", service = serviceName, error = exception.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/posts", (DiscussionRepository repository) => Results.Ok(repository.GetPosts()));
app.MapGet("/api/comments", (DiscussionRepository repository) => Results.Ok(repository.GetComments()));
app.MapGet("/api/likes", (DiscussionRepository repository) => Results.Ok(repository.GetLikes()));
app.MapGet("/api/images", (DiscussionRepository repository) => Results.Ok(repository.GetImages()));

app.Run();
