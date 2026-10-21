using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using LanguageWise.GarryAiService.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("openrouter", client =>
{
    client.BaseAddress = new Uri((builder.Configuration["OpenRouter:BaseUrl"] ?? "https://openrouter.ai/api/v1").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("OpenRouter:StartupTimeoutSeconds", 30));
});
builder.Services.AddHttpClient(McpToolHost.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Mcp:TimeoutSeconds", 15));
});
builder.Services.AddHttpClient(AdminSwitches.HttpClientName, client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Services:Database"] ?? "http://localhost:6000").TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<IAdminSwitches, AdminSwitches>();
builder.Services.AddScoped<CompletionProviders>();
builder.Services.AddSingleton<IMcpToolHost, McpToolHost>();
builder.Services.AddScoped<GarryToolLoop>();
builder.Services.AddScoped<GarryAgentPipeline>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("global-assistant", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
var verificationKeyPath = builder.Configuration["Auth:VerificationKeyPath"] ?? "/run/secrets/signing_public_key";
var rsa = RSA.Create();
rsa.ImportFromPem(builder.Configuration["Auth:VerificationKeyPem"] ?? File.ReadAllText(verificationKeyPath));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
app.MapPost("/api/completions", async (
    CompletionRequest? request,
    HttpContext context,
    GarryAgentPipeline pipeline,
    ILoggerFactory loggerFactory,
    IAdminSwitches adminSwitches,
    CancellationToken cancellationToken) =>
{
    if (!CompletionRequest.IsValid(request))
    {
        return Results.BadRequest();
    }

    var switches = await adminSwitches.GetAsync(cancellationToken);
    if (!switches.AiMode)
    {
        return Results.Problem(
            title: "Garry is turned off.",
            detail: "An administrator has turned off AI features.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["code"] = "ai_disabled" });
    }

    var authorization = context.Request.Headers.Authorization.ToString();
    var userToken = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : null;
    return (IResult)new GarryCompletionResult(request!, userToken, pipeline, loggerFactory, switches);
}).RequireRateLimiting("global-assistant");
app.Run();

public partial class Program;