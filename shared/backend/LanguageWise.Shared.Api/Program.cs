using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using LanguageWise.Shared.Api;
using LanguageWise.Shared.Api.Clients;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "shared-backend";
const string DocsSearchRateLimiterPolicy = "docs-search";
const int MaxDocsQueryLength = 500;

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6000";

builder.Services.AddHttpClient<UsersClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var achievementsServiceUrl = builder.Configuration["Services:Achievements"] ?? "http://localhost:5004";
builder.Services.AddHttpClient<AchievementsClient>(client =>
{
    client.BaseAddress = new Uri($"{achievementsServiceUrl}/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

// The RAG server runs on the host, not in Compose (reached via host.docker.internal).
var ragServiceUrl = builder.Configuration["Services:Rag"] ?? "http://localhost:8100";
builder.Services.AddHttpClient<RagClient>(client =>
{
    client.BaseAddress = new Uri(ragServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Load Signing Key
var signingKeyPath = builder.Configuration["Auth:SigningKeyPath"] ?? "/run/secrets/signing_key";
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(signingKeyPath));
var signingKey = new RsaSecurityKey(rsa);

AuthenticatedUser? ValidateToken(string token)
{
    var tokenHandler = new JwtSecurityTokenHandler
    {
        MapInboundClaims = false
    };
    var validationParams = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        IssuerSigningKey = signingKey
    };

    try
    {
        var claims = tokenHandler.ValidateToken(token, validationParams, out _);
        var subject = claims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var name = claims.FindFirst(JwtRegisteredClaimNames.Name)?.Value;

        return int.TryParse(subject, out var id) && id > 0 && !string.IsNullOrWhiteSpace(name)
            ? new AuthenticatedUser(id, name)
            : null;
    }
    catch
    {
        return null;
    }
}

AuthenticatedUser? ReadSessionUser(HttpContext ctx)
{
    var token = ctx.Request.Cookies["token"];
    return string.IsNullOrEmpty(token) ? null : ValidateToken(token);
}

void IssueSessionCookie(HttpContext ctx, int userId, string username)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, username)
        ]),
        Expires = DateTime.UtcNow.AddHours(1),
        SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256)
    };

    var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

    ctx.Response.Cookies.Append("token", token, new CookieOptions
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromHours(1)
    });
}

// Each docs search runs a local embedding model, so limit how often one session can search.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(DocsSearchRateLimiterPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ReadSessionUser(context)?.Id.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapPost("/api/login", async (HttpContext ctx, UsersClient usersClient) =>
{
    var body = await ctx.Request.ReadFromJsonAsync<LoginRequest>();
    if (body is null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
    {
        return Results.Unauthorized();
    }

    var response = await usersClient.VerifyAsync(body.Username, body.Password);

    if (!response.Authenticated)
    {
        return Results.Unauthorized();
    }

    IssueSessionCookie(ctx, response.UserId, body.Username);
    return Results.Ok();
});

app.MapPost("/api/check-login", async (
    HttpContext ctx,
    UsersClient usersClient,
    AchievementsClient achievementsClient,
    CancellationToken cancellationToken) =>
{
    var token = ctx.Request.Cookies["token"];
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var user = ValidateToken(token);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        var streak = await usersClient.RecordLoginAsync(user.Id, cancellationToken);
        if (streak is not null)
        {
            await achievementsClient.RecordLoginStreakAsync(
                user,
                streak.Value,
                token,
                cancellationToken);
        }
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        app.Logger.LogError(exception, "Failed to record login streak for user {UserId}.", user.Id);
    }

    return Results.Ok(user);
});

app.MapPost("/api/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("token");
    return Results.Ok();
});

// "Ask the docs" search for Garry on every feature. Proxies the RAG server's general endpoint,
// which never returns TECHNICAL- (internal) passages.
app.MapPost("/api/rag/query", async (
    HttpContext ctx,
    RagQueryApiRequest? request,
    RagClient ragClient,
    CancellationToken cancellationToken) =>
{
    if (ReadSessionUser(ctx) is null)
    {
        return Results.Unauthorized();
    }

    var query = request?.Query?.Trim();
    if (string.IsNullOrEmpty(query) || query.Length > MaxDocsQueryLength)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["query"] = [$"Enter a question of 1 to {MaxDocsQueryLength} characters."]
        });
    }

    try
    {
        return Results.Ok(await ragClient.QueryAsync(query, request!.NResults, cancellationToken));
    }
    catch (Exception exception) when (
        exception is HttpRequestException or System.Text.Json.JsonException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
    {
        app.Logger.LogWarning(
            "RAG server request failed with error type {ErrorType}.",
            exception.GetType().Name);
        return Results.Problem(
            title: "The documentation search service is unavailable.",
            detail: "Please try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
}).RequireRateLimiting(DocsSearchRateLimiterPolicy);

app.MapPost("/api/users", async (
    CreateAccountRequest body,
    UsersClient usersClient,
    CancellationToken cancellationToken) =>
{
    var username = body.Username?.Trim();
    var errors = new Dictionary<string, string[]>();
    AccountRules.ValidateUsername(username, errors);
    AccountRules.ValidatePassword(body.Password, "password", errors);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var result = await usersClient.CreateAsync(username!, body.Password!, cancellationToken);
    return result.Status == AccountChangeStatus.UsernameTaken
        ? Results.Conflict()
        : Results.Created($"/api/users/{result.Account!.Id}", new AuthenticatedUser(result.Account.Id, result.Account.Username));
});

app.MapPatch("/api/users/{userId:int}", async (
    int userId,
    UpdateAccountRequest body,
    HttpContext ctx,
    UsersClient usersClient,
    CancellationToken cancellationToken) =>
{
    var user = ReadSessionUser(ctx);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    if (user.Id != userId)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var username = body.Username?.Trim();
    var errors = new Dictionary<string, string[]>();
    if (username is null && body.NewPassword is null)
    {
        errors["username"] = ["Provide a new username or password."];
    }

    if (username is not null)
    {
        AccountRules.ValidateUsername(username, errors);
    }

    if (body.NewPassword is not null)
    {
        AccountRules.ValidatePassword(body.NewPassword, "newPassword", errors);
    }

    if (string.IsNullOrEmpty(body.CurrentPassword))
    {
        errors["currentPassword"] = ["Enter your current password."];
    }

    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var verified = await usersClient.VerifyAsync(user.Name, body.CurrentPassword!, cancellationToken);
    if (!verified.Authenticated || verified.UserId != user.Id)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["currentPassword"] = ["Current password is incorrect."]
        });
    }

    var result = await usersClient.UpdateAsync(user.Id, username, body.NewPassword, cancellationToken);
    switch (result.Status)
    {
        case AccountChangeStatus.NotFound:
            return Results.NotFound();
        case AccountChangeStatus.UsernameTaken:
            return Results.Conflict();
    }

    // The name claim is read by other services, so the session must carry the new username.
    IssueSessionCookie(ctx, result.Account!.Id, result.Account.Username);
    return Results.Ok(new AuthenticatedUser(result.Account.Id, result.Account.Username));
});

// Bytes are proxied rather than redirected to: the database service is not reachable from the browser.
app.MapGet("/api/users/{userId:int}/profile-picture", async (
    int userId,
    UsersClient usersClient,
    CancellationToken cancellationToken) =>
{
    var picture = await usersClient.GetProfilePictureAsync(userId, cancellationToken);
    return picture is null ? Results.NotFound() : Results.Ok(ToProfilePictureDetail(picture));
});

app.MapGet("/api/users/{userId:int}/profile-picture/content", async (
    int userId,
    HttpContext ctx,
    UsersClient usersClient,
    CancellationToken cancellationToken) =>
{
    var image = await usersClient.DownloadProfilePictureAsync(userId, cancellationToken);
    if (image is null)
    {
        return Results.NotFound();
    }

    // Unlike chat images the URL is reused when the picture is replaced, so it must be revalidated.
    ctx.Response.Headers.CacheControl = "no-cache";
    ctx.Response.Headers.XContentTypeOptions = "nosniff";
    return Results.File(image.Bytes, image.ContentType);
});

app.MapPut("/api/users/{userId:int}/profile-picture", async (
    int userId,
    HttpContext ctx,
    IFormFile? file,
    UsersClient usersClient,
    CancellationToken cancellationToken) =>
{
    var token = ctx.Request.Cookies["token"];
    var user = string.IsNullOrEmpty(token) ? null : ValidateToken(token);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    if (user.Id != userId)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var errors = ImageRules.ValidateUpload(file?.ContentType, file?.Length ?? 0);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    if (!await LooksLikeDeclaredFormatAsync(file!, cancellationToken))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["file"] = ["That file is not the image format it claims to be."]
        });
    }

    await using var content = file!.OpenReadStream();
    var picture = await usersClient.UploadProfilePictureAsync(
        userId,
        content,
        ImageRules.Normalise(file.ContentType),
        ImageRules.SafeFileName(file.FileName),
        cancellationToken);

    return picture is null ? Results.NotFound() : Results.Ok(ToProfilePictureDetail(picture));
})
    // The token is read explicitly from a SameSite=Strict cookie, so no ambient credential can be forged.
    .DisableAntiforgery();

app.MapDelete("/api/users/{userId:int}/profile-picture", async (
    int userId,
    HttpContext ctx,
    UsersClient usersClient,
    CancellationToken cancellationToken) =>
{
    var token = ctx.Request.Cookies["token"];
    var user = string.IsNullOrEmpty(token) ? null : ValidateToken(token);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    if (user.Id != userId)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    return await usersClient.DeleteProfilePictureAsync(userId, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
});

app.Run();

// The storage key is dropped: the browser addresses the picture by user ID.
static ProfilePictureDetail ToProfilePictureDetail(ProfilePicture picture) => new(
    picture.FileName,
    picture.ContentType,
    picture.SizeBytes,
    picture.UploadedAt);

// IFormFile buffers the part, so opening the stream a second time for the upload costs nothing.
static async Task<bool> LooksLikeDeclaredFormatAsync(IFormFile file, CancellationToken cancellationToken)
{
    var header = new byte[ImageRules.SignatureLength];

    await using var stream = file.OpenReadStream();
    var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

    return ImageRules.MatchesContentType(file.ContentType, header.AsSpan(0, read));
}

internal sealed record LoginRequest(string Username, string Password);

internal sealed record RagQueryApiRequest(string? Query, int? NResults);

internal sealed record CreateAccountRequest(string? Username, string? Password);

internal sealed record UpdateAccountRequest(string? Username, string? NewPassword, string? CurrentPassword);

internal sealed record VerifyResponse(bool Authenticated, int UserId);

internal sealed record AuthenticatedUser(int Id, string Name);

internal sealed record ProfilePictureDetail(string FileName, string ContentType, long SizeBytes, DateTime UploadedAt);

public sealed class SharedApiAssemblyMarker;
