using Microsoft.Data.Sqlite;

namespace LanguageWise.Shared.Db.Data;

public sealed class UserRepository(string connectionString)
{
    public long Count()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users;";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    /// <summary>Returns the user ID if credentials match, or null otherwise.</summary>
    public int? Verify(string username, string password)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM Users WHERE Username = $u AND Password = $p;";
        command.Parameters.AddWithValue("$u", username);
        command.Parameters.AddWithValue("$p", password);

        var result = command.ExecuteScalar();
        return result is long id ? (int)id : null;
    }

    public int? RecordLogin(int userId, DateOnly today)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Users
            SET CurrentStreak = CASE
                    WHEN date(LastLogin) = date($today, '-1 day') THEN CurrentStreak + 1
                    ELSE 0
                END,
                LastLogin = $today
            WHERE Id = $userId
              AND (LastLogin IS NULL OR date(LastLogin) <> date($today))
            RETURNING CurrentStreak;
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$today", today.ToString("yyyy-MM-dd"));

        var result = command.ExecuteScalar();
        return result is long streak ? (int)streak : null;
    }
}
