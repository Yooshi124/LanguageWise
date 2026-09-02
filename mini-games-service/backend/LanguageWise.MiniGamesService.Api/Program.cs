using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;
using LanguageWise.MiniGamesService.Api.Models;
using LanguageWise.MiniGamesService.Api.Options;
using LanguageWise.MiniGamesService.Api.Services;
using Microsoft.Extensions.Options;

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

// OpenRouter vocabulary generation (AI game mode), modelled on the quizzes-courses assistant setup.
builder.Services
    .AddOptions<OpenRouterOptions>()
    .Bind(builder.Configuration.GetSection(OpenRouterOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
        "OpenRouter:BaseUrl must be an absolute URL.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Model),
        "OpenRouter:Model is required.")
    .Validate(
        options => options.MaxOutputTokens is > 0 and <= 8192,
        "OpenRouter:MaxOutputTokens must be between 1 and 8192.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IVocabularyCompletionClient, OpenRouterVocabularyClient>(
    (services, client) =>
    {
        var options = services.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(60);
    });
builder.Services.AddSingleton<IAiVocabularyProvider, OpenRouterVocabularyProvider>();

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

// Which vocabulary modes are usable right now: content focus needs the courses service and
// unlocked vocabulary; AI generation needs a configured OpenRouter key. The frontend uses this
// to lock the toggle onto AI generation when the service runs standalone.
app.MapGet("/api/game-modes", async (HttpContext context, CourseVocabularyClient courseClient, IOptions<OpenRouterOptions> openRouter, CancellationToken cancellationToken) =>
{
    var vocabulary = await courseClient.GetUserVocabularyAsync(GetAccessToken(context), cancellationToken);
    var contentLanguages = vocabulary?.Courses
        .Select(course => new { code = course.Code, title = course.Title })
        .ToArray() ?? [];
    var contentAvailable = contentLanguages.Length > 0;
    var aiAvailable = !string.IsNullOrWhiteSpace(openRouter.Value.ApiKey);

    return Results.Ok(new
    {
        contentAvailable,
        aiAvailable,
        defaultMode = contentAvailable ? GameModes.Content : GameModes.Ai,
        contentLanguages,
        aiLanguages = SupportedLanguages.All
            .Select(language => new { code = language.Code, title = language.Title })
            .ToArray()
    });
});

// Successful completions per game type for the user, optionally scoped to one course
// (the language selected on the game page). Powers the completion tracker on the frontend.
app.MapGet("/api/stats/completions", async (int? userId, string? courseCode, GamesDatabaseClient databaseClient, CancellationToken cancellationToken) =>
{
    if (userId is null)
    {
        return Results.BadRequest(new { error = "userId query parameter is required" });
    }

    var games = await databaseClient.GetGamesByUserIdAsync(userId.Value, cancellationToken);
    var attempts = await databaseClient.GetGameAttemptsByUserIdAsync(userId.Value, cancellationToken);

    var gameTypesById = games
        .Where(game => courseCode is null || string.Equals(game.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(game => game.Id, game => game.GameType);

    var counts = attempts
        .Where(attempt => attempt.IsWon && gameTypesById.ContainsKey(attempt.GameId))
        .GroupBy(attempt => gameTypesById[attempt.GameId])
        .ToDictionary(group => group.Key, group => group.Count());

    return Results.Ok(new CompletionStatsResponse(
        courseCode,
        counts.GetValueOrDefault("guess_the_word"),
        counts.GetValueOrDefault("word_search"),
        counts.GetValueOrDefault("associations")));
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
    catch (AiVocabularyUnavailableException exception)
    {
        return Results.Json(new { code = "AI_UNAVAILABLE", error = exception.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
}

/// <summary>Resolve the requested vocabulary mode; unknown values fall back to the default.</summary>
static string ResolveMode(string? mode, bool contentAvailable) =>
    GameModes.IsValid(mode)
        ? mode!.ToLowerInvariant()
        : contentAvailable ? GameModes.Content : GameModes.Ai;

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

app.MapPost("/api/guess-the-word/init", async (int userId, HttpContext context, GameSessionManager gameManager, CourseVocabularyClient courseClient, string? courseCode, string? mode, string? language, CancellationToken cancellationToken) =>
{
    var resolvedMode = ResolveMode(mode, await IsContentAvailableAsync(courseClient, context, cancellationToken));
    return await InitializeGameAsync(() => gameManager.StartGuessTheWordGameAsync(userId, courseCode, GetAccessToken(context), resolvedMode, language));
});

app.MapPost("/api/guess-the-word/guess", async (int userId, GuessTheWordGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.SubmitGuessTheWordGuessAsync(userId, request.Guess);
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

app.MapPost("/api/word-search/init", async (int userId, HttpContext context, GameSessionManager gameManager, CourseVocabularyClient courseClient, string? courseCode, string? mode, string? language, CancellationToken cancellationToken) =>
{
    var resolvedMode = ResolveMode(mode, await IsContentAvailableAsync(courseClient, context, cancellationToken));
    return await InitializeGameAsync(() => gameManager.StartWordSearchGameAsync(userId, courseCode, GetAccessToken(context), resolvedMode, language));
});

app.MapPost("/api/word-search/guess", async (int userId, WordSearchGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.SubmitWordSearchWordAsync(userId, request.Word, request.Indices);
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

app.MapPost("/api/word-search/give-up", async (int userId, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.GiveUpWordSearchAsync(userId);
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

app.MapPost("/api/associations/init", async (int userId, HttpContext context, GameSessionManager gameManager, CourseVocabularyClient courseClient, string? courseCode, string? mode, string? language, CancellationToken cancellationToken) =>
{
    var resolvedMode = ResolveMode(mode, await IsContentAvailableAsync(courseClient, context, cancellationToken));
    return await InitializeGameAsync(() => gameManager.StartAssociationsGameAsync(userId, courseCode, GetAccessToken(context), resolvedMode, language));
});

app.MapPost("/api/associations/guess", async (int userId, AssociationsGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.SubmitAssociationsGuessAsync(userId, request.Words);
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

/// <summary>True when the user has unlocked course vocabulary to play content mode with.</summary>
static async Task<bool> IsContentAvailableAsync(CourseVocabularyClient courseClient, HttpContext context, CancellationToken cancellationToken)
{
    var vocabulary = await courseClient.GetUserVocabularyAsync(GetAccessToken(context), cancellationToken);
    return vocabulary is not null && vocabulary.Courses.Count > 0;
}

// The user's JWT arrives from the shared login either as a bearer header or as the
// "token" cookie, and is forwarded to the quizzes-courses API to scope vocabulary.
static string? GetAccessToken(HttpContext context)
{
    var header = context.Request.Headers.Authorization.ToString();
    return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? header["Bearer ".Length..].Trim()
        : context.Request.Cookies["token"];
}
