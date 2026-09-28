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
builder.Services.AddHttpClient("ollama", client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Services:Ollama"] ?? "http://localhost:11434").TrimEnd('/') + "/");
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddScoped<CompletionProviders>();
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
rsa.ImportFromPem(File.ReadAllText(verificationKeyPath));

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
    CompletionProviders providers,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    if (!CompletionRequest.IsValid(request))
    {
        return Results.BadRequest();
    }

    ProviderStream completion;
    try
    {
        completion = await providers.StartAsync(request!.BuildMessages(), cancellationToken);
    }
    catch (Exception exception) when (exception is HttpRequestException or ProviderException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
    {
        loggerFactory.CreateLogger("GarryProviders").LogWarning("Both assistant providers could not start: {ErrorType}", exception.GetType().Name);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return new CompletionResult(completion, loggerFactory.CreateLogger<CompletionResult>());
}).RequireRateLimiting("global-assistant");
app.Run();

public partial class Program;