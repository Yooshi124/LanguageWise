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
}
