using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
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
    var userId = GetUserId(context.User);
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
            preferences,
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
    var userId = GetUserId(context.User);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var request = await ReadPreferencesAsync(context.Request, cancellationToken);
    if (request is null || !MailAddress.TryCreate(request.Email, out _))
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
    CancellationToken cancellationToken) =>
{
    var actorUserId = GetUserId(context.User);
    if (actorUserId is null)
    {
        return Results.Unauthorized();
    }

    var validationErrors = ValidateEvent(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    try
    {
        var preferences = await client.GetPreferencesAsync(request.RecipientUserId, cancellationToken);
        if (preferences is null)
        {
            return Results.NotFound(new { error = "Recipient preferences were not found." });
        }

        if (string.IsNullOrWhiteSpace(preferences.Email))
        {
            return Results.Conflict(new { error = "Recipient notification email is not configured." });
        }

        var achievement = await client.GetAchievementAsync(request.AchievementId, cancellationToken);
        if (achievement is null)
        {
            return Results.NotFound(new { error = "Achievement was not found." });
        }

        var current = (await client.GetUserAchievementsAsync(request.RecipientUserId, cancellationToken))
            .SingleOrDefault(item => item.AchievementId == request.AchievementId);
        var oldProgress = current?.Progress ?? 0;
        var newProgress = Math.Min(oldProgress + request.Value, achievement.ProgressNeeded);
        var newlyAttained = oldProgress < achievement.ProgressNeeded && newProgress >= achievement.ProgressNeeded;

        var created = await client.CreateNotificationAsync(new NotificationInput(
            request.EventId.Trim(),
            request.RecipientUserId,
            request.EventType,
            request.OccurredAt,
            preferences.Email), cancellationToken);
        if (!created)
        {
            return Results.Conflict(new { error = "The event has already been processed." });
        }

        await client.UpsertUserAchievementAsync(new UserAchievement(
            request.RecipientUserId,
            request.AchievementId,
            newProgress), cancellationToken);

        return Results.Ok(new
        {
            eventId = request.EventId,
            actorUserId,
            recipientUserId = request.RecipientUserId,
            achievement = new
            {
                achievement.AchievementId,
                achievement.Name,
                progress = newProgress,
                achievement.ProgressNeeded,
                newlyAttained
            },
            shouldNotify = ShouldNotify(preferences, request.EventType, newlyAttained)
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

static int? GetUserId(ClaimsPrincipal user) =>
    int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ? userId : null;

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

static Dictionary<string, string[]> ValidateEvent(EventRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.EventId))
    {
        errors["eventId"] = ["Event ID is required."];
    }

    if (request.EventType is not ("post-engagement" or "course-completion" or "quiz-result" or "streak"))
    {
        errors["eventType"] = ["Event type is not supported."];
    }

    if (string.IsNullOrWhiteSpace(request.SubjectId))
    {
        errors["subjectId"] = ["Subject ID is required."];
    }

    if (request.RecipientUserId <= 0)
    {
        errors["recipientUserId"] = ["Recipient user ID must be positive."];
    }

    if (request.AchievementId <= 0)
    {
        errors["achievementId"] = ["Achievement ID must be positive."];
    }

    if (request.OccurredAt == default)
    {
        errors["occurredAt"] = ["Occurrence time is required."];
    }

    if (request.Value <= 0)
    {
        errors["value"] = ["Value must be positive."];
    }

    if (request.Metadata is { ValueKind: not JsonValueKind.Object })
    {
        errors["metadata"] = ["Metadata must be a JSON object."];
    }

    return errors;
}

static bool ShouldNotify(UserPreferences preferences, string eventType, bool newlyAttained)
{
    if (!preferences.NotifyAll)
    {
        return false;
    }

    var eventEnabled = eventType switch
    {
        "post-engagement" => preferences.NotifyPostEngagement,
        "course-completion" => preferences.NotifyCourseCompletion,
        "quiz-result" => preferences.NotifyQuizResults,
        "streak" => preferences.NotifyStreaks,
        _ => false
    };

    return eventEnabled || (newlyAttained && preferences.NotifyAchievements);
}
