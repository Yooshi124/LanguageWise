using LanguageWise.QuizzesCoursesService.Db.Data;
using LanguageWise.QuizzesCoursesService.Db.Models;
using Microsoft.AspNetCore.Routing;
using System.Text.Json.Serialization;

const string ServiceName = "quizzes-courses-service-db";
const string PublicCatalogError = "The SQLite catalog health check failed.";
const int DefaultMilestonePageSize = 100;
const int MaxMilestonePageSize = 200;

var builder = WebApplication.CreateBuilder(args);

// The database file lives on a named Docker volume so it survives container restarts.
var databasePath = builder.Configuration["Database:Path"] ?? "data/quizzes-courses-service.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    ForeignKeys = true
}.ToString();

builder.Services.AddSingleton(new CatalogRepository(connectionString));
builder.Services.AddSingleton(new LearningRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (CatalogRepository repository, EndpointDataSource endpointDataSource) =>
{
    var endpoints = GetRegisteredEndpoints(endpointDataSource);

    try
    {
        var courses = repository.CountCourses();
        return Results.Ok(new DatabaseServiceHealth(
            "healthy",
            ServiceName,
            courses,
            null,
            new Dictionary<string, DatabaseDependencyHealth>
            {
                ["catalog"] = new("healthy", "sqlite", courses)
            },
            endpoints));
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "The SQLite catalog health check failed.");
        return Results.Json(
            new DatabaseServiceHealth(
                "unhealthy",
                ServiceName,
                null,
                PublicCatalogError,
                new Dictionary<string, DatabaseDependencyHealth>
                {
                    ["catalog"] = new("unhealthy", "sqlite", Error: PublicCatalogError)
                },
                endpoints),
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

app.MapGet("/api/courses/{code}/quizzes", (
    string code,
    CatalogRepository catalog,
    LearningRepository learning) =>
    catalog.GetCourse(code) is null
        ? Results.NotFound()
        : Results.Ok(learning.GetQuizSummaries(code)));

app.MapGet("/api/quizzes/{quizId:int}", (int quizId, LearningRepository repository) =>
    repository.GetQuiz(quizId) is { } quiz ? Results.Ok(quiz) : Results.NotFound());

app.MapPost("/api/quizzes/{quizId:int}/attempts", (
    int quizId,
    StartQuizAttemptRequest request,
    LearningRepository repository) =>
{
    var result = repository.StartAttempt(quizId, request.UserId);
    return result.IsSuccess
        ? Results.Created($"/api/quiz-attempts/{result.Value!.Id}", result.Value)
        : ToHttpResult(result);
});

app.MapPost("/api/quiz-attempts/{attemptId:int}/submit", (
    int attemptId,
    SubmitQuizAttemptRequest request,
    LearningRepository repository) =>
    ToHttpResult(repository.SubmitAttempt(attemptId, request.UserId, request.Answers)));

app.MapGet("/api/courses/{code}/flashcard-decks", (
    string code,
    CatalogRepository catalog,
    LearningRepository learning) =>
    catalog.GetCourse(code) is null
        ? Results.NotFound()
        : Results.Ok(learning.GetFlashcardDecks(code)));

app.MapGet("/api/courses/{code}/flashcard-decks/{lessonSlug}", (
    string code,
    string lessonSlug,
    LearningRepository repository) =>
    repository.GetFlashcardDeck(code, lessonSlug) is { } deck
        ? Results.Ok(deck)
        : Results.NotFound());

app.MapGet("/api/courses/{code}/progress/{userId:int}", (
    string code,
    int userId,
    LearningRepository repository) =>
    ToHttpResult(repository.GetCourseProgress(code, userId)));

app.MapGet("/api/users/{userId:int}/course-progress", (
    int userId,
    LearningRepository repository) =>
    ToHttpResult(repository.GetStartedCoursesProgress(userId)));

app.MapGet("/api/milestones", (
    int? afterId,
    int? limit,
    LearningRepository repository) =>
    GetMilestones(repository, null, afterId, limit));

app.MapGet("/api/users/{userId:int}/milestones", (
    int userId,
    int? afterId,
    int? limit,
    LearningRepository repository) =>
    userId > 0
        ? GetMilestones(repository, userId, afterId, limit)
        : Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["userId"] = ["User ID must be greater than zero."]
        }));

app.MapPut("/api/lessons/{lessonId:int}/milestones/{userId:int}", (
    int lessonId,
    int userId,
    LearningRepository repository) =>
    ToHttpResult(repository.CompleteLesson(lessonId, userId)));

app.MapDelete("/api/lessons/{lessonId:int}/milestones/{userId:int}", (
    int lessonId,
    int userId,
    LearningRepository repository) =>
    ToHttpResult(repository.UncompleteLesson(lessonId, userId)));

app.MapPut("/api/courses/{code}/milestones/{userId:int}", (
    string code,
    int userId,
    LearningRepository repository) =>
    ToHttpResult(repository.CompleteCourse(code, userId)));

app.MapDelete("/api/courses/{code}/milestones/{userId:int}", (
    string code,
    int userId,
    LearningRepository repository) =>
    ToHttpResult(repository.UncompleteCourse(code, userId)));

app.Run();

static IResult GetMilestones(
    LearningRepository repository,
    int? userId,
    int? afterId,
    int? limit)
{
    var cursor = afterId ?? 0;
    var pageSize = limit ?? DefaultMilestonePageSize;
    var errors = new Dictionary<string, string[]>();

    if (cursor < 0)
    {
        errors["afterId"] = ["Cursor must be zero or greater."];
    }

    if (pageSize is < 1 or > MaxMilestonePageSize)
    {
        errors["limit"] = [$"Limit must be between 1 and {MaxMilestonePageSize}."];
    }

    return errors.Count > 0
        ? Results.ValidationProblem(errors)
        : Results.Ok(repository.GetMilestones(userId, cursor, pageSize));
}

static IResult ToHttpResult<T>(DomainResult<T> result)
{
    if (result.IsSuccess)
    {
        return Results.Ok(result.Value);
    }

    var error = result.Error!;
    var body = new { error = error.Code, message = error.Message };
    return error.Kind switch
    {
        DomainErrorKind.Validation => Results.BadRequest(body),
        DomainErrorKind.NotFound => Results.NotFound(body),
        DomainErrorKind.Conflict => Results.Conflict(body),
        _ => throw new ArgumentOutOfRangeException(nameof(error.Kind))
    };
}

static IReadOnlyList<RegisteredEndpoint> GetRegisteredEndpoints(
    EndpointDataSource endpointDataSource) =>
    endpointDataSource.Endpoints
        .OfType<RouteEndpoint>()
        .SelectMany(endpoint =>
        {
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["*"];
            var route = endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString() ?? string.Empty;
            return methods.Select(method => new RegisteredEndpoint(method, route, "registered"));
        })
        .OrderBy(endpoint => endpoint.Route, StringComparer.Ordinal)
        .ThenBy(endpoint => endpoint.Method, StringComparer.Ordinal)
        .ToArray();

internal sealed record DatabaseServiceHealth(
    string Status,
    string Service,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Courses,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error,
    IReadOnlyDictionary<string, DatabaseDependencyHealth> Dependencies,
    IReadOnlyList<RegisteredEndpoint> Endpoints);

internal sealed record DatabaseDependencyHealth(
    string Status,
    string Type,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? Courses = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error = null);

internal sealed record RegisteredEndpoint(string Method, string Route, string Status);
