using LanguageWise.ChatDiscussionService.Db.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.ChatDiscussionService.Db.Tests;

/// <summary>One throwaway SQLite file per test, built from the real schema and seed.</summary>
internal static class TestDatabase
{
    // The Db project copies sql/ to its output, and a project reference carries that
    // through to here, so this resolves the same way the service itself does.
    private static readonly string SqlDirectory = Path.Combine(AppContext.BaseDirectory, "sql");

    internal static string NewPath() =>
        Path.Combine(Path.GetTempPath(), $"languagewise-chat-{Guid.NewGuid():N}.db");

    internal static string ConnectionStringFor(string databasePath) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            ForeignKeys = true
        }.ToString();

    internal static void Initialise(string connectionString) =>
        new DatabaseInitializer(
            connectionString,
            SqlDirectory,
            NullLogger<DatabaseInitializer>.Instance).Initialise();

    internal static void Execute(string connectionString, string sql)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    internal static long Count(string connectionString, string sql)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    internal static void Delete(string databasePath)
    {
        // Pooled handles keep the file locked on Windows.
        SqliteConnection.ClearAllPools();

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
