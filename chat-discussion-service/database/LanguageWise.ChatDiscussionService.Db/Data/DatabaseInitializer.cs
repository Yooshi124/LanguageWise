using Microsoft.Data.Sqlite;

namespace LanguageWise.ChatDiscussionService.Db.Data;

/// <summary>
/// Applies the schema on every start-up and seeds the discussion tables the first time
/// they are empty, so that a brand new Docker volume always comes up with usable data.
/// </summary>
public sealed class DatabaseInitializer(string connectionString, string sqlDirectory, ILogger<DatabaseInitializer> logger)
{
    public void Initialise()
    {
        EnsureDatabaseDirectoryExists();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        Execute(connection, ReadSqlFile("schema.sql"));
        logger.LogInformation("Schema applied to {ConnectionString}.", connectionString);

        if (CountForums(connection) == 0)
        {
            Execute(connection, ReadSqlFile("seed.sql"));
            logger.LogInformation("Seeded {Count} forums.", CountForums(connection));
        }
    }

    private void EnsureDatabaseDirectoryExists()
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
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

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long CountForums(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Forums;";
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
