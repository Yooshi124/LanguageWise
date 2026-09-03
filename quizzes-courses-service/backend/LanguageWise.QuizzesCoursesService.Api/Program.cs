using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using LanguageWise.QuizzesCoursesService.Api.Clients;
using LanguageWise.QuizzesCoursesService.Api.Models;
using LanguageWise.QuizzesCoursesService.Api.Options;
using LanguageWise.QuizzesCoursesService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

const string ServiceName = "quizzes-courses-service-backend";

var builder = WebApplication.CreateBuilder(args);
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6003";

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

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
builder.Services.AddHttpClient<IAssistantCompletionClient, OpenRouterAssistantClient>(
    (services, client) =>
    {
        var options = services.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = Timeout.InfiniteTimeSpan;
    });
builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();
builder.Services.AddScoped<IAssistantContextService, AssistantContextService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            await Results.Problem(
                title: "Too many assistant requests.",
                detail: "Please wait before sending another assistant message.",
                statusCode: StatusCodes.Status429TooManyRequests)
                .ExecuteAsync(context.HttpContext);
        }
    };
    options.AddPolicy("assistant-per-user", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

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
app.UseRateLimiter();

app.MapGet("/health", async (
    CatalogClient client,
    EndpointDataSource endpointDataSource,
    IOptions<AuthorizationOptions> authorizationOptions,
    CancellationToken cancellationToken) =>
{
    var endpoints = GetRegisteredEndpoints(
        endpointDataSource,
        authorizationOptions.Value.FallbackPolicy is not null);

    try
    {
        var response = await client.GetHealthAsync(cancellationToken);
        var databaseHealth = response.Value;
        var databaseIsHealthy =
            response.IsSuccess &&
            string.Equals(databaseHealth?.Status, "healthy", StringComparison.OrdinalIgnoreCase);

        if (!databaseIsHealthy)
        {
            app.Logger.LogWarning(
                "Database health check returned HTTP {HttpStatus} with status {DependencyStatus}. " +
                "Downstream diagnostic: {DownstreamDiagnostic}",
                (int)response.StatusCode,
                databaseHealth?.Status ?? "unknown",
                databaseHealth?.Error ?? response.ErrorBody ?? "No diagnostic was returned.");
        }

        var dependency = new DependencyHealth(
            databaseIsHealthy ? "healthy" : "unhealthy",
            databaseHealth?.Service,
            (int)response.StatusCode,
            databaseIsHealthy ? null : "The database service is unhealthy.",
            "http");
        var health = new ServiceHealth(
            databaseIsHealthy ? "healthy" : "unhealthy",
            ServiceName,
            new Dictionary<string, DependencyHealth> { ["database"] = dependency },
            endpoints);

        return databaseIsHealthy
            ? Results.Ok(health)
            : Results.Json(health, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "The database service health check failed.");
        var health = new ServiceHealth(
            "unhealthy",
            ServiceName,
            new Dictionary<string, DependencyHealth>
            {
                ["database"] = new(
                    "unhealthy",
                    Error: "The database service is unhealthy.",
                    Type: "http")
            },
            endpoints);

        return Results.Json(health, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
    .AllowAnonymous();

app.MapPost("/api/assistant/messages", async (
    AssistantMessageRequest request,
    AssistantRequestValidator validator,
    IAssistantContextService contextService,
    IAssistantPromptBuilder promptBuilder,
    IAssistantCompletionClient completionClient,
    IOptions<OpenRouterOptions> openRouterOptions,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var validation = validator.Validate(request);
    if (validation.Request is null)
    {
        return Results.ValidationProblem(
            validation.Errors.ToDictionary(error => error.Key, error => error.Value));
    }

    if (string.IsNullOrWhiteSpace(openRouterOptions.Value.ApiKey))
    {
        return Results.Problem(
            title: "The assistant is not configured.",
            detail: "The assistant service is temporarily unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    AssistantContextResult assistantContext;
    try
    {
        assistantContext = await contextService.GetContextAsync(
            validation.Request.Context,
            cancellationToken);
    }
    catch (HttpRequestException exception)
    {
        app.Logger.LogWarning(
            "Assistant catalog lookup failed with HTTP request error type {ErrorType}.",
            exception.GetType().Name);
        return Results.Problem(
            title: "The course catalog is unavailable.",
            detail: "The assistant could not load current LanguageWise content.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (JsonException exception)
    {
        app.Logger.LogWarning(
            "Assistant catalog lookup returned invalid data of error type {ErrorType}.",
            exception.GetType().Name);
        return Results.Problem(
            title: "The course catalog is unavailable.",
            detail: "The assistant could not load current LanguageWise content.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
    {
        app.Logger.LogWarning(
            "Assistant catalog lookup timed out with error type {ErrorType}.",
            exception.GetType().Name);
        return Results.Problem(
            title: "The course catalog is unavailable.",
            detail: "The assistant could not load current LanguageWise content.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    if (!assistantContext.IsFound)
    {
        return Results.Problem(
            title: "Assistant context was not found.",
            detail: assistantContext.NotFoundMessage,
            statusCode: StatusCodes.Status404NotFound);
    }

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
                title: "Garry is temporarily rate limited.",
                detail: "OpenRouter's free model is busy or its request allowance has been reached. Please wait and try again.",
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
})
    .RequireRateLimiting("assistant-per-user");

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

app.MapGet("/api/quizzes/{quizId:int}", async (
    int quizId,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var quiz = await client.GetQuizAsync(quizId, cancellationToken);
        return quiz is null ? Results.NotFound() : Results.Ok(quiz);
    }, app.Logger));

app.MapPost("/api/quizzes/{quizId:int}/attempts", async (
    int quizId,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
        ToResult(await client.StartQuizAttemptAsync(
            quizId,
            userId,
            cancellationToken)), app.Logger));

app.MapPost("/api/quiz-attempts/{attemptId:int}/submit", async (
    int attemptId,
    SubmitQuizAttemptRequest request,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
        ToResult(await client.SubmitQuizAttemptAsync(
            attemptId,
            userId,
            request,
            cancellationToken)), app.Logger));

app.MapGet("/api/courses/{code}/flashcard-decks", async (
    string code,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var decks = await client.GetFlashcardDecksAsync(Normalize(code), cancellationToken);
        return decks is null ? Results.NotFound() : Results.Ok(decks);
    }, app.Logger));

app.MapGet("/api/courses/{code}/flashcard-decks/{lessonSlug}", async (
    string code,
    string lessonSlug,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteAsync(async () =>
    {
        var deck = await client.GetFlashcardDeckAsync(
            Normalize(code),
            Normalize(lessonSlug),
            cancellationToken);
        return deck is null ? Results.NotFound() : Results.Ok(deck);
    }, app.Logger));

app.MapGet("/api/courses/{code}/progress", async (
    string code,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
    {
        var progress = await client.GetCourseProgressAsync(
            Normalize(code),
            userId,
            cancellationToken);
        return progress is null ? Results.NotFound() : Results.Ok(progress);
    }, app.Logger));

// Vocabulary the user has unlocked: every course they have started, limited to the
// lessons whose milestone they have achieved. Other services (e.g. mini-games) call
// this endpoint with the user's token instead of reaching into the database service.
app.MapGet("/api/me/vocabulary", async (
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
    {
        var startedCourses = await client.GetStartedCourseProgressAsync(userId, cancellationToken);
        var courses = new List<CourseVocabulary>();

        foreach (var course in startedCourses)
        {
            var lessons = new List<LessonVocabulary>();
            foreach (var lesson in course.Lessons.Where(lesson => lesson.Completed).OrderBy(lesson => lesson.SortOrder))
            {
                var detail = await client.GetLessonAsync(course.CourseCode, lesson.Slug, cancellationToken);
                if (detail?.Vocabulary is { Count: > 0 } vocabulary)
                {
                    lessons.Add(new LessonVocabulary(lesson.LessonId, lesson.Slug, lesson.Title, vocabulary));
                }
            }

            if (lessons.Count > 0)
            {
                courses.Add(new CourseVocabulary(course.CourseCode, course.CourseTitle, lessons));
            }
        }

        return Results.Ok(new UserVocabulary(courses));
    }, app.Logger));

app.MapPut("/api/lessons/{lessonId:int}/milestone", async (
    int lessonId,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
        ToResult(await client.SetLessonMilestoneAsync(
            lessonId,
            userId,
            completed: true,
            cancellationToken)), app.Logger));

app.MapDelete("/api/lessons/{lessonId:int}/milestone", async (
    int lessonId,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
        ToResult(await client.SetLessonMilestoneAsync(
            lessonId,
            userId,
            completed: false,
            cancellationToken)), app.Logger));

app.MapPut("/api/courses/{code}/milestone", async (
    string code,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
        ToResult(await client.SetCourseMilestoneAsync(
            Normalize(code),
            userId,
            completed: true,
            cancellationToken)), app.Logger));

app.MapDelete("/api/courses/{code}/milestone", async (
    string code,
    HttpContext context,
    CatalogClient client,
    CancellationToken cancellationToken) =>
    await ExecuteForUserAsync(context, async userId =>
        ToResult(await client.SetCourseMilestoneAsync(
            Normalize(code),
            userId,
            completed: false,
            cancellationToken)), app.Logger));

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

static Task<IResult> ExecuteForUserAsync(
    HttpContext context,
    Func<int, Task<IResult>> action,
    ILogger logger)
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    return int.TryParse(subject, out var userId) && userId > 0
        ? ExecuteAsync(() => action(userId), logger)
        : Task.FromResult(Results.Unauthorized());
}

static IResult ToResult<T>(DatabaseResponse<T> response)
{
    if (response.StatusCode == HttpStatusCode.NoContent)
    {
        return Results.NoContent();
    }

    if (response.IsSuccess)
    {
        return Results.Ok(response.Value);
    }

    return Results.Content(
        response.ErrorBody ?? string.Empty,
        response.ContentType ?? "application/problem+json",
        statusCode: (int)response.StatusCode);
}

static IReadOnlyList<RegisteredEndpoint> GetRegisteredEndpoints(
    EndpointDataSource endpointDataSource,
    bool fallbackPolicyRequiresAuthentication) =>
    endpointDataSource.Endpoints
        .OfType<RouteEndpoint>()
        .SelectMany(endpoint =>
        {
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["*"];
            var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var hasAuthorizationMetadata = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;
            var authRequired =
                !allowsAnonymous &&
                (hasAuthorizationMetadata || fallbackPolicyRequiresAuthentication);
            var route = endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString() ?? string.Empty;

            return methods.Select(method =>
                new RegisteredEndpoint(method, route, "registered", authRequired));
        })
        .OrderBy(endpoint => endpoint.Route, StringComparer.Ordinal)
        .ThenBy(endpoint => endpoint.Method, StringComparer.Ordinal)
        .ToArray();

internal sealed record ServiceHealth(
    string Status,
    string Service,
    IReadOnlyDictionary<string, DependencyHealth> Dependencies,
    IReadOnlyList<RegisteredEndpoint> Endpoints);

internal sealed record DependencyHealth(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Service = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? HttpStatus = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Error = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Type = null);

internal sealed record RegisteredEndpoint(
    string Method,
    string Route,
    string Status,
    bool AuthRequired);
