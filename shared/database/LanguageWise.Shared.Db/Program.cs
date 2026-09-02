using LanguageWise.Shared.Db.Data;
const string ServiceName = "shared-db";

var builder = WebApplication.CreateBuilder(args);

// The database file lives on a named Docker volume so it survives container restarts.
var databasePath = builder.Configuration["Database:Path"] ?? "data/shared.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

builder.Services.AddSingleton(new UserRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (UserRepository repository) =>
{
    try
    {
        return Results.Ok(new { status = "healthy", service = ServiceName, users = repository.Count() });
    }
    catch (Exception exception)
    {
        return Results.Json(
            new { status = "unhealthy", service = ServiceName, error = exception.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/users/verify", (LoginInput input, UserRepository users) =>
{
    if (string.IsNullOrEmpty(input.Username) || string.IsNullOrEmpty(input.Password))
        return Results.Unauthorized();

    var userId = users.Verify(input.Username, input.Password);
    return userId is not null
        ? Results.Ok(new { authenticated = true, userId })
        : Results.Unauthorized();
});

app.Run();

internal sealed record LoginInput(string Username, string Password);
