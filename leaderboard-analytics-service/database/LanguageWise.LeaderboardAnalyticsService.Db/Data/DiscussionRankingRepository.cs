using LanguageWise.LeaderboardAnalyticsService.Db.Models;
using Microsoft.Data.Sqlite;

namespace LanguageWise.LeaderboardAnalyticsService.Db.Data;

public sealed class DiscussionRankingRepository(string connectionString)
{
    private const string SelectColumns =
        "SELECT Id, UserId, PostCount, CommentCount, LikeCount, Score, Rank, UpdatedAt FROM DiscussionRanking";

    public IReadOnlyList<DiscussionRanking> GetAll(int limit = 50, int offset = 0)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} ORDER BY Rank ASC LIMIT $limit OFFSET $offset;";
        command.Parameters.AddWithValue("$limit", limit);
        command.Parameters.AddWithValue("$offset", offset);

        var items = new List<DiscussionRanking>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) items.Add(Map(reader));
        return items;
    }

    public DiscussionRanking? GetById(int id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public DiscussionRanking? GetByUserId(int userId)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"{SelectColumns} WHERE UserId = $userId;";
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static DiscussionRanking Map(SqliteDataReader reader) =>
        new(reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetInt32(6),
            DateTime.Parse(reader.GetString(7)));
}
