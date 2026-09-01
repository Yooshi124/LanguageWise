using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;
using LanguageWise.MiniGamesService.Api.Services;

const string ServiceName = "mini-games-service-backend";

var builder = WebApplication.CreateBuilder(args);

// Database and external service URLs
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6005";
var courseServiceUrl = builder.Configuration["Services:Courses"] ?? "http://localhost:6003";

// Register HTTP clients for external services
builder.Services.AddHttpClient<GamesDatabaseClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<CourseVocabularyClient>(client =>
{
    client.BaseAddress = new Uri(courseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Register vocabulary providers and supporting services
builder.Services.AddSingleton<IVocabularyProvider>(serviceProvider =>
    new CourseVocabularyProvider(
        serviceProvider.GetRequiredService<CourseVocabularyClient>(),
        serviceProvider.GetRequiredService<ILogger<CourseVocabularyProvider>>()));

// Register the game session manager to handle concurrent games
builder.Services.AddSingleton<GameSessionManager>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

// Languages the user has unlocked vocabulary in (started courses with completed lessons).
// The frontend offers these as the per-user language selection for the games.
app.MapGet("/api/game-languages", async (HttpContext context, CourseVocabularyClient courseClient, CancellationToken cancellationToken) =>
{
    var vocabulary = await courseClient.GetUserVocabularyAsync(GetAccessToken(context), cancellationToken);
    var languages = vocabulary?.Courses
        .Select(course => new { code = course.Code, title = course.Title })
        .ToArray() ?? [];
    return Results.Ok(languages);
});

// Starts a game, translating "no vocabulary yet" into a 422 the frontend can show a friendly message for.
async Task<IResult> InitializeGameAsync<TState>(Func<Task<TState>> startGame)
{
    try
    {
        return Results.Ok(await startGame());
    }
    catch (NoVocabularyAvailableException exception)
    {
        return Results.UnprocessableEntity(new { code = "NO_VOCABULARY", error = exception.Message });
    }
    catch (Exception exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}

// Guess the Word endpoints
app.MapGet("/api/guess-the-word", (int? userId, GameSessionManager gameManager) =>
{
    if (userId is null)
    {
        return Results.BadRequest(new { error = "userId query parameter is required" });
    }
    var state = gameManager.GetGuessTheWordGameState(userId.Value);
    return state is not null ? Results.Ok(state) : Results.NotFound(new { error = "No active game. Use POST /api/guess-the-word/init to start one" });
});

app.MapPost("/api/guess-the-word/init", async (int userId, HttpContext context, GameSessionManager gameManager, string? courseCode) =>
    await InitializeGameAsync(() => gameManager.StartGuessTheWordGameAsync(userId, courseCode, GetAccessToken(context))));

app.MapPost("/api/guess-the-word/guess", (int userId, GuessTheWordGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = gameManager.SubmitGuessTheWordGuess(userId, request.Guess);
        return Results.Ok(result);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["guess"] = [exception.Message]
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/guess-the-word/reset", (int userId, GameSessionManager gameManager) =>
{
    gameManager.ResetGuessTheWordGame(userId);
    return Results.NoContent();
});

// Word Search endpoints
app.MapGet("/api/word-search", (int? userId, GameSessionManager gameManager) =>
{
    if (userId is null)
    {
        return Results.BadRequest(new { error = "userId query parameter is required" });
    }
    var state = gameManager.GetWordSearchGameState(userId.Value);
    return state is not null ? Results.Ok(state) : Results.NotFound(new { error = "No active game. Use POST /api/word-search/init to start one" });
});

app.MapPost("/api/word-search/init", async (int userId, HttpContext context, GameSessionManager gameManager, string? courseCode) =>
    await InitializeGameAsync(() => gameManager.StartWordSearchGameAsync(userId, courseCode, GetAccessToken(context))));

app.MapPost("/api/word-search/guess", (int userId, WordSearchGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = gameManager.SubmitWordSearchWord(userId, request.Word, request.Indices);
        return Results.Ok(result);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["word"] = [exception.Message] });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/word-search/hint", (int userId, GameSessionManager gameManager) =>
{
    try
    {
        var result = gameManager.UseWordSearchHint(userId);
        return Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/word-search/give-up", (int userId, GameSessionManager gameManager) =>
{
    try
    {
        var result = gameManager.GiveUpWordSearch(userId);
        return Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/word-search/reset", (int userId, GameSessionManager gameManager) =>
{
    gameManager.ResetWordSearchGame(userId);
    return Results.NoContent();
});

// Associations endpoints
app.MapGet("/api/associations", (int? userId, GameSessionManager gameManager) =>
{
    if (userId is null)
    {
        return Results.BadRequest(new { error = "userId query parameter is required" });
    }
    var state = gameManager.GetAssociationsGameState(userId.Value);
    return state is not null ? Results.Ok(state) : Results.NotFound(new { error = "No active game. Use POST /api/associations/init to start one" });
});

app.MapPost("/api/associations/init", async (int userId, HttpContext context, GameSessionManager gameManager, string? courseCode) =>
    await InitializeGameAsync(() => gameManager.StartAssociationsGameAsync(userId, courseCode, GetAccessToken(context))));

app.MapPost("/api/associations/guess", (int userId, AssociationsGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = gameManager.SubmitAssociationsGuess(userId, request.Words);
        return Results.Ok(result);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["words"] = [exception.Message] });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/associations/reset", (int userId, GameSessionManager gameManager) =>
{
    gameManager.ResetAssociationsGame(userId);
    return Results.NoContent();
});

app.Run();

// The user's JWT arrives from the shared login either as a bearer header or as the
// "token" cookie, and is forwarded to the quizzes-courses API to scope vocabulary.
static string? GetAccessToken(HttpContext context)
{
    var header = context.Request.Headers.Authorization.ToString();
    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? header["Bearer ".Length..].Trim()
        : context.Request.Cookies["token"];
}
