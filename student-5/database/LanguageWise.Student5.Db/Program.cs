using LanguageWise.Student5.Db.Data;
using LanguageWise.Student5.Db.Models;

const string ServiceName = "student-5-db";

var builder = WebApplication.CreateBuilder(args);

// The database file lives on a named Docker volume so it survives container restarts.
var databasePath = builder.Configuration["Database:Path"] ?? "data/student-5.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath
}.ToString();

builder.Services.AddSingleton(new SampleItemRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (SampleItemRepository repository) =>
{
    try
    {
        return Results.Ok(new { status = "healthy", service = ServiceName, items = repository.Count() });
    }
    catch (Exception exception)
    {
        return Results.Json(
            new { status = "unhealthy", service = ServiceName, error = exception.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/items", (SampleItemRepository repository) =>
    Results.Ok(repository.GetAll()));

app.MapGet("/api/items/{id:int}", (int id, SampleItemRepository repository) =>
    repository.GetById(id) is { } item ? Results.Ok(item) : Results.NotFound());

app.MapPost("/api/items", (SampleItemInput input, SampleItemRepository repository) =>
{
    if (Validate(input) is { } problem)
    {
        return problem;
    }

    var created = repository.Create(input.Name!.Trim(), input.Description?.Trim() ?? string.Empty);
    return Results.Created($"/api/items/{created.Id}", created);
});

app.MapPut("/api/items/{id:int}", (int id, SampleItemInput input, SampleItemRepository repository) =>
{
    if (Validate(input) is { } problem)
    {
        return problem;
    }

    var updated = repository.Update(id, input.Name!.Trim(), input.Description?.Trim() ?? string.Empty);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

app.MapDelete("/api/items/{id:int}", (int id, SampleItemRepository repository) =>
    repository.Delete(id) ? Results.NoContent() : Results.NotFound());

app.Run();

static IResult? Validate(SampleItemInput input) =>
    string.IsNullOrWhiteSpace(input.Name)
        ? Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = ["Name is required."]
        })
        : null;
