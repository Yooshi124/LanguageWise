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
builder.Services.PostConfigure<SmtpOptions>(options =>
{
    options.Username = builder.Configuration["SMTP_USERNAME"] ?? options.Username;
    options.Password = builder.Configuration["SMTP_PASSWORD"] ?? options.Password;
    options.FromName = builder.Configuration["SMTP_FROM_NAME"] ?? options.FromName;
});
builder.Services.AddHttpClient<IEmailContentGenerator, OllamaEmailGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(90);
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
                preferences.NotifyPostEngagement,
                preferences.NotifyCourseCompletion,
                preferences.NotifyQuizResults,
                preferences.NotifyStreaks,
                preferences.NotifyAchievements
            },
            achievements = view
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
        await client.UpsertPreferencesAsync(new UserPreferences(
            userId.Value,
            request.Email.Trim(),
            request.NotifyAll,
            request.NotifyPostEngagement,
            request.NotifyCourseCompletion,
            request.NotifyQuizResults,
            request.NotifyStreaks,
            request.NotifyAchievements), cancellationToken);

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
        var eventId = request.EventId.Trim();
        if (await client.EventExistsAsync(eventId, cancellationToken))
        {
            return Results.Conflict(new { error = "The event has already been processed." });
        }

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
                request.Value,
                achievement.ProgressNeeded);
            return new AchievementUpdate(
                achievement.AchievementId,
                achievement.Name,
                progressUpdate.Progress,
                achievement.ProgressNeeded,
                progressUpdate.NewlyAttained);
        }).ToList();

        var email = await emailGenerator.GenerateAsync(new EmailContext(
            request.Trigger,
            request.SubjectId,
            achievementUpdates), cancellationToken);

        var created = await client.CreateNotificationAsync(new NotificationInput(
            eventId,
            request.RecipientUserId,
            request.Trigger,
            request.OccurredAt,
            email.Subject,
            email.Body), cancellationToken);
        if (!created)
        {
            return Results.Conflict(new { error = "The event has already been processed." });
        }

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
                app.Logger.LogError(exception, "Failed to send notification email for event {EventId}.", request.EventId);
                emailError = "Email delivery failed.";
            }
        }

        return Results.Ok(new
        {
            eventId = request.EventId,
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
        app.Logger.LogError(exception, "Failed to process event {EventId}.", request.EventId);

        return Results.Problem(
            title: "The database microservice is unavailable.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

static UserPreferences DefaultPreferences(int userId) =>
    new(userId, null, true, true, true, true, true, true);

static async Task<PreferenceUpdateRequest?> ReadPreferencesAsync(
    HttpRequest request,
    CancellationToken cancellationToken)
{
    if (request.HasJsonContentType())
    {
        return await request.ReadFromJsonAsync<PreferenceUpdateRequest>(cancellationToken);
    }

    if (!request.HasFormContentType)
    {
        return null;
    }

    var form = await request.ReadFormAsync(cancellationToken);
    return new PreferenceUpdateRequest(
        form["email"].ToString(),
        form.ContainsKey("notifyAll"),
        form.ContainsKey("notifyPostEngagement"),
        form.ContainsKey("notifyCourseCompletion"),
        form.ContainsKey("notifyQuizResults"),
        form.ContainsKey("notifyStreaks"),
        form.ContainsKey("notifyAchievements"));
}

    public partial class Program;

