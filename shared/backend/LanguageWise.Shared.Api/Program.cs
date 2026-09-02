using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net;
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

app.MapPost("/api/check-login", (HttpContext ctx) =>
{
    var token = ctx.Request.Cookies["token"];
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var user = ValidateToken(token);
    return user is not null ? Results.Ok(user) : Results.Unauthorized();
});

app.MapPost("/api/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("token");
    return Results.Ok();
});

app.MapPost("/api/check-login/fragment", (HttpContext ctx) =>
{
    var token = ctx.Request.Cookies["token"] ?? "";
    var user = ValidateToken(token);

    return user is not null
        ? Results.Content(
            $"""<span>Logged in as {WebUtility.HtmlEncode(user.Name)}</span> <button hx-post="/api/logout" hx-on::after-request="window.location.reload()">Log out</button>""",
            "text/html")
        : Results.Content(
            """<a href="/login.html">Sign in</a>""",
            "text/html");
});

app.Run();

internal sealed record LoginRequest(string Username, string Password);

internal sealed record VerifyResponse(bool Authenticated, int UserId);

internal sealed record AuthenticatedUser(int Id, string Name);

public sealed class SharedApiAssemblyMarker;
