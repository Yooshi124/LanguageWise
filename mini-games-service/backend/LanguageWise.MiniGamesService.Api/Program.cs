using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using LanguageWise.MiniGamesService.Api.Clients;
using LanguageWise.MiniGamesService.Api.Feature.Associations;
using LanguageWise.MiniGamesService.Api.Feature.GuessTheWord;
using LanguageWise.MiniGamesService.Api.Feature.Vocabulary;
using LanguageWise.MiniGamesService.Api.Feature.WordSearch;
using LanguageWise.MiniGamesService.Api.Models;
using LanguageWise.MiniGamesService.Api.Options;
using LanguageWise.MiniGamesService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

const string ServiceName = "mini-games-service-backend";
const string AssistantToolNamePattern = "^games_[a-z_]{1,58}$";
const int MaxAssistantToolArgumentBytes = 2048;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

// Database and external service URLs
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6005";
var courseServiceUrl = builder.Configuration["Services:Courses"] ?? "http://localhost:6003";
var achievementsServiceUrl = builder.Configuration["Services:Achievements"] ?? "http://localhost:5004";

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

builder.Services.AddHttpClient<AchievementEventsClient>(client =>
{
    client.BaseAddress = new Uri($"{achievementsServiceUrl}/");
    client.Timeout = TimeSpan.FromSeconds(20);
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

// Mini games assistant (streaming chat), modelled on the quizzes-courses assistant setup.
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(McpToolClient.HttpClientName, client =>
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Mcp:TimeoutSeconds", 15)));
builder.Services.AddSingleton<IMcpToolClient, McpToolClient>();
builder.Services.AddHttpClient<IAssistantCompletionClient, GarryCompletionClient>(client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Services:Garry"] ?? "http://localhost:5010").TrimEnd('/') + "/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();
builder.Services.AddSingleton<IAssistantContextService, AssistantContextService>();

// Register vocabulary providers and supporting services
builder.Services.AddSingleton<IVocabularyProvider>(serviceProvider =>
    new CourseVocabularyProvider(
        serviceProvider.GetRequiredService<CourseVocabularyClient>(),
        serviceProvider.GetRequiredService<ILogger<CourseVocabularyProvider>>()));

// Register the game session manager to handle concurrent games
builder.Services.AddSingleton<GameSessionManager>();

var verificationKeyPath = builder.Configuration["Auth:VerificationKeyPath"] ?? "/run/secrets/signing_public_key";
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(verificationKeyPath));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            NameClaimType = JwtRegisteredClaimNames.Name,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    context.Token = context.Request.Cookies["token"];
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(context =>
            int.TryParse(
                context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                out var userId) && userId > 0)
        .Build());

// Per-user limit on gameplay actions (guesses and hints), so scripted submission cannot brute
// force a solution or exhaust hints faster than a human could play.
const string GameActionsRateLimiterPolicy = "game-actions";
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };
    options.AddPolicy(GameActionsRateLimiterPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetUserId(context).ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        context.Request.Query.ContainsKey("userId"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "userId is derived from the authenticated session and must not be supplied"
        });
        return;
    }

    await next(context);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }))
    .AllowAnonymous();

// Streaming mini games assistant. The backend owns the prompt, the canonical game rules
// context, and the provider credentials; the browser only renders the SSE stream.
app.MapPost("/api/assistant/messages", async (
    AssistantMessageRequest request,
    AssistantRequestValidator validator,
    IAssistantContextService contextService,
    IAssistantPromptBuilder promptBuilder,
    IAssistantCompletionClient completionClient,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var validation = validator.Validate(request);
    if (validation.Request is null)
    {
        return Results.ValidationProblem(
            validation.Errors.ToDictionary(error => error.Key, error => error.Value));
    }

    var assistantContext = contextService.GetContext(validation.Request.Context);

    try
    {
        var messages = promptBuilder.BuildMessages(
            validation.Request,
            assistantContext.CanonicalContext!);
        var completion = await completionClient.StartCompletionAsync(messages, cancellationToken);
        return new AssistantSseResult(
            completion,
            loggerFactory.CreateLogger<AssistantSseResult>());
    }
    catch (AssistantProviderException exception)
    {
        app.Logger.LogWarning(
            "Assistant provider rejected a request with HTTP status {HttpStatus}.",
            (int)exception.StatusCode);
        if (exception.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return Results.Problem(
                title: "The assistant is temporarily rate limited.",
                detail: "Please wait before sending another assistant message.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        return Results.Problem(
            title: "The assistant provider is unavailable.",
            detail: "The assistant could not start a response. Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (HttpRequestException exception)
    {
        app.Logger.LogWarning(
            "Assistant provider request failed with error type {ErrorType}.",
            exception.GetType().Name);
        return Results.Problem(
            title: "The assistant provider is unavailable.",
            detail: "The assistant could not start a response. Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
});

// Lists the MCP tools available to Garry for this feature (scoped to "games"), so the
// frontend can offer them as quick actions. Mirrors the quizzes-courses-service contract.
app.MapGet("/api/assistant/tools", async (
    IMcpToolClient mcpTools,
    HttpRequest httpRequest,
    CancellationToken cancellationToken) =>
{
    if (!mcpTools.Enabled)
    {
        return AssistantToolsDisabled();
    }

    try
    {
        var tools = await mcpTools.ListToolsAsync(UserTokenReader.Read(httpRequest), cancellationToken);
        return Results.Ok(new AssistantToolsResponse(tools
            .Select(tool => new AssistantToolDescriptor(tool.Name, tool.Title, tool.Description))
            .ToList()));
    }
    catch (Exception exception) when (IsAssistantToolFailure(exception, cancellationToken))
    {
        app.Logger.LogWarning(
            "Assistant tool listing failed with error type {ErrorType}.",
            exception.GetType().Name);
        return AssistantToolsUnavailable();
    }
});

// Calls one MCP tool directly (not through a chat turn), forwarding the caller's own token so
// the tool can never see more than the signed-in user is allowed to.
app.MapPost("/api/assistant/tools/{name}", async (
    string name,
    HttpRequest httpRequest,
    IMcpToolClient mcpTools,
    CancellationToken cancellationToken) =>
{
    if (!mcpTools.Enabled)
    {
        return AssistantToolsDisabled();
    }

    if (!Regex.IsMatch(name, AssistantToolNamePattern))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = ["Unknown assistant tool."]
        });
    }

    var arguments = await ReadAssistantToolArgumentsAsync(httpRequest, cancellationToken);
    if (arguments is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["arguments"] = [$"Tool arguments must be a JSON object of at most {MaxAssistantToolArgumentBytes} bytes."]
        });
    }

    try
    {
        var result = await mcpTools.CallToolAsync(
            name,
            arguments,
            UserTokenReader.Read(httpRequest),
            cancellationToken);
        return Results.Ok(new AssistantToolCallResponse(name, result.IsError, result.Result));
    }
    catch (ModelContextProtocol.McpProtocolException exception) when (
        exception.ErrorCode is ModelContextProtocol.McpErrorCode.InvalidParams or ModelContextProtocol.McpErrorCode.MethodNotFound)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = ["Unknown assistant tool or invalid arguments."]
        });
    }
    catch (Exception exception) when (IsAssistantToolFailure(exception, cancellationToken))
    {
        app.Logger.LogWarning(
            "Assistant tool call failed with error type {ErrorType}.",
            exception.GetType().Name);
        return AssistantToolsUnavailable();
    }
});

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
app.MapGet("/api/stats/completions", async (HttpContext context, string? courseCode, GamesDatabaseClient databaseClient, CancellationToken cancellationToken) =>
{
    var userId = GetUserId(context);

    var games = await databaseClient.GetGamesForUserAsync(userId, cancellationToken);
    var attempts = await databaseClient.GetGameAttemptsByUserIdAsync(userId, cancellationToken);

    var gameTypesById = games
        .Where(game => courseCode is null || string.Equals(game.CourseCode, courseCode, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(game => game.Id, game => game.GameType);

    var scopedAttempts = attempts.Where(attempt => gameTypesById.ContainsKey(attempt.GameId)).ToList();

    var counts = scopedAttempts
        .Where(attempt => attempt.IsWon)
        .GroupBy(attempt => gameTypesById[attempt.GameId])
        .ToDictionary(group => group.Key, group => group.Count());

    var bestTimes = scopedAttempts
        .Where(attempt => attempt.IsWon && attempt.TimeSpentSeconds > 0)
        .GroupBy(attempt => gameTypesById[attempt.GameId])
        .ToDictionary(group => group.Key, group => group.Min(attempt => attempt.TimeSpentSeconds));

    var streak = StreakCalculator.CalculateStreak(scopedAttempts.Where(attempt => attempt.IsComplete));

    return Results.Ok(new CompletionStatsResponse(
        courseCode,
        counts.GetValueOrDefault("guess_the_word"),
        counts.GetValueOrDefault("word_search"),
        counts.GetValueOrDefault("associations"),
        bestTimes.GetValueOrDefault("guess_the_word") is int guessBest and > 0 ? guessBest : null,
        bestTimes.GetValueOrDefault("word_search") is int searchBest and > 0 ? searchBest : null,
        bestTimes.GetValueOrDefault("associations") is int associationsBest and > 0 ? associationsBest : null,
        streak));
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
app.MapGet("/api/guess-the-word", (HttpContext context, GameSessionManager gameManager) =>
{
    var state = gameManager.GetGuessTheWordGameState(GetUserId(context));
    return state is not null ? Results.Ok(state) : Results.NotFound(new { error = "No active game. Use POST /api/guess-the-word/init to start one" });
});

app.MapPost("/api/guess-the-word/init", async (HttpContext context, GameSessionManager gameManager, CourseVocabularyClient courseClient, string? courseCode, string? mode, string? language, CancellationToken cancellationToken) =>
{
    var resolvedMode = ResolveMode(mode, await IsContentAvailableAsync(courseClient, context, cancellationToken));
    return await InitializeGameAsync(() => gameManager.StartGuessTheWordGameAsync(GetUserId(context), courseCode, GetAccessToken(context), resolvedMode, language));
});

app.MapPost("/api/guess-the-word/guess", async (HttpContext context, GuessTheWordGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.SubmitGuessTheWordGuessAsync(
            GetUserId(context),
            context.User.Identity!.Name!,
            GetAccessToken(context)!,
            request.Guess);
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
}).RequireRateLimiting(GameActionsRateLimiterPolicy);

app.MapPost("/api/guess-the-word/reset", (HttpContext context, GameSessionManager gameManager) =>
{
    gameManager.ResetGuessTheWordGame(GetUserId(context));
    return Results.NoContent();
});

// Word Search endpoints
app.MapGet("/api/word-search", (HttpContext context, GameSessionManager gameManager) =>
{
    var state = gameManager.GetWordSearchGameState(GetUserId(context));
    return state is not null ? Results.Ok(state) : Results.NotFound(new { error = "No active game. Use POST /api/word-search/init to start one" });
});

app.MapPost("/api/word-search/init", async (HttpContext context, GameSessionManager gameManager, CourseVocabularyClient courseClient, string? courseCode, string? mode, string? language, CancellationToken cancellationToken) =>
{
    var resolvedMode = ResolveMode(mode, await IsContentAvailableAsync(courseClient, context, cancellationToken));
    return await InitializeGameAsync(() => gameManager.StartWordSearchGameAsync(GetUserId(context), courseCode, GetAccessToken(context), resolvedMode, language));
});

app.MapPost("/api/word-search/guess", async (HttpContext context, WordSearchGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.SubmitWordSearchWordAsync(
            GetUserId(context),
            context.User.Identity!.Name!,
            GetAccessToken(context)!,
            request.Word,
            request.Indices);
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
}).RequireRateLimiting(GameActionsRateLimiterPolicy);

app.MapPost("/api/word-search/hint", (HttpContext context, GameSessionManager gameManager) =>
{
    try
    {
        var result = gameManager.UseWordSearchHint(GetUserId(context));
        return Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
}).RequireRateLimiting(GameActionsRateLimiterPolicy);

app.MapPost("/api/word-search/give-up", async (HttpContext context, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.GiveUpWordSearchAsync(GetUserId(context));
        return Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

app.MapPost("/api/word-search/reset", (HttpContext context, GameSessionManager gameManager) =>
{
    gameManager.ResetWordSearchGame(GetUserId(context));
    return Results.NoContent();
});

// Associations endpoints
app.MapGet("/api/associations", (HttpContext context, GameSessionManager gameManager) =>
{
    var state = gameManager.GetAssociationsGameState(GetUserId(context));
    return state is not null ? Results.Ok(state) : Results.NotFound(new { error = "No active game. Use POST /api/associations/init to start one" });
});

app.MapPost("/api/associations/init", async (HttpContext context, GameSessionManager gameManager, CourseVocabularyClient courseClient, string? courseCode, string? mode, string? language, CancellationToken cancellationToken) =>
{
    var resolvedMode = ResolveMode(mode, await IsContentAvailableAsync(courseClient, context, cancellationToken));
    return await InitializeGameAsync(() => gameManager.StartAssociationsGameAsync(GetUserId(context), courseCode, GetAccessToken(context), resolvedMode, language));
});

app.MapPost("/api/associations/guess", async (HttpContext context, AssociationsGuessRequest request, GameSessionManager gameManager) =>
{
    try
    {
        var result = await gameManager.SubmitAssociationsGuessAsync(
            GetUserId(context),
            context.User.Identity!.Name!,
            GetAccessToken(context)!,
            request.Words);
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
}).RequireRateLimiting(GameActionsRateLimiterPolicy);

app.MapPost("/api/associations/reset", (HttpContext context, GameSessionManager gameManager) =>
{
    gameManager.ResetAssociationsGame(GetUserId(context));
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

static int GetUserId(HttpContext context) =>
    int.Parse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

static IResult AssistantToolsDisabled() =>
    Results.Problem(
        title: "Assistant tools are disabled.",
        detail: "Garry's tools are not enabled on this server.",
        statusCode: StatusCodes.Status503ServiceUnavailable,
        extensions: new Dictionary<string, object?> { ["code"] = "mcp_disabled" });

static IResult AssistantToolsUnavailable() =>
    Results.Problem(
        title: "Assistant tools are unavailable.",
        detail: "Garry's tools could not be reached. Please try again.",
        statusCode: StatusCodes.Status502BadGateway,
        extensions: new Dictionary<string, object?> { ["code"] = "mcp_unavailable" });

static bool IsAssistantToolFailure(Exception exception, CancellationToken cancellationToken) =>
    exception is HttpRequestException or ModelContextProtocol.McpException or TimeoutException or IOException or JsonException
    || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);

static async Task<IReadOnlyDictionary<string, JsonElement>?> ReadAssistantToolArgumentsAsync(
    HttpRequest request,
    CancellationToken cancellationToken)
{
    var buffer = new byte[MaxAssistantToolArgumentBytes + 1];
    var length = 0;
    int read;
    while (length < buffer.Length &&
        (read = await request.Body.ReadAsync(buffer.AsMemory(length), cancellationToken)) > 0)
    {
        length += read;
    }

    if (length > MaxAssistantToolArgumentBytes)
    {
        return null;
    }

    if (length == 0 || buffer.AsSpan(0, length).Trim(" \t\r\n"u8).IsEmpty)
    {
        return new Dictionary<string, JsonElement>();
    }

    try
    {
        using var document = JsonDocument.Parse(buffer.AsMemory(0, length));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.Clone());
    }
    catch (JsonException)
    {
        return null;
    }
}

public sealed class MiniGamesApiAssemblyMarker;
