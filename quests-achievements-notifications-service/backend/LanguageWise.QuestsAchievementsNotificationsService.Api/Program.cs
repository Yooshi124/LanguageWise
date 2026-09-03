using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using LanguageWise.QuestsAchievementsNotificationsService.Api;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

var ollamaServiceUrl = builder.Configuration["Services:Ollama"] ?? "http://localhost:11434";
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddHttpClient<IEmailContentGenerator, OllamaEmailGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }))
    .AllowAnonymous();

app.MapGet("/api/profile", async (
    HttpContext context,
    AppDataClient client,
    CancellationToken cancellationToken) =>
{
    var userId = NotificationRules.GetUserId(context.User);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var preferences = await client.GetPreferencesAsync(userId.Value, cancellationToken)
            ?? DefaultPreferences(userId.Value);
        var achievements = await client.GetAchievementsAsync(cancellationToken);
        var progress = (await client.GetUserAchievementsAsync(userId.Value, cancellationToken))
            .ToDictionary(item => item.AchievementId, item => item.Progress);
        var notifications = await client.GetNotificationsAsync(userId.Value, cancellationToken);
        var view = achievements.Select(achievement => new AchievementProgress(
            achievement.AchievementId,
            achievement.Name,
            achievement.Image,
            progress.GetValueOrDefault(achievement.AchievementId),
            achievement.ProgressNeeded)).ToList();

        return Results.Ok(new
        {
            username = context.User.Identity?.Name ?? string.Empty,
            preferences = new
            {
                preferences.Email,
                preferences.NotifyAll,
                preferences.NotifyCommunityContribution,
                preferences.NotifyPostEngagement,
                preferences.NotifyLessonCompletion,
                preferences.NotifyCourseCompletion,
                preferences.NotifyQuizResult,
                preferences.NotifyMinigameWin,
                preferences.NotifyLoginStreak,
                preferences.NotifyAchievements
            },
            achievements = view,
            notifications = notifications.Select(notification => new
            {
                notification.NotificationId,
                notification.Trigger,
                notification.Time,
                notification.EmailSubject,
                notification.EmailBody
            })
        });
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Failed to load profile for user {UserId}.", userId);
        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

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

static UserPreferences DefaultPreferences(int userId) =>
    new(userId, null, true, true, true, true, true, true, true, true, true);

static async Task<PreferenceUpdateRequest?> ReadPreferencesAsync(
    HttpRequest request,
    CancellationToken cancellationToken)
{
    return request.HasJsonContentType()
        ? await request.ReadFromJsonAsync<PreferenceUpdateRequest>(cancellationToken)
        : null;
}

    public partial class Program;

