using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Clients;
using LanguageWise.QuestsAchievementsNotificationsService.Api.Rendering;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Supabase.Postgrest;
using Supabase.Postgrest.Interfaces;

const string ServiceName = "quests-achievements-notifications-service-backend";

var builder = WebApplication.CreateBuilder(args);

// Inside Docker this resolves to the database service by container name.
var databaseServiceUrl = builder.Configuration["Services:Database"] ?? "http://localhost:6004";

builder.Services.AddSingleton<IPostgrestClient>(new Client(
    databaseServiceUrl.TrimEnd('/'),
    new ClientOptions { Schema = "api" }));
builder.Services.AddSingleton<SampleItemsClient>();

var signingKeyPath = builder.Configuration["Auth:VerificationKeyPath"] ?? "/run/secrets/signing_public_key";
var rsa = RSA.Create();
rsa.ImportFromPem(File.ReadAllText(signingKeyPath));

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

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }))
    .AllowAnonymous();

app.MapGet("/api/sample-items", async (SampleItemsClient client, CancellationToken cancellationToken) =>
{
    try
    {
        return Results.Ok(await client.GetAllAsync(cancellationToken));
    }
    catch (Exception exception)
    {
        return Results.Problem(
            title: "The database microservice is unavailable.",
            detail: exception.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

// HTMX target: returns table rows rather than JSON so the browser can swap them straight in.
app.MapGet("/api/sample-items/fragment", async (SampleItemsClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var items = await client.GetAllAsync(cancellationToken);
        return Results.Content(SampleItemHtmlRenderer.RenderRows(items), "text/html");
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Failed to load sample items from {Url}.", databaseServiceUrl);

        return Results.Content(
            SampleItemHtmlRenderer.RenderError("The database microservice is unavailable."),
            "text/html",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
