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

string? ValidateToken(string token)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var validationParams = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = signingKey
    };

    try
    {
        var claims = tokenHandler.ValidateToken(token, validationParams, out _);
        return claims.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Name)?.Value;
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

app.MapPost("/api/check-login", async (HttpContext ctx) =>
{
    CheckLoginRequest? request = null;
    if (ctx.Request.HasJsonContentType() && ctx.Request.ContentLength is > 0)
    {
        request = await ctx.Request.ReadFromJsonAsync<CheckLoginRequest>();
    }
    else
    {
        request = new CheckLoginRequest(ctx.Request.Cookies["token"] ?? "");
    }

    var token = request?.token;
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var name = ValidateToken(token);
    return name is not null ? Results.Ok(name) : Results.Unauthorized();
});

app.MapPost("/api/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("token");
    return Results.Ok();
});

app.MapPost("/api/check-login/fragment", (HttpContext ctx) =>
{
    var token = ctx.Request.Cookies["token"] ?? "";
    var name = ValidateToken(token);

    return name is not null
        ? Results.Content(
            $"""<span>Logged in as {WebUtility.HtmlEncode(name)}</span> <button hx-post="/api/logout" hx-on::after-request="window.location.reload()">Log out</button>""",
            "text/html")
        : Results.Content(
            """<a href="/login.html">Sign in</a>""",
            "text/html");
});

app.Run();

internal sealed record LoginRequest(string Username, string Password);

internal sealed record VerifyResponse(bool Authenticated, int UserId);

internal sealed record CheckLoginRequest(string token);
