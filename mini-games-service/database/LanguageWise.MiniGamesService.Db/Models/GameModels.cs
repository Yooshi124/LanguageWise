using System.Text.Json;

namespace LanguageWise.MiniGamesService.Db.Models;

/// <summary>A row of the Games table. Users relate to games through GameAttempts, not directly.</summary>
public sealed record Game(
    int Id,
    string GameType,
    string CourseCode,
    string Solution,
    JsonElement Words,
    string Difficulty,
    string CreatedAt,
    string? ExpiresAt);

/// <summary>A row of the GameAttempts table.</summary>
public sealed record GameAttempt(
    int Id,
    int GameId,
    int UserId,
    int Score,
    bool IsWon,
    bool IsComplete,
    int AttemptCount,
    string StartedAt,
    string? CompletedAt,
    int TimeSpentSeconds);

/// <summary>Input for creating a new game.</summary>
public sealed record CreateGameRequest(
    string GameType,
    string CourseCode,
    string Solution,
    IReadOnlyList<string> Words,
    string? Difficulty = "intermediate",
    string? ExpiresAt = null);

/// <summary>Input for creating a game attempt.</summary>
public sealed record CreateGameAttemptRequest(
    int GameId,
    int UserId);

/// <summary>Input for updating a game attempt.</summary>
public sealed record UpdateGameAttemptRequest(
    int? Score = null,
    bool? IsWon = null,
    bool? IsComplete = null,
    int? AttemptCount = null,
    string? CompletedAt = null,
    int? TimeSpentSeconds = null);
