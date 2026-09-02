using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LanguageWise.LeaderboardAnalyticsService.Api.Clients;
using LanguageWise.LeaderboardAnalyticsService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:5006";

builder.Services.AddHttpClient<LeaderboardClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
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

app.MapGet("/api/me", (HttpContext context) =>
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    var username = context.User.Identity?.Name;

    return int.TryParse(subject, out var userId) && !string.IsNullOrWhiteSpace(username)
        ? Results.Ok(new { id = userId, username })
        : Results.Unauthorized();
});

// ---------------------------------------------------------------------------
// Language Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/language-rankings", (
    LeaderboardClient client,
    CancellationToken cancellationToken,
    string? language = null,
    int limit = 50,
    int offset = 0) =>
    client.GetLanguageRankingsAsync(language, limit, offset, cancellationToken));

app.MapGet("/api/language-rankings/{id:int}", async (int id, LeaderboardClient client, CancellationToken cancellationToken) =>
    await client.GetLanguageRankingAsync(id, cancellationToken) is { } ranking
        ? Results.Ok(ranking)
        : Results.NotFound());

app.MapGet("/api/language-rankings/user/{userId:int}", (int userId, LeaderboardClient client, CancellationToken cancellationToken) =>
    client.GetLanguageRankingsByUserAsync(userId, cancellationToken));

app.MapGet("/api/my-language-rankings", async (
    HttpContext context,
    LeaderboardClient client,
    CancellationToken cancellationToken) =>
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(subject, out var userId))
    {
        return Results.Unauthorized();
    }

    var rankings = await client.GetLanguageRankingsByUserAsync(userId, cancellationToken);
    return Results.Ok(rankings);
});

// ---------------------------------------------------------------------------
// Discussion Rankings
// ---------------------------------------------------------------------------

app.MapGet("/api/discussion-rankings", (
    LeaderboardClient client,
    CancellationToken cancellationToken,
    int limit = 50,
    int offset = 0) =>
    client.GetDiscussionRankingsAsync(limit, offset, cancellationToken));

app.MapGet("/api/discussion-rankings/{id:int}", async (int id, LeaderboardClient client, CancellationToken cancellationToken) =>
    await client.GetDiscussionRankingAsync(id, cancellationToken) is { } ranking
        ? Results.Ok(ranking)
        : Results.NotFound());

app.MapGet("/api/discussion-rankings/user/{userId:int}", async (int userId, LeaderboardClient client, CancellationToken cancellationToken) =>
    await client.GetDiscussionRankingByUserAsync(userId, cancellationToken) is { } ranking
        ? Results.Ok(ranking)
        : Results.NotFound());

app.MapGet("/api/lessons-completed-over-time", (HttpContext context) =>
{
    var subject = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    if (!int.TryParse(subject, out var userId))
    {
        return Results.Unauthorized();
    }

    var to = DateOnly.FromDateTime(DateTime.UtcNow);
    var from = to.AddDays(-29);
    return Results.Ok(MockLessonsCompletedGenerator.Generate(userId, from, to));
});

app.Run();
