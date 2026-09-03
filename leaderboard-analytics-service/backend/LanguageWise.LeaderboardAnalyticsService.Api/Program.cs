using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Models;
using LanguageWise.LeaderboardAnalyticsService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var quizzesCoursesServiceUrl = builder.Configuration["Services:QuizzesCourses"] ?? "http://localhost:5003";

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<QuizzesCoursesClient>(client =>
{
    client.BaseAddress = new Uri(quizzesCoursesServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var ollamaServiceUrl = builder.Configuration["Services:Ollama"] ?? "http://localhost:11434";
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection("Ollama"));
builder.Services.AddHttpClient<ISummaryGenerator, OllamaSummaryGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
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
    await Task.WhenAll(myMilestonesTask, allMilestonesTask, coursesTask);

    var rankings = AnalyticsProjector.BuildLanguageRankings(
        userId,
        myMilestonesTask.Result,
        allMilestonesTask.Result,
        coursesTask.Result);
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
    await Task.WhenAll(myMilestonesTask, coursesTask);

    var response = AnalyticsProjector.BuildLessonsCompleted(
        userId,
        myMilestonesTask.Result,
        coursesTask.Result,
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
    await Task.WhenAll(myMilestonesTask, coursesTask);

    var chartData = AnalyticsProjector.BuildLessonsCompleted(
        userId,
        myMilestonesTask.Result,
        coursesTask.Result,
        DateOnly.FromDateTime(DateTime.UtcNow));
    var summary = await generator.GenerateAsync(chartData, cancellationToken);
    return Results.Ok(summary);
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

