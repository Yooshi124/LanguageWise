using LanguageWise.MiniGamesService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.MiniGamesService.Db.Data;

/// <summary>Plain ADO.NET data access for the GameAttempts table.</summary>
public sealed class GameAttemptRepository(string connectionString)
{
    private const string SelectColumns = "SELECT Id, GameId, UserId, Score, IsWon, IsComplete, AttemptCount, StartedAt, CompletedAt, TimeSpentSeconds FROM GameAttempts";

    public IReadOnlyList<GameAttempt> GetByUserId(int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE UserId = $userId ORDER BY StartedAt DESC;";
        command.Parameters.AddWithValue("$userId", userId);

        var attempts = new List<GameAttempt>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            attempts.Add(Map(reader));
        }

        return attempts;
    }

    public IReadOnlyList<GameAttempt> GetByGameId(int gameId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE GameId = $gameId ORDER BY StartedAt;";
        command.Parameters.AddWithValue("$gameId", gameId);

        var attempts = new List<GameAttempt>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            attempts.Add(Map(reader));
        }

        return attempts;
    }

    public GameAttempt? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public GameAttempt? GetLatestByGameIdAndUserId(int gameId, int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE GameId = $gameId AND UserId = $userId ORDER BY StartedAt DESC LIMIT 1;";
        command.Parameters.AddWithValue("$gameId", gameId);
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public GameAttempt Create(int gameId, int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        var startedAt = DateTime.UtcNow.ToString("O");

        command.CommandText =
            """
            INSERT INTO GameAttempts (GameId, UserId, StartedAt)
            VALUES ($gameId, $userId, $startedAt)
            RETURNING Id, GameId, UserId, Score, IsWon, IsComplete, AttemptCount, StartedAt, CompletedAt, TimeSpentSeconds;
            """;
        command.Parameters.AddWithValue("$gameId", gameId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$startedAt", startedAt);

        using var reader = command.ExecuteReader();
        reader.Read();
        return Map(reader);
    }

    public GameAttempt? Update(int id, int? score = null, bool? isWon = null, bool? isComplete = null, int? attemptCount = null, string? completedAt = null, int? timeSpentSeconds = null)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();

        var setClauses = new List<string>();
        if (score.HasValue)
        {
            setClauses.Add("Score = $score");
            command.Parameters.AddWithValue("$score", score.Value);
        }
        if (isWon.HasValue)
        {
            setClauses.Add("IsWon = $isWon");
            command.Parameters.AddWithValue("$isWon", isWon.Value ? 1 : 0);
        }
        if (isComplete.HasValue)
        {
            setClauses.Add("IsComplete = $isComplete");
            command.Parameters.AddWithValue("$isComplete", isComplete.Value ? 1 : 0);
        }
        if (attemptCount.HasValue)
        {
            setClauses.Add("AttemptCount = $attemptCount");
            command.Parameters.AddWithValue("$attemptCount", attemptCount.Value);
        }
        if (!string.IsNullOrEmpty(completedAt))
        {
            setClauses.Add("CompletedAt = $completedAt");
            command.Parameters.AddWithValue("$completedAt", completedAt);
        }
        if (timeSpentSeconds.HasValue)
        {
            setClauses.Add("TimeSpentSeconds = $timeSpentSeconds");
            command.Parameters.AddWithValue("$timeSpentSeconds", timeSpentSeconds.Value);
        }

        if (setClauses.Count == 0)
        {
            return GetById(id);
        }

        command.Parameters.AddWithValue("$id", id);
        command.CommandText =
            $"""
            UPDATE GameAttempts
            SET {string.Join(", ", setClauses)}
            WHERE Id = $id
            RETURNING Id, GameId, UserId, Score, IsWon, IsComplete, AttemptCount, StartedAt, CompletedAt, TimeSpentSeconds;
            """;

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public bool Delete(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM GameAttempts WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>Cheap query used by the health endpoint to prove the database is reachable.</summary>
    public long Count()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM GameAttempts;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static GameAttempt Map(SqliteDataReader reader) =>
        new(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4) == 1,
            reader.GetInt32(5) == 1,
            reader.GetInt32(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.GetInt32(9));
}
