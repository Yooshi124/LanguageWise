using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Endpoints;
using LanguageWise.ChatDiscussionService.Api.Options;
using LanguageWise.ChatDiscussionService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

const string ServiceName = "chat-discussion-service-backend";

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6002";
var achievementsServiceUrl = builder.Configuration["Services:Achievements"] ?? "http://localhost:5004";

builder.Services.AddHttpClient<DiscussionClient>(client =>
{
    client.BaseAddress = new Uri(databaseServiceUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<AchievementEventsClient>(client =>
{
    client.BaseAddress = new Uri($"{achievementsServiceUrl}/");
    client.Timeout = TimeSpan.FromSeconds(20);
});

// AI mode. The model runs in the shared 'ollama' container, so there is nothing
// to configure beyond its address, which resolves by container name inside
// Docker exactly as the database address above does.
var ollamaServiceUrl = builder.Configuration["Services:Ollama"] ?? "http://localhost:11434";

builder.Services
    .AddOptions<OllamaOptions>()
    .Bind(builder.Configuration.GetSection(OllamaOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Model),
        "Ollama:Model is required.")
    .Validate(
        options => options.MaxOutputTokens is > 0 and <= 8192,
        "Ollama:MaxOutputTokens must be between 1 and 8192.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IAssistantCompletionClient, OllamaAssistantClient>(client =>
{
    client.BaseAddress = new Uri(ollamaServiceUrl.TrimEnd('/') + "/");

    // No timeout: the response is a stream that stays open for as long as the
    // model keeps writing, and the first token after a cold start is slow.
    // HttpClient's default would abort a long answer part-way.
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();
builder.Services.AddScoped<IAssistantContextService, AssistantContextService>();

// The model is metered, so one signed-in user cannot spend the whole allowance.
// Partitioned by 'sub' rather than IP: everyone here is signed in anyway, and a
// shared campus address should not be one bucket.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            await Results.Problem(
                title: "Too many assistant requests.",
                detail: "Please wait a moment before sending another question.",
                statusCode: StatusCodes.Status429TooManyRequests)
                .ExecuteAsync(context.HttpContext);
        }
    };
    options.AddPolicy(AssistantEndpoints.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// Tokens are minted by shared-backend and signed with the private half of this
// key pair. This service only ever verifies them.
var verificationKeyPath = builder.Configuration["Auth:VerificationKeyPath"] ?? "/run/secrets/signing_public_key";
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(verificationKeyPath));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Leave 'sub' alone. With the default mapping it is renamed to
        // NameIdentifier and DiscussionRules.GetUserId silently returns null.
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

// Deliberately no fallback policy: reading the forum works signed out, and each
// write opts in with RequireAuthorization instead.
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapPostEndpoints();
app.MapCommentEndpoints();
app.MapLikeEndpoints();
app.MapImageEndpoints();
app.MapAssistantEndpoints();

app.Run();

public partial class Program;
