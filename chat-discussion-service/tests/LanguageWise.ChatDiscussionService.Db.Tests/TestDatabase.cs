using LanguageWise.ChatDiscussionService.Db.Data;
using Npgsql;
using Microsoft.Extensions.Logging.Abstractions;

namespace LanguageWise.ChatDiscussionService.Db.Tests;

internal static class TestDatabase
{
    private static readonly string SqlDirectory = Path.Combine(AppContext.BaseDirectory, "sql");

    internal static void Initialise(string connectionString) =>
        new DatabaseInitializer(
            connectionString,
            SqlDirectory,
            NullLogger<DatabaseInitializer>.Instance).Initialise();

    internal static void Execute(string connectionString, string sql)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    internal static long Count(string connectionString, string sql)
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

}
