using LanguageWise.QuizzesCoursesService.Api.Clients;

const string ServiceName = "quizzes-courses-service-backend";

var builder = WebApplication.CreateBuilder(args);
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6003";

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapGet("/api/courses", async (CatalogClient client, CancellationToken cancellationToken) =>
    await ExecuteAsync(
        async () => Results.Ok(await client.GetCoursesAsync(cancellationToken)),
        app.Logger));

app.MapGet("/api/courses/{code}", async (
    string code,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var course = await client.GetCourseAsync(Normalize(code), cancellationToken);
        return course is null ? Results.NotFound() : Results.Ok(course);
    }, app.Logger));

app.MapGet("/api/courses/{code}/lessons", async (
    string code,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var lessons = await client.GetLessonsAsync(Normalize(code), cancellationToken);
        return lessons is null ? Results.NotFound() : Results.Ok(lessons);
    }, app.Logger));

app.MapGet("/api/courses/{code}/lessons/{slug}", async (
    string code,
    string slug,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var lesson = await client.GetLessonAsync(Normalize(code), Normalize(slug), cancellationToken);
        return lesson is null ? Results.NotFound() : Results.Ok(lesson);
    }, app.Logger));

app.MapGet("/api/courses/{code}/quizzes", async (
    string code,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var quizzes = await client.GetQuizzesAsync(Normalize(code), cancellationToken);
        return quizzes is null ? Results.NotFound() : Results.Ok(quizzes);
    }, app.Logger));

app.MapGet("/api/courses/{code}/flashcards", async (
    string code,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var flashcards = await client.GetFlashcardsAsync(Normalize(code), cancellationToken);
        return flashcards is null ? Results.NotFound() : Results.Ok(flashcards);
    }, app.Logger));

app.Run();

static string Normalize(string value) => value.Trim().ToLowerInvariant();

static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action, ILogger logger)
{
    try
    {
        return await action();
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "The database microservice request failed.");
        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
