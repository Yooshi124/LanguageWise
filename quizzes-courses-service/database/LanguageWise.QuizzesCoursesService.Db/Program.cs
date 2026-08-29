using LanguageWise.QuizzesCoursesService.Db.Data;
using LanguageWise.QuizzesCoursesService.Db.Models;

const string ServiceName = "quizzes-courses-service-db";

var builder = WebApplication.CreateBuilder(args);

// The database file lives on a named Docker volume so it survives container restarts.
var databasePath = builder.Configuration["Database:Path"] ?? "data/quizzes-courses-service.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    ForeignKeys = true
}.ToString();

builder.Services.AddSingleton(new CatalogRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (CatalogRepository repository) =>
{
    try
    {
        return Results.Ok(new { status = "healthy", service = ServiceName, courses = repository.CountCourses() });
    }
    catch (Exception exception)
    {
        return Results.Json(
            new { status = "unhealthy", service = ServiceName, error = exception.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapGet("/api/courses", (CatalogRepository repository) =>
    Results.Ok(repository.GetCourses()));

app.MapGet("/api/courses/{code}", (string code, CatalogRepository repository) =>
    repository.GetCourse(code) is { } course ? Results.Ok(course) : Results.NotFound());

app.MapGet("/api/courses/{code}/lessons", (string code, CatalogRepository repository) =>
    repository.GetCourse(code) is null
        ? Results.NotFound()
        : Results.Ok(repository.GetLessons(code)));

app.MapGet("/api/courses/{code}/lessons/{slug}", (string code, string slug, CatalogRepository repository) =>
    repository.GetLesson(code, slug) is { } lesson ? Results.Ok(lesson) : Results.NotFound());

app.MapGet("/api/courses/{code}/quizzes", (string code, CatalogRepository repository) =>
    repository.GetCourse(code) is null
        ? Results.NotFound()
        : Results.Ok(repository.GetQuizzes(code)));

app.MapGet("/api/courses/{code}/flashcards", (string code, CatalogRepository repository) =>
    repository.GetCourse(code) is null
        ? Results.NotFound()
        : Results.Ok(repository.GetFlashcards(code)));

app.Run();
