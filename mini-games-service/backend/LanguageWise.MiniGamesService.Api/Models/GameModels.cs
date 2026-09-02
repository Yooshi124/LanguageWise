using System.Text.Json;

namespace LanguageWise.MiniGamesService.Api.Models;

/// <summary>API response for a game.</summary>
public sealed record GameResponse(
    int Id,
    string GameType,
    int UserId,
    string CourseCode,
    string Solution,
    IReadOnlyList<string> Words,
    string Difficulty,
    string CreatedAt,
    string? ExpiresAt);

/// <summary>API response for a game attempt.</summary>
public sealed record GameAttemptResponse(
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

/// <summary>Successful completions per game type for a user, optionally scoped to one course (language).</summary>
public sealed record CompletionStatsResponse(
    string? CourseCode,
    int GuessTheWord,
    int WordSearch,
    int Associations);

/// <summary>Database response wrapper.</summary>
public sealed record DatabaseResponse<T>(
    int StatusCode,
    T? Data,
    string? Error,
    string? ContentType)
{
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}

/// <summary>Database health response.</summary>
public sealed record DatabaseHealthResponse(
    string Status,
    string Service,
    int Endpoints);
