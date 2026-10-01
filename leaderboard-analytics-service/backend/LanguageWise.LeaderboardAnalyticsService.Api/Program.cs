using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using LanguageWise.LeaderboardAnalyticsService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var quizzesCoursesServiceUrl = builder.Configuration["Services:QuizzesCourses"] ?? "http://localhost:5003";

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<QuizzesCoursesClient>(client =>
{
    client.BaseAddress = new Uri(quizzesCoursesServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<AnalyticsProfileService>();

var ollamaServiceUrl = builder.Configuration["Services:Ollama"] ?? "http://localhost:11434";
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<ISummaryGenerator, OllamaSummaryGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IAssistantCompletionClient, GarryCompletionClient>(client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Services:Garry"] ?? "http://localhost:5010").TrimEnd('/') + "/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();

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
        .Build());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok())
    .AllowAnonymous();

// ---------------------------------------------------------------------------
// Language Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/my-language-rankings", async (
    HttpContext context,
    QuizzesCoursesClient client,
    CancellationToken cancellationToken) =>
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(subject, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!TryGetIncomingBearerToken(context, out var token))
    {
        return Results.Unauthorized();
    }

    var myMilestonesTask = client.GetAllMyMilestonesAsync(token, cancellationToken);
    var allMilestonesTask = client.GetAllMilestonesAsync(token, cancellationToken);
    var coursesTask = client.GetCoursesAsync(token, cancellationToken);
    var lessonMapTask = client.GetLessonToCourseMapAsync(token, cancellationToken);
    await Task.WhenAll(myMilestonesTask, allMilestonesTask, coursesTask, lessonMapTask);

    var rankings = AnalyticsProjector.BuildLanguageRankings(
        userId,
        myMilestonesTask.Result,
        allMilestonesTask.Result,
        coursesTask.Result,
        lessonMapTask.Result);
    return Results.Ok(rankings);
});

// ---------------------------------------------------------------------------
// Lessons Completed Analytics
// ---------------------------------------------------------------------------

app.MapGet("/api/lessons-completed-over-time", async (
    HttpContext context,
    QuizzesCoursesClient client,
    CancellationToken cancellationToken) =>
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(subject, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!TryGetIncomingBearerToken(context, out var token))
    {
        return Results.Unauthorized();
    }

    var myMilestonesTask = client.GetAllMyMilestonesAsync(token, cancellationToken);
    var coursesTask = client.GetCoursesAsync(token, cancellationToken);
    var lessonMapTask = client.GetLessonToCourseMapAsync(token, cancellationToken);
    await Task.WhenAll(myMilestonesTask, coursesTask, lessonMapTask);

    var response = AnalyticsProjector.BuildLessonsCompleted(
        userId,
        myMilestonesTask.Result,
        coursesTask.Result,
        lessonMapTask.Result,
        DateOnly.FromDateTime(DateTime.UtcNow));
    return Results.Ok(response);
});

app.MapPost("/api/lessons-completed-summary", async (
    HttpContext context,
    QuizzesCoursesClient client,
    ISummaryGenerator generator,
    CancellationToken cancellationToken) =>
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(subject, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!TryGetIncomingBearerToken(context, out var token))
    {
        return Results.Unauthorized();
    }

    var myMilestonesTask = client.GetAllMyMilestonesAsync(token, cancellationToken);
    var coursesTask = client.GetCoursesAsync(token, cancellationToken);
    var lessonMapTask = client.GetLessonToCourseMapAsync(token, cancellationToken);
    await Task.WhenAll(myMilestonesTask, coursesTask, lessonMapTask);

    var chartData = AnalyticsProjector.BuildLessonsCompleted(
        userId,
        myMilestonesTask.Result,
        coursesTask.Result,
        lessonMapTask.Result,
        DateOnly.FromDateTime(DateTime.UtcNow));
    var summary = await generator.GenerateAsync(chartData, cancellationToken);
    return Results.Ok(summary);
});

// ---------------------------------------------------------------------------
// Garry Assistant
// ---------------------------------------------------------------------------

app.MapPost("/api/assistant/messages", async (
    AssistantMessageRequest? request,
    HttpContext context,
    AssistantRequestValidator validator,
    AnalyticsProfileService profileService,
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

    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(subject, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!TryGetIncomingBearerToken(context, out var token))
    {
        return Results.Unauthorized();
    }

    AnalyticsProfile profile;
    try
    {
        profile = await profileService.GetAsync(
            userId,
            context.User.Identity?.Name ?? string.Empty,
            token,
            cancellationToken);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        app.Logger.LogError(exception, "Failed to load assistant profile for user {UserId}.", userId);
        return Results.Problem(
            title: "The analytics profile is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        var messages = promptBuilder.BuildMessages(validation.Request, profile);
        var completion = await completionClient.StartCompletionAsync(messages, cancellationToken);
        return new AssistantSseResult(
            completion,
            loggerFactory.CreateLogger<AssistantSseResult>());
    }
    catch (AssistantProviderException exception)
    {
        app.Logger.LogWarning(
            "Garry could not start the request; HTTP status was {HttpStatus}.",
            (int)exception.StatusCode);
        if (exception.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            return Results.Problem(
                title: "Too many assistant requests.",
                detail: "Please wait before sending another question.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }
        return Results.Problem(
            title: "Garry is unavailable.",
            detail: "The assistant could not start a response. Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (Exception exception) when (
        exception is HttpRequestException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
    {
        app.Logger.LogWarning(
            "All assistant providers were unreachable with error type {ErrorType}.",
            exception.GetType().Name);
        return Results.Problem(
            title: "Garry is unavailable.",
            detail: "The assistant could not start a response. Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

static bool TryGetIncomingBearerToken(HttpContext context, out string token)
{
    var header = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(header) && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        token = header["Bearer ".Length..].Trim();
        if (token.Length > 0)
        {
            return true;
        }
    }

    var cookie = context.Request.Cookies["token"];
    if (!string.IsNullOrWhiteSpace(cookie))
    {
        token = cookie;
        return true;
    }

    token = string.Empty;
    return false;
}

