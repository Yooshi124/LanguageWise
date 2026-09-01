using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageWise.MiniGamesService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.MiniGamesService.Db.Data;

/// <summary>Plain ADO.NET data access for the Games table.</summary>
public sealed class GameRepository(string connectionString)
{
    private const string SelectColumns = "SELECT Id, GameType, UserId, CourseCode, Solution, Words, Difficulty, CreatedAt, ExpiresAt FROM Games";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public IReadOnlyList<Game> GetByUserId(int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE UserId = $userId ORDER BY CreatedAt DESC;";
        command.Parameters.AddWithValue("$userId", userId);

        var games = new List<Game>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            games.Add(Map(reader));
        }

        return games;
    }

    public IReadOnlyList<Game> GetByUserIdAndGameType(int userId, string gameType)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE UserId = $userId AND GameType = $gameType ORDER BY CreatedAt DESC;";
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$gameType", gameType);

        var games = new List<Game>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            games.Add(Map(reader));
        }

        return games;
    }

    public Game? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public Game Create(string gameType, int userId, string courseCode, string solution, IReadOnlyList<string> words, string difficulty = "intermediate", string? expiresAt = null)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        
        var wordsJson = JsonSerializer.Serialize(words, JsonOptions);
        var createdAt = DateTime.UtcNow.ToString("O");

        command.CommandText =
            """
            INSERT INTO Games (GameType, UserId, CourseCode, Solution, Words, Difficulty, CreatedAt, ExpiresAt)
            VALUES ($gameType, $userId, $courseCode, $solution, $words, $difficulty, $createdAt, $expiresAt)
            RETURNING Id, GameType, UserId, CourseCode, Solution, Words, Difficulty, CreatedAt, ExpiresAt;
            """;
        command.Parameters.AddWithValue("$gameType", gameType);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$courseCode", courseCode);
        command.Parameters.AddWithValue("$solution", solution);
        command.Parameters.AddWithValue("$words", wordsJson);
        command.Parameters.AddWithValue("$difficulty", difficulty);
        command.Parameters.AddWithValue("$expiresAt", expiresAt ?? (object)DBNull.Value);

        using var reader = command.ExecuteReader();
        reader.Read();
        return Map(reader);
    }

    public bool Delete(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Games WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        return command.ExecuteNonQuery() > 0;
    }

    /// <summary>Cheap query used by the health endpoint to prove the database is reachable.</summary>
    public long Count()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Games;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static Game Map(SqliteDataReader reader)
    {
        var wordsJson = reader.GetString(5);
        var wordsElement = JsonSerializer.Deserialize<JsonElement>(wordsJson);
        
        return new Game(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            wordsElement,
            reader.GetString(6),
            reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8));
    }
}
