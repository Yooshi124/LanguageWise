using Npgsql;

namespace LanguageWise.MiniGamesService.Db.Data;

/// <summary>
/// Applies the schema transactionally on every start-up.
/// </summary>
public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    public void Initialise()
    {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = ReadSqlFile("schema.sql");
        command.ExecuteNonQuery();
        transaction.Commit();
        logger.LogInformation("Mini-games database schema applied.");
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

}
