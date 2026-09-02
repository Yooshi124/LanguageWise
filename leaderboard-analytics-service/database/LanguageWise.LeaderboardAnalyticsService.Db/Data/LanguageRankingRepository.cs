using LanguageWise.LeaderboardAnalyticsService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.LeaderboardAnalyticsService.Db.Data;

public sealed class LanguageRankingRepository(string connectionString)
{
    private const string SelectColumns =
        "SELECT Id, UserId, Language, Score, Rank, UpdatedAt FROM LanguageRanking";

    public IReadOnlyList<LanguageRanking> GetAll(string? language = null, int limit = 50, int offset = 0)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();

        var where = language is not null ? "WHERE Language = $language" : "";
        command.CommandText = $"{SelectColumns} {where} ORDER BY Rank ASC LIMIT $limit OFFSET $offset;";
        if (language is not null) command.Parameters.AddWithValue("$language", language);
        command.Parameters.AddWithValue("$limit", limit);
        command.Parameters.AddWithValue("$offset", offset);

        var items = new List<LanguageRanking>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(Map(reader));
        return items;
    }

    public LanguageRanking? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public IReadOnlyList<LanguageRanking> GetByUserId(int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE UserId = $userId ORDER BY Language;";
        command.Parameters.AddWithValue("$userId", userId);

        var items = new List<LanguageRanking>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(Map(reader));
        return items;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static LanguageRanking Map(SqliteDataReader reader) =>
        new(reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            DateTime.Parse(reader.GetString(5)));
}
