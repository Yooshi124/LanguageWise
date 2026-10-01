using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LanguageWise.ChatDiscussionService.Api.Clients;
using LanguageWise.ChatDiscussionService.Api.Endpoints;
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IAssistantCompletionClient, GarryCompletionClient>(client =>
{
    client.BaseAddress = new Uri((builder.Configuration["Services:Garry"] ?? "http://localhost:5010").TrimEnd('/') + "/");

    // No timeout: the response is a stream that stays open for as long as the
    // model keeps writing, and the first token after a cold start is slow.
    // HttpClient's default would abort a long answer part-way.
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.AddSingleton<AssistantRequestValidator>();
builder.Services.AddSingleton<IAssistantPromptBuilder, AssistantPromptBuilder>();
builder.Services.AddScoped<IAssistantContextService, AssistantContextService>();

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapPostEndpoints();
app.MapCommentEndpoints();
app.MapLikeEndpoints();
app.MapImageEndpoints();
app.MapAssistantEndpoints();

app.Run();

public partial class Program;
