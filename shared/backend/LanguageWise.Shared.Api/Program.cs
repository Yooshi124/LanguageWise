using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LanguageWise.Shared.Api;
using LanguageWise.Shared.Api.Clients;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "shared-backend";

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

var app = builder.Build();

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

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, response.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, body.Username)
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

internal sealed record VerifyResponse(bool Authenticated, int UserId);

internal sealed record AuthenticatedUser(int Id, string Name);

internal sealed record ProfilePictureDetail(string FileName, string ContentType, long SizeBytes, DateTime UploadedAt);

public sealed class SharedApiAssemblyMarker;
