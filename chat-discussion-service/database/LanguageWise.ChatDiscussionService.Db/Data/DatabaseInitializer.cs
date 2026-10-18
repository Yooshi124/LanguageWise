using Npgsql;

namespace LanguageWise.ChatDiscussionService.Db.Data;

/// <summary>
/// Applies the schema on every start-up and seeds the discussion tables the first time
/// they are empty, so that a brand new Docker volume always comes up with usable data.
/// </summary>
public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    public void Initialise()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        Execute(connection, transaction, "SELECT pg_advisory_xact_lock(hashtext(current_schema()), 0);");
        Execute(connection, transaction, ReadSqlFile("schema.sql"));

        var seeded = CountForums(connection, transaction) == 0;
        if (seeded)
        {
            Execute(connection, transaction, ReadSqlFile("seed.sql"));
        }
        transaction.Commit();
        logger.LogInformation("Discussion schema applied. Seed data inserted: {Seeded}.", seeded);
    }

    private string ReadSqlFile(string fileName)
    {
        var path = Path.Combine(sqlDirectory, fileName);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required SQL script '{fileName}' was not found.", path);
        }

        return File.ReadAllText(path);
    }

    private static void Execute(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long CountForums(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM Forums;";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
