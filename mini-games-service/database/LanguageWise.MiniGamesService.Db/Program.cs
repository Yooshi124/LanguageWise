using System.Text.Json.Serialization;
using LanguageWise.MiniGamesService.Db.Data;
using LanguageWise.MiniGamesService.Db.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Json;

const string ServiceName = "mini-games-service-db";
const string PublicCatalogError = "The SQLite catalog health check failed.";

var builder = WebApplication.CreateBuilder(args);

// The database file lives on a named Docker volume so it survives container restarts.
var databasePath = builder.Configuration["Database:Path"] ?? "data/mini-games-service.db";
var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    ForeignKeys = true
}.ToString();

builder.Services.AddSingleton(new GameRepository(connectionString));
builder.Services.AddSingleton(new GameAttemptRepository(connectionString));
builder.Services.AddSingleton(serviceProvider => new DatabaseInitializer(
    connectionString,
    Path.Combine(AppContext.BaseDirectory, "sql"),
    serviceProvider.GetRequiredService<ILogger<DatabaseInitializer>>()));

builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.Services.GetRequiredService<DatabaseInitializer>().Initialise();

app.MapGet("/health", (GameRepository gameRepository, GameAttemptRepository attemptRepository, EndpointDataSource endpointDataSource) =>
{
    try
    {
        _ = gameRepository.Count();
        _ = attemptRepository.Count();
        return Results.Ok(new
        {
            status = "healthy",
            service = ServiceName,
            endpoints = endpointDataSource.Endpoints.Count
        });
    }
    catch (Exception exception)
    {
        return Results.Problem(PublicCatalogError, statusCode: 500);
    }
})
.WithName("HealthCheck")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status500InternalServerError);

// Games endpoints
app.MapGet("/api/games/for-user/{userId}", (int userId, GameRepository gameRepository) =>
    Results.Ok(gameRepository.GetForAttemptUser(userId)))
.WithName("GetGamesForUser")
.Produces<IReadOnlyList<Game>>(StatusCodes.Status200OK);

app.MapGet("/api/games/{id}", (int id, GameRepository gameRepository) =>
{
    var game = gameRepository.GetById(id);
    return game is null ? Results.NotFound() : Results.Ok(game);
})
.WithName("GetGameById")
.Produces<Game>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/games", (CreateGameRequest request, GameRepository gameRepository) =>
{
    var game = gameRepository.Create(
        request.GameType,
        request.CourseCode,
        request.Solution,
        request.Words,
        request.Difficulty ?? "intermediate",
        request.ExpiresAt);
    return Results.Created($"/api/games/{game.Id}", game);
})
.WithName("CreateGame")
.Produces<Game>(StatusCodes.Status201Created);

app.MapDelete("/api/games/{id}", (int id, GameRepository gameRepository) =>
    gameRepository.Delete(id) ? Results.NoContent() : Results.NotFound())
.WithName("DeleteGame")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// GameAttempts endpoints
app.MapGet("/api/game-attempts/user/{userId}", (int userId, GameAttemptRepository attemptRepository) =>
    Results.Ok(attemptRepository.GetByUserId(userId)))
.WithName("GetAttemptsByUserId")
.Produces<IReadOnlyList<GameAttempt>>(StatusCodes.Status200OK);

app.MapGet("/api/game-attempts/game/{gameId}", (int gameId, GameAttemptRepository attemptRepository) =>
    Results.Ok(attemptRepository.GetByGameId(gameId)))
.WithName("GetAttemptsByGameId")
.Produces<IReadOnlyList<GameAttempt>>(StatusCodes.Status200OK);

app.MapGet("/api/game-attempts/{id}", (int id, GameAttemptRepository attemptRepository) =>
{
    var attempt = attemptRepository.GetById(id);
    return attempt is null ? Results.NotFound() : Results.Ok(attempt);
})
.WithName("GetGameAttemptById")
.Produces<GameAttempt>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/api/game-attempts/game/{gameId}/user/{userId}/latest", (int gameId, int userId, GameAttemptRepository attemptRepository) =>
{
    var attempt = attemptRepository.GetLatestByGameIdAndUserId(gameId, userId);
    return attempt is null ? Results.NotFound() : Results.Ok(attempt);
})
.WithName("GetLatestAttempt")
.Produces<GameAttempt>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapPost("/api/game-attempts", (CreateGameAttemptRequest request, GameAttemptRepository attemptRepository) =>
{
    var attempt = attemptRepository.Create(request.GameId, request.UserId);
    return Results.Created($"/api/game-attempts/{attempt.Id}", attempt);
})
.WithName("CreateGameAttempt")
.Produces<GameAttempt>(StatusCodes.Status201Created);

app.MapPatch("/api/game-attempts/{id}", (int id, UpdateGameAttemptRequest request, GameAttemptRepository attemptRepository) =>
{
    var attempt = attemptRepository.Update(
        id,
        request.Score,
        request.IsWon,
        request.IsComplete,
        request.AttemptCount,
        request.CompletedAt,
        request.TimeSpentSeconds);
    return attempt is null ? Results.NotFound() : Results.Ok(attempt);
})
.WithName("UpdateGameAttempt")
.Produces<GameAttempt>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

app.MapDelete("/api/game-attempts/{id}", (int id, GameAttemptRepository attemptRepository) =>
    attemptRepository.Delete(id) ? Results.NoContent() : Results.NotFound())
.WithName("DeleteGameAttempt")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.Run();
