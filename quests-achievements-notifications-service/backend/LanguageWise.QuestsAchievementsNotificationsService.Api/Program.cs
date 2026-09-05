using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using LanguageWise.QuestsAchievementsNotificationsService.Api;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "quests-achievements-notifications-service-backend";

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6004";

builder.Services.AddHttpClient<AppDataClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ProfileService>();

var ollamaServiceUrl = builder.Configuration["Services:Ollama"] ?? "http://localhost:11434";
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddHttpClient<IEmailContentGenerator, OllamaEmailGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.Configure<OpenRouterOptions>(
    builder.Configuration.GetSection(OpenRouterOptions.SectionName));
builder.Services.AddHttpClient<OpenRouterAssistantClient>(client =>
{
    client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<OllamaAssistantClient>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddTransient<IAssistantCompletionClient, FallbackAssistantCompletionClient>();
builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
builder.Services.AddSingleton<ISmtpTransport, MailKitSmtpTransport>();
builder.Services.AddSingleton<IEmailSender, GmailEmailSender>();

var signingKeyPath = builder.Configuration["Auth:VerificationKeyPath"] ?? "/run/secrets/signing_public_key";
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(signingKeyPath));

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
            NameClaimType = JwtRegisteredClaimNames.Name
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }))
    .AllowAnonymous();

app.MapGet("/api/profile", async (
    HttpContext context,
    ProfileService profileService,
    CancellationToken cancellationToken) =>
{
    var userId = NotificationRules.GetUserId(context.User);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        return Results.Ok(await profileService.GetAsync(
            userId.Value,
            context.User.Identity?.Name ?? string.Empty,
            cancellationToken));
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Failed to load profile for user {UserId}.", userId);
        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/assistant/messages", async (
    AssistantMessageRequest? request,
    HttpContext context,
    AssistantRequestValidator validator,
    ProfileService profileService,
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

    var userId = NotificationRules.GetUserId(context.User);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    ProfileResponse profile;
    try
    {
        profile = await profileService.GetAsync(
            userId.Value,
            context.User.Identity?.Name ?? string.Empty,
            cancellationToken);
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        app.Logger.LogError(exception, "Failed to load assistant profile for user {UserId}.", userId);
        return Results.Problem(
            title: "The profile service is unavailable.",
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
            "All assistant providers rejected the request; final HTTP status was {HttpStatus}.",
            (int)exception.StatusCode);
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
})
    .RequireRateLimiting("assistant-per-user");

app.MapPut("/api/preferences", async (
    HttpContext context,
    AppDataClient client,
    IEmailContentGenerator emailGenerator,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    var userId = NotificationRules.GetUserId(context.User);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    PreferenceUpdateRequest? request;
    try
    {
        request = await ReadPreferencesAsync(context.Request, cancellationToken);
    }
    catch (JsonException)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["email"] = ["A valid notification email is required."]
        });
    }

    if (request is null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["email"] = ["A valid notification email is required."]
        });
    }

    if (!NotificationRules.IsValidEmail(request.Email))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["email"] = ["A valid notification email is required."]
        });
    }

    try
    {
        var existingPreferences = await client.GetPreferencesAsync(userId.Value, cancellationToken);
        var notificationsEnabled = existingPreferences is { NotifyAll: false } && request.NotifyAll;
        var emailAddress = request.Email.Trim();

        await client.UpsertPreferencesAsync(new UserPreferences(
            userId.Value,
            emailAddress,
            request.NotifyAll,
            request.NotifyCommunityContribution,
            request.NotifyPostEngagement,
            request.NotifyLessonCompletion,
            request.NotifyCourseCompletion,
            request.NotifyQuizResult,
            request.NotifyMinigameWin,
            request.NotifyLoginStreak,
            request.NotifyAchievements), cancellationToken);

        if (notificationsEnabled)
        {
            var email = await emailGenerator.GenerateAsync(new EmailContext(
                context.User.Identity?.Name ?? "LanguageWise learner",
                true,
                "Welcome the learner to LanguageWise notifications. Explain that they can receive community contribution, post engagement, lesson completion, course completion, quiz result, mini-game win, login streak, and achievement notifications.",
                []), cancellationToken);

            await client.CreateNotificationAsync(new NotificationInput(
                userId.Value,
                "notifications-enabled",
                DateTimeOffset.UtcNow,
                email.Subject,
                email.Body), cancellationToken);

            if (emailSender.IsConfigured)
            {
                try
                {
                    await emailSender.SendAsync(emailAddress, email, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    app.Logger.LogError(
                        exception,
                        "Failed to send notifications welcome email for user {UserId}.",
                        userId);
                }
            }
        }

        return Results.Ok(new { message = "Notification preferences saved." });
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Failed to save preferences for user {UserId}.", userId);
        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPost("/api/events", async (
    HttpContext context,
    EventRequest request,
    AppDataClient client,
    IEmailContentGenerator emailGenerator,
    IEmailSender emailSender,
    CancellationToken cancellationToken) =>
{
    var actorUserId = NotificationRules.GetUserId(context.User);
    if (actorUserId is null)
    {
        return Results.Unauthorized();
    }

    var validationErrors = NotificationRules.ValidateEvent(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    try
    {
        var achievements = await client.GetAchievementsByTriggerAsync(request.Trigger, cancellationToken);
        if (achievements.Count == 0)
        {
            return Results.NotFound(new { error = "No achievements are configured for this trigger." });
        }

        var currentProgress = (await client.GetUserAchievementsAsync(request.RecipientUserId, cancellationToken))
            .ToDictionary(item => item.AchievementId, item => item.Progress);
        var achievementUpdates = achievements.Select(achievement =>
        {
            var progressUpdate = NotificationRules.CalculateProgress(
                currentProgress.GetValueOrDefault(achievement.AchievementId),
                achievement.ProgressNeeded,
                request.Value);
            return new AchievementUpdate(
                achievement.AchievementId,
                achievement.Name,
                achievement.Description,
                progressUpdate.Progress,
                achievement.ProgressNeeded,
                progressUpdate.NewlyAttained);
        }).ToList();

        var email = await emailGenerator.GenerateAsync(new EmailContext(
            request.RecipientName.Trim(),
            false,
            request.Subject,
            achievementUpdates), cancellationToken);

        await client.CreateNotificationAsync(new NotificationInput(
            request.RecipientUserId,
            request.Trigger,
            DateTimeOffset.UtcNow,
            email.Subject,
            email.Body), cancellationToken);

        await client.UpsertUserAchievementsAsync(achievementUpdates.Select(update => new UserAchievement(
            request.RecipientUserId,
            update.AchievementId,
            update.Progress)).ToList(), cancellationToken);

        var preferences = await client.GetPreferencesAsync(request.RecipientUserId, cancellationToken);
        var shouldNotify = preferences is not null && NotificationRules.ShouldNotify(
            preferences,
            request.Trigger,
            achievementUpdates.Any(item => item.NewlyAttained));
        var emailSent = false;
        string? emailError = null;

        if (shouldNotify
            && !string.IsNullOrWhiteSpace(preferences!.Email)
            && emailSender.IsConfigured)
        {
            try
            {
                await emailSender.SendAsync(preferences.Email, email, cancellationToken);
                emailSent = true;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                app.Logger.LogError(exception, "Failed to send notification email for trigger {Trigger} and user {UserId}.", request.Trigger, request.RecipientUserId);
                emailError = "Email delivery failed.";
            }
        }

        return Results.Ok(new
        {
            actorUserId,
            recipientUserId = request.RecipientUserId,
            achievements = achievementUpdates,
            notification = new
            {
                email.Subject,
                email.Body,
                email.UsedFallback
            },
            shouldNotify,
            email = new
            {
                sent = emailSent,
                configured = emailSender.IsConfigured,
                error = emailError
            }
        });
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Failed to process trigger {Trigger} for user {UserId}.", request.Trigger, request.RecipientUserId);

        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

static async Task<PreferenceUpdateRequest?> ReadPreferencesAsync(
    HttpRequest request,
    CancellationToken cancellationToken)
{
    return request.HasJsonContentType()
        ? await request.ReadFromJsonAsync<PreferenceUpdateRequest>(cancellationToken)
        : null;
}

    public partial class Program;

